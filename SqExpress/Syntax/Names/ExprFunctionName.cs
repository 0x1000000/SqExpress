using System;

namespace SqExpress.Syntax.Names
{
    public class ExprFunctionName : IExpr, IEquatable<ExprFunctionName>
    {
        public ExprFunctionName(bool builtIn, string name)
        {
            this.BuiltIn = builtIn;
            this.Name = name;
            this.LowerInvariantName = name.ToLowerInvariant();
        }

        public bool BuiltIn { get; }

        public string Name { get; }

        public string LowerInvariantName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprFunctionName(this, arg);

        public bool Equals(ExprFunctionName? other)
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
            return this.Equals((ExprFunctionName) obj);
        }

        public override int GetHashCode()
        {
            return this.LowerInvariantName.GetHashCode();
        }
    }
}