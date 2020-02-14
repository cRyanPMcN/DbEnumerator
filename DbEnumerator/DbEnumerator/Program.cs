using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml.XPath;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Data.SqlClient;
using System.Data.Sql;
using DbEnumerator;
using System.Diagnostics;

namespace DbEnuemratorCLI
{
    partial class Program
    {
        static void Print_XML(XmlNode doc, Int32 depth = 0)
        {
            Console.Write(new string('\t', depth));
            Console.WriteLine(doc.OuterXml);
            foreach (XmlNode child in doc.ChildNodes)
            {
                Print_XML(child, ++depth);
            }
        }

        static void PrintProgramArguments()
        {
            Console.WriteLine("DbEnumerator.exe ([Connectionstring]|[Data Source] [UserId] [Password] [Initial Catalog]");
        }

        static bool RunEnumerator(SqlConnection dbConn)
        {
            Enumerator dbEnumerator = new Enumerator(dbConn);
            ICollection<IDatabaseProgram> programs = dbEnumerator.GetDatabasePrograms();
            // Scope to separate programCount from rest of function
            {
                int programCount = 0;
                foreach (IDatabaseProgram p in programs)
                {
                    dbEnumerator.GetProgramParameters(p);
                    Console.WriteLine($"[{programCount++}]: {p.ToString()}");
                }
            }
            int selectedProgram = -1;
            while (selectedProgram < 0 || selectedProgram > programs.Count-1) {
                Console.WriteLine($"Please enter an integer between 0 and {programs.Count-1}.");
                try
                {
                    selectedProgram = int.Parse(Console.ReadLine());
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

            dbEnumerator.SetXMLPlanOn();
            XmlDocument doc = new XmlDocument();
            string qryplan = dbEnumerator.GetQueryPlan(programs.Skip(selectedProgram).First());
            StringReader strReader = new StringReader(qryplan);
            doc.Load(strReader);

            IEnumerable<Database> queryData = dbEnumerator.DatabaseCollectionFactory(doc);
            foreach (Database database in queryData)
            {
                foreach (Schema schema in database.Schemas)
                {
                    foreach (DataTable table in schema.Tables)
                    {
                        foreach (string column in table.Columns)
                        {
                            Console.WriteLine($"{database.Name}.{schema.Name}.{table.Name}.{column}");
                        }
                    }
                }
            }
            dbEnumerator.SetXMLPlanOff();
            return true;
        }

        static void Main(string[] args)
        {
            try
            {

                Console.WriteLine("Welcome to DbEnumerator");
                Console.WriteLine("Checking program Args");
                if (args.Length > 0)
                {
                    if (args.Length == 4)
                    {
                        SqlConnectionStringBuilder sqlConnBuilder = new SqlConnectionStringBuilder()
                        {
                            DataSource = args[0],
                            UserID = args[1],
                            Password = args[2],
                            InitialCatalog = args[3],
                        };
                        RunEnumerator(new SqlConnection(sqlConnBuilder.ConnectionString));
                    }
                    else if (args.Length == 1)
                    {
                        using SqlConnection dbConn = new SqlConnection(args[0]);
                        RunEnumerator(new SqlConnection(args[0]));
                    }
                    else
                    {
                        PrintProgramArguments();
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("No arguments found, please enter a MSSQL-Server Connection string to continue:");
                    string connstring = Console.ReadLine();
                    using SqlConnection dbConn = new SqlConnection(connstring);
                    RunEnumerator(dbConn);
                }
            }
            catch (Exception ex)
            {
                // Currently dropping StackTrace
                Console.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
                Exception innerException = ex.InnerException;
                while (innerException != null)
                {
                    Console.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
                    innerException = ex.InnerException;
                }
            }
        }
    }
}
