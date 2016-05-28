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
        public CEQHandler(SQLiteConnection dataConnection, SQLiteConnection metaConnection)
        {
            m_dbConnection = dataConnection;
            m_mbConnection = metaConnection;
        }

        public void ceqExecute(string input)
        {
            try
            {
                SQLiteDataReader reader;
                reader = Program.ExecuteCommand("SELECT * FROM bandwith", m_mbConnection);
                double[] Bandwiths = new double[8];
                for(int i =0;i<8;i++)
                {
                    reader.Read();
                    Bandwiths[i] = reader.GetDouble(1);
                }
                reader = Program.ExecuteCommand("SELECT COUNT(*) FROM autompg", m_dbConnection);
                reader.Read();
                int count = reader.GetInt32(0);
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
                double[] scores = new double[count];
                reader = Program.ExecuteCommand("SELECT * FROM autompg", m_dbConnection);
                SQLiteDataReader MetaValue;
               
                for(int tuplenumber = 0; tuplenumber<count;tuplenumber++)
                {
                    reader.Read();
                    double sum = 0;
                    for(int i = 0; i<11;i++)
                    {
                        string table = Program.tables[i];
                        double s = 0;
                        if (values.ContainsKey(table))
                        {

                            if (i < 8)
                            {
                                MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = " + reader.GetDouble(i), m_mbConnection);
                                MetaValue.Read();
                                s += Math.Pow(Math.E, -0.5 * (Math.Pow((((double)values[table] - MetaValue.GetDouble(0)) / Bandwiths[i]),2)));
                            }
                            else
                            {
                                MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = '" + reader.GetString(i) + "'", m_mbConnection);
                            }
                            s *= reader.GetDouble(2);
                            MetaValue.Read();
                            
                        }
                        sum += s;
                    }
                    scores[tuplenumber] = sum;
                }
                
            }
            catch
            {
                Console.WriteLine("\nSomething went wrong when finding results,\nmake sure that the names have single quotes");
            }
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
