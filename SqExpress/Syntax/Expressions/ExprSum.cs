using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Expressions
{
    public class ExprSum : ExprArithmetic
    {
        public ExprSum(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprValue Left { get; }

        public ExprValue Right { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprSum(this, arg);
    }
}