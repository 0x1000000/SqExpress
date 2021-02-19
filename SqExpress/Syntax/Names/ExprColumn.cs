using System;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Names
{
    public class ExprAllColumns : IExprSelecting
    {
        public ExprAllColumns(IExprColumnSource? source)
        {
            this.Source = source;
        }

        public IExprColumnSource? Source { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprAllColumns(this, arg);
    }

    public class ExprColumn : ExprValue, IExprNamedSelecting, IEquatable<ExprColumn>
    {
        public IExprColumnSource? Source { get; }

        public ExprColumnName ColumnName { get; }

        public ExprColumn(IExprColumnSource? source, ExprColumnName columnName)
        {
            this.Source = source;
            this.ColumnName = columnName;
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprColumn(this, arg);

        public static implicit operator ExprColumn(ExprColumnName columnName) => new ExprColumn(null, columnName);

        string IExprNamedSelecting.OutputName => this.ColumnName.Name;

        public bool Equals(ExprColumn? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(this.Source, other.Source) && Equals(this.ColumnName, other.ColumnName);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ExprColumn) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Source != null ? this.Source.GetHashCode() : 0) * 397) ^
                       (this.ColumnName != null ? this.ColumnName.GetHashCode() : 0);
            }
        }
    }
}