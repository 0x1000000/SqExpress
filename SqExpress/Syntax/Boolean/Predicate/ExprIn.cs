using System.Collections.Generic;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Boolean.Predicate
{
    public abstract class ExprIn : ExprPredicate
    {
        protected ExprIn(ExprValue testExpression)
        {
            this.TestExpression = testExpression;
        }

        public ExprValue TestExpression { get; }
    }

    public class ExprInSubQuery : ExprIn
    {
        public ExprInSubQuery(ExprValue testExpression, IExprSubQuery subQuery) : base(testExpression)
        {
            this.SubQuery = subQuery;
        }

        public IExprSubQuery SubQuery { get; }


        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprInSubQuery(this);
    }

    public class ExprInValues : ExprIn
    {
        public ExprInValues(ExprValue testExpression, IReadOnlyList<ExprValue> items) : base(testExpression)
        {
            this.Items = items;
        }

        public IReadOnlyList<ExprValue> Items { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprInValues(this);
    }
}