using System;

namespace SqExpress.DbMetadata.Internal.Model
{
    internal class TableRef : IEquatable<TableRef>, IComparable<TableRef>
    {
        public TableRef(string schema, string name)
        {
            Schema = schema;
            Name = name;
        }

        public string Schema { get; }
        public string Name { get; }

        public override string ToString()
        {
            return $"{Schema}.{Name}";
        }

        public bool Equals(TableRef? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Schema == other.Schema && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TableRef)obj);
        }

        public override int GetHashCode()
        {
#if NETSTANDARD
            unchecked
            {
                return Schema.GetHashCode() * 397 ^ Name.GetHashCode();
            }
#else
            var hashCode = new HashCode();
            hashCode.Add(Schema, StringComparer.InvariantCultureIgnoreCase);
            hashCode.Add(Name, StringComparer.InvariantCultureIgnoreCase);
            return hashCode.ToHashCode();
#endif
        }

        public int CompareTo(TableRef? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var schemaComparison = string.Compare(Schema, other.Schema, StringComparison.Ordinal);
            if (schemaComparison != 0) return schemaComparison;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }
}