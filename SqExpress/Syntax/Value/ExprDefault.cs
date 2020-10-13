namespace SqExpress.Syntax.Value
{
    public class ExprDefault : IExprAssigning
    {
        public static ExprDefault Instance => new ExprDefault();

        private ExprDefault() { }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDefault(this);
    }
}