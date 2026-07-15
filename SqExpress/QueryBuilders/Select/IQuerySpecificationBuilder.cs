using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

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

        IQuerySpecificationBuilderJoin CrossApply(IExprTableSource join);

        IQuerySpecificationBuilderJoin OuterApply(IExprTableSource join);

        IQuerySpecificationBuilderFiltered Where(ExprBoolean? where);
    }

    public interface IQuerySpecificationBuilderFiltered : IQuerySpecificationBuilderFinal
    {
        public IQuerySpecificationBuilderFinal GroupBy(ExprValue value, params ExprValue[] otherValues);

        public IQuerySpecificationBuilderFinal GroupBy(ExprValue value1, ExprValue value2, params ExprValue[] otherValues);

        public IQuerySpecificationBuilderFinal GroupBy(IReadOnlyList<ExprValue> values);
    }

    public interface IQuerySpecificationBuilderFinal : IQueryExpressionBuilder, IExprSubQueryFinal
    {
        ISelectOffsetFetchBuilderFinal Offset(int offset);

        ISelectBuilder OrderBy(ExprOrderBy orderBy);

        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems);

        new ExprQuerySpecification Done();
    }


    public interface IQueryExpressionBuilderFinal: IQueryExpressionBuilder, IExprSubQueryFinal
    {
        ISelectOffsetFetchBuilderFinal Offset(int offset);

        ISelectBuilder OrderBy(ExprOrderBy orderBy);

        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems);

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
