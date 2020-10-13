namespace SqExpress.QueryBuilders.Select
{
    public interface IQueryExpressionBuilder
    {
        IQueryExpressionBuilderFinal UnionAll(IQuerySpecificationBuilderFinal expressionBuilder);

        IQueryExpressionBuilderFinal Union(IQuerySpecificationBuilderFinal expressionBuilder);

        IQueryExpressionBuilderFinal Except(IQuerySpecificationBuilderFinal expressionBuilder);

        IQueryExpressionBuilderFinal Intersect(IQuerySpecificationBuilderFinal expressionBuilder);

        IQueryExpressionBuilderFinal UnionAll(IQueryExpressionBuilderFinal expressionBuilder);

        IQueryExpressionBuilderFinal Union(IQueryExpressionBuilderFinal expressionBuilder);

        IQueryExpressionBuilderFinal Except(IQueryExpressionBuilderFinal expressionBuilder);

        IQueryExpressionBuilderFinal Intersect(IQueryExpressionBuilderFinal expressionBuilder);
    }
}