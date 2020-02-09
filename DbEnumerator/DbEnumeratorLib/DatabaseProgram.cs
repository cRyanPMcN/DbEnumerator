using System.Collections.Generic;

namespace DbEnumerator
{
    public enum ProgramType { View, ScalarValueFunction, TableValueFunction, StoredProcedure }
    public class DatabaseProgram
    {
        public class Parameter
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsNullable { get; set; }
            // Force value to null, query plans stay the same
            // Written this way so that later it is easier to properly implement
            public object Value { get { return null; } }
        }
        public ProgramType Type {get;set;}
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public List<Parameter> Parameters { get; set; }
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
}
