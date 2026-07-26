namespace SqExpress.QueryBuilders.Select
{
    /// <summary>Combines query specifications or existing query expressions with SQL set operators.</summary>
    public interface IQueryExpressionBuilder
    {
        /// <summary>Appends all rows from another query specification without duplicate elimination.</summary>
        /// <param name="expressionBuilder">The right operand; its projection must be union-compatible with the left operand.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal UnionAll(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Appends rows from another query specification and asks the database to eliminate duplicates.</summary>
        /// <param name="expressionBuilder">The union-compatible right operand.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal Union(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Returns left-side rows that do not occur in the supplied query specification.</summary>
        /// <param name="expressionBuilder">The set-compatible right operand.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal Except(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Returns rows common to the existing expression and the supplied query specification.</summary>
        /// <param name="expressionBuilder">The set-compatible right operand.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal Intersect(IQuerySpecificationBuilderFinal expressionBuilder);

        /// <summary>Appends all rows from an existing set-operation expression without duplicate elimination.</summary>
        /// <param name="expressionBuilder">The union-compatible right expression.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal UnionAll(IQueryExpressionBuilderFinal expressionBuilder);

        /// <summary>Appends an existing set-operation expression and eliminates duplicate rows.</summary>
        /// <param name="expressionBuilder">The union-compatible right expression.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal Union(IQueryExpressionBuilderFinal expressionBuilder);

        /// <summary>Returns left-side rows absent from the supplied set-operation expression.</summary>
        /// <param name="expressionBuilder">The set-compatible right expression.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal Except(IQueryExpressionBuilderFinal expressionBuilder);

        /// <summary>Returns rows common to both set-operation expressions.</summary>
        /// <param name="expressionBuilder">The set-compatible right expression.</param>
        /// <returns>The combined-query stage.</returns>
        IQueryExpressionBuilderFinal Intersect(IQueryExpressionBuilderFinal expressionBuilder);
    }
}
