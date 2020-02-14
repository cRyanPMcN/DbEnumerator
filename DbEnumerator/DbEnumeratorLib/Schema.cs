using System.Collections.Generic;


namespace DbEnumerator
{
    public class Schema
    {
        public string Name { get; set; }
        public ICollection<DataTable> Tables { get; set; }
    }
}
