using System;
using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Names
{
    public class ExprColumnName : IExprNamedSelecting, IExprName, IEquatable<ExprColumnName>
    {
        private string? _lowerInvariantName;

        public ExprColumnName(string name)
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
            => visitor.VisitExprColumnName(this, arg);

        public static implicit operator ExprColumnName(ExprColumn column) => column.ColumnName;

        public static implicit operator ExprColumnName(string columnName) => new ExprColumnName(columnName);

        string IExprNamedSelecting.OutputName => this.Name;

        public bool Equals(ExprColumnName? other)
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
            return Equals((ExprColumnName) obj);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}