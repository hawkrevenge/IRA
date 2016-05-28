using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCode
{
    class CEQHandler
    {
        void ceqExecute(string input)
        {
            try
            {
                SQLiteDataReader reader;
                input = StringTrim(input);
                int k = 10;
                string[] splitted = input.Split(',');
                StringBuilder s = new StringBuilder();
                s.Append("SELECT * FROM autompg WHERE ");
                foreach (string attribute in splitted)
                {
                    if (s[s.Length - 1] != ' ')
                        s.Append(" AND ");
                    string[] tmpsplit = attribute.Split('=');
                    if (tmpsplit[0] == "k")
                        k = int.Parse(tmpsplit[1]);
                    else
                    {
                        // top k zoeken
                        s.Append(attribute);
                    }
                }
                reader = mb.ExecuteCommand(s.ToString(), m_dbConnection);
                int counter = 1;
                while (reader.Read())
                {
                    s.Clear();
                    s.Append(counter + ": " + reader.GetDouble(1));
                    for (int i = 1; i < 9; i++)
                        s.Append(", " + reader.GetDouble(i));
                    for (int i = 9; i < 12; i++)
                        s.Append(", " + reader.GetString(i));
                    Console.WriteLine(s.ToString());
                    counter++;
                }
            }
            catch
            {
                Console.WriteLine("\nSomething went wrong when finding results,\nmake sure that the names have single quotes");
            }
        }
    }
}
