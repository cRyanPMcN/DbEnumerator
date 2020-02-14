using System.Collections.Generic;


namespace DbEnumerator
{
    public class Database
    {
        public string Name { get; set; }
        public ICollection<Schema> Schemas { get; set; }
    }
}
