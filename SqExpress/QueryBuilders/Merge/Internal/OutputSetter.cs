using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.QueryBuilders.Merge.Internal
{
    public class OutputSetter : IOutputSetter<IOutputSetterNext>, IOutputSetterNext
    {
        private readonly List<IExprOutputColumn> _columns = new List<IExprOutputColumn>();

        public IReadOnlyList<IExprOutputColumn> Columns => this._columns;

        public IOutputSetterNext Inserted(ExprColumn column)
        {
            this._columns.Add(new ExprOutputColumnInserted(new ExprAliasedColumnName(column.ColumnName, null)));
            return this;
        }

        public IOutputSetterNext Inserted(ExprAliasedColumn column)
        {
            this._columns.Add(new ExprOutputColumnInserted(new ExprAliasedColumnName(column.Column.ColumnName, column.Alias)));
            return this;
        }

        public IOutputSetterNext Deleted(ExprColumn column)
        {
            this._columns.Add(new ExprOutputColumnDeleted(new ExprAliasedColumnName(column.ColumnName, null)));
            return this;
        }

        public IOutputSetterNext Deleted(ExprAliasedColumn column)
        {
            this._columns.Add(new ExprOutputColumnDeleted(new ExprAliasedColumnName(column.Column.ColumnName, column.Alias)));
            return this;
        }

        public IOutputSetterNext Column(ExprColumn column)
        {
            this._columns.Add(new ExprOutputColumn(new ExprAliasedColumn(column, null)));
            return this;
        }

        public IOutputSetterNext Column(ExprAliasedColumn column)
        {
            this._columns.Add(new ExprOutputColumn(column));
            return this;
        }

        public IOutputSetterNext Action(ExprColumnAlias? alias = null)
        {
            this._columns.Add(new ExprOutputAction(alias));
            return this;
        }
    }
}