using System.Collections.Generic;
using SqExpress.Syntax.Select;

namespace SqExpress.QueryBuilders.Select.Internal
{
    internal class QueryExpressionBuilder : QueryExpressionBuilderBase, IQueryExpressionBuilderFinal
    {
        private readonly ExprQueryExpression _queryExpression;

        public QueryExpressionBuilder(ExprQueryExpression queryExpression)
        {
            this._queryExpression = queryExpression;
        }

        protected override IExprQueryExpression BuildQueryExpression()
        {
            return this._queryExpression;
        }

        public new ExprQueryExpression Done()
        {
            return this._queryExpression;
        }

        public ISelectBuilder OrderBy(ExprOrderBy orderBy)
        {
            return this.OrderByInternal(orderBy);
        }

        public ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest)
        {
            return this.OrderByInternal(item, rest);
        }

        public ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems)
        {
            return this.OrderByInternal(orderItems);
        }
    }
}