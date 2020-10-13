using System.Collections.Generic;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Select.Internal
{
    internal abstract class QueryExpressionBuilderBase : IQueryExpressionBuilder, ISelectBuilder, ISelectOffsetFetchBuilderFinal
    {
        private IReadOnlyList<ExprOrderByItem>? _orderItems;

        private ExprOffsetFetch? _exprOffsetFetch;

        public IQueryExpressionBuilderFinal UnionAll(IQuerySpecificationBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.UnionAll);
            return new QueryExpressionBuilder(expr);
        }

        public IQueryExpressionBuilderFinal Union(IQuerySpecificationBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.Union);
            return new QueryExpressionBuilder(expr);
        }

        public IQueryExpressionBuilderFinal Except(IQuerySpecificationBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.Except);
            return new QueryExpressionBuilder(expr);
        }

        public IQueryExpressionBuilderFinal Intersect(IQuerySpecificationBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.Intersect);
            return new QueryExpressionBuilder(expr);
        }

        public IQueryExpressionBuilderFinal UnionAll(IQueryExpressionBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.UnionAll);
            return new QueryExpressionBuilder(expr);
        }

        public IQueryExpressionBuilderFinal Union(IQueryExpressionBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.Union);
            return new QueryExpressionBuilder(expr);
        }

        public IQueryExpressionBuilderFinal Except(IQueryExpressionBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.Except);
            return new QueryExpressionBuilder(expr);
        }

        public IQueryExpressionBuilderFinal Intersect(IQueryExpressionBuilderFinal expressionBuilder)
        {
            var expr = new ExprQueryExpression(this.BuildQueryExpression(), expressionBuilder.Done(), ExprQueryExpressionType.Intersect);
            return new QueryExpressionBuilder(expr);
        }

        protected ISelectBuilder OrderByInternal(ExprOrderByItem item, params ExprOrderByItem[] rest)
        {
            this._orderItems.AssertFatalNull(nameof(this._orderItems));
            this._orderItems = Helpers.Combine(item, rest);
            return this;
        }

        public ExprSelect Done()
        {
            this._exprOffsetFetch.AssertFatalNull(nameof(this._exprOffsetFetch));
            return new ExprSelect(this.BuildQueryExpression(), new ExprOrderBy(this._orderItems.AssertFatalNotNull(nameof(this._orderItems))));
        }

        ExprSelectOffsetFetch ISelectOffsetFetchBuilderFinal.Done()
        {
            return new ExprSelectOffsetFetch(this.BuildQueryExpression(),
                new ExprOrderByOffsetFetch(this._orderItems.AssertFatalNotNull(nameof(this._orderItems)), this._exprOffsetFetch.AssertFatalNotNull(nameof(this._exprOffsetFetch))));
        }

        public ISelectOffsetFetchBuilderFinal OffsetFetch(int offset, int fetch)
        {
            this._exprOffsetFetch.AssertFatalNull(nameof(this._exprOffsetFetch));
            this._exprOffsetFetch = new ExprOffsetFetch(new ExprInt32Literal(offset), new ExprInt32Literal(fetch));
            return this;
        }

        public ISelectOffsetFetchBuilderFinal Offset(int offset)
        {
            this._exprOffsetFetch.AssertFatalNull(nameof(this._exprOffsetFetch));
            this._exprOffsetFetch = new ExprOffsetFetch(new ExprInt32Literal(offset), null);
            return this;
        }

        IExprQuery IExprQueryFinal.Done() => this.BuildQuery();

        IExprSubQuery IExprSubQueryFinal.Done() => this.BuildSubQuery();

        protected abstract IExprQueryExpression BuildQueryExpression();

        private IExprSubQuery BuildSubQuery()
        {
            if (this._exprOffsetFetch != null && this._orderItems != null)
            {
                return new ExprSelectOffsetFetch(this.BuildQueryExpression(), new ExprOrderByOffsetFetch(this._orderItems, this._exprOffsetFetch));
            }

            this._orderItems.AssertFatalNull("The query cannot be ordered");

            return this.BuildQueryExpression();
        }

        private IExprQuery BuildQuery()
        {
            if (this._exprOffsetFetch != null && this._orderItems != null)
            {
                return new ExprSelectOffsetFetch(this.BuildQueryExpression(), new ExprOrderByOffsetFetch(this._orderItems, this._exprOffsetFetch));
            }
            if (this._orderItems != null)
            {
                return new ExprSelect(this.BuildQueryExpression(), new ExprOrderBy(this._orderItems));
            }

            return this.BuildQueryExpression();
        }
    }
}