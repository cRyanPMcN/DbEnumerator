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

        static void Main(string[] args)
        {
            //XmlReader xmlReader = XmlReader.Create(new System.IO.StreamReader(XML_FILE_LOCATION));
            //xmlReader.Read();
            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(xmlFileText.Replace("xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\"", "xmlns:ms=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\""));
            //XmlElement root = doc.DocumentElement;

            Console.WriteLine("Welcome to DbEnumerator");
            Console.WriteLine("Checking program Args");
            if (args.Length > 0)
            {
                if (args.Length == 4)
                {
                    SqlConnectionStringBuilder sqlConnBuilder = new SqlConnectionStringBuilder();

                    sqlConnBuilder.DataSource = args[0];
                    sqlConnBuilder.UserID = args[1];
                    sqlConnBuilder.Password = args[2];
                    sqlConnBuilder.InitialCatalog = args[3];

                    using SqlConnection dbConn = new SqlConnection(sqlConnBuilder.ConnectionString);
                    Enumerator dbEnumerator = new Enumerator(dbConn);
                    List<DatabaseProgram> programs = dbEnumerator.GetDatabasePrograms();
                    //Enumerator.SetXMLPlanOn(dbConn);

                    //Enumerator.SetXMLPlanOff(dbConn);
                    foreach (DatabaseProgram program in programs)
                    {
                        Console.WriteLine(program.ToString());
                    }
                }
                else if (args.Length == 1)
                {
                    using SqlConnection dbConn = new SqlConnection(args[0]);
                    Enumerator dbEnumerator = new Enumerator(dbConn);
                    List<DatabaseProgram> dbInfo = dbEnumerator.GetDatabasePrograms();
                    List<DatabaseProgram> programs = dbEnumerator.GetDatabasePrograms();
                    foreach (DatabaseProgram program in programs)
                    {
                        Console.WriteLine(program.ToString());
                    }
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
                dbConn.Open();

            }
        }
    }
}
