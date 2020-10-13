using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter.Internal
{
    internal readonly struct ColumnValueInsertSelectMap
    {
        public readonly ExprColumnName Column;
        public readonly ExprValue Value;

        public ColumnValueInsertSelectMap(ExprColumnName column, ExprValue value)
        {
            this.Column = column;
            this.Value = value;
        }
    }
}