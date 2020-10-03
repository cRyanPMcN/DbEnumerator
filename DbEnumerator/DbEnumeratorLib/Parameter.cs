using System;
using System.Collections.Generic;
using System.Text;

namespace DbEnumeratorLib
{
    public class Parameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNullable { get; set; }
        public object Value { get { return null; } }
    }
}
