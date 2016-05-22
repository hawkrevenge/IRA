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
        SQLiteConnection m_dbConnection;

        public Metabase(SQLiteConnection connection, SQLiteConnection dbconnenction)
        {
            instance = this;
            m_mbConnection = connection;
            m_dbConnection = dbconnenction;
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
            Dictionary<string,Dictionary<string, int>> Dictionary = new Dictionary<string, Dictionary<string, int>>();
            StreamReader reader = new StreamReader("workload.txt");
            reader.ReadLine(); reader.ReadLine();
            string line;
            string[] splitted;
            do
            {
                line = reader.ReadLine();
                splitted = Regex.Split(line, " times: ");
                ExecuteCommand(splitted[1], m_dbConnection);
                //SQLiteCommand command = new SQLiteCommand(line, );
            }
            while (line != "");
            //ExecuteCommand("SELECT name FROM sqlite_temp_master WHERE type=\'table\'");
            //ExecuteCommand("SELECT name FROM sqlite_master \nWHERE type IN('table', 'view') AND name NOT LIKE 'sqlite_%'\nUNION ALL\nSELECT name FROM sqlite_temp_master\nWHERE type IN('table', 'view')\nORDER BY 1");
            ExecuteCommand("SELECT * FROM autompg WHERE brand = 'saab' ", m_dbConnection);
            Console.WriteLine("executed: QF-Values");
        }
    }
}
