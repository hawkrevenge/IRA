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
        static String[] tables = { "mpg", "cylinders", "displacement", "horsepower", "weight", "acceleration", "model_year", "origin", "brand", "model", "type" };
        Dictionary<string, Dictionary<object, Tuple<double, double, double>>> localDictionary;

        SQLiteConnection m_mbConnection;
        SQLiteConnection m_dbConnection;

        public Metabase(SQLiteConnection connection, SQLiteConnection dbconnection)
        {
            instance = this;
            m_mbConnection = connection;
            m_dbConnection = dbconnection;

            localDictionary = new Dictionary<string, Dictionary<object, Tuple<double, double, double>>>();
            for (int i=0; i<tables.Length; i++)
            {
                localDictionary[tables[i]] = new Dictionary<object, Tuple<double, double, double>>();
            }
        }

        private void EditTupleInDictionary(string table, object key, double value, int index)
        {
            Dictionary<object, Tuple<double, double, double>> tableDict = localDictionary[table];
            if (tableDict.ContainsKey(key))
            {
                tableDict[key] = AddValueToTuple(tableDict[key], value, index);
            }
            else
            {
                tableDict[key] = new Tuple<double, double, double>(
                    index == 0 ? value : 0,
                    index == 1 ? value : 0,
                    index == 2 ? value : 0
                );
            }
        }

        private Tuple<double, double, double> AddValueToTuple(Tuple<double, double, double> tuple, double value, int index)
        {
            return new Tuple<double, double, double>(
                index == 0 ? value : tuple.Item1,
                index == 1 ? value : tuple.Item2,
                index == 2 ? value : tuple.Item3
            );
        }

        private SQLiteDataReader ExecuteCommand(String s, SQLiteConnection connection)
        {
            SQLiteCommand command = new SQLiteCommand(s, connection);
            //command.ExecuteNonQuery();
            return command.ExecuteReader();
            //while (reader.Read())
            //{
            //    Console.WriteLine(reader.GetFloat(0));
            //    //Console.WriteLine(reader.GetString(0));
            //}
        }

        public void InsertAll()
        {
            InsertQF();
            InsertIDF();
        }

        public void InsertQF()
        {
            Console.WriteLine("Calculating: QF-Values");
            Dictionary<string, Dictionary<string, int>> QFDictionary = new Dictionary<string, Dictionary<string, int>>();
            StreamReader reader = new StreamReader("workload.txt");
            reader.ReadLine(); reader.ReadLine();
            string line; int n; string[] splitted; // predefined variables
            Dictionary<string, int> max = new Dictionary<string, int>();
            while ((line = reader.ReadLine()) != "" && line != null)
            {
                // splits the amounnt and the query
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
                            AddtoDictionary(ref QFDictionary,ref max, key, s2, n);
                    }
                    else
                    {
                        //replace
                        string[] tmps = StringTrim(s).Split('=');
                        if (!QFDictionary.ContainsKey(tmps[0]))
                            QFDictionary.Add(tmps[0], new Dictionary<string, int>());
                        if (!QFDictionary[tmps[0]].ContainsKey(tmps[1]))
                            QFDictionary[tmps[0]].Add(tmps[1], 0);
                        QFDictionary[tmps[0]][tmps[1]] += n;
                        if (!max.ContainsKey(tmps[0]))
                            max.Add(tmps[0], QFDictionary[tmps[0]][tmps[1]]);
                        else if (QFDictionary[tmps[0]][tmps[1]] > max[tmps[0]])
                            max[tmps[0]] = QFDictionary[tmps[0]][tmps[1]];
                    }
                }
            }
            foreach (KeyValuePair<string, Dictionary<string, int>> PairSD in QFDictionary)
            {
                int maxValue = max[PairSD.Key];

                foreach (KeyValuePair<string, int> PairSI in PairSD.Value)
                {
                    //string commandstring;
                    //if (PairSD.Key == "brand" || PairSD.Key == "model" || PairSD.Key == "type")
                    //    commandstring = "INSERT INTO " + PairSD.Key + " VALUES (" + PairSI.Key + ", " + (float)PairSI.Value / (float)maxValue + ", 'IDFVALUE')";
                    //else
                    //    commandstring = "INSERT INTO " + PairSD.Key + " VALUES (\'" + PairSI.Key + "\', " + (float)PairSI.Value / (float)maxValue + ", 'IDFVALUE')";

                    //Console.WriteLine("executing: " + commandstring);
                    //ExecuteCommand(commandstring, m_mbConnection);

                    EditTupleInDictionary(PairSD.Key, PairSI.Key, PairSI.Value, 0);

                    //ExecuteCommand("SELECT name FROM sqlite_temp_master WHERE type=\'table\'");
                    //ExecuteCommand("SELECT name FROM sqlite_master \nWHERE type IN('table', 'view') AND name NOT LIKE 'sqlite_%'\nUNION ALL\nSELECT --->
                    // ---> name FROM sqlite_temp_master\nWHERE type IN('table', 'view')\nORDER BY 1");

                }
            }
            Console.WriteLine("executed: QF-Values");
        }

        float StandardDev(float[] Ti)
        {
            float Mean = 0;
            foreach(float f in Ti)          
                Mean += f;
            Mean /= Ti.Length;

            float Var = 0;
            foreach (float f in Ti)
                Var += (float) Math.Pow((f - Mean), 2);
            Var /= Ti.Length; 
            return (float)Math.Sqrt(Var);
        }

        void  InsertIDF()
        {
            SQLiteDataReader reader = ExecuteCommand("SELECT COUNT(*) FROM autompg", m_dbConnection);
            reader.Read();
            int count = reader.GetInt32(0);
            float[][] Values = new float[8][];
            for(int i =0; i < 8;i++)
                Values[i] = new float[count];
            reader = ExecuteCommand("SELECT * FROM autompg", m_dbConnection);
            Dictionary<string, Dictionary<string, int>> IDFDictionary = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> max = new Dictionary<string, int>();
            int counter = 0;
            while (reader.Read())
            {
                for (int i = 1; i < 9; i++)
                {
                    Values[i][counter] = (float)reader.GetDouble(i);
                }
                 //   Console.WriteLine(reader.GetDouble(i));

                for(int i = 9; i<12; i++)
                {
                    string name = reader.GetString(i);

                    IDFDictionary[tables[i - 1]][name] += 1;
                    AddtoDictionary(ref IDFDictionary,ref max, tables[i - 1], name, 1);
                }
                counter++;
            }
            float[] bandwidth = new float[8];
            for (int i = 0; i < 8; i++)
                bandwidth[i] = (float)1.06 * StandardDev(Values[i]) * (float) Math.Pow(count, 2);
        }

        void AddtoDictionary(ref Dictionary<string, Dictionary<string, int>> d, ref Dictionary<string, int> max, string key1, string key2, int amount)
        {
            if (!d.ContainsKey(key1))
                d.Add(key1, new Dictionary<string, int>());
            if (!d[key1].ContainsKey(key2))
                d[key1].Add(key2, amount);
            else
                d[key1][key2] += amount;
            if (!max.ContainsKey(key1))
                max.Add(key1, d[key1][key2]);
            else if (d[key1][key2] > max[key1])
                max[key1] = d[key1][key2];
        }

        string StringTrim(string s)
        {
            StringBuilder builder = new StringBuilder();
            string trimChars = " )(";
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
