using System.Collections.Generic;

namespace DbEnumerator
{
    public enum ProgramType { View, ScalarValueFunction, TableValueFunction, StoredProcedure }
    public class DatabaseProgram
    {
        public class Parameter
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
        public ProgramType Type {get;set;}
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
}
