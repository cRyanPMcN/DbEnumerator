using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace DbEnumerator
{
    public enum ProgramType { Null, View, ScalarValueFunction, TableValueFunction, StoredProcedure }
    public class Parameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNullable { get; set; }
        // Force value to null, query plans stay the same
        // Written this way so that later it is easier to properly implement
        public object Value { get { return null; } }
    }
    public interface IDatabaseProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public Int32 Id { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
        public string QueryString { get; }
        public string ParameterString { get; }
    }
    public class DatabaseView : IDatabaseProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public Int32 Id { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
        public string QueryString => $"SELECT * FROM {ToString()}{ParameterString}";
        public string ParameterString => $"";
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
    public class DatabaseProcedure : IDatabaseProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public Int32 Id { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
        public string QueryString => $"EXEC {ToString()} {ParameterString}";
        public string ParameterString => $"{string.Join(", ", from parameter in Parameters select $"{parameter.Name} = {parameter.Value ?? "NULL"}")}";
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
    public class DatabaseScalarFunction : IDatabaseProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public Int32 Id { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
        public string QueryString => $"SELECT {ToString()}{ParameterString}";
        public string ParameterString => $"({string.Join(", ", from parameter in Parameters select $"{parameter.Name} = {parameter.Value ?? "NULL"}")})";
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
    public class DatabaseTableFunction : IDatabaseProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public Int32 Id { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
        public string QueryString => $"SELECT * FROM {ToString()}{ParameterString}";
        public string ParameterString => $"({string.Join(", ", from parameter in Parameters select parameter.Value ?? "NULL")})";
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
}
