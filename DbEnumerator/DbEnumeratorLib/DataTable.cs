using System.Collections.Generic;


namespace DbEnumerator
{
    public class DataTable
    {
        public string Name { get; set; }
        public ICollection<string> Columns { get; set; }
    }
}
