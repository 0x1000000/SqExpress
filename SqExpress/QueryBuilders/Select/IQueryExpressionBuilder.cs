namespace SqExpress.QueryBuilders.Select
{
    /// <summary>Combines query specifications or existing query expressions with SQL set operators.</summary>
    public interface IQueryExpressionBuilder
    {
        /// <summary>Appends a query specification with <c>UNION ALL</c>.</summary>
        IQueryExpressionBuilderFinal UnionAll(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Appends a query specification with duplicate-eliminating <c>UNION</c>.</summary>
        IQueryExpressionBuilderFinal Union(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Subtracts a query specification with <c>EXCEPT</c>.</summary>
        IQueryExpressionBuilderFinal Except(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Intersects with a query specification.</summary>
        IQueryExpressionBuilderFinal Intersect(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Appends a combined query expression with <c>UNION ALL</c>.</summary>
        IQueryExpressionBuilderFinal UnionAll(IQueryExpressionBuilderFinal expressionBuilder);

        /// <summary>Appends a combined query expression with duplicate-eliminating <c>UNION</c>.</summary>
        IQueryExpressionBuilderFinal Union(IQueryExpressionBuilderFinal expressionBuilder);

        /// <summary>Subtracts a combined query expression with <c>EXCEPT</c>.</summary>
        IQueryExpressionBuilderFinal Except(IQueryExpressionBuilderFinal expressionBuilder);

        /// <summary>Intersects with a combined query expression.</summary>
        IQueryExpressionBuilderFinal Intersect(IQueryExpressionBuilderFinal expressionBuilder);
    }
}
