namespace SqExpress.Syntax.Boolean.Predicate
{
    public class ExprExists : ExprPredicate
    {
        public ExprExists(IExprSubQuery subQuery)
        {
            this.SubQuery = subQuery;
        }

        public IExprSubQuery SubQuery { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprExists(this, arg);
    }
}