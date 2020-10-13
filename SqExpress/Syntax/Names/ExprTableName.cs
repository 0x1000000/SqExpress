using System;

namespace SqExpress.Syntax.Names
{
    public class ExprTableName : IExpr, IEquatable<ExprTableName>
    {
        public ExprTableName(string name)
        {
            this.Name = name.Trim();
            this.LowerInvariantName = this.Name.ToLowerInvariant();
        }

        public string Name { get; }

        public string LowerInvariantName { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTableName(this);

        public bool Equals(ExprTableName? other)
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
            return this.Equals((ExprTableName) obj);
        }

        public override int GetHashCode() => this.LowerInvariantName.GetHashCode();
    }
}