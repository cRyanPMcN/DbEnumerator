using System.Collections.Generic;
using System.Text;
using System;

namespace DbEnumeratorLib
{
    public interface IProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public Int32 Id { get; set; }
        public ICollection<Parameter> Parameters { get; set; }
        public string QueryString { get; }
        public string ParameterString { get; }
    }
}
