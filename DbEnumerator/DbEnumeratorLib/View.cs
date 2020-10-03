using System.Collections.Generic;
using System;

namespace DbEnumeratorLib
{
    public class View : IProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public Int32 Id { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
        public string QueryString => $"SELECT * FROM {ToString()}";
        public string ParameterString => $"";
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
}
