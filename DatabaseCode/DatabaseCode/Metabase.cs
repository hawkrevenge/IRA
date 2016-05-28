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
    class Metabase
    {
        public static Metabase instance;
        public static Dictionary<string, double> QFmax = new Dictionary<string, double>();
        static String[] tables = { "mpg", "cylinders", "displacement", "horsepower", "weight", "acceleration", "model_year", "origin", "brand", "model", "type" };
        Dictionary<string, Dictionary<object, Tuple<string, string, string>>> localDictionary;

        SQLiteConnection m_mbConnection;
        SQLiteConnection m_dbConnection;

        public Metabase(SQLiteConnection connection, SQLiteConnection dbconnection)
        {
            instance = this;
            m_mbConnection = connection;
            m_dbConnection = dbconnection;

            localDictionary = new Dictionary<string, Dictionary<object, Tuple<string, string, string>>>();
            for (int i=0; i<tables.Length; i++)
            {
                localDictionary[tables[i]] = new Dictionary<object, Tuple<string, string, string>>();
            }
        }

        private void EditTupleInDictionary(string table, string key, string value, int index)
        {
            Dictionary<object, Tuple<string, string, string>> tableDict = localDictionary[table];

            if (tableDict.ContainsKey(key))
            {
                tableDict[key] = AddValueToTuple(tableDict[key], value, index);
            }
            else
            {
                tableDict[key] = new Tuple<string, string, string>(
                    index == 0 ? value : (1 / (QFmax[table] + 1)).ToString(),
                    index == 1 ? value : "0",
                    index == 2 ? value : "0"
                );
            }
        }

        private Tuple<string, string, string> AddValueToTuple(Tuple<string, string, string> tuple, string value, int index)
        {
            return new Tuple<string, string, string>(
                index == 0 ? value : tuple.Item1,
                index == 1 ? value : tuple.Item2,
                index == 2 ? value : tuple.Item3
            );
        }

        public SQLiteDataReader ExecuteCommand(String s, SQLiteConnection connection)
        {
            SQLiteCommand command = new SQLiteCommand(s, connection);
            return command.ExecuteReader();
        }

        public void InsertAll()
        {
            InsertQF();
            InsertIDF();

            foreach (String table in tables)
            {
                Dictionary<object, Tuple<string, string, string>> tableDict = localDictionary[table];
                foreach (KeyValuePair<object, Tuple<string, string, string>> tuple in tableDict)
                {
                    string commandstring;
                    if (table == "brand" || table == "model" || table == "type")
                        commandstring = "INSERT INTO " + table + " VALUES (\'" + tuple.Key + "\', ";
                    else
                        commandstring = "INSERT INTO " + table + " VALUES (" + tuple.Key + ", ";

                    commandstring += tuple.Value.Item1 + ", " + tuple.Value.Item2 + ", " + tuple.Value.Item3 + ")";
                    Console.WriteLine("executing: " + commandstring);
                    ExecuteCommand(commandstring, m_mbConnection);
                }
            }
        }

        public void InsertQF()
        {
            Console.WriteLine("Calculating: QF-Values and Attribute Similarity values");
            Dictionary<string, Dictionary<string, double>> QFDictionary = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<string, Dictionary<string, string>> ASDictionary = new Dictionary<string, Dictionary<string, string>>();
            StreamReader reader = new StreamReader("workload.txt");
            reader.ReadLine(); reader.ReadLine();
            // predefined variables
            string line;
            int n;
            string[] splitted;
            int i = 1;
            while ((line = reader.ReadLine()) != "" && line != null)
            {
                // splits the amount and the query
                splitted = Regex.Split(line, " times: ");
                n = int.Parse(splitted[0]);
                //splits each attribute
                splitted = Regex.Split(splitted[1], " WHERE ");
                splitted = Regex.Split(splitted[1], " AND ");

                foreach (string s in splitted)
                {
                    // splits the special IN case
                    if (s.Contains("IN"))
                    {
                        string[] tmps = Regex.Split(StringTrim(s), "IN");
                        string key = tmps[0];
                        foreach (string s2 in tmps[1].Split(','))
                        {
                            //string trimmed = s2.TrimEnd('0').TrimEnd('.');
                            AddToQFDictionary(ref QFDictionary, ref QFmax, key, s2, n);
                            AddToASDictionary(ref ASDictionary, key, s2, n + " q" + i + ","); // AS values look like this: 95 q4, 20 q60, 13 q70,
                        }
                    }
                    else
                    {
                        string[] tmps = StringTrim(s).Split('=');
                        {
                            string trimmed = tmps[1].TrimEnd('0').TrimEnd('.');
                            AddToQFDictionary(ref QFDictionary, ref QFmax, tmps[0],trimmed, n);
                        }
                    }
                }
                i++;
            }
            foreach (KeyValuePair<string, Dictionary<string, double>> PairSD in QFDictionary)
            {
                double maxValue = QFmax[PairSD.Key];
                foreach (KeyValuePair<string, double> PairSI in PairSD.Value)
                {
                    EditTupleInDictionary(PairSD.Key, PairSI.Key, ((PairSI.Value+1)/(maxValue+1)).ToString(), 0);
                }
            }
            foreach (KeyValuePair<string, Dictionary<string, string>> pairSD in ASDictionary)
            {
                foreach(KeyValuePair<string, string> PairSI in pairSD.Value)
                {
                    EditTupleInDictionary(pairSD.Key, PairSI.Key, "\'"+PairSI.Value+"\'", 2);
                }
            }

            foreach(string table in tables)
            {
                if (!QFmax.ContainsKey(table))
                    QFmax.Add(table, 0);
            }
            Console.WriteLine("executed: QF-Values and Attribute Similarity values");
        }

        double StandardDev(double[] Ti)
        {
            double Mean = 0;
            foreach(double f in Ti)          
                Mean += f;
            Mean /= Ti.Length;

            double Var = 0;
            foreach (double f in Ti)
                Var += (double) Math.Pow((f - Mean), 2);
            Var /= Ti.Length; 
            return (double)Math.Sqrt(Var);
        }

        void InsertIDF()
        {
            SQLiteDataReader reader = ExecuteCommand("SELECT COUNT(*) FROM autompg", m_dbConnection);
            reader.Read();
            int count = reader.GetInt32(0);
            double[][] Values = new double[8][];
            double[] bandwidths = new double[8];
            for (int i = 0; i < 8; i++)
                Values[i] = new double[count];
            reader = ExecuteCommand("SELECT * FROM autompg", m_dbConnection);
            Dictionary<string, Dictionary<string, double>> IDFDictionary = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<string, double> max = new Dictionary<string, double>();
            int counter = 0;
            while (reader.Read())
            {
                for (int i = 1; i < 9; i++)
                {
                    Values[i-1][counter] = (double)reader.GetDouble(i);
                }

                for (int i = 9; i < 12; i++)
                {
                    string name = reader.GetString(i);
                    AddToQFDictionary(ref IDFDictionary, ref max, tables[i - 1], name, 1);
                }
                counter++;
            }
            for (int i = 0; i < 8; i++)
            {
                bandwidths[i] = (double)1.06 * StandardDev(Values[i]) * (double)Math.Pow(count, -0.2);
                for (int j = 0; j < count; j++)
                {
                    double sum = 0;
                    for (int j2 = 0; j2 < count; j2++)
                    {
                        sum += (double)Math.Pow(Math.E, (double)-0.5 * Math.Pow(((Values[i][j2] - Values[i][j]) / bandwidths[i]), 2));
                    }
                    AddToQFDictionary(ref IDFDictionary, ref max, tables[i], Values[i][j] + "", (double)Math.Log10(count / sum));
                    string name = Values[i][j].ToString();
                    EditTupleInDictionary(tables[i], name, Math.Log10(count / sum).ToString(), 1);
                }
            }
            for (int i = 9; i < 12; i++)
                foreach (KeyValuePair<string, double> PairSF in IDFDictionary[tables[i - 1]])
                    EditTupleInDictionary(tables[i - 1], PairSF.Key, Math.Log10(count / PairSF.Value).ToString(), 1);
            for (int i=0; i<bandwidths.Length; i++)
            {
                /*Program.*/ExecuteCommand("INSERT INTO bandwidth VALUES ("+ i + ", " + bandwidths[i] +");", m_mbConnection);
            }
        }

        void AddToQFDictionary(ref Dictionary<string, Dictionary<string, double>> d, ref Dictionary<string, double> max, string key1, string key2, double amount)
        {
            if (!d.ContainsKey(key1))
                d.Add(key1, new Dictionary<string, double>());
            if (!d[key1].ContainsKey(key2))
                d[key1].Add(key2, amount);
            else
                d[key1][key2] += amount;
            if (!max.ContainsKey(key1))
                max.Add(key1, d[key1][key2]);
            else if (d[key1][key2] > max[key1])
                max[key1] = d[key1][key2];
        }

        void AddToASDictionary(ref Dictionary<string, Dictionary<string, string>> d, string key1, string key2, string value)
        {
            if (!d.ContainsKey(key1))
                d.Add(key1, new Dictionary<string, string>());
            if (!d[key1].ContainsKey(key2))
                d[key1].Add(key2, value);
            else
                d[key1][key2] += value;
        }

        private string StringTrim(string s)
        {
            StringBuilder builder = new StringBuilder();
            string trimChars = " )(;";
            int name = -1;
            foreach(char c in s)
            {
                if (c == '\'')
                    name *= -1;
                else
                {
                    if (!trimChars.Contains(c)||name>0)
                        builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }
}
