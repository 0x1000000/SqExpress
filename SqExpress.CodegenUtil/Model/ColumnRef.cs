using System;

namespace SqExpress.CodeGenUtil.Model
{
    internal class ColumnRef : IEquatable<ColumnRef>, IComparable<ColumnRef>
    {
        public ColumnRef(string schema, string tableName, string name)
        {
            this.Name = name;
            this.Table = new TableRef(schema, tableName);
        }

        public TableRef Table { get; }

        public string Schema => Table.Schema;

        public string TableName => Table.Name;

        public string Name { get; }

        public bool Equals(ColumnRef? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Table.Equals(other.Table) && this.Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ColumnRef) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Table, this.Name);
        }

        public int CompareTo(ColumnRef? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var tableComparison = this.Table.CompareTo(other.Table);
            if (tableComparison != 0) return tableComparison;
            return string.Compare(this.Name, other.Name, StringComparison.Ordinal);
        }
    }
}