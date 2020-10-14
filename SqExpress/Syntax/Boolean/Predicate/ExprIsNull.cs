using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Boolean.Predicate
{
    public class ExprIsNull : ExprPredicate
    {
        public ExprIsNull(ExprValue test, bool @not)
        {
            this.Test = test;
            this.Not = not;
        }

        public ExprValue Test { get; }

        public bool Not { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprIsNull(this, arg);
    }
}