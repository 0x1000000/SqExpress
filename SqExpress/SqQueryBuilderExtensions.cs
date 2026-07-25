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
        /// <summary>Creates a column reference with the specified qualifier.</summary>
        public static ExprColumn WithSource(this ExprColumnName columnName, IExprColumnSource? newColumnSource)
            => new ExprColumn(newColumnSource, columnName);

        /// <summary>Completes and aliases a subquery for use as a derived table.</summary>
        public static ExprDerivedTableQuery As(this IExprSubQueryFinal expressionBuilder, ExprTableAlias tableAlias) 
            => new ExprDerivedTableQuery(expressionBuilder.Done(), tableAlias, null);

        /// <summary>Completes and aliases a subquery while assigning names to its output columns.</summary>
        public static ExprDerivedTableQuery As(this IExprSubQueryFinal expressionBuilder, ExprTableAlias tableAlias, params ExprColumnName[] columns)
            => new ExprDerivedTableQuery(expressionBuilder.Done(), tableAlias, columns);

        /// <summary>Aliases a completed subquery for use as a derived table.</summary>
        public static ExprDerivedTableQuery As(this IExprSubQuery expressionBuilder, ExprTableAlias tableAlias) 
            => new ExprDerivedTableQuery(expressionBuilder, tableAlias, null);

        /// <summary>Aliases a completed subquery and assigns names to its output columns.</summary>
        public static ExprDerivedTableQuery As(this IExprSubQuery expressionBuilder, ExprTableAlias tableAlias, params ExprColumnName[] columns)
            => new ExprDerivedTableQuery(expressionBuilder, tableAlias, columns);

        /// <summary>Assigns an output alias to a column.</summary>
        public static ExprAliasedColumn As(this ExprColumn column, ExprColumnAlias alias) =>
            new ExprAliasedColumn(column, alias);

        /// <summary>Assigns an output alias to a selecting expression.</summary>
        public static ExprAliasedSelecting As(this IExprSelecting value, ExprColumnAlias alias) =>
            new ExprAliasedSelecting(value, alias);

        /// <summary>Adapts a selecting expression for use as a value expression.</summary>
        public static ExprSelectingValue AsValue(this IExprSelecting selecting)
            => new ExprSelectingValue(selecting);

        /// <summary>Aliases a table value constructor and assigns names to its columns.</summary>
        public static ExprDerivedTableValues As(this ExprTableValueConstructor valueConstructor, Alias alias, params ExprColumnName[] columns)
            => new ExprDerivedTableValues(valueConstructor, new ExprTableAlias(alias.BuildAliasExpression() ?? throw new SqExpressException("Derived Table Values has to have not empty alias")), columns);

        /// <summary>Aliases a table-valued function for use as a table source.</summary>
        public static ExprAliasedTableFunction As(this ExprTableFunction tableFunction, ExprTableAlias alias)
            => new ExprAliasedTableFunction(tableFunction, alias);

        /// <summary>Assigns column names and an automatically generated alias to a table value constructor.</summary>
        public static ExprDerivedTableValues AsColumns(this ExprTableValueConstructor valueConstructor, params ExprColumnName[] columns)
            => new ExprDerivedTableValues(valueConstructor, new ExprTableAlias(Alias.Auto.BuildAliasExpression() ?? throw new SqExpressException("Derived Table Values has to have not empty alias")), columns);

        /// <summary>Creates a column reference qualified by this column source.</summary>
        public static ExprColumn Column(this IExprColumnSource columnSource, ExprColumnName columnName) 
            => new ExprColumn(columnSource, columnName);

        /// <summary>Creates a column reference qualified by this derived table's alias.</summary>
        public static ExprColumn Column(this ExprDerivedTable derivedTable, ExprColumnName columnName)
            => derivedTable.Alias.Column(columnName);

        /// <summary>Creates a column reference qualified by this table's alias or full name.</summary>
        public static ExprColumn Column(this ExprTable table, ExprColumnName columnName)
            => table.Alias != null ? table.Alias.Column(columnName) : new ExprColumn(table.FullName, columnName);

        /// <summary>Creates a column reference qualified by this table function's alias.</summary>
        public static ExprColumn Column(this ExprAliasedTableFunction table, ExprColumnName columnName)
            => table.Alias.Column(columnName);

        /// <summary>Creates a qualified all-columns selection for this column source.</summary>
        public static ExprAllColumns AllColumns(this IExprColumnSource columnSource)
            => new ExprAllColumns(columnSource);

        /// <summary>Selects all columns from this derived table.</summary>
        public static ExprAllColumns AllColumns(this ExprDerivedTable derivedTable)
            => derivedTable.Alias.AllColumns();

        /// <summary>Selects all columns from this table, using its alias when present.</summary>
        public static ExprAllColumns AllColumns(this ExprTable table)
            => table.Alias != null ? table.Alias.AllColumns() : new ExprAllColumns(table.FullName);

        /// <summary>Selects all columns from this aliased table function.</summary>
        public static ExprAllColumns AllColumns(this ExprAliasedTableFunction table)
            => table.Alias.AllColumns();

        /// <summary>Creates an <c>IN</c> predicate from one or more value expressions.</summary>
        public static ExprInValues In(this ExprColumn column, ExprValue value, params ExprValue[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest));

        /// <summary>Creates an <c>IN</c> predicate from a non-empty value-expression list.</summary>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<ExprValue> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty"));

        /// <summary>Creates an <c>IN</c> predicate against a completed subquery.</summary>
        public static ExprInSubQuery In(this ExprColumn column, IExprSubQueryFinal subQuery)
            => new ExprInSubQuery(column, subQuery.Done());

        /// <summary>Creates an <c>IN</c> predicate from one or more integer literals.</summary>
        public static ExprInValues In(this ExprColumn column, int value, params int[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest, i=> Literal(i)));

        /// <summary>Creates an <c>IN</c> predicate from a non-empty integer list.</summary>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<int> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty").SelectToReadOnlyList(i=>Literal(i)));

        /// <summary>Creates an <c>IN</c> predicate from one or more string literals.</summary>
        public static ExprInValues In(this ExprColumn column, string value, params string[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest, i=> Literal(i)));

        /// <summary>Creates an <c>IN</c> predicate from a non-empty string list.</summary>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<string> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty").SelectToReadOnlyList(i=>Literal(i)));

        /// <summary>Creates an <c>IN</c> predicate from one or more GUID literals.</summary>
        public static ExprInValues In(this ExprColumn column, Guid value, params Guid[] rest)
            => new ExprInValues(column, Helpers.Combine(value, rest, i=> Literal(i)));

        /// <summary>Creates an <c>IN</c> predicate from a non-empty GUID list.</summary>
        public static ExprInValues In(this ExprColumn column, IReadOnlyList<Guid> items)
            => new ExprInValues(column, items.AssertNotEmpty("'IN' expressions list cannot be empty").SelectToReadOnlyList(i=>Literal(i)));

        /// <summary>Creates a <c>LIKE</c> predicate for a non-nullable string table column.</summary>
        public static ExprLike Like(this StringTableColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Creates a <c>LIKE</c> predicate for a nullable string table column.</summary>
        public static ExprLike Like(this NullableStringTableColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Creates a <c>LIKE</c> predicate for a non-nullable custom string column.</summary>
        public static ExprLike Like(this StringCustomColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Creates a <c>LIKE</c> predicate for a nullable custom string column.</summary>
        public static ExprLike Like(this NullableStringCustomColumn column, string pattern)
            => SqQueryBuilder.Like(column, pattern);

        /// <summary>Combines statements into one executable statement list.</summary>
        public static IStatement Combine(this IEnumerable<IStatement> statements) =>
            StatementList.Combine(statements is IReadOnlyList<IStatement> l ? l : statements.ToList());

        /// <summary>Adds a SQL <c>OUTPUT</c> projection to a delete statement.</summary>
        public static ExprDeleteOutput Output(this ExprDelete exprDelete, ExprColumn outputColumn, params ExprAliasedColumn[] rest) 
            => new ExprDeleteOutput(exprDelete, Helpers.Combine(outputColumn, rest));

        /// <summary>Appends one or more selecting expressions to a selection list.</summary>
        public static IReadOnlyList<IExprSelecting> Combine(this IReadOnlyList<IExprSelecting> source, SelectingProxy column, params SelectingProxy[] rest) 
            => Helpers.Combine(source, column, rest, SelectingProxy.MapSelectionProxy);

        /// <summary>Concatenates two selection lists.</summary>
        public static IReadOnlyList<IExprSelecting> Combine(this IReadOnlyList<IExprSelecting> source, IReadOnlyList<IExprSelecting> source2) 
            => Helpers.Combine(source, source2);

        /// <summary>Combines a non-empty predicate sequence with SQL <c>AND</c>.</summary>
        public static ExprBoolean JoinAsAnd(this IEnumerable<ExprBoolean> predicates) 
            => predicates is IReadOnlyList<ExprBoolean> list 
                ? JoinAsAnd(list) 
                : JoinAsAnd(predicates.ToList());

        /// <summary>Combines a non-empty predicate list with SQL <c>AND</c>.</summary>
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

        /// <summary>Combines a non-empty predicate sequence with SQL <c>OR</c>.</summary>
        public static ExprBoolean JoinAsOr(this IEnumerable<ExprBoolean> predicates)
            => predicates is IReadOnlyList<ExprBoolean> list
                ? JoinAsOr(list)
                : JoinAsOr(predicates.ToList());

        /// <summary>Combines a non-empty predicate list with SQL <c>OR</c>.</summary>
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

        /// <summary>Starts a multi-item ordering from two order-by items.</summary>
        public static ExprOrderBy ThenBy(this ExprOrderByItem item, ExprOrderByItem thenBy)
        {
            return new ExprOrderBy(new[] { item, thenBy });
        }

        /// <summary>Appends an item to an existing ordering.</summary>
        public static ExprOrderBy ThenBy(this ExprOrderBy order, ExprOrderByItem thenBy)
        {
            return new ExprOrderBy(Helpers.Combine(order.OrderList, thenBy));
        }

        /// <summary>Adds a window frame clause to an aggregate window function.</summary>
        public static ExprAggregateOverFunction FrameClause(this ExprAggregateOverFunction aggregateOverFunction, SqQueryBuilder.FrameBorder start, SqQueryBuilder.FrameBorder? end)
        {
            return aggregateOverFunction.WithOver(
                new ExprOver(
                    aggregateOverFunction.Over.Partitions,
                    aggregateOverFunction.Over.OrderBy,
                    new ExprFrameClause(start.BuildExpression(), end?.BuildExpression())));
        }

        /// <summary>Removes the window frame clause from an aggregate window function.</summary>
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
