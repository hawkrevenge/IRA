using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;

namespace DatabaseCode
{
    class CEQHandler
    {
        SQLiteConnection m_dbConnection;
        SQLiteConnection m_mbConnection;
        static int count;
        static double[] Bandwidths;
        static object[,] dbSets;
        
        //Initalize the handler with connections, and make a local copy of the database & bandwidths
        public CEQHandler(SQLiteConnection dataConnection, SQLiteConnection metaConnection)
        {
            SQLiteDataReader reader;
            m_dbConnection = dataConnection;
            m_mbConnection = metaConnection;
            reader = Program.ExecuteCommand("SELECT COUNT(*) FROM autompg", m_dbConnection);
            reader.Read();
            count = reader.GetInt32(0);
            reader = Program.ExecuteCommand("SELECT * FROM bandwidth", m_mbConnection);
            Bandwidths = new double[8];
            for (int i = 0; i < 8; i++)
            {
                reader.Read();
                Bandwidths[i] = reader.GetDouble(1);
            }
            reader = Program.ExecuteCommand("SELECT * FROM autompg", m_dbConnection);
            dbSets = new object[count, 12];
            for (int tuplenumber = 0; tuplenumber < count; tuplenumber++)
            {
                reader.Read();
                dbSets[tuplenumber, 0] = tuplenumber;
                for (int i = 1; i < 12; i++)
                    if (i < 9)
                        dbSets[tuplenumber, i] = reader.GetDouble(i);
                    else
                        dbSets[tuplenumber, i] = reader.GetString(i);
            }
        }

        //Compare, rank and display documents w.r.t. a CEQ
        public void ceqExecute(string input)
        {
            //Split the CEQ
            Dictionary<string, object> values = new Dictionary<string, object>();
            input = StringTrim(input);
            int k = 10;
            string[] splitted = input.Split(',');
            foreach (string attribute in splitted)
            {
                string[] tmpsplit = attribute.Split('=');
                if (tmpsplit[0] == "k")
                    k = int.Parse(tmpsplit[1]);
                else
                {
                    values.Add(tmpsplit[0], tmpsplit[1]);
                }
            }

            //Query QF, IDF and AS values from the metadatabase, calculate the document score, and save it to the list of tuples as (index, score, missing attribute score)
            List<Tuple<int, double, double>> tuples = new List<Tuple<int, double, double>>();
            SQLiteDataReader MetaValue;
            for (int tuplenumber = 0; tuplenumber < count; tuplenumber++)
            {
                double scoresSum = 0;
                double missingSum = 0;

                for (int i = 0; i < 11; i++)
                {
                    string table = Program.tables[i];
                    double IDFs = 0;
                    if (values.ContainsKey(table))
                    {
                        double QF;
                        double J;
                        SQLiteDataReader JacQueryReader;
                        JacQueryReader = Program.ExecuteCommand("Select * From " + table + " Where id = " + values[table], m_mbConnection);
                        bool equalcheck;
                        if (i < 8)
                        {
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = " + Convert.ToDouble(dbSets[tuplenumber, i + 1]), m_mbConnection);
                            MetaValue.Read();
                            if (!JacQueryReader.Read())
                                continue;
                            IDFs = Math.Pow(Math.E, -0.5 * (Math.Pow(((MetaValue.GetDouble(0) - Convert.ToDouble(values[table])) / Bandwidths[i]), 2))) * JacQueryReader.GetDouble(1);
                            equalcheck = true;
                        }
                        else
                        {
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = '" + dbSets[tuplenumber, i + 1] + "'", m_mbConnection);
                            if (!MetaValue.Read() || !JacQueryReader.Read())
                                continue;
                            IDFs = MetaValue.GetDouble(1);
                            equalcheck = MetaValue.GetString(0) == JacQueryReader.GetString(0);
                        }
                        J = Jacquard(MetaValue.GetString(3), JacQueryReader.GetString(3));
                        if (equalcheck)
                            QF = JacQueryReader.GetDouble(2);
                        else
                            QF = 0;
                        scoresSum += IDFs * QF * J;
                    }
                    else
                    {
                        if (i < 8)
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = " + Convert.ToDouble(dbSets[tuplenumber, i + 1]), m_mbConnection);
                        else
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = '" + dbSets[tuplenumber, i + 1] + "'", m_mbConnection);
                        MetaValue.Read();
                        missingSum += Math.Log10(MetaValue.GetDouble(2));
                    }
                }
                tuples.Add(new Tuple<int, double, double>(tuplenumber, scoresSum, missingSum));
            }
            //Sort the tuples using CompareTuple
            tuples.Sort(CompareTuple);
            for (int i = 0; i < k; i++)
            {
                StringBuilder s = new StringBuilder();
                s.Append(tuples[i].Item1);
                for (int t = 1; t < 12; t++)
                    s.Append(", " + dbSets[tuples[i].Item1, t]);
                Console.WriteLine(s.ToString());
            }
        }

        //Compares tuples based on score - if they are equal, break the tie using missing attribute score
        private int CompareTuple(Tuple<int, double, double> t1, Tuple<int, double, double> t2)
        {
            if (t1.Item2.CompareTo(t2.Item2) != 0)
                return -t1.Item2.CompareTo(t2.Item2);
            else
                return -t1.Item3.CompareTo(t2.Item3);
        }
        
        //Jacquard function finds the intersection of queries between a set of queries Wt and Wq, and calculates intersection / union
        public static double Jacquard(string Wt, string Wq)
        {
            if (Wq == "none")
                return 1;

            List<String> allvalues = new List<String>();
            //because AS-strings always end in , the last element in the array will be "". This is why we loop over values.length-1 later
            string[] wvalues = Wt.Split(',');
            string[] qvalues = Wq.Split(',');

            double amount = 0;
            double total = 0;

            //All qvalues must be in the union, so the total contains all their query amounts
            for (int i=0; i<qvalues.Length-1; i++)
            {
                string value = qvalues[i];
                allvalues.Add(value);
                total += Convert.ToInt32(value.Split(' ')[0]);
            }

            for (int i = 0; i < wvalues.Length - 1; i++)
            {
                string value = wvalues[i];
                //All wvalues that are already in q must be in the intersection, so the amount contains all their query amounts
                if (allvalues.Contains(value))
                {
                    amount += Convert.ToInt32(value.Split(' ')[0]);
                }
                //Otherwise, they are not in q but are in the union, so the total contains all their query amounts
                else
                {
                    allvalues.Add(value);
                    total += Convert.ToInt32(value.Split(' ')[0]);
                }
                
            }

            return (amount / total);
        }

        private string StringTrim(string s)
        {
            StringBuilder builder = new StringBuilder();
            string trimChars = " )(;";
            int name = -1;
            foreach (char c in s)
            {
                if (c == '\'')
                {
                    builder.Append(c);
                    name *= -1;
                }
                else
                {
                    if (!trimChars.Contains(c) || name > 0)
                        builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }
}
