﻿using System;
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
        CEQHandler handler;
        Metabase mb;
        static public String[] tables = { "mpg", "cylinders", "displacement", "horsepower", "weight", "acceleration", "model_year", "origin", "brand", "model", "type" };

        static void Main(string[] args)
        {
            Program p = new Program();
        }

        public Program()
        {
            //Connect to or make the database (and then connect to it)
            if (File.Exists(Directory.GetCurrentDirectory() + "\\MyDatabase.sqlite"))
                Connect(ref m_dbConnection, "MyDatabase.sqlite");
            else
                Build(ref m_dbConnection, "MyDatabase.sqlite", "autompg.sql");

            //Connect to or make the metadatabase (and then connect to it)
            if (File.Exists(Directory.GetCurrentDirectory() + "\\MyMetabase.sqlite"))
                Connect(ref m_mbConnection, "MyMetabase.sqlite");
            else
            {
                Build(ref m_mbConnection, "MyMetabase.sqlite", "metadb.txt");
                mb = new Metabase(m_mbConnection, m_dbConnection);
                mb.InsertAll();
            }

            //mb = new Metabase(m_mbConnection, m_dbConnection);
            handler = new CEQHandler(m_dbConnection, m_mbConnection);
            TextCommand("help");
            Console.Write("\nAwaiting command: ");
            while (TextCommand(Console.ReadLine())) {
                Console.Write("\nAwaiting command: ");
            }
        }

        //Create a database using a SQL file
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

        //Handle user input, return false if input is 'quit'
        bool TextCommand(string input)
        {
            input.Trim(' ');
            //Rebuild the database
            if (input == "rebuildData")
            {
                Console.WriteLine("rebuilding database");
                Disconnect(ref m_dbConnection);
                Build(ref m_dbConnection, "MyDatabase.sqlite", "autompg.sql");
                handler = new CEQHandler(m_dbConnection, m_mbConnection);
            }
            //Rebuild the metadatabase
            else if (input == "rebuildMeta")
            {
                Console.WriteLine("rebuilding metabase");
                Disconnect(ref m_mbConnection);
                Build(ref m_mbConnection, "MyMetabase.sqlite", "metadb.txt");
                mb = new Metabase(m_mbConnection, m_dbConnection);
                mb.InsertAll();
                handler = new CEQHandler(m_dbConnection, m_mbConnection);
            }
            //Quit
            else if (input == "quit")
            {
                Console.WriteLine("quitting");
                DisconnectAll();
                return false;
            }
            //Show explanation of input
            else if (input == "help")
            {
                Console.WriteLine("\nOur program uses the following commands (case sensitive):");
                Console.WriteLine("-Typing help returns the usable commands.");
                Console.WriteLine("-Typing a ceq will return the top-k results where the default k = 10, ending with a semicolon.");
                Console.WriteLine("-Typing rebuildData will rebuild the database using the autompg.sql file.");
                Console.WriteLine("-Typing rebuildMeta will rebuild the meta-database using the metadb.txt and workload.txt files.");
                Console.WriteLine("-Typing quit will exit the program.");
            }
            //Handle queries
            else if (input[input.Length - 1] == ';')
            {
                handler.ceqExecute(input);
            }
            else
                Console.WriteLine("Command not recognised, type help for the usable commands");
            return true;
        }

        //Execute SQL commands on a database(connection), return a reader that contains any output of SELECT statements
        static public SQLiteDataReader ExecuteCommand(String s, SQLiteConnection connection)
        {
            SQLiteCommand command = new SQLiteCommand(s, connection);
            return command.ExecuteReader();
        }

        // Creates an empty database file
        void createNewDatabase(string name)
        {
            SQLiteConnection.CreateFile(name);
        }

        //Connect to a database, set the connection
        void Connect(ref SQLiteConnection connection, String fileLocation)
        {
            Console.WriteLine("Connecting to DB: "+fileLocation);
            connection = new SQLiteConnection("Data Source="+ fileLocation + ";Version=3;");
            connection.Open();
        }

        //The whole process of creating a database
        void Build(ref SQLiteConnection connection, String databaseLocation, String SQLFile)
        {
            Disconnect(ref connection);
            createNewDatabase(databaseLocation);
            Connect(ref connection, databaseLocation);
            UseStandardDB(connection, SQLFile);
        }
        
        //Disconnect from a database
        void Disconnect(ref SQLiteConnection connection)
        {
            if (connection != null)
                connection.Close();
        }

        //Disconnect from all databases we use
        void DisconnectAll()
        {
            Disconnect(ref m_dbConnection);
            Disconnect(ref m_mbConnection);
        }
    }
}
