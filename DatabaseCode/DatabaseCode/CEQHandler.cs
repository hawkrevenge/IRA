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
            SQLiteDataReader reader;
            reader = Program.ExecuteCommand("SELECT * FROM bandwidth", m_mbConnection);
            double[] Bandwiths = new double[8];
            for (int i = 0; i < 8; i++)
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
            double[] missing = new double[count];
            reader = Program.ExecuteCommand("SELECT * FROM autompg", m_dbConnection);
            SQLiteDataReader MetaValue;

            for (int tuplenumber = 0; tuplenumber < count; tuplenumber++)
            {
                missing[tuplenumber] = 0;
                reader.Read();
                double sum = 0;
                for (int i = 0; i < 11; i++)
                {
                    string table = Program.tables[i];
                    double s = 0;
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
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = " + reader.GetDouble(i + 1), m_mbConnection);
                            MetaValue.Read();
                            s += Math.Pow(Math.E, -0.5 * (Math.Pow(((Convert.ToDouble(values[table]) - MetaValue.GetDouble(0)) / Bandwiths[i]), 2))) * MetaValue.GetDouble(1);
                            J = Jacquard(MetaValue.GetString(3), JacQueryReader.GetString(3));
                            equalcheck = MetaValue.GetDouble(0) == JacQueryReader.GetDouble(0);
                        }
                        else
                        {
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = '" + reader.GetString(i + 1) + "'", m_mbConnection);
                            MetaValue.Read();
                            s += MetaValue.GetDouble(1);
                            J = Jacquard(MetaValue.GetString(3), JacQueryReader.GetString(3));
                            equalcheck = MetaValue.GetString(0) == JacQueryReader.GetString(0);
                        }
                        if (J < 0)
                            if (equalcheck)
                            {
                                QF = MetaValue.GetDouble(2);
                                J = 1;
                            }
                            else
                                QF = 0;
                        else
                            QF = MetaValue.GetDouble(2);
                        sum = s * QF * J;
                    }
                    else
                    {
                        if (i < 8)
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = " + reader.GetDouble(i + 1), m_mbConnection);
                        else
                            MetaValue = Program.ExecuteCommand("Select * From " + table + " Where id = '" + reader.GetString(i + 1) + "'", m_mbConnection);
                        MetaValue.Read();
                        missing[tuplenumber] += Math.Log10(MetaValue.GetDouble(2));
                    }
                }
                scores[tuplenumber] = sum;
            }
        }
        
        public static double Jacquard(string Wt, string Wq)
        {
            if (Wq == "none")
                return -1;

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
