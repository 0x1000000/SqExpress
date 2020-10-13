using System.Collections.Generic;

namespace SqExpress.Syntax.Select
{
    public class ExprSelect : IExprQuery
    {
        public ExprSelect(IExprSubQuery selectQuery, ExprOrderBy orderBy)
        {
            this.SelectQuery = selectQuery;
            this.OrderBy = orderBy;
        }

        public IExprSubQuery SelectQuery { get; }

        public ExprOrderBy OrderBy { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprSelect(this);

        public IReadOnlyList<string?> GetOutputColumnNames() => this.SelectQuery.GetOutputColumnNames();
    }
}