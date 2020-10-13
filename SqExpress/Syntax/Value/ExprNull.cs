namespace SqExpress.Syntax.Value
{
    public class ExprNull : ExprValue
    {
        public static ExprNull Instance=> new ExprNull();

        private ExprNull() { }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprNull(this);
    }
}