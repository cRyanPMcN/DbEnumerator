using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace DbEnumerator
{
    public class Enumerator
    {
        public SqlConnection DatabaseConnection { get; set; }
        #region Constant SQL Queries
        const string SHOW_XML_PLAN_ON = "SET SHOWPLAN_XML ON";
        const string SHOW_XML_PLAN_OFF = "SET SHOWPLAN_XML OFF";
        #endregion

        public Enumerator(SqlConnection dbConn)
        {
            DatabaseConnection = dbConn;
            DatabaseConnection.Open();
        }

        ~Enumerator()
        {
            SetXMLPlanOff();
            DatabaseConnection.Close();
        }

        public void SetXMLPlanOn()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = SHOW_XML_PLAN_ON;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();
        }

        public void SetXMLPlanOff()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = SHOW_XML_PLAN_OFF;
            command.CommandType = System.Data.CommandType.Text;
            command.ExecuteNonQuery();
        }

        public XmlNodeList GetColumnReferenceNodes(XmlDocument doc)
        {
            return doc.DocumentElement.SelectNodes("//ColumnReference[@Database]");
        }

        public ICollection<Database> GetDistinctDatabases(XmlDocument doc, XmlNamespaceManager xnsm)
        {
            XmlNodeList databaseNames = doc.DocumentElement.SelectNodes("//ColumnReference[@Database]/@Database", xnsm);
            List<string> distinctNames = (from names in databaseNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new Database { Name = name, Schemas = null }).ToList();
        }

        public ICollection<Schema> GetDistinctSchemas(XmlDocument doc, XmlNamespaceManager xnsm, string database)
        {
            XmlNodeList schemaNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}']/@Schema", xnsm);

            List<string> distinctNames = (from names in schemaNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new Schema { Name = name, Tables = null }).ToList();
        }

        public ICollection<DataTable> GetDistinctTables(XmlDocument doc, XmlNamespaceManager xnsm, string database, string schema)
        {
            XmlNodeList tableNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}']/@Table", xnsm);
            List<string> distinctNames = (from names in tableNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new DataTable { Name = name, Columns = null }).ToList();
        }

        public ICollection<string> GetDistinctColumns(XmlDocument doc, XmlNamespaceManager xnsm, string database, string schema, string table)
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
            ICollection<Database> databases = GetDistinctDatabases(doc, xnsm);
            foreach (Database database in databases)
            {
                database.Schemas = GetDistinctSchemas(doc, xnsm, database.Name);
                foreach (Schema schema in database.Schemas)
                {
                    schema.Tables = GetDistinctTables(doc, xnsm, database.Name, schema.Name);
                    foreach (DataTable table in schema.Tables)
                    {
                        table.Columns = GetDistinctColumns(doc, xnsm, database.Name, schema.Name, table.Name);
                        foreach (string column in table.Columns)
                        {
                            System.Diagnostics.Debug.WriteLine($"{database.Name}\t{schema.Name}\t{table.Name}\t{column}");
                        }
                    }
                }
            }
            return databases;
        }

        public IDatabaseProgram DatabaseProgramFactory(SqlDataReader programData)
        {
            switch (programData.GetString(0).Trim())
            {
                case "V":
                    return new DatabaseView
                    {
                        Database = programData.GetString(2),
                        Schema = programData.GetString(3),
                        Name = programData.GetString(4),
                        Id = programData.GetInt32(5),
                        Parameters = new List<Parameter>()
                    };
                case "FN":
                    return new DatabaseScalarFunction
                    {
                        Database = programData.GetString(2),
                        Schema = programData.GetString(3),
                        Name = programData.GetString(4),
                        Id = programData.GetInt32(5),
                        Parameters = new List<Parameter>()
                    };
                case "TF":
                    return new DatabaseTableFunction
                    {
                        Database = programData.GetString(2),
                        Schema = programData.GetString(3),
                        Name = programData.GetString(4),
                        Id = programData.GetInt32(5),
                        Parameters = new List<Parameter>()
                    };
                case "P":
                    return new DatabaseProcedure
                    {
                        Database = programData.GetString(2),
                        Schema = programData.GetString(3),
                        Name = programData.GetString(4),
                        Id = programData.GetInt32(5),
                        Parameters = new List<Parameter>()
                    };
                default:
                    throw new ArgumentException($"Unexpected DatabaseProgram Type of {programData.GetString(0)} in Enumerator::DatabaseProgramFactory(SqlDataReader)");
            }
        }
        
        static readonly string QueryGetDatabasePrograms =
            "SELECT [type], [type_desc], DB_NAME(), SCHEMA_NAME([schema_id]), OBJECT_NAME([object_id]), [object_id] FROM sys.objects " +
            "WHERE type_desc IN('SQL_SCALAR_FUNCTION', 'SQL_STORED_PROCEDURE', 'SQL_TABLE_VALUED_FUNCTION', 'VIEW') " +
            "ORDER BY name";
        public ICollection<IDatabaseProgram> GetDatabasePrograms()
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = QueryGetDatabasePrograms;
            command.CommandType = System.Data.CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            List<IDatabaseProgram> dbPrograms = new List<IDatabaseProgram>();
            while (dataReader.Read())
            {
                dbPrograms.Add(DatabaseProgramFactory(dataReader));
            }
            return dbPrograms;
        }

        static readonly string QueryGetProgramParameters =
            "SELECT [name], TYPE_NAME([user_type_id]), [is_nullable] FROM sys.parameters " +
            "WHERE [is_output] = 0 AND [object_id] = @ProgramId " +
            "ORDER BY [object_id], [parameter_id]";
        public void GetProgramParameters(IDatabaseProgram program)
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = QueryGetProgramParameters;
            command.Parameters.AddWithValue("@ProgramId", program.Id);
            command.CommandType = System.Data.CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                program.Parameters.Add(new Parameter
                {
                    Name = dataReader.GetString(0),
                    Type = dataReader.GetString(1),
                    IsNullable = dataReader.GetBoolean(2),
                    // TODO: Implement Value properly
                    // Planned to have a dictionary which holds default values for all types
                    // This seems unreasonable to implement, will be implemented if a query plan changes based on Arguments being inputted
                    // Value = ParameterTypeDictionary[dataReader.GetString(3)]
                });
            }
        }

        public void GetProgramParameters(IEnumerable<IDatabaseProgram> programList)
        {
            foreach (IDatabaseProgram program in programList)
            {
                GetProgramParameters(program);
            }
        }

        public string GetQueryPlan(IDatabaseProgram program)
        {
            using SqlCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = program.QueryString;
            command.CommandType = System.Data.CommandType.Text;
            using SqlDataReader dataReader = command.ExecuteReader();
            List<string> queryResults = new List<string>();
            while (dataReader.Read())
            {
                queryResults.Add(dataReader.GetString(0).Replace("xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\"", "xmlns:ms=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\""));
            }
            if (queryResults.Count != 1)
            {
                // TODO: Switch Exception to more descriptive exception name
                throw new Exception($"Error getting query plan for {program.ToString()}: Unexpected number of query plans '{queryResults.Count}' was returned by the database.");
            }
            return queryResults[0];
        }
    }
}
