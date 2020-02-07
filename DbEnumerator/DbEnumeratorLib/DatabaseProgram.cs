namespace DbEnumerator
{
    public class DatabaseProgram
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return $"{Database}.{Schema}.{Name}";
        }
    }
}
