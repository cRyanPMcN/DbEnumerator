using System.Collections.Generic;


namespace DbEnumerator
{
    public class Database
    {
        public string Name { get; set; }
        public List<Schema> Schemas { get; set; }
    }
}
