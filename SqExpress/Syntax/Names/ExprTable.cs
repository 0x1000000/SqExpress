using System;
using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Names
{
    public class ExprTable : IExprTableSource, IEquatable<ExprTable>
    {
        public ExprTableAlias? Alias { get; }

        public ExprTableFullName FullName { get; }

        public ExprTable(ExprTableFullName fullName, ExprTableAlias? alias)
        {
            this.Alias = alias;
            this.FullName = fullName;
        }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTable(this);

        public (IReadOnlyList<IExprTableSource> Tables, ExprBoolean? On) ToTableMultiplication()
        {
            return (new[] {this}, null);
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