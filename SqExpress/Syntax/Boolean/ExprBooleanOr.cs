namespace SqExpress.Syntax.Boolean
{
    public class ExprBooleanOr : ExprBoolean
    {
        public ExprBooleanOr(ExprBoolean left, ExprBoolean right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprBoolean Left { get; }

        public ExprBoolean Right { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprBooleanOr(this);
    }
}