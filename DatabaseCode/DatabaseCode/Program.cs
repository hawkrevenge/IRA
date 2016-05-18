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

        static void Main(string[] args)
        {
            Program p = new Program();
        }

        public Program()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\MyDatabase.sqlite"))
                connectToDatabase();
            else
            {
                //TODO add build metaDatabase
                createNewDatabase("MyDatabase.sqlite");
                connectToDatabase();
                UseStandardDB();
            }
            if (File.Exists(Directory.GetCurrentDirectory() + "\\MyMetabase.sqlite"))
                connectToDatabase();
            else
            {
                //TODO add build metaDatabase
                createNewDatabase("MyMetabase.sqlite");
                connectToDatabase();
                UseStandardDB();
            }
            while (true)
            {
                string input;
                input = Console.ReadLine();
                if (input == "rebuildData")
                {
                    //TODO add build metaDatabase
                    disconnectDatabase();
                    createNewDatabase("MyDatabase.sqlite");
                    connectToDatabase();
                    UseStandardDB();
                }

                else if (input == "quit")
                    break;
                else
                {

                    //ingevoerde query acties
                }


            }

            Console.ReadLine();
            //createTable();
            //fillTable();
            //printHighscores();
        }

        void UseStandardDB()
        {
            StreamReader reader = new StreamReader("autompg.sql");
            string line;
            do
            {
                line = reader.ReadLine();
                SQLiteCommand command = new SQLiteCommand(line, m_dbConnection);
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

        // Creates a connection with our database file.
        void connectToDatabase()
        {
            m_dbConnection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
            m_dbConnection.Open();
        }

        void disconnectDatabase()
        {
            m_dbConnection.Close();
        }

        // Creates a table named 'highscores' with two columns: name (a string of max 20 characters) and score (an int)
        void createTable()
        {
            string sql = "create table highscores (name varchar(20), score int)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        // Inserts some values in the highscores table.
        // As you can see, there is quite some duplicate code here, we'll solve this in part two.
        void fillTable()
        {
            string sql = "insert into highscores (name, score) values ('Me', 3000)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
            sql = "insert into highscores (name, score) values ('Myself', 6000)";
            command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
            sql = "insert into highscores (name, score) values ('And I', 9001)";
            command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        // Writes the highscores to the console sorted on score in descending order.
        void printHighscores()
        {
            string sql = "select * from highscores order by score desc";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                Console.WriteLine("Name: " + reader["name"] + "\tScore: " + reader["score"]);
            Console.ReadLine();
        }
    }
}
