using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Select.SelectItems
{
    public class ExprAliasedColumnName : IExprNamedSelecting
    {
        public ExprAliasedColumnName(ExprColumnName column, ExprColumnAlias? alias)
        {
            this.Column = column;
            this.Alias = alias;
        }

        public ExprColumnName Column { get; }

        public ExprColumnAlias? Alias { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprAliasedColumnName(this, arg);

        string IExprNamedSelecting.OutputName
        {
            get
            {
                if (this.Alias != null)
                {
                    return this.Alias.Name;
                }

                return this.Column.Name;
            }
        }

        public static implicit operator ExprAliasedColumnName(ExprColumn column) => new ExprAliasedColumnName(column.ColumnName, null);

        public static implicit operator ExprAliasedColumnName(ExprColumnName column) => new ExprAliasedColumnName(column, null);
    }
}