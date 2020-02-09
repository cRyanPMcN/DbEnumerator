using System.Collections.Generic;


namespace DbEnumerator
{
    public class DataTable
    {
        public string Name { get; set; }
        public IEnumerable<string> Columns { get; set; }
    }
}
