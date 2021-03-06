﻿using System;
using System.Collections.Generic;

namespace DbEnumeratorLib
{
    public class Schema : IEquatable<Schema>
    {
        public string Name { get; set; }
        public ICollection<Table> Tables { get; set; }
        bool IEquatable<Schema>.Equals(Schema other)
        {
            if (other is null)
            {
                return false;
            }

            return this.GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj) => this.Equals(obj as Schema);

        public override int GetHashCode() => HashCode.Combine(Name, Tables);
    }
}
