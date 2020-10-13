namespace SqExpress.Syntax.Boolean
{
    public class ExprBooleanNot : ExprBoolean
    {
        public ExprBooleanNot(ExprBoolean expr)
        {
            this.Expr = expr;
        }

        public ExprBoolean Expr { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprBooleanNot(this);
    }
}