using System;

namespace SqExpress.DbMetadata.Internal.Model
{
    internal class ColumnRef : IEquatable<ColumnRef>, IComparable<ColumnRef>
    {
        public ColumnRef(string schema, string tableName, string name)
        {
            Name = name;
            Table = new TableRef(schema, tableName);
        }

        public TableRef Table { get; }

        public string Schema => Table.Schema;

        public string TableName => Table.Name;

        public string Name { get; }

        public bool Equals(ColumnRef? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Table.Equals(other.Table) && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ColumnRef)obj);
        }


        public override string ToString()
        {
            return $"{Schema}.{TableName}.{Name}";
        }

        public override int GetHashCode()
        {
#if NETSTANDARD
            unchecked
            {
                return Table.GetHashCode() * 397 ^ Name.GetHashCode();
            }
#else
            var hashCode = new HashCode();
            hashCode.Add(Table);
            hashCode.Add(Name, StringComparer.InvariantCultureIgnoreCase);
            return hashCode.ToHashCode();
#endif
        }


        public int CompareTo(ColumnRef? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var tableComparison = Table.CompareTo(other.Table);
            if (tableComparison != 0) return tableComparison;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }
}