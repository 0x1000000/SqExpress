using System.Collections.Generic;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Select.Internal
{
    internal abstract class QueryExpressionBuilderBase : IQueryExpressionBuilder, ISelectBuilder, ISelectOffsetFetchBuilderFinal
    {
        private OrderByStorage _orderBy;

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

        protected ISelectBuilder OrderByInternal(ExprOrderBy orderBy)
        {
            if (this._orderBy.HasValue)
            {
                throw new SqExpressException("Order has already been set");
            }

            this._orderBy = new OrderByStorage(null, orderBy);
            return this;
        }

        protected ISelectBuilder OrderByInternal(ExprOrderByItem item, params ExprOrderByItem[] rest)
        {
            if (this._orderBy.HasValue)
            {
                throw new SqExpressException("Order has already been set");
            }

            this._orderBy = new OrderByStorage(Helpers.Combine(item, rest), null);
            return this;
        }

        protected ISelectBuilder OrderByInternal(IReadOnlyList<ExprOrderByItem> orderItems)
        {
            if (this._orderBy.HasValue)
            {
                throw new SqExpressException("Order has already been set");
            }

            this._orderBy = new OrderByStorage(orderItems.AssertNotEmpty("Order item list cannot be empty"), null);
            return this;
        }

        public ExprSelect Done()
        {
            this._exprOffsetFetch.AssertFatalNull(nameof(this._exprOffsetFetch));
            return new ExprSelect(this.BuildQueryExpression(), this._orderBy.GetOrderBy());
        }

        ExprSelectOffsetFetch ISelectOffsetFetchBuilderFinal.Done()
        {
            return new ExprSelectOffsetFetch(this.BuildQueryExpression(),
                new ExprOrderByOffsetFetch(this._orderBy.GetOrderItems(), this._exprOffsetFetch.AssertFatalNotNull(nameof(this._exprOffsetFetch))));
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
            if (this._exprOffsetFetch != null && this._orderBy.HasValue)
            {
                return new ExprSelectOffsetFetch(this.BuildQueryExpression(), new ExprOrderByOffsetFetch(this._orderBy.GetOrderItems(), this._exprOffsetFetch));
            }

            if (this._orderBy.HasValue)
            {
                throw new SqExpressException("The query cannot be ordered");
            }

            return this.BuildQueryExpression();
        }

        private IExprQuery BuildQuery()
        {
            if (this._exprOffsetFetch != null && this._orderBy.HasValue)
            {
                return new ExprSelectOffsetFetch(this.BuildQueryExpression(), new ExprOrderByOffsetFetch(this._orderBy.GetOrderItems(), this._exprOffsetFetch));
            }
            if (this._orderBy.HasValue)
            {
                return new ExprSelect(this.BuildQueryExpression(), this._orderBy.GetOrderBy());
            }

            return this.BuildQueryExpression();
        }

        private readonly struct OrderByStorage
        {
            public OrderByStorage(IReadOnlyList<ExprOrderByItem>? orderItems, ExprOrderBy? orderBy)
            {
                this._orderItems = orderItems;
                this._orderBy = orderBy;
            }

            private readonly IReadOnlyList<ExprOrderByItem>? _orderItems;

            private readonly ExprOrderBy? _orderBy;

            public bool HasValue => this._orderItems != null || this._orderBy != null;

            public ExprOrderBy GetOrderBy()
                => this._orderBy ?? new ExprOrderBy(this.GetOrderItems());

            public IReadOnlyList<ExprOrderByItem> GetOrderItems()
                =>  this._orderBy?.OrderList ?? this._orderItems.AssertFatalNotNull(nameof(this._orderItems));
        }
    }
}