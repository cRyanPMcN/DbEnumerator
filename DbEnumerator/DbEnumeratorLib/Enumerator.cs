using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Linq;

namespace DbEnumerator
{
    public class Enumerator
    {
        public SqlConnection DatabaseConnection { get; set; }
        public Enumerator(SqlConnection dbConn)
        {
            DatabaseConnection = dbConn;
            DatabaseConnection.Open();
        }

        ~Enumerator()
        {
            // Safety before efficiency
            SetXMLPlanOff();
            DatabaseConnection.Close();
        }
        #region Constant SQL Queries
        const string SHOW_XML_PLAN_ON = "SET SHOWPLAN_XML ON";
        const string SHOW_XML_PLAN_OFF = "SET SHOWPLAN_XML OFF";
        #endregion
        public void SetXMLPlanOn()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = SHOW_XML_PLAN_ON;
            command.CommandType = System.Data.CommandType.Text;
            // ExecuteNonQuery returns a number of rows affected
            // SHOW_XML_PLAN_ON will not return a row
            // therefore this does not provie a method of checking
            // whether the command was successful
            command.ExecuteNonQuery();
        }
        public void SetXMLPlanOff()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = SHOW_XML_PLAN_OFF;
            command.CommandType = System.Data.CommandType.Text;
            // ExecuteNonQuery returns a number of rows affected
            // SHOW_XML_PLAN_ON will not return a row
            // therefore this does not provie a method of checking
            // whether the command was successful
            command.ExecuteNonQuery();
        }

        // Fix an issue that stops the plan from being enumerated
        // Once the code is complete to pull the plan this will be moved directly into the function that uses it;
        public static string FixXMLPlan(string xmlPlan)
        {
            return xmlPlan.Replace("xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\"", "xmlns:ms=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\"");
        }

        public string GetProgramInfo(DatabaseProgram program)
        {
            throw new NotImplementedException("GetProgramInfo requires more data from get DatabasePrograms");
        }

        public XmlNodeList GetColumnReferenceNodes(XmlDocument doc)
        {
            return doc.DocumentElement.SelectNodes("//ColumnReference[@Database]");
        }

        public IEnumerable<Database> GetDistinctDatabases(XmlDocument doc)
        {
            XmlNodeList databaseNames = doc.DocumentElement.SelectNodes("//ColumnReference[@Database]/@Database");
            List<string> distinctNames = (from names in databaseNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new Database { Name = name, Schemas = null });
        }

        public IEnumerable<Schema> GetDistinctSchemas(XmlDocument doc, string database)
        {
            XmlNodeList schemaNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}']/@Schema");

            List<string> distinctNames = (from names in schemaNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new Schema { Name = name, Tables = null });
        }

        public IEnumerable<DataTable> GetDistinctTables(XmlDocument doc, string database, string schema)
        {
            XmlNodeList tableNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}']/@Table");
            List<string> distinctNames = (from names in tableNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new DataTable { Name = name, Columns = null });
        }

        public IEnumerable<string> GetDistinctColumns(XmlDocument doc, string database, string schema, string table)
        {
            XmlNodeList columnNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}' and @Table='{table}']/@Column");

            return (from names in columnNames.Cast<XmlAttribute>()
                    select names.Value).Distinct();
        }

        public IEnumerable<Database> ConstructDatabaseList(XmlDocument doc)
        {
            IEnumerable<Database> databases = GetDistinctDatabases(doc);
            foreach (Database database in databases)
            {
                database.Schemas = GetDistinctSchemas(doc, database.Name);
                foreach (Schema schema in database.Schemas)
                {
                    schema.Tables = GetDistinctTables(doc, database.Name, schema.Name);
                    foreach (DataTable table in schema.Tables)
                    {
                        table.Columns = GetDistinctColumns(doc, database.Name, schema.Name, table.Name);
#if DEBUG
                        foreach (string column in table.Columns)
                        {
                            Console.WriteLine($"{database.Name}\t{schema.Name}\t{table.Name}\t{column}");
                        }
#endif
                    }
                }
            }
            return databases;
        }

        // In C++ this would be defined in the function, C# does not allow in-function statically allocated variables
        static readonly Dictionary<string, ProgramType> ProgramTypeDictionaries = new Dictionary<string, ProgramType>
        {
            { "V", ProgramType.View },
            { "FN", ProgramType.ScalarValueFunction },
            { "TF", ProgramType.TableValueFunction },
            { "P", ProgramType.StoredProcedure }
        };
        public IEnumerable<DatabaseProgram> GetDatabasePrograms()
        {
            Console.WriteLine("Pulling database information");
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.AppendLine("SELECT");
            queryBuilder.AppendLine("    [type]");
            queryBuilder.AppendLine("    , [type_desc]");
            queryBuilder.AppendLine("    , DB_NAME()");
            queryBuilder.AppendLine("    , SCHEMA_NAME([schema_id])");
            queryBuilder.AppendLine("    , OBJECT_NAME([object_id])");
            queryBuilder.AppendLine("    , [object_id]");
            queryBuilder.AppendLine("FROM sys.objects");
            queryBuilder.AppendLine("WHERE type_desc IN('SQL_SCALAR_FUNCTION', 'SQL_STORED_PROCEDURE', 'SQL_TABLE_VALUED_FUNCTION', 'VIEW')");
            queryBuilder.AppendLine("ORDER BY name");


            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = queryBuilder.ToString();
            queryBuilder.Clear();
            command.CommandType = System.Data.CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            List<DatabaseProgram> dbPrograms = new List<DatabaseProgram>();
            while (dataReader.Read())
            {
                dbPrograms.Add(new DatabaseProgram { 
                    Type = ProgramTypeDictionaries[dataReader.GetString(0)],
                    Database = dataReader.GetString(2),
                    Schema = dataReader.GetString(3),
                    Name = dataReader.GetString(4),
                    Id = dataReader.GetString(5) 
                });
            }
            return dbPrograms;
        }

        public void GetProgramParameters(DatabaseProgram program)
        {
            Console.WriteLine($"Pulling parameters for {program.ToString()}");

            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.AppendLine("SELECT");
            queryBuilder.AppendLine("	[name]");
            queryBuilder.AppendLine("	, TYPE_NAME([user_type_id])");
            queryBuilder.AppendLine("	, [is_nullable]");
            queryBuilder.AppendLine("	, TYPE_NAME([system_type_id])");
            queryBuilder.AppendLine("	, *");
            queryBuilder.AppendLine("FROM sys.parameters");
            queryBuilder.AppendLine($"WHERE [is_output] = 0 AND [object_id] = {program.Id}");
            queryBuilder.AppendLine("ORDER BY [object_id], [parameter_id]");

            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = queryBuilder.ToString();
            queryBuilder.Clear();
            command.CommandType = System.Data.CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                program.Parameters.Add(new DatabaseProgram.Parameter
                {
                    Name = dataReader.GetString(0),
                    Type = dataReader.GetString(1),
                    IsNullable = dataReader.GetBoolean(2),
                    // TODO: Implement Value properly
                    // Dictionary would hold default values for all types
                    // This seems unreasonable to implement, will be implemented if a query plan changes based on Arguments being inputted
                    // Value = ParameterTypeDictionary[dataReader.GetString(3)]
                });
            }
        }

        public void GetProgramParameters(IEnumerable<DatabaseProgram> programList)
        {
            foreach (DatabaseProgram program in programList)
            {
                GetProgramParameters(program);
            }
        }
    }
}
