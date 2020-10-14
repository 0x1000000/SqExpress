using System;

namespace SqExpress.Syntax.Names
{
    public class ExprTableAlias : IExprColumnSource, IEquatable<ExprTableAlias>
    {
        public ExprTableAlias(IExprAlias alias)
        {
            this.Alias = alias;
        }

        public IExprAlias Alias { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTableAlias(this, arg);

        public bool Equals(ExprTableAlias? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Alias.Equals(other.Alias);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ExprTableAlias) obj);
        }

        public override int GetHashCode()
        {
            return this.Alias.GetHashCode();
        }
    }
}