using System;

namespace SqExpress.Syntax.Names
{
    public class ExprSchemaName : IExpr, IEquatable<ExprSchemaName>
    {
        public ExprSchemaName(string name)
        {
            this.Name = name.Trim();
            this.LowerInvariantName = this.Name.ToLowerInvariant();
        }

        public string Name { get; }

        public string LowerInvariantName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprSchemaName(this, arg);

        public bool Equals(ExprSchemaName? other)
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
            return this.Equals((ExprSchemaName) obj);
        }

        public override int GetHashCode() => this.LowerInvariantName.GetHashCode();
    }
}