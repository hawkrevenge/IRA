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


        public void ceqExecute(string input)
        {
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
                        JacQueryReader.Read();
                        bool equalcheck;
                        if (i < 8)
                        {
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = " + Convert.ToDouble(dbSets[tuplenumber, i + 1]), m_mbConnection);
                            MetaValue.Read();
                            IDFs = Math.Pow(Math.E, -0.5 * (Math.Pow(((Convert.ToDouble(values[table]) - MetaValue.GetDouble(0)) / Bandwidths[i]), 2))) * MetaValue.GetDouble(1);

                            equalcheck = MetaValue.GetDouble(0) == JacQueryReader.GetDouble(0);
                        }
                        else
                        {
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = '" + dbSets[tuplenumber, i + 1] + "'", m_mbConnection);
                            MetaValue.Read();
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

        private int CompareTuple(Tuple<int, double, double> t1, Tuple<int, double, double> t2)
        {
            if (t1.Item2.CompareTo(t2.Item2) != 0)
                return -t1.Item2.CompareTo(t2.Item2);
            else
                return -t1.Item3.CompareTo(t2.Item3);
        }
        
        public static double Jacquard(string Wt, string Wq)
        {
            if (Wq == "none")
                return 1;

            List<String> allvalues = new List<String>();
            string[] wvalues = Wt.Split(',');
            string[] qvalues = Wq.Split(',');

            double amount = 0;
            double total = 0;

            for (int i=0; i<qvalues.Length-1; i++)
            {
                string value = qvalues[i];
                allvalues.Add(value);
                total += Convert.ToInt32(value.Split(' ')[0]);
            }

            for (int i = 0; i < wvalues.Length - 1; i++)
            {
                string value = wvalues[i];
                if (allvalues.Contains(value))
                {
                    amount += Convert.ToInt32(value.Split(' ')[0]);
                }
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
