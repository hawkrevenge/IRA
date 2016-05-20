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
            reader.GetBoolean(1);
        }

        public void InsertAll()
        {
            InsertQF();
        }

        public void InsertQF()
        {
            ExecuteCommand("SELECT name FROM sqlite_master WHERE type=\'table\'");
        }
    }
}
