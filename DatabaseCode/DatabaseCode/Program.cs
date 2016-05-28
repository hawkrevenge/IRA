using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;

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
                Build(ref m_mbConnection, "MyMetabase.sqlite", "meta.sql");
                mb = new Metabase(m_mbConnection, m_dbConnection);
                mb.InsertAll();
            }
            Build(ref m_mbConnection, "MyMetabase.sqlite", "meta.sql");
            mb = new Metabase(m_mbConnection,m_dbConnection);
            mb.InsertAll();
            Console.Write("\nAwaiting command: ");
            while (TextCommand(Console.ReadLine())) { Console.Write("\nAwaiting command: "); }
        }

        void UseStandardDB(SQLiteConnection s, string fileName)
        {
            StreamReader reader = new StreamReader(fileName);
            string line;
            while ((line = reader.ReadLine()) != "" && line != null)
            {
                SQLiteCommand command = new SQLiteCommand(line, s);
                command.ExecuteNonQuery();
                Console.WriteLine("executed: " + line);
            }

        }

        bool TextCommand(string input)
        {
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
                mb = new Metabase(m_mbConnection, m_dbConnection);
                mb.InsertAll();
            }

            else if (input == "quit")
            {
                Console.WriteLine("quitting");
                DisconnectAll();
                return false;
            }
            else if (input == "help")
            {
                Console.WriteLine("\nOur program uses the following commands (case sensitive):");
                Console.WriteLine("-Typing help returns the usable commands.");
                Console.WriteLine("-Typing a ceq will return the top-k results where the default k = 10, ending with a semicolon.");
                Console.WriteLine("-Typing rebuildData will rebuild the database using the autompg.sql file.");
                Console.WriteLine("-Typing rebuildMeta will rebuild the meta-database using the Meta.sql and workload.txt files.");
                Console.WriteLine("-Typing quit will exit the program.");
            }
            else if (input[input.Length - 1] == ';')
            {
                ceqExecute(input);
                //ingevoerde query acties
            }
            else
                Console.WriteLine("Command not recognised, type help for the usable commands");
            return true;
        }

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

        public string StringTrim(string s)
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
            Disconnect(ref connection);
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
