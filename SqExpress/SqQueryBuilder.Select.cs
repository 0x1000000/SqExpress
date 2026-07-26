using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.QueryBuilders.RecordSetter.Internal;
using SqExpress.QueryBuilders.Select;
using SqExpress.QueryBuilders.Select.Internal;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress
{
    public partial class SqQueryBuilder
    {
        /// <summary>Begins a <c>SELECT</c> statement using an already constructed list of projection items.</summary>
        /// <param name="selection">The columns, expressions, or wildcard items to project, in output order.</param>
        /// <returns>The initial fluent stage, from which a source, filter, grouping, ordering, or completion can be selected.</returns>
        public static IQuerySpecificationBuilderInitial Select(IReadOnlyList<IExprSelecting> selection) 
            => new QuerySpecificationBuilder(null, false, selection);

        /// <summary>Begins a <c>SELECT</c> statement and converts the supplied expressions or CLR values into projection items.</summary>
        /// <param name="selection">The first projection item. CLR values are represented by the corresponding SqExpress literal node.</param>
        /// <param name="selections">Additional projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial Select(SelectingProxy selection, params SelectingProxy[] selections) 
            => new QuerySpecificationBuilder(null, false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Begins a <c>SELECT</c> statement whose projection consists of value expressions.</summary>
        /// <param name="selection">The first expression to project.</param>
        /// <param name="selections">Additional expressions to project, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial Select(ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(null, false, Helpers.Combine(selection, selections));

        /// <summary>Begins <c>SELECT 1</c>, commonly used as the projection of an <c>EXISTS</c> subquery.</summary>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectOne()
            => new QuerySpecificationBuilder(null, false, new[] { Literal(1) });

        /// <summary>Begins a <c>SELECT DISTINCT</c> statement and converts expressions or CLR values into projection items.</summary>
        /// <param name="selection">The first projection item.</param>
        /// <param name="selections">Additional projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the distinct query.</returns>
        public static IQuerySpecificationBuilderInitial SelectDistinct(SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(null, true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Begins a <c>SELECT DISTINCT</c> statement using an already constructed projection list.</summary>
        /// <param name="selection">The columns, expressions, or wildcard items to project, in output order.</param>
        /// <returns>The initial fluent stage for composing the distinct query.</returns>
        public static IQuerySpecificationBuilderInitial SelectDistinct(IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(null, true, selection);

        /// <summary>Begins a row-limited <c>SELECT</c> whose limit is a fixed integer.</summary>
        /// <remarks>The selected SQL exporter renders the limit in its native form, such as <c>TOP</c> or <c>LIMIT</c>.</remarks>
        /// <param name="top">The maximum number of rows requested from the database.</param>
        /// <param name="selection">The projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the limited query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTop(int top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(Literal(top), false, selection);

        /// <summary>Begins a row-limited <c>SELECT</c> and converts the supplied expressions or CLR values into projection items.</summary>
        /// <remarks>The selected SQL exporter renders the limit in its native dialect form.</remarks>
        /// <param name="top">The maximum number of rows requested from the database.</param>
        /// <param name="selection">The first projection item.</param>
        /// <param name="selections">Additional projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the limited query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTop(int top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(Literal(top), false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Begins a row-limited <c>SELECT</c> whose limit is represented by a SQL value expression.</summary>
        /// <remarks>Use an expression limit when the value must be supplied through a parameter or another supported SQL expression.</remarks>
        /// <param name="top">An expression that evaluates to the maximum number of rows.</param>
        /// <param name="selection">The first projection item.</param>
        /// <param name="selections">Additional projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the limited query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(top, false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Begins a row-limited <c>SELECT</c> from an expression limit and a prebuilt projection list.</summary>
        /// <param name="top">An expression that evaluates to the maximum number of rows.</param>
        /// <param name="selection">The projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the limited query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(top, false, selection);

        /// <summary>Begins a fixed-limit <c>SELECT DISTINCT</c> statement.</summary>
        /// <param name="top">The maximum number of distinct rows requested from the database.</param>
        /// <param name="selection">The first projection item.</param>
        /// <param name="selections">Additional projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(Literal(top), true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Begins a fixed-limit <c>SELECT DISTINCT</c> statement using a prebuilt projection list.</summary>
        /// <param name="top">The maximum number of distinct rows requested from the database.</param>
        /// <param name="selection">The projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(Literal(top), true, selection);

        /// <summary>Begins a <c>SELECT DISTINCT</c> with an expression-based row limit and a prebuilt projection list.</summary>
        /// <param name="top">An expression that evaluates to the maximum number of distinct rows.</param>
        /// <param name="selection">The projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(top, true, selection);

        /// <summary>Begins a <c>SELECT DISTINCT</c> whose row limit is represented by a SQL value expression.</summary>
        /// <param name="top">An expression that evaluates to the maximum number of distinct rows.</param>
        /// <param name="selection">The first projection item.</param>
        /// <param name="selections">Additional projection items, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(top, true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Begins a <c>SELECT DISTINCT</c> whose projection consists of value expressions.</summary>
        /// <param name="selection">The first expression to project.</param>
        /// <param name="selections">Additional expressions to project, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectDistinct(ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(null, true, Helpers.Combine(selection, selections));

        /// <summary>Begins a fixed-limit <c>SELECT</c> whose projection consists of value expressions.</summary>
        /// <param name="top">The maximum number of rows requested from the database.</param>
        /// <param name="selection">The first expression to project.</param>
        /// <param name="selections">Additional expressions to project, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTop(int top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(Literal(top), false, Helpers.Combine(selection, selections));

        /// <summary>Begins an expression-limited <c>SELECT</c> whose projection consists of value expressions.</summary>
        /// <param name="top">An expression that evaluates to the maximum number of rows.</param>
        /// <param name="selection">The first expression to project.</param>
        /// <param name="selections">Additional expressions to project, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(top, false, Helpers.Combine(selection, selections));

        /// <summary>Begins a fixed-limit <c>SELECT DISTINCT</c> whose projection consists of value expressions.</summary>
        /// <param name="top">The maximum number of distinct rows requested from the database.</param>
        /// <param name="selection">The first expression to project.</param>
        /// <param name="selections">Additional expressions to project, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(Literal(top), true, Helpers.Combine(selection, selections));

        /// <summary>Begins an expression-limited <c>SELECT DISTINCT</c> whose projection consists of value expressions.</summary>
        /// <param name="top">An expression that evaluates to the maximum number of distinct rows.</param>
        /// <param name="selection">The first expression to project.</param>
        /// <param name="selections">Additional expressions to project, in output order.</param>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(top, true, Helpers.Combine(selection, selections));

        /// <summary>Begins a one-row <c>SELECT 1</c>, suitable for existence probes and dialect-neutral test queries.</summary>
        /// <returns>The initial fluent stage for composing the query.</returns>
        public static IQuerySpecificationBuilderInitial SelectTopOne()
            => new QuerySpecificationBuilder(Literal(1), false, new[] { Literal(1) });

        /// <summary>Marks an expression as an ascending sort key for an <c>ORDER BY</c> clause or window definition.</summary>
        /// <param name="value">The expression whose values determine row order.</param>
        /// <returns>An ascending ordering item.</returns>
        public static ExprOrderByItem Asc(ExprValue value)=>new ExprOrderByItem(value, false);

        /// <summary>Marks an expression as a descending sort key for an <c>ORDER BY</c> clause or window definition.</summary>
        /// <param name="value">The expression whose values determine row order.</param>
        /// <returns>A descending ordering item.</returns>
        public static ExprOrderByItem Desc(ExprValue value)=>new ExprOrderByItem(value, true);

        /// <summary>Creates an unqualified <c>*</c> projection that selects every column exposed by the query source.</summary>
        /// <returns>An all-columns selection expression.</returns>
        public static ExprAllColumns AllColumns() => new ExprAllColumns(null);

        /// <summary>Creates a SQL table value constructor from explicitly supplied rows.</summary>
        /// <remarks>Every row should have the same number of values; database-specific support and rendering are determined by the selected exporter.</remarks>
        /// <param name="valueRows">The rows and their values, in declaration order.</param>
        /// <returns>A table expression that can be used where a value constructor is accepted.</returns>
        public static ExprTableValueConstructor Values(IReadOnlyList<IReadOnlyList<ExprValue>> valueRows) 
            => new ExprTableValueConstructor(valueRows.SelectToReadOnlyList(i=> new ExprValueRow(i)));

        /// <summary>Creates a SQL table value constructor with one column and one row per supplied value.</summary>
        /// <param name="values">The values to place in consecutive rows of the single-column constructor.</param>
        /// <returns>A single-column table value constructor.</returns>
        public static ExprTableValueConstructor Values(IReadOnlyList<ExprValue> values) 
            => new ExprTableValueConstructor(values.SelectToReadOnlyList(i=> new ExprValueRow(new[]{i})));

        /// <summary>Creates a SQL table value constructor with one column and one row per supplied value.</summary>
        /// <param name="values">The values to place in consecutive rows of the single-column constructor.</param>
        /// <returns>A single-column table value constructor.</returns>
        public static ExprTableValueConstructor Values(params ExprValue[] values) 
            => new ExprTableValueConstructor(values.SelectToReadOnlyList(i=> new ExprValueRow(new[]{i})));

        /// <summary>Maps an in-memory sequence to a named, column-addressable SQL value table.</summary>
        /// <remarks>The mapping is evaluated for every input item. Its first invocation establishes the column list, and subsequent invocations must assign the same columns.</remarks>
        /// <typeparam name="T">The application record type being mapped.</typeparam>
        /// <param name="data">The records to convert to value rows. The sequence must contain at least one item.</param>
        /// <param name="mapping">A callback that assigns each source member to a named value-table column.</param>
        /// <param name="alias">The table alias used to qualify the generated columns; a default alias is generated when omitted.</param>
        /// <returns>An aliased derived table containing the mapped values and column names.</returns>
        /// <exception cref="SqExpressException">The data sequence is empty or the mapping produces no columns.</exception>
        public static ExprDerivedTableValues ValueTable<T>(IEnumerable<T> data, ValueConstructorMapping<T> mapping, Alias alias = default)
        {
            IReadOnlyList<ExprColumnName>? columns = null;
            ValueConstructorSetter<T>? setter = null;
            List<ExprValueRow> ? records = null;

            foreach (var item in data)
            {
                setter ??= new ValueConstructorSetter<T>(default!);
                setter.NextItem(item, columns?.Count);
                mapping(setter);
                columns ??= setter.Columns;
                var record = setter.Record.AssertFatalNotNull(nameof(setter.Record));

                if (record.Count < 1)
                {
                    throw new SqExpressException("There should have been at least one column");
                }
                setter.EnsureRecordLength();

                records ??= new List<ExprValueRow>();
                records.Add(new ExprValueRow(record));
            }

            if (records == null || columns == null)
            {
                throw new SqExpressException("There should have been at least item in the passed collection");
            }

            var exprTableValueConstructor = new ExprTableValueConstructor(records);

            return new ExprDerivedTableValues(exprTableValueConstructor, TableAlias(alias), columns);
        }
        
        /// <summary>Wraps a completed subquery so it can be used where a scalar SQL value is expected.</summary>
        /// <remarks>The database requires the subquery to return no more than one row and one column; SqExpress does not execute it to validate that cardinality.</remarks>
        /// <param name="query">The completed subquery to wrap.</param>
        /// <returns>A scalar-subquery value expression.</returns>
        public static ExprValueQuery ValueQuery(IExprSubQuery query) 
            => new ExprValueQuery(query);

        /// <summary>Completes a fluent subquery builder and wraps its result as a scalar SQL value.</summary>
        /// <remarks>The database requires the subquery to return no more than one row and one column.</remarks>
        /// <param name="query">The final fluent stage of the subquery.</param>
        /// <returns>A scalar-subquery value expression.</returns>
        public static ExprValueQuery ValueQuery(IExprSubQueryFinal query) 
            => new ExprValueQuery(query.Done());

        /// <summary>Creates an <c>EXISTS</c> predicate for a table descriptor without requiring the caller to instantiate that descriptor.</summary>
        /// <typeparam name="TTable">A constructible table-source descriptor used as the subquery source.</typeparam>
        /// <param name="on">Builds the correlated or uncorrelated predicate applied inside the existence subquery.</param>
        /// <returns>An <c>EXISTS (SELECT 1 ...)</c> Boolean expression.</returns>
        public static ExprBoolean ExistsIn<TTable>(Func<TTable, ExprBoolean> on)
            where TTable : IExprTableSource, new()
        {
            var tbl = new TTable();
            return Exists(SelectOne().From(tbl).Where(on(tbl)));
        }
    }

    /// <summary>
    /// Provides implicit conversions used by <see cref="SqQueryBuilder.Select(SelectingProxy, SelectingProxy[])"/>
    /// to accept selectable AST nodes and common CLR literal values in one projection list.
    /// </summary>
    /// <remarks>A default instance does not contain a selection and is rejected when the query is constructed.</remarks>
    public readonly struct SelectingProxy
    {
        private readonly IExprSelecting? Expr;

        internal SelectingProxy(IExprSelecting expr)
        {
            this.Expr = expr;
        }

        internal static IExprSelecting MapSelectionProxy(SelectingProxy sp)
        {
            return sp.Expr ?? throw new SqExpressException("Selection cannot be default here");
        }

        /// <summary>Allows any SQL value expression to appear directly in a projection list.</summary>
        /// <param name="value">The value expression to project.</param>
        /// <returns>A proxy containing the supplied expression.</returns>
        public static implicit operator SelectingProxy(ExprValue value) => new SelectingProxy(value);

        /// <summary>Allows a qualified or unqualified <c>*</c> expression to appear in a projection list.</summary>
        /// <param name="value">The all-columns expression to project.</param>
        /// <returns>A proxy containing the supplied expression.</returns>
        public static implicit operator SelectingProxy(ExprAllColumns value) => new SelectingProxy(value);

        /// <summary>Allows an analytic function result to appear directly in a projection list.</summary>
        /// <param name="value">The analytic function to project.</param>
        /// <returns>A proxy containing the supplied function.</returns>
        public static implicit operator SelectingProxy(ExprAnalyticFunction value) => new SelectingProxy(value);

        /// <summary>Allows an aggregate result to appear directly in a projection list.</summary>
        /// <param name="value">The aggregate function to project.</param>
        /// <returns>A proxy containing the supplied function.</returns>
        public static implicit operator SelectingProxy(ExprAggregateFunction value) => new SelectingProxy(value);

        /// <summary>Allows a windowed aggregate result to appear directly in a projection list.</summary>
        /// <param name="value">The windowed aggregate to project.</param>
        /// <returns>A proxy containing the supplied function.</returns>
        public static implicit operator SelectingProxy(ExprAggregateOverFunction value) => new SelectingProxy(value);

        /// <summary>Preserves a column and its output alias when it is supplied to a projection list.</summary>
        /// <param name="value">The aliased column to project.</param>
        /// <returns>A proxy containing the supplied column.</returns>
        public static implicit operator SelectingProxy(ExprAliasedColumn value) => new SelectingProxy(value);

        /// <summary>Preserves an expression and its output alias when it is supplied to a projection list.</summary>
        /// <param name="value">The aliased expression to project.</param>
        /// <returns>A proxy containing the supplied expression.</returns>
        public static implicit operator SelectingProxy(ExprAliasedSelecting value) => new SelectingProxy(value);

        /// <summary>Allows a column name to be projected without manually creating a value-column node.</summary>
        /// <param name="value">The unqualified column name to project.</param>
        /// <returns>A proxy containing an unqualified column expression.</returns>
        public static implicit operator SelectingProxy(ExprColumnName value) => new SelectingProxy(value);

        //Types
        /// <summary>Allows a nullable string to be projected as a SQL string or <c>NULL</c> literal.</summary>
        /// <param name="value">The string value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding string literal node.</returns>
        public static implicit operator SelectingProxy(string? value)
            => new SelectingProxy(new ExprStringLiteral(value));

        /// <summary>Allows a Boolean value to be projected using the selected dialect's Boolean representation.</summary>
        /// <param name="value">The Boolean value.</param>
        /// <returns>A proxy containing the corresponding Boolean literal node.</returns>
        public static implicit operator SelectingProxy(bool value)
            => new SelectingProxy(new ExprBoolLiteral(value));

        /// <summary>Allows a nullable Boolean to be projected as a dialect-appropriate Boolean or <c>NULL</c> literal.</summary>
        /// <param name="value">The Boolean value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding Boolean literal node.</returns>
        public static implicit operator SelectingProxy(bool? value)
            => new SelectingProxy(new ExprBoolLiteral(value));

        /// <summary>Allows a 32-bit integer to be projected as a numeric SQL literal.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A proxy containing the corresponding integer literal node.</returns>
        public static implicit operator SelectingProxy(int value)
            => new SelectingProxy(new ExprInt32Literal(value));

        /// <summary>Allows a nullable 32-bit integer to be projected as a numeric or <c>NULL</c> literal.</summary>
        /// <param name="value">The integer value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding integer literal node.</returns>
        public static implicit operator SelectingProxy(int? value)
            => new SelectingProxy(new ExprInt32Literal(value));

        /// <summary>Allows an unsigned byte to be projected as a numeric SQL literal.</summary>
        /// <param name="value">The byte value.</param>
        /// <returns>A proxy containing the corresponding byte literal node.</returns>
        public static implicit operator SelectingProxy(byte value)
            => new SelectingProxy(new ExprByteLiteral(value));

        /// <summary>Allows a nullable byte to be projected as a numeric or <c>NULL</c> literal.</summary>
        /// <param name="value">The byte value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding byte literal node.</returns>
        public static implicit operator SelectingProxy(byte? value)
            => new SelectingProxy(new ExprByteLiteral(value));

        /// <summary>Allows a 16-bit integer to be projected as a numeric SQL literal.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A proxy containing the corresponding integer literal node.</returns>
        public static implicit operator SelectingProxy(short value)
            => new SelectingProxy(new ExprInt16Literal(value));

        /// <summary>Allows a nullable 16-bit integer to be projected as a numeric or <c>NULL</c> literal.</summary>
        /// <param name="value">The integer value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding integer literal node.</returns>
        public static implicit operator SelectingProxy(short? value)
            => new SelectingProxy(new ExprInt16Literal(value));

        /// <summary>Allows a 64-bit integer to be projected as a numeric SQL literal.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A proxy containing the corresponding integer literal node.</returns>
        public static implicit operator SelectingProxy(long value)
            => new SelectingProxy(new ExprInt64Literal(value));

        /// <summary>Allows a nullable 64-bit integer to be projected as a numeric or <c>NULL</c> literal.</summary>
        /// <param name="value">The integer value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding integer literal node.</returns>
        public static implicit operator SelectingProxy(long? value)
            => new SelectingProxy(new ExprInt64Literal(value));

        /// <summary>Allows a decimal value to be projected as an exact numeric SQL literal.</summary>
        /// <param name="value">The decimal value.</param>
        /// <returns>A proxy containing the corresponding decimal literal node.</returns>
        public static implicit operator SelectingProxy(decimal value)
            => new SelectingProxy(new ExprDecimalLiteral(value));

        /// <summary>Allows a nullable decimal to be projected as an exact numeric or <c>NULL</c> literal.</summary>
        /// <param name="value">The decimal value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding decimal literal node.</returns>
        public static implicit operator SelectingProxy(decimal? value)
            => new SelectingProxy(new ExprDecimalLiteral(value));

        /// <summary>Allows a double-precision value to be projected as an approximate numeric SQL literal.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns>A proxy containing the corresponding double literal node.</returns>
        public static implicit operator SelectingProxy(double value)
            => new SelectingProxy(new ExprDoubleLiteral(value));

        /// <summary>Allows a nullable double to be projected as an approximate numeric or <c>NULL</c> literal.</summary>
        /// <param name="value">The floating-point value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding double literal node.</returns>
        public static implicit operator SelectingProxy(double? value)
            => new SelectingProxy(new ExprDoubleLiteral(value));

        /// <summary>Allows a GUID to be projected using the selected dialect's GUID-compatible literal representation.</summary>
        /// <param name="value">The GUID value.</param>
        /// <returns>A proxy containing the corresponding GUID literal node.</returns>
        public static implicit operator SelectingProxy(Guid value)
            => new SelectingProxy(new ExprGuidLiteral(value));

        /// <summary>Allows a nullable GUID to be projected as a GUID-compatible or <c>NULL</c> literal.</summary>
        /// <param name="value">The GUID value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding GUID literal node.</returns>
        public static implicit operator SelectingProxy(Guid? value)
            => new SelectingProxy(new ExprGuidLiteral(value));

        /// <summary>Allows a date and time to be projected using the selected dialect's temporal literal representation.</summary>
        /// <param name="value">The date and time value.</param>
        /// <returns>A proxy containing the corresponding date-time literal node.</returns>
        public static implicit operator SelectingProxy(DateTime value)
            => new SelectingProxy(new ExprDateTimeLiteral(value));

        /// <summary>Allows a nullable date and time to be projected as a temporal or <c>NULL</c> literal.</summary>
        /// <param name="value">The date and time value, or <see langword="null"/>.</param>
        /// <returns>A proxy containing the corresponding date-time literal node.</returns>
        public static implicit operator SelectingProxy(DateTime? value)
            => new SelectingProxy(new ExprDateTimeLiteral(value));
    }
}
