using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Update
{
    public class ExprColumnSetClause : IExpr
    {
        public ExprColumnSetClause(ExprColumn column, IExprAssigning value)
        {
            this.Column = column;
            this.Value = value;
        }

        public ExprColumn Column { get; }

        public IExprAssigning Value { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprColumnSetClause(this, arg);
    }
}