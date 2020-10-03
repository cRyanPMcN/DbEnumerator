using System;
using System.Collections.Generic;
using System.Xml;
using System.Data.SqlClient;
using System.Linq;
using System.Data;
using System.IO;

namespace DbEnumeratorLib
{
    public class Enumerator
    {
        public SqlConnection DatabaseConnection { get; set; }
        private bool ShowXmlPlan { get; set; }
        #region SQL Querie Constants
        const string SHOW_XML_PLAN_ON = "SET SHOWPLAN_XML ON";
        const string SHOW_XML_PLAN_OFF = "SET SHOWPLAN_XML OFF";

        const string GET_DATABASE_PROGRAMS =
            "SELECT " +
                "[type], [type_desc], DB_NAME(), SCHEMA_NAME([schema_id]), OBJECT_NAME([object_id]), [object_id] " +
            "FROM sys.objects " +
            "WHERE type_desc IN('SQL_SCALAR_FUNCTION', 'SQL_STORED_PROCEDURE', 'SQL_TABLE_VALUED_FUNCTION', 'VIEW') " +
            "ORDER BY name";

        const string GET_PROGRAM_PARAMETERS =
            "SELECT " +
                "[name], TYPE_NAME([user_type_id]), [is_nullable] " +
            "FROM sys.parameters " +
            "WHERE [is_output] = 0 AND [object_id] = @ProgramId " +
            "ORDER BY [object_id], [parameter_id]";

        const string VIEW               = "V";
        const string SCALAR_FUNCTION    = "FN";
        const string TABLE_FUNCTION     = "TF";
        const string PROCEDURE          = "P";
        #endregion

        public Enumerator(SqlConnection dbConn)
        {
            DatabaseConnection = dbConn;
            DatabaseConnection.Open();
            ShowXmlPlan = false;
        }

        ~Enumerator()
        {
            if (ShowXmlPlan)
            {
                SetXMLPlanOff();
            }
            DatabaseConnection.Close();
        }

        public void SetXMLPlanOn()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = SHOW_XML_PLAN_ON;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
            ShowXmlPlan = true;
        }

