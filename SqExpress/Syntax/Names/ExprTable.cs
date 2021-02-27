using System;
using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Names
{
    public class ExprTable : IExprTableSource, IEquatable<ExprTable>
    {
        public ExprTable(IExprTableFullName fullName, ExprTableAlias? alias)
        {
            this.Alias = alias;
            this.FullName = fullName;
        }

        public ExprTableAlias? Alias { get; }

        public IExprTableFullName FullName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTable(this, arg);

        public TableMultiplication ToTableMultiplication()
        {
            return new TableMultiplication(new[] {this}, null);
        }

        public bool Equals(ExprTable? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(this.Alias, other.Alias) && this.FullName.Equals(other.FullName);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ExprTable) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Alias != null ? this.Alias.GetHashCode() : 0) * 397) ^ this.FullName.GetHashCode();
            }
        }
    }
}