using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.Select;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;
using static SqExpress.SqQueryBuilder;

namespace SqExpress
{
    /// <summary>Provides fluent composition helpers for SqExpress query and statement syntax nodes.</summary>
    public static class SqQueryBuilderExtensions
    {
        /// <summary>Turns a column name into a reference qualified by a table, alias, or other column source.</summary>
        /// <param name="columnName">The database column name.</param>
        /// <param name="newColumnSource">The qualifier, or <see langword="null"/> to create an unqualified reference.</param>
        /// <returns>A new column expression; the name and source objects are not modified.</returns>
        public static ExprColumn WithSource(this ExprColumnName columnName, IExprColumnSource? newColumnSource)
            => new ExprColumn(newColumnSource, columnName);

        /// <summary>Completes a fluent subquery and gives it the alias required for use in <c>FROM</c> or a join.</summary>
        /// <param name="expressionBuilder">The final subquery builder stage.</param>
        /// <param name="tableAlias">The alias used to qualify the derived table's projected columns.</param>
        /// <returns>An aliased derived-table query.</returns>
        public static ExprDerivedTableQuery As(this IExprSubQueryFinal expressionBuilder, ExprTableAlias tableAlias) 
            => new ExprDerivedTableQuery(expressionBuilder.Done(), tableAlias, null);

        /// <summary>Completes a fluent subquery, aliases it, and declares names for its projected columns.</summary>
        /// <param name="expressionBuilder">The final subquery builder stage.</param>
        /// <param name="tableAlias">The alias used to qualify the derived table.</param>
        /// <param name="columns">Output column names in the same order as the subquery projection.</param>
        /// <returns>An aliased, column-addressable derived-table query.</returns>
        public static ExprDerivedTableQuery As(this IExprSubQueryFinal expressionBuilder, ExprTableAlias tableAlias, params ExprColumnName[] columns)
            => new ExprDerivedTableQuery(expressionBuilder.Done(), tableAlias, columns);

        /// <summary>Gives a completed subquery the alias required for use in <c>FROM</c> or a join.</summary>
        /// <param name="expressionBuilder">The completed subquery.</param>
        /// <param name="tableAlias">The alias used to qualify its projected columns.</param>
        /// <returns>An aliased derived-table query.</returns>
        public static ExprDerivedTableQuery As(this IExprSubQuery expressionBuilder, ExprTableAlias tableAlias) 
            => new ExprDerivedTableQuery(expressionBuilder, tableAlias, null);

        /// <summary>Aliases a completed subquery and declares names by which its projected columns can be referenced.</summary>
        /// <param name="expressionBuilder">The completed subquery.</param>
        /// <param name="tableAlias">The alias used to qualify the derived table.</param>
        /// <param name="columns">Output column names in projection order.</param>
        /// <returns>An aliased, column-addressable derived-table query.</returns>
        public static ExprDerivedTableQuery As(this IExprSubQuery expressionBuilder, ExprTableAlias tableAlias, params ExprColumnName[] columns)
            => new ExprDerivedTableQuery(expressionBuilder, tableAlias, columns);

        /// <summary>Assigns the output name emitted for a projected column without changing its source qualification.</summary>
        /// <param name="column">The column being projected.</param>
        /// <param name="alias">The output column alias.</param>
        /// <returns>An aliased projection item.</returns>
        public static ExprAliasedColumn As(this ExprColumn column, ExprColumnAlias alias) =>
            new ExprAliasedColumn(column, alias);

        /// <summary>Assigns a stable output name to a projected expression, aggregate, or analytic result.</summary>
        /// <param name="value">The selecting expression to name.</param>
        /// <param name="alias">The output column alias.</param>
        /// <returns>An aliased projection item.</returns>
        public static ExprAliasedSelecting As(this IExprSelecting value, ExprColumnAlias alias) =>
            new ExprAliasedSelecting(value, alias);

