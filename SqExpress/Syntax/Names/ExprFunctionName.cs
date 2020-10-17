using System;

namespace SqExpress.Syntax.Names
{
    public class ExprFunctionName : IExprName, IEquatable<ExprFunctionName>
    {
        private string? _lowerInvariantName;

        public ExprFunctionName(bool builtIn, string name)
        {
            this.BuiltIn = builtIn;
            this.Name = name.Trim();
        }

        public bool BuiltIn { get; }

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
            => visitor.VisitExprFunctionName(this, arg);

        public bool Equals(ExprFunctionName? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.BuiltIn == other.BuiltIn && this.Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExprFunctionName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.BuiltIn.GetHashCode() * 397) ^ this.Name.GetHashCode();
            }
        }
    }
}