using System.Collections.Generic;
using System.Linq;
using System;

namespace DbEnumeratorLib
{
    public class TableFunction : IProgram
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
