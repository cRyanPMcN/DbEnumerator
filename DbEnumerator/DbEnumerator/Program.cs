using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Data.SqlClient;
using DbEnumeratorLib;

namespace DbEnuemratorCLI
{
    partial class Program
    {
        static void PrintProgramUsage()
        {
            Console.WriteLine("Usage: DbEnumerator.exe ([Connectionstring]) | ([Data Source] [UserId] [Password] [Initial Catalog])");
        }

        static void RunEnumerator(SqlConnection dbConn)
        {
            Enumerator dbEnumerator = new Enumerator(dbConn);
            ICollection<IProgram> programs = dbEnumerator.GetDatabasePrograms();
            int programCount = 1;
            foreach (IProgram p in programs)
            {
                Console.WriteLine($"[{programCount}]: {p}");
                ++programCount;
            }

            int selectedIndex = -1;
            while (selectedIndex < 1 || selectedIndex > programs.Count) 
            {
                Console.WriteLine($"Please enter an integer between 1 and {programs.Count}.");
                try
                {
                    selectedIndex = int.Parse(Console.ReadLine());
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("No input detected.");
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid input detected.");
                }
                catch (OverflowException)
                {
                    Console.WriteLine("Input was too large to parse.");
                }
            }

            IProgram selectedProgram = programs.ElementAt(selectedIndex - 1);
            dbEnumerator.SetXMLPlanOn();
            XmlDocument queryPlan = dbEnumerator.GetQueryPlan(selectedProgram);
            dbEnumerator.SetXMLPlanOff();

            ICollection<Database> databases = dbEnumerator.DatabaseCollectionFactory(queryPlan);
            foreach (Database database in databases)
            {
                foreach (Schema schema in database.Schemas)
                {
                    foreach (Table table in schema.Tables)
                    {
                        foreach (string column in table.Columns)
                        {
                            Console.WriteLine($"{database.Name}.{schema.Name}.{table.Name}.{column}");
                        }
                    }
                }
            }

            if (databases.Count == 0)
            {
                Console.WriteLine($"Query plan for {selectedProgram} does not contain a reference to a database column");
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Welcome to DbEnumerator");
                Console.WriteLine("Checking program Args");
                switch (args.Length)
                {
                    case 0:
                        Console.WriteLine("No arguments found, please enter a MSSQL-Server Connection string to continue:");
                        string connstring = Console.ReadLine();
                        RunEnumerator(new SqlConnection(connstring));
                        break;
                    case 1:
                        RunEnumerator(new SqlConnection(args[0]));
                        break;
                    case 4:
                        SqlConnectionStringBuilder sqlConnBuilder = new SqlConnectionStringBuilder()
                        {
                            DataSource = args[0],
                            UserID = args[1],
                            Password = args[2],
                            InitialCatalog = args[3],
                        };

                        RunEnumerator(new SqlConnection(sqlConnBuilder.ConnectionString));
                        break;
                    default:
                        PrintProgramUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
            }
        }
    }
}
