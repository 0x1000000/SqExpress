using System;
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

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprSelect(this, arg);

        public IReadOnlyList<string?> GetOutputColumnNames() => this.SelectQuery.GetOutputColumnNames();
    }


    public class ExprUnsafeQuery : IExprSubQuery, IExprQuery
    {
        public ExprUnsafeQuery(string rawQuery)
        {
            this.RawQuery = rawQuery;
        }

        public string RawQuery { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            return visitor.VisitExprUnsafeQuery(this, arg);
        }

        public IReadOnlyList<string?> GetOutputColumnNames()
        {
            return Array.Empty<string?>();
        }
    }
}