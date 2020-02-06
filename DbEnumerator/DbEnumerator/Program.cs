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


namespace TestingCS
{
    class Program
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

        static XmlNodeList GetColumnReferenceNodes(XmlDocument doc)
        {
            return doc.DocumentElement.SelectNodes("//ColumnReference[@Database]");
        }

        static List<Database> GetDistinctDatabases(XmlDocument doc)
        {
            XmlNodeList databaseNames = doc.DocumentElement.SelectNodes("//ColumnReference[@Database]/@Database");
            try
            {
                return (from distinctNames in (from names in databaseNames.Cast<XmlAttribute>() select names.Value).Distinct()
                        select new Database { Name = distinctNames, Schemas = null }).ToList();
            }
            catch (InvalidCastException)
            {
                // This catch should not be hit, it is only here just in case the above XPath statement is incorrect.
                HashSet<string> uniqueDatabaseNames = new HashSet<string>();
                foreach (XmlNode node in databaseNames)
                {
                    if (node is XmlAttribute)
                    {
                        uniqueDatabaseNames.Add((node as XmlAttribute).Value);
                    }
                }
                List<Database> databases = new List<Database>();
                foreach (string name in uniqueDatabaseNames)
                {
                    databases.Add(new Database { Name = name, Schemas = null });
                }
                return databases;
            }
        }

        static List<Schema> GetDistinctSchemas(XmlDocument doc, string database)
        {
            XmlNodeList schemaNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}']/@Schema");
            return (from distinctNames in (from names in schemaNames.Cast<XmlAttribute>() select names.Value).Distinct()
                    select new Schema { Name = distinctNames, Tables = null }).ToList();
        }

        static List<DataTable> GetDistinctTables(XmlDocument doc, string database, string schema)
        {
            XmlNodeList tableNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}']/@Table");
            return (from distinctNames in (from names in tableNames.Cast<XmlAttribute>() select names.Value).Distinct()
                    select new DataTable { Name = distinctNames, Columns = null }).ToList();
        }

        static List<string> GetDistinctColumns(XmlDocument doc, string database, string schema, string table)
        {
            XmlNodeList columnNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}' and @Table='{table}']/@Column");
            return (from distinctNames in (from names in columnNames.Cast<XmlAttribute>() select names.Value).Distinct()
                    select distinctNames).ToList();
        }

        static List<Database> ConstructDatabaseList(XmlDocument doc)
        {
            List<Database> databases = GetDistinctDatabases(doc);
            foreach (Database database in databases)
            {
                database.Schemas = GetDistinctSchemas(doc, database.Name);
                foreach (Schema schema in database.Schemas)
                {
                    schema.Tables = GetDistinctTables(doc, database.Name, schema.Name);
                    foreach (DataTable table in schema.Tables)
                    {
                        table.Columns = GetDistinctColumns(doc, database.Name, schema.Name, table.Name);
                        foreach (string column in table.Columns)
                        {
                            Console.WriteLine($"{database.Name}\t{schema.Name}\t{table.Name}\t{column}");
                        }
                    }
                }
            }
            return databases;
        }
        public class Database
        {
            public string Name { get; set; }
            public List<Schema> Schemas { get; set; }
        }
        public class Schema
        {
            public string Name { get; set; }
            public List<DataTable> Tables { get; set; }
        }
        public class DataTable
        {
            public string Name { get; set; }
            public List<string> Columns { get; set; }
        }
        class DatabaseProgram
        {
            public string Database { get; set; }
            public string Schema { get; set; }
            public string Name { get; set; }
        }

        static void PrintProgramArguments()
        {
            Console.WriteLine("DbEnumerator.exe ([Connectionstring]|[Data Source] [UserId] [Password] [Initial Catalog]");
        }

        static List<DatabaseProgram> GetDatabasePrograms(SqlConnection dbConn)
        {
            dbConn.Open();

            Console.WriteLine("Pulling database information");
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT v_routines.ROUTINE_CATALOG AS[Database], SCHEMA_NAME(t_objects.schema_id) AS[Schema], v_routines.ROUTINE_NAME AS[Name]");
            queryBuilder.Append(" FROM INFORMATION_SCHEMA.ROUTINES v_routines");
            queryBuilder.Append(" INNER JOIN sys.objects t_objects");
            queryBuilder.Append(" ON v_routines.ROUTINE_NAME = t_objects.name");
            queryBuilder.Append(" ORDER BY ROUTINE_TYPE");
            string pullInformation = queryBuilder.ToString();
            queryBuilder.Clear();

            const string SHOW_XML_PLAN_ON = "SET SHOWPLAN_XML ON";
            const string SHOW_XML_PLAN_OFF = "SET SHOWPLAN_XML OFF";

            using SqlCommand command = dbConn.CreateCommand();
            command.CommandText = pullInformation;
            command.CommandType = System.Data.CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            List<DatabaseProgram> dbPrograms = new List<DatabaseProgram>();
            while (dataReader.Read())
            {
                dbPrograms.Add(new DatabaseProgram { Database = dataReader[0].ToString(), Schema = dataReader[1].ToString(), Name = dataReader[2].ToString() });
            }
            return dbPrograms;
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
                    List<DatabaseProgram> dbInfo = GetDatabasePrograms(dbConn);
                }
                else if (args.Length == 1)
                {
                    using SqlConnection dbConn = new SqlConnection(args[0]);
                    List<DatabaseProgram> dbInfo = GetDatabasePrograms(dbConn);
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
