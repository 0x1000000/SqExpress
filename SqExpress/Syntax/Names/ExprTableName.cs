using System;

namespace SqExpress.Syntax.Names
{
    public class ExprTableName : IExprName, IEquatable<ExprTableName>
    {
        private string? _lowerInvariantName;

        public ExprTableName(string name)
        {
            this.Name = name.Trim();
        }

        public string Name { get; }

        public string LowerInvariantName
        {
            get
            {
                this._lowerInvariantName ??= this.Name.ToLowerInvariant();
                return this._lowerInvariantName;
            }
        }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTableName(this, arg);

        public bool Equals(ExprTableName? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExprTableName) obj);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}