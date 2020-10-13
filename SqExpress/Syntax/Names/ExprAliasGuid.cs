using System;

namespace SqExpress.Syntax.Names
{
    public class ExprAliasGuid : IExprAlias, IEquatable<ExprAliasGuid>
    {
        public Guid Id { get; }

        public ExprAliasGuid(Guid id)
        {
            this.Id = id;
        }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprAliasGuid(this);

        public bool Equals(ExprAliasGuid? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ExprAliasGuid) obj);
        }

        public override int GetHashCode() => this.Id.GetHashCode();
    }
}