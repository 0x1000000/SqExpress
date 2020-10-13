using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Insert
{
    public readonly struct InsertBuilder
    {
        private readonly ExprTable _table;

        private readonly IReadOnlyList<ExprColumnName> _columns;

        public InsertBuilder(ExprTable table, IReadOnlyList<ExprColumnName> columns)
        {
            this._table = table;
            this._columns = columns;
        }

        public ExprInsert From(IExprQueryFinal query) => this.From(query.Done());

        public ExprInsert From(IExprQuery query)
        {
            return new ExprInsert(this._table.FullName, this._columns, new ExprInsertQuery(query));
        }
    }
}