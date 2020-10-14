using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Select.SelectItems
{
    public class ExprAliasedColumn : IExprNamedSelecting
    {
        public ExprAliasedColumn(ExprColumn column, ExprColumnAlias? alias)
        {
            this.Column = column;
            this.Alias = alias;
        }

        public ExprColumn Column { get; }

        public ExprColumnAlias? Alias { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprAliasedColumn(this, arg);

        string IExprNamedSelecting.OutputName
        {
            get
            {
                if (this.Alias != null)
                {
                    return this.Alias.Name;
                }

                return this.Column.ColumnName.Name;
            }
        }

        public static implicit operator ExprAliasedColumn(ExprColumn column) => new ExprAliasedColumn(column, null);
    }
}