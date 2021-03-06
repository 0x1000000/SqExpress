﻿using System;

namespace SqExpress.CodeGenUtil.Model
{
    internal class TableRef : IEquatable<TableRef>, IComparable<TableRef>
    {
        public TableRef(string schema, string name)
        {
            this.Schema = schema;
            this.Name = name;
        }

        public string Schema { get; }
        public string Name { get; }

        public override string ToString()
        {
            return $"{this.Schema}.{this.Name}";
        }

        public bool Equals(TableRef? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(this.Schema, other.Schema, StringComparison.InvariantCultureIgnoreCase) && string.Equals(this.Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TableRef) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(this.Schema, StringComparer.InvariantCultureIgnoreCase);
            hashCode.Add(this.Name, StringComparer.InvariantCultureIgnoreCase);
            return hashCode.ToHashCode();
        }

        public int CompareTo(TableRef? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var schemaComparison = string.Compare(this.Schema, other.Schema, StringComparison.Ordinal);
            if (schemaComparison != 0) return schemaComparison;
            return string.Compare(this.Name, other.Name, StringComparison.Ordinal);
        }
    }
}