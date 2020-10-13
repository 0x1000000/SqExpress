using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter.Internal
{
    internal readonly struct ColumnValueUpdateMap
    {
        public readonly ExprColumnName Column;
        public readonly IExprAssigning Value;

        public ColumnValueUpdateMap(ExprColumnName column, IExprAssigning value)
        {
            this.Column = column;
            this.Value = value;
        }
    }
}