using System;
using System.Collections.Generic;
using System.Linq;

namespace DbEnumeratorLib
{
    public class Database : IEquatable<Database>
    {
        public string Name { get; set; }
        public ICollection<Schema> Schemas { get; set; }

        bool IEquatable<Database>.Equals(Database other)
        {
            if (other is null)
            {
                return false;
            }

            return this.GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj) => this.Equals(obj as Database);

        public override int GetHashCode() => HashCode.Combine(Name, Schemas);
    }
}