        /// <summary>Adapts a selectable-only AST node so it can participate in a larger value expression.</summary>
        /// <param name="selecting">The aggregate, analytic function, or other selectable expression to wrap.</param>
        /// <returns>A value-expression wrapper around the supplied node.</returns>
        public static ExprSelectingValue AsValue(this IExprSelecting selecting)
            => new ExprSelectingValue(selecting);

        /// <summary>Makes a table value constructor usable as a named table source with addressable columns.</summary>
        /// <param name="valueConstructor">The rows of values.</param>
        /// <param name="alias">A non-empty table alias.</param>
        /// <param name="columns">Column names ordered to match each value row.</param>
        /// <returns>An aliased derived value table.</returns>
        public static ExprDerivedTableValues As(this ExprTableValueConstructor valueConstructor, Alias alias, params ExprColumnName[] columns)
            => new ExprDerivedTableValues(valueConstructor, new ExprTableAlias(alias.BuildAliasExpression() ?? throw new SqExpressException("Derived Table Values has to have not empty alias")), columns);

        /// <summary>Assigns the qualifier required to reference columns returned by a table-valued function.</summary>
        /// <param name="tableFunction">The table-valued function call.</param>
        /// <param name="alias">The table alias used by downstream column references.</param>
        /// <returns>An aliased table-function source.</returns>
        public static ExprAliasedTableFunction As(this ExprTableFunction tableFunction, ExprTableAlias alias)
            => new ExprAliasedTableFunction(tableFunction, alias);

        /// <summary>Names the columns of a table value constructor and assigns it an automatically generated alias.</summary>
        /// <param name="valueConstructor">The rows of values.</param>
        /// <param name="columns">Column names ordered to match each value row.</param>
        /// <returns>A column-addressable derived value table.</returns>
        public static ExprDerivedTableValues AsColumns(this ExprTableValueConstructor valueConstructor, params ExprColumnName[] columns)
            => new ExprDerivedTableValues(valueConstructor, new ExprTableAlias(Alias.Auto.BuildAliasExpression() ?? throw new SqExpressException("Derived Table Values has to have not empty alias")), columns);

        /// <summary>Creates a dynamic column reference qualified by this source.</summary>
        /// <param name="columnSource">The table, alias, or derived source used as qualifier.</param>
        /// <param name="columnName">The database/output column name.</param>
        /// <returns>A qualified column expression.</returns>
        public static ExprColumn Column(this IExprColumnSource columnSource, ExprColumnName columnName) 
            => new ExprColumn(columnSource, columnName);

        /// <summary>References a projected column through this derived table's alias.</summary>
        /// <param name="derivedTable">The derived table that exposes the column.</param>
        /// <param name="columnName">The projected output column name.</param>
        /// <returns>An alias-qualified column expression.</returns>
        public static ExprColumn Column(this ExprDerivedTable derivedTable, ExprColumnName columnName)
            => derivedTable.Alias.Column(columnName);

        /// <summary>References a database column through the table alias when present, otherwise through its full name.</summary>
        /// <param name="table">The table that owns the column.</param>
        /// <param name="columnName">The database column name.</param>
        /// <returns>A properly qualified column expression.</returns>
        public static ExprColumn Column(this ExprTable table, ExprColumnName columnName)
            => table.Alias != null ? table.Alias.Column(columnName) : new ExprColumn(table.FullName, columnName);

        /// <summary>References a result column through an aliased table-valued function.</summary>
        /// <param name="table">The aliased table-function source.</param>
        /// <param name="columnName">The result column name.</param>
        /// <returns>An alias-qualified column expression.</returns>
        public static ExprColumn Column(this ExprAliasedTableFunction table, ExprColumnName columnName)
            => table.Alias.Column(columnName);

        /// <summary>Creates a qualified <c>source.*</c> projection, avoiding columns from other joined sources.</summary>
        /// <param name="columnSource">The source whose columns should be projected.</param>
        /// <returns>A qualified wildcard projection.</returns>
        public static ExprAllColumns AllColumns(this IExprColumnSource columnSource)
            => new ExprAllColumns(columnSource);

