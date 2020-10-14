using System.Collections.Generic;

namespace SqExpress.Syntax.Select
{
    public class ExprSelectOffsetFetch : IExprSubQuery
    {
        public ExprSelectOffsetFetch(IExprSubQuery selectQuery, ExprOrderByOffsetFetch orderBy)
        {
            this.SelectQuery = selectQuery;
            this.OrderBy = orderBy;
        }

        public IExprSubQuery SelectQuery { get; }

        public ExprOrderByOffsetFetch OrderBy { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprSelectOffsetFetch(this, arg);

        public IReadOnlyList<string?> GetOutputColumnNames() => this.SelectQuery.GetOutputColumnNames();
    }
}