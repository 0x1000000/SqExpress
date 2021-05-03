using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Type
{
    public class ExprCast : ExprValue
    {
        public ExprCast(IExprSelecting expression, ExprType sqlType)
        {
            this.Expression = expression;
            this.SqlType = sqlType;
        }

        public IExprSelecting Expression { get; }

        public ExprType SqlType { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprCast(this, arg);
    }
}