        /// <summary>Creates an <c>alias.*</c> projection for this derived table.</summary>
        /// <param name="derivedTable">The derived source whose projected columns should be selected.</param>
        /// <returns>A wildcard projection qualified by the derived-table alias.</returns>
        public static ExprAllColumns AllColumns(this ExprDerivedTable derivedTable)
            => derivedTable.Alias.AllColumns();

        /// <summary>Creates a qualified wildcard projection using the table alias when present and its full name otherwise.</summary>
        /// <param name="table">The table whose columns should be selected.</param>
        /// <returns>A wildcard projection restricted to that table.</returns>
        public static ExprAllColumns AllColumns(this ExprTable table)
            => table.Alias != null ? table.Alias.AllColumns() : new ExprAllColumns(table.FullName);

        /// <summary>Creates an <c>alias.*</c> projection for a table-valued function.</summary>
        /// <param name="table">The aliased table-function source.</param>
        /// <returns>A wildcard projection qualified by the function alias.</returns>
        public static ExprAllColumns AllColumns(this ExprAliasedTableFunction table)
            => table.Alias.AllColumns();

        /// <summary>Tests a column against a non-empty list of SQL value expressions.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="value">The first required candidate value.</param>
        /// <param name="rest">Additional candidate values.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, ExprValue value, params ExprValue[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest));

        /// <summary>Tests a column against a prebuilt, non-empty list of SQL value expressions.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="items">Candidate values; an empty list is rejected.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<ExprValue> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty"));

        /// <summary>Tests whether a column value occurs in the single-column result of a subquery.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="subQuery">The final builder stage whose query supplies candidate values.</param>
        /// <returns>An <c>IN (subquery)</c> Boolean expression.</returns>
        public static ExprInSubQuery In(this ExprColumn column, IExprSubQueryFinal subQuery)
            => new ExprInSubQuery(column, subQuery.Done());

        /// <summary>Tests a column against integer values converted to typed SQL literal nodes.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="value">The first required integer.</param>
        /// <param name="rest">Additional integers.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, int value, params int[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest, i=> Literal(i)));

        /// <summary>Tests a column against a non-empty integer list converted to typed SQL literal nodes.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="items">Candidate integers; an empty list is rejected.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<int> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty").SelectToReadOnlyList(i=>Literal(i)));

        /// <summary>Tests a column against string values that the exporter safely escapes or parameterizes.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="value">The first required string.</param>
        /// <param name="rest">Additional strings.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, string value, params string[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest, i=> Literal(i)));

        /// <summary>Tests a column against a non-empty string list converted to typed SQL value nodes.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="items">Candidate strings; an empty list is rejected.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<string> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty").SelectToReadOnlyList(i=>Literal(i)));

        /// <summary>Tests a column against GUID values rendered in the selected dialect's compatible form.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="value">The first required GUID.</param>
        /// <param name="rest">Additional GUIDs.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, Guid value, params Guid[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest, i=> Literal(i)));

        /// <summary>Tests a column against a non-empty GUID list converted to typed SQL value nodes.</summary>
        /// <param name="column">The expression on the left of <c>IN</c>.</param>
        /// <param name="items">Candidate GUIDs; an empty list is rejected.</param>
        /// <returns>An <c>IN (...)</c> Boolean expression.</returns>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<Guid> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty").SelectToReadOnlyList(i=>Literal(i)));

