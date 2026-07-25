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
        /// <summary>Starts a query with the supplied selection list.</summary>
        public static IQuerySpecificationBuilderInitial Select(IReadOnlyList<IExprSelecting> selection) 
            => new QuerySpecificationBuilder(null, false, selection);

        /// <summary>Starts a query with one or more selecting expressions or CLR literals.</summary>
        public static IQuerySpecificationBuilderInitial Select(SelectingProxy selection, params SelectingProxy[] selections) 
            => new QuerySpecificationBuilder(null, false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Starts a query with one or more value expressions.</summary>
        public static IQuerySpecificationBuilderInitial Select(ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(null, false, Helpers.Combine(selection, selections));

        /// <summary>Starts a query that selects the integer literal <c>1</c>.</summary>
        public static IQuerySpecificationBuilderInitial SelectOne()
            => new QuerySpecificationBuilder(null, false, new[] { Literal(1) });

        /// <summary>Starts a distinct query with one or more selecting expressions or CLR literals.</summary>
        public static IQuerySpecificationBuilderInitial SelectDistinct(SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(null, true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Starts a distinct query with the supplied selection list.</summary>
        public static IQuerySpecificationBuilderInitial SelectDistinct(IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(null, true, selection);

        /// <summary>Starts a query limited to a fixed number of rows.</summary>
        public static IQuerySpecificationBuilderInitial SelectTop(int top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(Literal(top), false, selection);

        /// <summary>Starts a query limited to a fixed number of rows with one or more selecting expressions.</summary>
        public static IQuerySpecificationBuilderInitial SelectTop(int top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(Literal(top), false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Starts a query whose row limit is supplied as an expression.</summary>
        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(top, false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Starts a query whose row limit is supplied as an expression and whose selection is supplied as a list.</summary>
        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(top, false, selection);

        /// <summary>Starts a distinct query limited to a fixed number of rows.</summary>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(Literal(top), true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Starts a distinct, fixed-limit query with the supplied selection list.</summary>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(Literal(top), true, selection);

        /// <summary>Starts a distinct query with an expression row limit and supplied selection list.</summary>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(top, true, selection);

        /// <summary>Starts a distinct query with an expression row limit.</summary>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(top, true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        /// <summary>Starts a distinct query with one or more value expressions.</summary>
        public static IQuerySpecificationBuilderInitial SelectDistinct(ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(null, true, Helpers.Combine(selection, selections));

        /// <summary>Starts a fixed-limit query with one or more value expressions.</summary>
        public static IQuerySpecificationBuilderInitial SelectTop(int top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(Literal(top), false, Helpers.Combine(selection, selections));

        /// <summary>Starts an expression-limit query with one or more value expressions.</summary>
        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(top, false, Helpers.Combine(selection, selections));

        /// <summary>Starts a distinct, fixed-limit query with one or more value expressions.</summary>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(Literal(top), true, Helpers.Combine(selection, selections));

        /// <summary>Starts a distinct, expression-limit query with one or more value expressions.</summary>
        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(top, true, Helpers.Combine(selection, selections));

        /// <summary>Starts a query limited to one row that selects the integer literal <c>1</c>.</summary>
        public static IQuerySpecificationBuilderInitial SelectTopOne()
            => new QuerySpecificationBuilder(Literal(1), false, new[] { Literal(1) });

        /// <summary>Creates an ascending order-by item.</summary>
        public static ExprOrderByItem Asc(ExprValue value)=>new ExprOrderByItem(value, false);

        /// <summary>Creates a descending order-by item.</summary>
        public static ExprOrderByItem Desc(ExprValue value)=>new ExprOrderByItem(value, true);

        /// <summary>Creates an unqualified all-columns selection.</summary>
        public static ExprAllColumns AllColumns() => new ExprAllColumns(null);

        /// <summary>Creates a multi-row table value constructor.</summary>
        public static ExprTableValueConstructor Values(IReadOnlyList<IReadOnlyList<ExprValue>> valueRows) 
            => new ExprTableValueConstructor(valueRows.SelectToReadOnlyList(i=> new ExprValueRow(i)));

        /// <summary>Creates a single-column table value constructor from a list of values.</summary>
        public static ExprTableValueConstructor Values(IReadOnlyList<ExprValue> values) 
            => new ExprTableValueConstructor(values.SelectToReadOnlyList(i=> new ExprValueRow(new[]{i})));

        /// <summary>Creates a single-column table value constructor from one or more values.</summary>
        public static ExprTableValueConstructor Values(params ExprValue[] values) 
            => new ExprTableValueConstructor(values.SelectToReadOnlyList(i=> new ExprValueRow(new[]{i})));

        /// <summary>Maps application data to an aliased table value constructor.</summary>
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
        
        /// <summary>Wraps a scalar subquery as a value expression.</summary>
        public static ExprValueQuery ValueQuery(IExprSubQuery query) 
            => new ExprValueQuery(query);

        /// <summary>Completes and wraps a scalar subquery as a value expression.</summary>
        public static ExprValueQuery ValueQuery(IExprSubQueryFinal query) 
            => new ExprValueQuery(query.Done());

        /// <summary>Creates an <c>EXISTS</c> predicate against a newly constructed table descriptor.</summary>
        public static ExprBoolean ExistsIn<TTable>(Func<TTable, ExprBoolean> on)
            where TTable : IExprTableSource, new()
        {
            var tbl = new TTable();
            return Exists(SelectOne().From(tbl).Where(on(tbl)));
        }
    }

    /// <summary>Converts supported expressions and CLR values into query selection items.</summary>
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

        /// <summary>Converts a value expression to a selection item.</summary>
        public static implicit operator SelectingProxy(ExprValue value) => new SelectingProxy(value);

        /// <summary>Converts an all-columns expression to a selection item.</summary>
        public static implicit operator SelectingProxy(ExprAllColumns value) => new SelectingProxy(value);

        /// <summary>Converts an analytic function to a selection item.</summary>
        public static implicit operator SelectingProxy(ExprAnalyticFunction value) => new SelectingProxy(value);

        /// <summary>Converts an aggregate function to a selection item.</summary>
        public static implicit operator SelectingProxy(ExprAggregateFunction value) => new SelectingProxy(value);

        /// <summary>Converts an aggregate window function to a selection item.</summary>
        public static implicit operator SelectingProxy(ExprAggregateOverFunction value) => new SelectingProxy(value);

        /// <summary>Converts an aliased column to a selection item.</summary>
        public static implicit operator SelectingProxy(ExprAliasedColumn value) => new SelectingProxy(value);

        /// <summary>Converts an aliased selecting expression to a selection item.</summary>
        public static implicit operator SelectingProxy(ExprAliasedSelecting value) => new SelectingProxy(value);

        /// <summary>Converts a column name to an unqualified selection item.</summary>
        public static implicit operator SelectingProxy(ExprColumnName value) => new SelectingProxy(value);

        //Types
        /// <summary>Converts a nullable string to a selection literal.</summary>
        public static implicit operator SelectingProxy(string? value)
            => new SelectingProxy(new ExprStringLiteral(value));

        /// <summary>Converts a Boolean value to a selection literal.</summary>
        public static implicit operator SelectingProxy(bool value)
            => new SelectingProxy(new ExprBoolLiteral(value));

        /// <summary>Converts a nullable Boolean value to a selection literal.</summary>
        public static implicit operator SelectingProxy(bool? value)
            => new SelectingProxy(new ExprBoolLiteral(value));

        /// <summary>Converts a 32-bit integer to a selection literal.</summary>
        public static implicit operator SelectingProxy(int value)
            => new SelectingProxy(new ExprInt32Literal(value));

        /// <summary>Converts a nullable 32-bit integer to a selection literal.</summary>
        public static implicit operator SelectingProxy(int? value)
            => new SelectingProxy(new ExprInt32Literal(value));

        /// <summary>Converts a byte to a selection literal.</summary>
        public static implicit operator SelectingProxy(byte value)
            => new SelectingProxy(new ExprByteLiteral(value));

        /// <summary>Converts a nullable byte to a selection literal.</summary>
        public static implicit operator SelectingProxy(byte? value)
            => new SelectingProxy(new ExprByteLiteral(value));

        /// <summary>Converts a 16-bit integer to a selection literal.</summary>
        public static implicit operator SelectingProxy(short value)
            => new SelectingProxy(new ExprInt16Literal(value));

        /// <summary>Converts a nullable 16-bit integer to a selection literal.</summary>
        public static implicit operator SelectingProxy(short? value)
            => new SelectingProxy(new ExprInt16Literal(value));

        /// <summary>Converts a 64-bit integer to a selection literal.</summary>
        public static implicit operator SelectingProxy(long value)
            => new SelectingProxy(new ExprInt64Literal(value));

        /// <summary>Converts a nullable 64-bit integer to a selection literal.</summary>
        public static implicit operator SelectingProxy(long? value)
            => new SelectingProxy(new ExprInt64Literal(value));

        /// <summary>Converts a decimal value to a selection literal.</summary>
        public static implicit operator SelectingProxy(decimal value)
            => new SelectingProxy(new ExprDecimalLiteral(value));

        /// <summary>Converts a nullable decimal value to a selection literal.</summary>
        public static implicit operator SelectingProxy(decimal? value)
            => new SelectingProxy(new ExprDecimalLiteral(value));

        /// <summary>Converts a double-precision value to a selection literal.</summary>
        public static implicit operator SelectingProxy(double value)
            => new SelectingProxy(new ExprDoubleLiteral(value));

        /// <summary>Converts a nullable double-precision value to a selection literal.</summary>
        public static implicit operator SelectingProxy(double? value)
            => new SelectingProxy(new ExprDoubleLiteral(value));

        /// <summary>Converts a GUID to a selection literal.</summary>
        public static implicit operator SelectingProxy(Guid value)
            => new SelectingProxy(new ExprGuidLiteral(value));

        /// <summary>Converts a nullable GUID to a selection literal.</summary>
        public static implicit operator SelectingProxy(Guid? value)
            => new SelectingProxy(new ExprGuidLiteral(value));

        /// <summary>Converts a date and time to a selection literal.</summary>
        public static implicit operator SelectingProxy(DateTime value)
            => new SelectingProxy(new ExprDateTimeLiteral(value));

        /// <summary>Converts a nullable date and time to a selection literal.</summary>
        public static implicit operator SelectingProxy(DateTime? value)
            => new SelectingProxy(new ExprDateTimeLiteral(value));
    }
}
