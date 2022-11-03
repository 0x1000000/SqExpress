using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Expressions
{
    public class ExprBitwiseNot : ExprBitwise
    {
        public ExprBitwiseNot(ExprValue value)
        {
            this.Value = value;
        }

        public ExprValue Value { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprBitwiseNot(this, arg);
    }
}