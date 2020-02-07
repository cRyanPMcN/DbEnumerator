﻿using System;
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
        public SqlConnection Databaseconnection { get; set; }
        public Enumerator(SqlConnection dbConn)
        {
            Databaseconnection = dbConn;
        }
        #region Constant SQL Queries
        const string SHOW_XML_PLAN_ON = "SET SHOWPLAN_XML ON";
        const string SHOW_XML_PLAN_OFF = "SET SHOWPLAN_XML OFF";
        #endregion
        public void SetXMLPlanOn()
        {
            using SqlCommand command = Databaseconnection.CreateCommand();
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
            using SqlCommand command = Databaseconnection.CreateCommand();
            command.CommandText = SHOW_XML_PLAN_OFF;
            command.CommandType = System.Data.CommandType.Text;
            // ExecuteNonQuery returns a number of rows affected
            // SHOW_XML_PLAN_ON will not return a row
            // therefore this does not provie a method of checking
            // whether the command was successful
            command.ExecuteNonQuery();
        }

        public string GetProgramInfo(DatabaseProgram program)
        {
            throw new NotImplementedException("GetProgramInfo requires more data from get DatabasePrograms");
        }

        public XmlNodeList GetColumnReferenceNodes(XmlDocument doc)
        {
            return doc.DocumentElement.SelectNodes("//ColumnReference[@Database]");
        }

        public List<Database> GetDistinctDatabases(XmlDocument doc)
        {
            XmlNodeList databaseNames = doc.DocumentElement.SelectNodes("//ColumnReference[@Database]/@Database");
            try
            {
                List<string> distinctNames = (from names in databaseNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

                return (from name in distinctNames 
                        select new Database { Name = name, Schemas = null }).ToList();
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

        public List<Schema> GetDistinctSchemas(XmlDocument doc, string database)
        {
            XmlNodeList schemaNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}']/@Schema");

            List<string> distinctNames = (from names in schemaNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new Schema { Name = name, Tables = null }).ToList();
        }

        public List<DataTable> GetDistinctTables(XmlDocument doc, string database, string schema)
        {
            XmlNodeList tableNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}']/@Table");
            List<string> distinctNames = (from names in tableNames.Cast<XmlAttribute>() select names.Value).Distinct().ToList();

            return (from name in distinctNames 
                    select new DataTable { Name = name, Columns = null }).ToList();
        }

        public List<string> GetDistinctColumns(XmlDocument doc, string database, string schema, string table)
        {
            XmlNodeList columnNames = doc.DocumentElement.SelectNodes($"//ColumnReference[@Database='{database}' and @Schema='{schema}' and @Table='{table}']/@Column");

            return (from names in columnNames.Cast<XmlAttribute>()
                    select names.Value).Distinct().ToList();
        }

        public List<Database> ConstructDatabaseList(XmlDocument doc)
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

        public List<DatabaseProgram> GetDatabasePrograms()
        {
            Databaseconnection.Open();

            // TODO: Pull Parameters with the functions
            // --SELECT * FROM INFORMATION_SCHEMA.PARAMETERS
            Console.WriteLine("Pulling database information");
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT v_routines.ROUTINE_CATALOG AS[Database], SCHEMA_NAME(t_objects.schema_id) AS[Schema], v_routines.ROUTINE_NAME AS[Name]");
            queryBuilder.Append(" FROM INFORMATION_SCHEMA.ROUTINES v_routines");
            queryBuilder.Append(" INNER JOIN sys.objects t_objects");
            queryBuilder.Append(" ON v_routines.ROUTINE_NAME = t_objects.name");
            queryBuilder.Append(" ORDER BY ROUTINE_TYPE");
            string pullInformation = queryBuilder.ToString();
            queryBuilder.Clear();

            using SqlCommand command = Databaseconnection.CreateCommand();
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
    }
}