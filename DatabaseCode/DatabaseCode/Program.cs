using System;
using System.Data.SQLite;
using System.IO;

// Our program has some commands:
// rebuild = rebuilds the database and metadatabase
// quit = exit the program
// any CEQ = a query which returns the top-k results asked

namespace DatabaseCode
{
    class Program
    {
        // Holds our connection with the database
        SQLiteConnection m_dbConnection;
        SQLiteConnection m_mbConnection;
        Metabase mb;

        static void Main(string[] args)
        {
            Program p = new Program();
        }

        public Program()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\MyDatabase.sqlite"))
                Connect(ref m_dbConnection, "MyDatabase.sqlite");
            else
                Build(ref m_dbConnection, "MyDatabase.sqlite", "autompg.sql");
            if (File.Exists(Directory.GetCurrentDirectory() + "\\MyMetabase.sqlite"))
                Connect(ref m_mbConnection, "MyMetabase.sqlite");
            else
            {
                Build(ref m_mbConnection, "MyDatabase.sqlite", "meta.sql");
                mb = new Metabase(m_mbConnection);
                mb.InsertAll();
            }

            mb = new Metabase(m_mbConnection);
            mb.InsertAll();

            while (true)
            {
                string input;
                input = Console.ReadLine();
                if (input == "rebuildData")
                {
                    Console.WriteLine("rebuilding database");
                    Disconnect(ref m_dbConnection);
                    Build(ref m_dbConnection, "MyDatabase.sqlite", "autompg.sql");
                }
                else if (input == "rebuildMeta")
                {
                    Console.WriteLine("rebuilding metabase");
                    Disconnect(ref m_mbConnection);
                    Build(ref m_mbConnection, "MyMetabase.sqlite", "meta.sql");
                    mb = new Metabase(m_mbConnection);
                    mb.InsertAll();
                }

                else if (input == "quit")
                {
                    Console.WriteLine("quitting");
                    DisconnectAll();
                    return;
                }
                else
                {

                    //ingevoerde query acties
                }


            }
        }

        void UseStandardDB(SQLiteConnection s, string fileName)
        {
            StreamReader reader = new StreamReader(fileName);
            string line;
            do
            {
                line = reader.ReadLine();
                SQLiteCommand command = new SQLiteCommand(line, s);
                command.ExecuteNonQuery();
                Console.WriteLine("executed: " + line);
            }
            while (line != "");

        }

        // Creates an empty database file
        void createNewDatabase(string name)
        {
            SQLiteConnection.CreateFile(name);
        }

        void Connect(ref SQLiteConnection connection, String fileLocation)
        {
            Console.WriteLine("Connecting to DB: "+fileLocation);
            connection = new SQLiteConnection("Data Source="+ fileLocation + ";Version=3;");
            connection.Open();
        }

        void Build(ref SQLiteConnection connection, String databaseLocation, String SQLFile)
        {
            createNewDatabase(databaseLocation);
            Connect(ref connection, databaseLocation);
            UseStandardDB(connection, SQLFile);
        }

        void Disconnect(ref SQLiteConnection connection)
        {
            connection.Close();
        }

        void DisconnectAll()
        {
            Disconnect(ref m_dbConnection);
            Disconnect(ref m_mbConnection);
        }
    }
}
