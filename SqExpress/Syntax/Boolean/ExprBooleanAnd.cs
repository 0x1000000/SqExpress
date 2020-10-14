namespace SqExpress.Syntax.Boolean
{
    public class ExprBooleanAnd : ExprBoolean
    {
        public ExprBooleanAnd(ExprBoolean left, ExprBoolean right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprBoolean Left { get; }

        public ExprBoolean Right { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprBooleanAnd(this, arg);
    }
}