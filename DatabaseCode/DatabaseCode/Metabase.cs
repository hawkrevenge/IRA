using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

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

        private void ExecuteCommand(String s)
        {
            SQLiteCommand command = new SQLiteCommand(s, m_mbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(reader.GetInt32(0));
                //Console.WriteLine(reader.GetString(0));
            }
        }

        public void InsertAll()
        {
            InsertQF();
        }

        public void InsertQF()
        {
            //ExecuteCommand("SELECT name FROM sqlite_temp_master WHERE type=\'table\'");
            //ExecuteCommand("SELECT name FROM sqlite_master \nWHERE type IN('table', 'view') AND name NOT LIKE 'sqlite_%'\nUNION ALL\nSELECT name FROM sqlite_temp_master\nWHERE type IN('table', 'view')\nORDER BY 1");
            ExecuteCommand("SELECT * FROM mpg");
        }
    }
}
