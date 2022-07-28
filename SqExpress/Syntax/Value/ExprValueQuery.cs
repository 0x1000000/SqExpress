namespace SqExpress.Syntax.Value
{
    public class ExprValueQuery : ExprValue
    {
        public IExprSubQuery Query { get; }

        public ExprValueQuery(IExprSubQuery query)
        {
            this.Query = query;
        }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprValueQuery(this, arg);
    }
}