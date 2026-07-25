using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.Select
{
    internal interface IQuerySpecificationBuilder : IQuerySpecificationBuilderInitial, IQuerySpecificationBuilderJoin, IQuerySpecificationBuilderFiltered, IQuerySpecificationBuilderFinal, IQueryExpressionBuilder
    { }

    /// <summary>Represents a new <c>SELECT</c> that may add a <c>FROM</c> source or be completed immediately.</summary>
    public interface IQuerySpecificationBuilderInitial : IQuerySpecificationBuilderFinal
    {
        /// <summary>Adds the initial table source and enables joins and filtering.</summary>
        IQuerySpecificationBuilderJoin From(IExprTableSource tableSource);
    }

    /// <summary>Builds joins and an optional filter for a query specification.</summary>
    public interface IQuerySpecificationBuilderJoin : IQuerySpecificationBuilderFinal, IQuerySpecificationBuilderFiltered
    {
        /// <summary>Adds an inner join with its matching predicate.</summary>
        IQuerySpecificationBuilderJoin InnerJoin(IExprTableSource join, ExprBoolean on);

        /// <summary>Adds a left outer join with its matching predicate.</summary>
        IQuerySpecificationBuilderJoin LeftJoin(IExprTableSource join, ExprBoolean on);

        /// <summary>Adds a full outer join with its matching predicate.</summary>
        IQuerySpecificationBuilderJoin FullJoin(IExprTableSource join, ExprBoolean on);

        /// <summary>Adds a cross join.</summary>
        IQuerySpecificationBuilderJoin CrossJoin(IExprTableSource join);

        /// <summary>Adds a lateral <c>CROSS APPLY</c> source.</summary>
        IQuerySpecificationBuilderJoin CrossApply(IExprTableSource join);

        /// <summary>Adds a lateral <c>OUTER APPLY</c> source.</summary>
        IQuerySpecificationBuilderJoin OuterApply(IExprTableSource join);

        /// <summary>Adds the row filter and advances to grouping or completion.</summary>
        /// <remarks>A null predicate is treated as no filter.</remarks>
        IQuerySpecificationBuilderFiltered Where(ExprBoolean? where);
    }

    /// <summary>Adds optional grouping after the source or row filter has been specified.</summary>
    public interface IQuerySpecificationBuilderFiltered : IQuerySpecificationBuilderFinal
    {
        /// <summary>Groups by one or more value expressions.</summary>
        public IQuerySpecificationBuilderFinal GroupBy(ExprValue value, params ExprValue[] otherValues);

        /// <summary>Groups by at least two value expressions.</summary>
        public IQuerySpecificationBuilderFinal GroupBy(ExprValue value1, ExprValue value2, params ExprValue[] otherValues);

        /// <summary>Groups by the supplied expression list.</summary>
        public IQuerySpecificationBuilderFinal GroupBy(IReadOnlyList<ExprValue> values);
    }

    /// <summary>Completes, orders, paginates, or combines a query specification.</summary>
    public interface IQuerySpecificationBuilderFinal : IQueryExpressionBuilder, IExprSubQueryFinal
    {
        /// <summary>Adds an offset without an explicit ordering.</summary>
        ISelectOffsetFetchBuilderFinal Offset(int offset);

        /// <summary>Adds an existing ordering.</summary>
        ISelectBuilder OrderBy(ExprOrderBy orderBy);

        /// <summary>Adds one or more order-by items.</summary>
        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        /// <summary>Adds a list of order-by items.</summary>
        ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems);

        /// <summary>Completes the query specification syntax tree.</summary>
        new ExprQuerySpecification Done();
    }


    /// <summary>Completes, orders, paginates, or further combines a set-operation query expression.</summary>
    public interface IQueryExpressionBuilderFinal: IQueryExpressionBuilder, IExprSubQueryFinal
    {
        /// <summary>Adds an offset to the combined query expression.</summary>
        ISelectOffsetFetchBuilderFinal Offset(int offset);

        /// <summary>Adds an existing ordering to the combined query expression.</summary>
        ISelectBuilder OrderBy(ExprOrderBy orderBy);

        /// <summary>Adds one or more order-by items to the combined query expression.</summary>
        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        /// <summary>Adds a list of order-by items to the combined query expression.</summary>
        ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems);

        /// <summary>Completes the set-operation query expression.</summary>
        new ExprQueryExpression Done();
    }

    /// <summary>Adds offset/fetch pagination after an ordering has been specified.</summary>
    public interface ISelectBuilder : ISelectBuilderFinal
    {
        /// <summary>Adds both the number of rows to skip and the maximum number to fetch.</summary>
        public ISelectOffsetFetchBuilderFinal OffsetFetch(int offset, int fetch);

        /// <summary>Adds the number of rows to skip without a fetch limit.</summary>
        public ISelectOffsetFetchBuilderFinal Offset(int offset);
    }

    /// <summary>Represents an ordered select ready to produce a query syntax tree.</summary>
    public interface ISelectBuilderFinal : IExprQueryFinal
    {
        /// <summary>Completes the ordered select.</summary>
        new ExprSelect Done();
    }

    /// <summary>Represents an offset/fetch select ready to produce a subquery syntax tree.</summary>
    public interface ISelectOffsetFetchBuilderFinal : IExprSubQueryFinal
    {
        /// <summary>Completes the paginated select.</summary>
        new ExprSelectOffsetFetch Done();
    }
}
