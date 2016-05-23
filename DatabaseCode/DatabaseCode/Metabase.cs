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

        SQLiteConnection m_mbConnection;

        public Metabase(SQLiteConnection connection)
        {
            instance = this;
            m_mbConnection = connection;
        }

        private void ExecuteCommand(String s, SQLiteConnection connection)
        {
            SQLiteCommand command = new SQLiteCommand(s, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(reader.GetFloat(0));
                //Console.WriteLine(reader.GetString(0));
            }
        }

        public void InsertAll()
        {
            InsertQF();

        }

        public void InsertQF()
        {
            Console.WriteLine("Calculating: QF-Values");
            Dictionary<string,Dictionary<string, int>> QFDictionary = new Dictionary<string, Dictionary<string, int>>();
            StreamReader reader = new StreamReader("workload.txt");
            reader.ReadLine(); reader.ReadLine();
            string line; int n; string[] splitted; // predefined variables
            Dictionary<string, int> max = new Dictionary<string, int>();
            while((line = reader.ReadLine()) != ""&& line != null)
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
                        
                        string[] tmps = Regex.Split(StringTrim(s), "IN" );
                        string key = tmps[0];
                        foreach (string s2 in tmps[1].Split(','))
                        {
                            if (!QFDictionary.ContainsKey(key))
                                QFDictionary.Add(key, new Dictionary<string, int>());
                            if (!QFDictionary[key].ContainsKey(s2))
                                QFDictionary[key].Add(s2, 0);
                            QFDictionary[key][s2] += n;
                            if (!max.ContainsKey(key))
                                max.Add(key, QFDictionary[key][s2]);
                            else if (QFDictionary[key][s2] > max[key])
                                max[key] = QFDictionary[key][s2];
                        }
                    }
                    else
                    {
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
                int Counter = 1;
                if (PairSD.Key == "brand" || PairSD.Key == "model" || PairSD.Key == "type")
                    foreach (KeyValuePair<string, int> PairSI in PairSD.Value)
                    {

                        string commandstring = "INSERT INTO " + PairSD.Key + " VALUES (\'" + PairSI.Key + "\', " + (float)PairSI.Value / (float)maxValue + ", 'IDFVALUE')";
                        Console.WriteLine("executing: " + commandstring);
                        ExecuteCommand(commandstring, m_mbConnection);
                    }
                else
                    foreach (KeyValuePair<string, int> PairSI in PairSD.Value)
                    {

                        string commandstring = "INSERT INTO " + PairSD.Key + " VALUES (" + PairSI.Key + ", " + (float)PairSI.Value / (float)maxValue + ", 'IDFVALUE')";
                        Console.WriteLine("executing: " + commandstring);
                        ExecuteCommand(commandstring, m_mbConnection);
                    }

            }

            //ExecuteCommand("SELECT name FROM sqlite_temp_master WHERE type=\'table\'");
            //ExecuteCommand("SELECT name FROM sqlite_master \nWHERE type IN('table', 'view') AND name NOT LIKE 'sqlite_%'\nUNION ALL\nSELECT --->
            // ---> name FROM sqlite_temp_master\nWHERE type IN('table', 'view')\nORDER BY 1");
            Console.WriteLine("executed: QF-Values");
        }

        string StringTrim(string s)
        {
            StringBuilder builder = new StringBuilder();
            string trimChars = " \')(";
            foreach(char c in s)
            {
                if (!trimChars.Contains(c))
                    builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
