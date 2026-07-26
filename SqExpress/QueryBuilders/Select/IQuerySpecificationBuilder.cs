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
        /// <param name="tableSource">The table, CTE, derived table, function, or joined source selected from.</param>
        /// <returns>The stage that accepts joins, filtering, grouping, ordering, or completion.</returns>
        IQuerySpecificationBuilderJoin From(IExprTableSource tableSource);
    }

    /// <summary>Builds joins and an optional filter for a query specification.</summary>
    public interface IQuerySpecificationBuilderJoin : IQuerySpecificationBuilderFinal, IQuerySpecificationBuilderFiltered
    {
        /// <summary>Adds an inner join, retaining only row combinations for which the predicate is true.</summary>
        /// <param name="join">The right-side table source.</param><param name="on">The join predicate.</param>
        /// <returns>The join-capable query stage.</returns>
        IQuerySpecificationBuilderJoin InnerJoin(IExprTableSource join, ExprBoolean on);

        /// <summary>Adds a left outer join, preserving unmatched rows from the existing source.</summary>
        /// <param name="join">The right-side table source.</param><param name="on">The join predicate.</param>
        /// <returns>The join-capable query stage.</returns>
        IQuerySpecificationBuilderJoin LeftJoin(IExprTableSource join, ExprBoolean on);

        /// <summary>Adds a full outer join, preserving unmatched rows from both source sides.</summary>
        /// <param name="join">The right-side table source.</param><param name="on">The join predicate.</param>
        /// <returns>The join-capable query stage.</returns>
        IQuerySpecificationBuilderJoin FullJoin(IExprTableSource join, ExprBoolean on);

        /// <summary>Adds a Cartesian product with the supplied table source.</summary>
        /// <param name="join">The source combined with every row from the existing source.</param>
        /// <returns>The join-capable query stage.</returns>
        IQuerySpecificationBuilderJoin CrossJoin(IExprTableSource join);

        /// <summary>Adds a required lateral source that may reference columns from preceding sources.</summary>
        /// <param name="join">The correlated source evaluated for each preceding row.</param>
        /// <returns>The join-capable query stage.</returns>
        IQuerySpecificationBuilderJoin CrossApply(IExprTableSource join);

        /// <summary>Adds an optional lateral source, preserving preceding rows when the correlated source is empty.</summary>
        /// <param name="join">The correlated source evaluated for each preceding row.</param>
        /// <returns>The join-capable query stage.</returns>
        IQuerySpecificationBuilderJoin OuterApply(IExprTableSource join);

        /// <summary>Adds the row filter and advances to grouping or completion.</summary>
        /// <remarks>A null predicate is treated as no filter.</remarks>
        /// <param name="where">The SQL predicate evaluated before grouping.</param>
        /// <returns>The stage that accepts grouping or completion.</returns>
        IQuerySpecificationBuilderFiltered Where(ExprBoolean? where);
    }

    /// <summary>Adds optional grouping after the source or row filter has been specified.</summary>
    public interface IQuerySpecificationBuilderFiltered : IQuerySpecificationBuilderFinal
    {
        /// <summary>Groups rows by one or more SQL value expressions before projection aggregates are evaluated.</summary>
        /// <param name="value">The first required grouping key.</param>
        /// <param name="otherValues">Additional grouping keys.</param>
        /// <returns>The final query-specification stage.</returns>
        public IQuerySpecificationBuilderFinal GroupBy(ExprValue value, params ExprValue[] otherValues);

        /// <summary>Groups rows by two or more SQL value expressions before projection aggregates are evaluated.</summary>
        /// <param name="value1">The first grouping key.</param>
        /// <param name="value2">The second grouping key.</param>
        /// <param name="otherValues">Additional grouping keys.</param>
        /// <returns>The final query-specification stage.</returns>
        public IQuerySpecificationBuilderFinal GroupBy(ExprValue value1, ExprValue value2, params ExprValue[] otherValues);

        /// <summary>Groups rows using a prebuilt list of SQL value expressions.</summary>
        /// <param name="values">The grouping keys in emitted clause order.</param>
        /// <returns>The final query-specification stage.</returns>
        public IQuerySpecificationBuilderFinal GroupBy(IReadOnlyList<ExprValue> values);
    }

    /// <summary>Completes, orders, paginates, or combines a query specification.</summary>
    public interface IQuerySpecificationBuilderFinal : IQueryExpressionBuilder, IExprSubQueryFinal
    {
        /// <summary>Adds a row offset without an explicit ordering; returned rows may therefore be nondeterministic.</summary>
        /// <param name="offset">The number of rows to skip.</param>
        /// <returns>The paginated final stage.</returns>
        ISelectOffsetFetchBuilderFinal Offset(int offset);

        /// <summary>Adds a prebuilt ordered list of sort keys.</summary>
        /// <param name="orderBy">The ordering applied before any later offset/fetch.</param>
        /// <returns>The ordered select stage.</returns>
        ISelectBuilder OrderBy(ExprOrderBy orderBy);

        /// <summary>Adds one or more sort keys in descending precedence order.</summary>
        /// <param name="item">The primary sort key.</param><param name="rest">Additional tie-breakers.</param>
        /// <returns>The ordered select stage.</returns>
        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        /// <summary>Adds a prebuilt list of sort keys in precedence order.</summary>
        /// <param name="orderItems">The sort keys to emit.</param>
        /// <returns>The ordered select stage.</returns>
        ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems);

        /// <summary>Materializes the current clauses as a query specification without executing it.</summary>
        /// <returns>The completed query-specification syntax tree.</returns>
        new ExprQuerySpecification Done();
    }


    /// <summary>Completes, orders, paginates, or further combines a set-operation query expression.</summary>
    public interface IQueryExpressionBuilderFinal: IQueryExpressionBuilder, IExprSubQueryFinal
    {
        /// <summary>Adds a row offset to the complete set-operation result without explicit ordering.</summary>
        /// <param name="offset">The number of result rows to skip.</param>
        /// <returns>The paginated final stage.</returns>
        ISelectOffsetFetchBuilderFinal Offset(int offset);

        /// <summary>Orders the complete set-operation result using a prebuilt ordering.</summary>
        /// <param name="orderBy">The ordering applied after set operations.</param>
        /// <returns>The ordered select stage.</returns>
        ISelectBuilder OrderBy(ExprOrderBy orderBy);

        /// <summary>Orders the complete set-operation result by one or more sort keys.</summary>
        /// <param name="item">The primary sort key.</param><param name="rest">Additional tie-breakers.</param>
        /// <returns>The ordered select stage.</returns>
        ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest);

        /// <summary>Orders the complete set-operation result by a prebuilt sort-key list.</summary>
        /// <param name="orderItems">The sort keys in precedence order.</param>
        /// <returns>The ordered select stage.</returns>
        ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems);

        /// <summary>Materializes the complete set-operation chain without executing it.</summary>
        /// <returns>The completed query-expression syntax tree.</returns>
        new ExprQueryExpression Done();
    }

    /// <summary>Adds offset/fetch pagination after an ordering has been specified.</summary>
    public interface ISelectBuilder : ISelectBuilderFinal
    {
        /// <summary>Adds dialect-rendered offset/fetch pagination after the established ordering.</summary>
        /// <param name="offset">The number of ordered rows to skip.</param>
        /// <param name="fetch">The maximum number of following rows to return.</param>
        /// <returns>The completed pagination stage.</returns>
        public ISelectOffsetFetchBuilderFinal OffsetFetch(int offset, int fetch);

        /// <summary>Skips a fixed number of ordered rows without imposing a fetch limit.</summary>
        /// <param name="offset">The number of ordered rows to skip.</param>
        /// <returns>The completed pagination stage.</returns>
        public ISelectOffsetFetchBuilderFinal Offset(int offset);
    }

    /// <summary>Represents an ordered select ready to produce a query syntax tree.</summary>
    public interface ISelectBuilderFinal : IExprQueryFinal
    {
        /// <summary>Materializes the ordered select without executing it.</summary>
        /// <returns>The completed ordered-select syntax tree.</returns>
        new ExprSelect Done();
    }

    /// <summary>Represents an offset/fetch select ready to produce a subquery syntax tree.</summary>
    public interface ISelectOffsetFetchBuilderFinal : IExprSubQueryFinal
    {
        /// <summary>Materializes the ordered or unordered offset/fetch select without executing it.</summary>
        /// <returns>The completed paginated-select syntax tree.</returns>
        new ExprSelectOffsetFetch Done();
    }
}