        public void SetXMLPlanOff()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = SHOW_XML_PLAN_OFF;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
            ShowXmlPlan = false;
        }

        public IProgram DatabaseProgramFactory(SqlDataReader programData)
        {
            string programType = programData.GetString(0).Trim();
            IProgram program;

            if (programType == VIEW)
            {
                program = new View();
            }
            else if (programType == SCALAR_FUNCTION)
            {
                program = new ScalarFunction();
            }
            else if (programType == TABLE_FUNCTION)
            {
                program = new TableFunction();
            }
            else if (programType == PROCEDURE)
            {
                program = new Procedure();
            }
            else
            {
                throw new ArgumentException($"Unexpected DatabaseProgram Type of {programData.GetString(0)} in Enumerator::DatabaseProgramFactory(SqlDataReader)");
            }

            program.Database = programData.GetString(2);
            program.Schema = programData.GetString(3);
            program.Name = programData.GetString(4);
            program.Id = programData.GetInt32(5);
            program.Parameters = new List<Parameter>();

            return program;
        }

        public void GetProgramParameters(IProgram program)
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = GET_PROGRAM_PARAMETERS;
            command.Parameters.AddWithValue("@ProgramId", program.Id);
            command.CommandType = CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                program.Parameters.Add(new Parameter
                {
                    Name = dataReader.GetString(0),
                    Type = dataReader.GetString(1),
                    IsNullable = dataReader.GetBoolean(2)
                });
            }
        }

        public void GetProgramParameters(IEnumerable<IProgram> programList)
        {
            foreach (IProgram program in programList)
            {
                GetProgramParameters(program);
            }
        }

        public ICollection<IProgram> GetDatabasePrograms()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = GET_DATABASE_PROGRAMS;
            command.CommandType = CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            List<IProgram> dbPrograms = new List<IProgram>();
            while (dataReader.Read())
            {
                IProgram newProgram = DatabaseProgramFactory(dataReader);
                GetProgramParameters(newProgram);
                dbPrograms.Add(newProgram);
            }
            return dbPrograms;
        }

        public ICollection<Database> GetDatabasesDistinct(XmlDocument doc, XmlNamespaceManager xnsm)
        {
            XmlNodeList databaseNames = doc.DocumentElement.SelectNodes("//ColumnReference[@Database]/@Database", xnsm);
            IEnumerable<string> distinctNames = (from names in databaseNames.Cast<XmlAttribute>() select names.Value).Distinct();

            return (from name in distinctNames 
                    select new Database { Name = name, Schemas = null }).ToList();
        }

        public ICollection<Schema> GetSchemasDistinct(XmlDocument doc, XmlNamespaceManager xnsm, string database)
        {
            XmlNodeList schemaNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}']/@Schema", xnsm);

            IEnumerable<string> distinctNames = (from names in schemaNames.Cast<XmlAttribute>() select names.Value).Distinct();

            return (from name in distinctNames 
                    select new Schema { Name = name, Tables = null }).ToList();
        }

        public ICollection<Table> GetTablesDistinct(XmlDocument doc, XmlNamespaceManager xnsm, string database, string schema)
        {
            XmlNodeList tableNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}']/@Table", xnsm);
            IEnumerable<string> distinctNames = (from names in tableNames.Cast<XmlAttribute>() select names.Value).Distinct();

            return (from name in distinctNames 
                    select new Table { Name = name, Columns = null }).ToList();
        }

        public ICollection<string> GetColumnsDistinct(XmlDocument doc, XmlNamespaceManager xnsm, string database, string schema, string table)
        {
            XmlNodeList columnNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}' and @Table='{table}']/@Column", xnsm);

            return (from names in columnNames.Cast<XmlAttribute>()
                    select names.Value).Distinct().ToList();
        }

        public ICollection<Database> DatabaseCollectionFactory(XmlDocument doc)
        {
            XmlNamespaceManager xnsm = new XmlNamespaceManager(doc.NameTable);
            xnsm.AddNamespace("ms", "http://schemas.microsoft.com/sqlserver/2004/07/showplan/sql2017/showplanxml.xsd");
            xnsm.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xnsm.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            ICollection<Database> databases = GetDatabasesDistinct(doc, xnsm);
            foreach (Database database in databases)
            {
                database.Schemas = GetSchemasDistinct(doc, xnsm, database.Name);
                foreach (Schema schema in database.Schemas)
                {
                    schema.Tables = GetTablesDistinct(doc, xnsm, database.Name, schema.Name);
                    foreach (Table table in schema.Tables)
                    {
                        table.Columns = GetColumnsDistinct(doc, xnsm, database.Name, schema.Name, table.Name);
                        foreach (string column in table.Columns)
                        {
                            System.Diagnostics.Debug.WriteLine($"{database.Name}\t{schema.Name}\t{table.Name}\t{column}");
                        }
                    }
                }
            }
            return databases;
        }

        public XmlDocument GetQueryPlan(IProgram program)
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = program.QueryString;
            command.CommandType = CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();

            List<string> queryResults = new List<string>();
            while (dataReader.Read())
            {
                // Query plans have an improper namespace
                queryResults.Add(dataReader.GetString(0).Replace("xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\"", "xmlns:ms=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\""));
            }

            // Cannot check the result count ahead of time.
            if (queryResults.Count != 1)
            {
                // TODO: Utilize more descriptive exception type
                throw new Exception($"Error getting query plan for {program}: Unexpected number of query plans of '{queryResults.Count}' was returned by the database {DatabaseConnection.Database}.");
            }

            XmlDocument doc = new XmlDocument();
            StringReader strReader = new StringReader(queryResults[0]);
            doc.Load(strReader);

            return doc;
        }
    }
}
