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
            char[] trimAND = new char[2] { ' ', '\'' };

            reader.ReadLine(); reader.ReadLine();
            string line;
            int n;
            string[] splitted;
            int max = 0;
            while((line = reader.ReadLine()) != ""&& line != null)
            {
                splitted = Regex.Split(line, " times: ");

                n = int.Parse(splitted[0]);
                splitted = Regex.Split(splitted[1], " WHERE ");
                splitted = Regex.Split(splitted[1], " AND ");
                foreach (string s in splitted)
                {
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
                            if (QFDictionary[key][s2] > max)
                                max = QFDictionary[key][s2];
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
                        if (QFDictionary[tmps[0]][tmps[1]] > max)
                            max = QFDictionary[tmps[0]][tmps[1]];
                    }
                }
            }
            //ExecuteCommand("SELECT name FROM sqlite_temp_master WHERE type=\'table\'");
            //ExecuteCommand("SELECT name FROM sqlite_master \nWHERE type IN('table', 'view') AND name NOT LIKE 'sqlite_%'\nUNION ALL\nSELECT name FROM sqlite_temp_master\nWHERE type IN('table', 'view')\nORDER BY 1");
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
