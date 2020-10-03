using System;
using System.Collections.Generic;

namespace DbEnumeratorLib
{
    public class Table : IEquatable<Table>
    {
        public string Name { get; set; }
        public ICollection<string> Columns { get; set; }
        bool IEquatable<Table>.Equals(Table other)
        {
            if (other is null)
            {
                return false;
            }

            return this.GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj) => this.Equals(obj as Table);

        public override int GetHashCode() => HashCode.Combine(Name, Columns);
    }
}
