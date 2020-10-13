using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;

namespace SqExpress.QueryBuilders.Select
{
    internal interface IQuerySpecificationBuilder : IQuerySpecificationBuilderInitial, IQuerySpecificationBuilderJoin, IQuerySpecificationBuilderFiltered, IQuerySpecificationBuilderFinal, IQueryExpressionBuilder
    { }

    public interface IQuerySpecificationBuilderInitial : IQuerySpecificationBuilderFinal
    {
        IQuerySpecificationBuilderJoin From(IExprTableSource tableSource);
    }

    public interface IQuerySpecificationBuilderJoin : IQuerySpecificationBuilderFinal, IQuerySpecificationBuilderFiltered
    {
        IQuerySpecificationBuilderJoin InnerJoin(IExprTableSource join, ExprBoolean on);

        IQuerySpecificationBuilderJoin LeftJoin(IExprTableSource join, ExprBoolean on);

        IQuerySpecificationBuilderJoin FullJoin(IExprTableSource join, ExprBoolean on);

        IQuerySpecificationBuilderJoin CrossJoin(IExprTableSource join);

        IQuerySpecificationBuilderFiltered Where(ExprBoolean where);
    }

    public interface IQuerySpecificationBuilderFiltered : IQuerySpecificationBuilderFinal
    {
        public IQuerySpecificationBuilderFinal GroupBy(ExprColumn column, params ExprColumn[] otherColumns);

        public IQuerySpecificationBuilderFinal GroupBy(ExprColumn column1, ExprColumn column2, params ExprColumn[] otherColumns);
    }

    public interface IQuerySpecificationBuilderFinal : IQueryExpressionBuilder, IExprSubQueryFinal
    {
        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        new ExprQuerySpecification Done();
    }


    public interface IQueryExpressionBuilderFinal: IQueryExpressionBuilder, IExprSubQueryFinal
    {
        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        new ExprQueryExpression Done();
    }

    public interface ISelectBuilder : ISelectBuilderFinal
    {
        public ISelectOffsetFetchBuilderFinal OffsetFetch(int offset, int fetch);

        public ISelectOffsetFetchBuilderFinal Offset(int offset);
    }

    public interface ISelectBuilderFinal : IExprQueryFinal
    {
        new ExprSelect Done();
    }

    public interface ISelectOffsetFetchBuilderFinal : IExprSubQueryFinal
    {
        new ExprSelectOffsetFetch Done();
    }
}