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
        public static IQuerySpecificationBuilderInitial Select(IReadOnlyList<IExprSelecting> selection) 
            => new QuerySpecificationBuilder(null, false, selection);

        public static IQuerySpecificationBuilderInitial Select(SelectingProxy selection, params SelectingProxy[] selections) 
            => new QuerySpecificationBuilder(null, false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        public static IQuerySpecificationBuilderInitial Select(ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(null, false, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectOne()
            => new QuerySpecificationBuilder(null, false, new[] { Literal(1) });

        public static IQuerySpecificationBuilderInitial SelectDistinct(SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(null, true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        public static IQuerySpecificationBuilderInitial SelectDistinct(IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(null, true, selection);

        public static IQuerySpecificationBuilderInitial SelectTop(int top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(Literal(top), false, selection);

        public static IQuerySpecificationBuilderInitial SelectTop(int top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(Literal(top), false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(top, false, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(top, false, selection);

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(Literal(top), true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(Literal(top), true, selection);

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, IReadOnlyList<IExprSelecting> selection)
            => new QuerySpecificationBuilder(top, true, selection);

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, SelectingProxy selection, params SelectingProxy[] selections)
            => new QuerySpecificationBuilder(top, true, Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy));

        public static IQuerySpecificationBuilderInitial SelectDistinct(ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(null, true, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTop(int top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(Literal(top), false, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(top, false, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(Literal(top), true, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, ExprValue selection, params ExprValue[] selections)
            => new QuerySpecificationBuilder(top, true, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTopOne()
            => new QuerySpecificationBuilder(Literal(1), false, new[] { Literal(1) });

        public static ExprOrderByItem Asc(ExprValue value)=>new ExprOrderByItem(value, false);

        public static ExprOrderByItem Desc(ExprValue value)=>new ExprOrderByItem(value, true);

        public static ExprAllColumns AllColumns() => new ExprAllColumns(null);

        public static ExprTableValueConstructor Values(IReadOnlyList<IReadOnlyList<ExprValue>> valueRows) 
            => new ExprTableValueConstructor(valueRows.SelectToReadOnlyList(i=> new ExprValueRow(i)));

        public static ExprTableValueConstructor Values(IReadOnlyList<ExprValue> values) 
            => new ExprTableValueConstructor(values.SelectToReadOnlyList(i=> new ExprValueRow(new[]{i})));

        public static ExprTableValueConstructor Values(params ExprValue[] values) 
            => new ExprTableValueConstructor(values.SelectToReadOnlyList(i=> new ExprValueRow(new[]{i})));

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
        
        public static ExprValueQuery ValueQuery(IExprSubQuery query) 
            => new ExprValueQuery(query);

        public static ExprValueQuery ValueQuery(IExprSubQueryFinal query) 
            => new ExprValueQuery(query.Done());

        public static ExprBoolean ExistsIn<TTable>(Func<TTable, ExprBoolean> on)
            where TTable : IExprTableSource, new()
        {
            var tbl = new TTable();
            return Exists(SelectOne().From(tbl).Where(on(tbl)));
        }
    }

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

        public static implicit operator SelectingProxy(ExprValue value) => new SelectingProxy(value);

        public static implicit operator SelectingProxy(ExprAllColumns value) => new SelectingProxy(value);

        public static implicit operator SelectingProxy(ExprAnalyticFunction value) => new SelectingProxy(value);

        public static implicit operator SelectingProxy(ExprAggregateFunction value) => new SelectingProxy(value);

        public static implicit operator SelectingProxy(ExprAliasedColumn value) => new SelectingProxy(value);

        public static implicit operator SelectingProxy(ExprAliasedSelecting value) => new SelectingProxy(value);

        public static implicit operator SelectingProxy(ExprColumnName value) => new SelectingProxy(value);

        //Types
        public static implicit operator SelectingProxy(string? value)
            => new SelectingProxy(new ExprStringLiteral(value));

        public static implicit operator SelectingProxy(bool value)
            => new SelectingProxy(new ExprBoolLiteral(value));

        public static implicit operator SelectingProxy(bool? value)
            => new SelectingProxy(new ExprBoolLiteral(value));

        public static implicit operator SelectingProxy(int value)
            => new SelectingProxy(new ExprInt32Literal(value));

        public static implicit operator SelectingProxy(int? value)
            => new SelectingProxy(new ExprInt32Literal(value));

        public static implicit operator SelectingProxy(byte value)
            => new SelectingProxy(new ExprByteLiteral(value));

        public static implicit operator SelectingProxy(byte? value)
            => new SelectingProxy(new ExprByteLiteral(value));

        public static implicit operator SelectingProxy(short value)
            => new SelectingProxy(new ExprInt16Literal(value));

        public static implicit operator SelectingProxy(short? value)
            => new SelectingProxy(new ExprInt16Literal(value));

        public static implicit operator SelectingProxy(long value)
            => new SelectingProxy(new ExprInt64Literal(value));

        public static implicit operator SelectingProxy(long? value)
            => new SelectingProxy(new ExprInt64Literal(value));

        public static implicit operator SelectingProxy(decimal value)
            => new SelectingProxy(new ExprDecimalLiteral(value));

        public static implicit operator SelectingProxy(decimal? value)
            => new SelectingProxy(new ExprDecimalLiteral(value));

        public static implicit operator SelectingProxy(double value)
            => new SelectingProxy(new ExprDoubleLiteral(value));

        public static implicit operator SelectingProxy(double? value)
            => new SelectingProxy(new ExprDoubleLiteral(value));

        public static implicit operator SelectingProxy(Guid value)
            => new SelectingProxy(new ExprGuidLiteral(value));

        public static implicit operator SelectingProxy(Guid? value)
            => new SelectingProxy(new ExprGuidLiteral(value));

        public static implicit operator SelectingProxy(DateTime value)
            => new SelectingProxy(new ExprDateTimeLiteral(value));

        public static implicit operator SelectingProxy(DateTime? value)
            => new SelectingProxy(new ExprDateTimeLiteral(value));
    }
}