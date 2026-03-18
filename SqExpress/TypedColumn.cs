using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

namespace SqExpress
{
    public abstract class TypedColumn : ExprColumn
    {
        protected TypedColumn(IExprColumnSource? source, ExprColumnName columnName, ExprType sqlType, bool isNullable)
            : base(source, columnName)
        {
            this.SqlType = sqlType;
            this.IsNullable = isNullable;
        }

        public ExprType SqlType { get; }

        public bool IsNullable { get; }
    }
}
