using System;
using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Names
{
    public class ExprColumnName : IExprNamedSelecting, IEquatable<ExprColumnName>
    {
        public ExprColumnName(string name)
        {
            this.Name = name.Trim();
            this.LowerInvariantName = this.Name.ToLowerInvariant();
        }

        public string Name { get; }

        public string LowerInvariantName { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprColumnName(this);

        public bool Equals(ExprColumnName? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.LowerInvariantName == other.LowerInvariantName;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ExprColumnName) obj);
        }

        public override int GetHashCode() => this.LowerInvariantName.GetHashCode();

        public static implicit operator ExprColumnName(ExprColumn column) => column.ColumnName;

        string IExprNamedSelecting.OutputName => this.Name;
    }
}