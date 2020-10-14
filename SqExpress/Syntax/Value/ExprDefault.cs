namespace SqExpress.Syntax.Value
{
    public class ExprDefault : IExprAssigning
    {
        public static ExprDefault Instance => new ExprDefault();

        private ExprDefault() { }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDefault(this, arg);
    }
}