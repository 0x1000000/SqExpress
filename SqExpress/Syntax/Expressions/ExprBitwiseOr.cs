using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Expressions
{
    public class ExprBitwiseOr : ExprBitwise
    {
        public ExprBitwiseOr(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprValue Left { get; }

        public ExprValue Right { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprBitwiseOr(this, arg);
    }
}