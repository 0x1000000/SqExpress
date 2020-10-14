namespace SqExpress.Syntax.Value
{
    public class ExprNull : ExprValue
    {
        public static ExprNull Instance=> new ExprNull();

        private ExprNull() { }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprNull(this, arg);
    }
}