        /// <summary>Applies the database's <c>LIKE</c> pattern semantics to a non-nullable table string column.</summary>
        /// <param name="column">The column to test.</param>
        /// <param name="pattern">A SQL pattern whose wildcards and escaping follow the target dialect.</param>
        /// <returns>A pattern-matching Boolean expression.</returns>
        public static ExprLike Like(this StringTableColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Applies the database's <c>LIKE</c> pattern semantics to a nullable table string column.</summary>
        /// <param name="column">The column to test; SQL null input produces the database's unknown result.</param>
        /// <param name="pattern">A SQL pattern whose wildcards and escaping follow the target dialect.</param>
        /// <returns>A pattern-matching Boolean expression.</returns>
        public static ExprLike Like(this NullableStringTableColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Applies the database's <c>LIKE</c> pattern semantics to a non-nullable derived/custom string column.</summary>
        /// <param name="column">The column to test.</param>
        /// <param name="pattern">A SQL pattern whose wildcards and escaping follow the target dialect.</param>
        /// <returns>A pattern-matching Boolean expression.</returns>
        public static ExprLike Like(this StringCustomColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Applies the database's <c>LIKE</c> pattern semantics to a nullable derived/custom string column.</summary>
        /// <param name="column">The column to test; SQL null input produces the database's unknown result.</param>
        /// <param name="pattern">A SQL pattern whose wildcards and escaping follow the target dialect.</param>
        /// <returns>A pattern-matching Boolean expression.</returns>
        public static ExprLike Like(this NullableStringCustomColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Combines statements for sequential execution as one SqExpress statement tree.</summary>
        /// <param name="statements">The statements in execution order.</param>
        /// <returns>A combined statement; the implementation may preserve an existing list representation.</returns>
        public static IStatement Combine(this IEnumerable<IStatement> statements) =>
            StatementList.Combine(statements is IReadOnlyList<IStatement> l ? l : statements.ToList());

        /// <summary>Adds a SQL <c>OUTPUT</c> projection that returns values from rows affected by a delete.</summary>
        /// <remarks>Support and exact syntax are database-specific; use an exporter that supports delete output.</remarks>
        /// <param name="exprDelete">The completed delete statement.</param>
        /// <param name="outputColumn">The first required affected-row column to return.</param>
        /// <param name="rest">Additional aliased output columns.</param>
        /// <returns>A delete statement with an output projection.</returns>
        public static ExprDeleteOutput Output(this ExprDelete exprDelete, ExprColumn outputColumn, params ExprAliasedColumn[] rest) 
            => new ExprDeleteOutput(exprDelete, Helpers.Combine(outputColumn, rest));

        /// <summary>Builds a new projection list by appending expressions or CLR-literal proxies to an existing list.</summary>
        /// <param name="source">The existing projection items.</param>
        /// <param name="column">The first item to append.</param>
        /// <param name="rest">Additional items to append.</param>
        /// <returns>A combined read-only list preserving source and append order.</returns>
        public static IReadOnlyList<IExprSelecting> Combine(this IReadOnlyList<IExprSelecting> source, SelectingProxy column, params SelectingProxy[] rest) 
            => Helpers.Combine(source, column, rest, SelectingProxy.MapSelectionProxy);

        /// <summary>Builds a projection list containing both input lists in order.</summary>
        /// <param name="source">The leading projection items.</param>
        /// <param name="source2">The projection items to append.</param>
        /// <returns>A combined read-only projection list.</returns>
        public static IReadOnlyList<IExprSelecting> Combine(this IReadOnlyList<IExprSelecting> source, IReadOnlyList<IExprSelecting> source2) 
            => Helpers.Combine(source, source2);

        /// <summary>Folds a non-empty predicate sequence into a left-associated SQL <c>AND</c> expression.</summary>
        /// <param name="predicates">The conditions to require; the sequence is materialized when necessary and must not be empty.</param>
        /// <returns>A single Boolean expression preserving enumeration order.</returns>
        public static ExprBoolean JoinAsAnd(this IEnumerable<ExprBoolean> predicates) 
            => predicates is IReadOnlyList<ExprBoolean> list 
                ? JoinAsAnd(list) 
                : JoinAsAnd(predicates.ToList());

        /// <summary>Folds a non-empty predicate list into a left-associated SQL <c>AND</c> expression.</summary>
        /// <param name="predicates">The conditions to require; an empty list is rejected.</param>
        /// <returns>A single Boolean expression preserving list order.</returns>
        public static ExprBoolean JoinAsAnd(this IReadOnlyList<ExprBoolean> predicates)
        {
            predicates.AssertNotEmpty("Predicates list cannot be empty");

            ExprBoolean result = predicates[0];
            for (int i = 1; i < predicates.Count; i++)
            {
                result = new ExprBooleanAnd(result, predicates[i]);
            }

            return result;
        }

        /// <summary>Folds a non-empty predicate sequence into a left-associated SQL <c>OR</c> expression.</summary>
        /// <param name="predicates">The alternative conditions; the sequence is materialized when necessary and must not be empty.</param>
        /// <returns>A single Boolean expression preserving enumeration order.</returns>
        public static ExprBoolean JoinAsOr(this IEnumerable<ExprBoolean> predicates)
            => predicates is IReadOnlyList<ExprBoolean> list
                ? JoinAsOr(list)
                : JoinAsOr(predicates.ToList());

        /// <summary>Folds a non-empty predicate list into a left-associated SQL <c>OR</c> expression.</summary>
        /// <param name="predicates">The alternative conditions; an empty list is rejected.</param>
        /// <returns>A single Boolean expression preserving list order.</returns>
        public static ExprBoolean JoinAsOr(this IReadOnlyList<ExprBoolean> predicates)
        {
            predicates.AssertNotEmpty("Predicates list cannot be empty");

            ExprBoolean result = predicates[0];
            for (int i = 1; i < predicates.Count; i++)
            {
                result = new ExprBooleanOr(result, predicates[i]);
            }

            return result;
        }

        /// <summary>Creates an ordered sort-key list from an initial key and its next tie-breaker.</summary>
        /// <param name="item">The primary sort key.</param>
        /// <param name="thenBy">The secondary sort key.</param>
        /// <returns>An ordering containing both keys in precedence order.</returns>
        public static ExprOrderBy ThenBy(this ExprOrderByItem item, ExprOrderByItem thenBy)
        {
            return new ExprOrderBy(new[] { item, thenBy });
        }

        /// <summary>Appends a lower-priority tie-breaker to an existing ordering.</summary>
        /// <param name="order">The existing sort-key list.</param>
        /// <param name="thenBy">The sort key to evaluate after all existing keys compare equal.</param>
        /// <returns>A new ordering; the original ordering is unchanged.</returns>
        public static ExprOrderBy ThenBy(this ExprOrderBy order, ExprOrderByItem thenBy)
        {
            return new ExprOrderBy(Helpers.Combine(order.OrderList, thenBy));
        }

        /// <summary>Rebuilds a windowed aggregate with an explicit row/range frame while preserving partitioning and ordering.</summary>
        /// <param name="aggregateOverFunction">The windowed aggregate to modify.</param>
        /// <param name="start">The frame's starting boundary.</param>
        /// <param name="end">The ending boundary for a <c>BETWEEN</c> frame, or <see langword="null"/> for a single-boundary frame.</param>
        /// <returns>A new aggregate-over expression with the requested frame.</returns>
        public static ExprAggregateOverFunction FrameClause(this ExprAggregateOverFunction aggregateOverFunction, SqQueryBuilder.FrameBorder start, SqQueryBuilder.FrameBorder? end)
        {
            return aggregateOverFunction.WithOver(
                new ExprOver(
                    aggregateOverFunction.Over.Partitions,
                    aggregateOverFunction.Over.OrderBy,
                    new ExprFrameClause(start.BuildExpression(), end?.BuildExpression())));
        }

        /// <summary>Rebuilds a windowed aggregate without an explicit frame, leaving the database's default frame semantics in effect.</summary>
        /// <param name="aggregateOverFunction">The windowed aggregate to modify.</param>
        /// <returns>A new aggregate-over expression that preserves partitioning and ordering but has no frame clause.</returns>
        public static ExprAggregateOverFunction FrameClauseEmpty(this ExprAggregateOverFunction aggregateOverFunction)
        {
            return aggregateOverFunction.WithOver(
                new ExprOver(
                    aggregateOverFunction.Over.Partitions,
                    aggregateOverFunction.Over.OrderBy,
                    null));
        }
    }
}
