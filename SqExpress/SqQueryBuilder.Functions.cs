﻿using System;
using System.Collections.Generic;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress
{
    public static partial class SqQueryBuilder
    {
        public static ExprAggregateFunction AggregateFunction(string name, bool distinct, ExprValue expression)
            =>new ExprAggregateFunction(distinct, new ExprFunctionName(true, name), expression);

        public static AnalyticFunctionOverPartitionsBuilder AnalyticFunction(string name, ExprValue argument, params ExprValue[] rest)
            =>new AnalyticFunctionOverPartitionsBuilder(name, Helpers.Combine(argument, rest));

        public static AnalyticFunctionOverPartitionsBuilder AnalyticFunction(string name)
            =>new AnalyticFunctionOverPartitionsBuilder(name, null);

        public static AnalyticFunctionOverPartitionsFrameBuilder AnalyticFunctionFrame(string name, ExprValue argument, params ExprValue[] rest)
            =>new AnalyticFunctionOverPartitionsFrameBuilder(name, Helpers.Combine(argument, rest));

        public static ExprAnalyticFunction AnalyticFunction(string name, IReadOnlyList<ExprValue>? arguments, ExprOver over)
            =>new ExprAnalyticFunction(new ExprFunctionName(true, name), arguments, over);

        public static ExprScalarFunction ScalarFunctionSys(string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(null, new ExprFunctionName(true, name), arguments);

        public static ExprScalarFunction ScalarFunctionSys(string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(null, new ExprFunctionName(true, name), Helpers.Combine(argument1, rest));

        public static ExprScalarFunction ScalarFunctionCustom(string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        public static ExprScalarFunction ScalarFunctionCustom(string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        public static ExprScalarFunction ScalarFunctionDbCustom(string databaseName, string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        public static ExprScalarFunction ScalarFunctionDbCustom(string databaseName, string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        public static ExprTableFunction TableFunctionSys(string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(null, new ExprFunctionName(true, name), arguments);

        public static ExprTableFunction TableFunctionSys(string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(null, new ExprFunctionName(true, name), Helpers.Combine(argument1, rest));

        public static ExprTableFunction TableFunctionCustom(string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        public static ExprTableFunction TableFunctionCustom(string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        public static ExprTableFunction TableFunctionDbCustom(string databaseName, string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        public static ExprTableFunction TableFunctionDbCustom(string databaseName, string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        public static ExprAggregateOverFunction Over(this ExprAggregateFunction function, IReadOnlyList<ExprValue>? partitions = null, ExprOrderBy? order = null)
            => new ExprAggregateOverFunction(function, new ExprOver(partitions, order, null));

        public static ExprAggregateOverFunction OverOrderBy(this ExprAggregateFunction function, ExprOrderByItem item, params ExprOrderByItem[] rest) 
            => new ExprAggregateOverFunction(function, new ExprOver(null, new ExprOrderBy(Helpers.Combine(item, rest)), null));

        public static ExprAggregateOverFunction OverOrderBy(this ExprAggregateFunction function, ExprOrderBy orderBy)
            => new ExprAggregateOverFunction(function, new ExprOver(null, orderBy, null));

        public static AggregateOverFunctionOrderByBuilder OverPartitionBy(this ExprAggregateFunction function, ExprValue item, params ExprValue[] rest)
            => new AggregateOverFunctionOrderByBuilder(function, Helpers.Combine(item, rest));

        public static AggregateOverFunctionOrderByBuilder OverPartitionBy(this ExprAggregateFunction function, IReadOnlyList<ExprValue> partition)
            => new AggregateOverFunctionOrderByBuilder(function, partition);


        //Known agg and analytic functions

        public static ExprAggregateFunction CountOne() => AggregateFunction("COUNT", false, Literal(1));
        public static ExprAggregateFunction Count(ExprValue expression) => AggregateFunction("COUNT", false, expression);
        public static ExprAggregateFunction CountDistinct(ExprValue expression) => AggregateFunction("COUNT", true, expression);

        [Obsolete($"Use {nameof(Count)}().{nameof(Over)}() instead.")]
        public static ExprAnalyticFunction CountOver(ExprValue expression,params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ expression }, new ExprOver(partitions.Length == 0 ? null : partitions, null, null));
        [Obsolete($"Use {nameof(CountOne)}().{nameof(Over)}() instead.")]
        public static ExprAnalyticFunction CountOneOver(params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ Literal(1) }, new ExprOver(partitions.Length == 0 ? null : partitions, null, null));

        public static ExprAggregateFunction Min(ExprValue expression)         => AggregateFunction("MIN", false, expression);
        public static ExprAggregateFunction MinDistinct(ExprValue expression) => AggregateFunction("MIN", true, expression);

        public static ExprAggregateFunction Max(ExprValue expression)         => AggregateFunction("MAX", false, expression);
        public static ExprAggregateFunction MaxDistinct(ExprValue expression) => AggregateFunction("MAX", true, expression);

        public static ExprAggregateFunction Sum(ExprValue expression)         => AggregateFunction("SUM", false, expression);
        public static ExprAggregateFunction SumDistinct(ExprValue expression) => AggregateFunction("SUM", true, expression);

        public static ExprAggregateFunction Avg(ExprValue expression)         => AggregateFunction("AVG", false, expression);
        public static ExprAggregateFunction AvgDistinct(ExprValue expression) => AggregateFunction("AVG", true, expression);

        public static AnalyticFunctionOverPartitionsBuilder RowNumber() => AnalyticFunction("ROW_NUMBER");
        public static AnalyticFunctionOverPartitionsBuilder Rank() => AnalyticFunction("RANK");
        public static AnalyticFunctionOverPartitionsBuilder DenseRank() => AnalyticFunction("DENSE_RANK");
        public static AnalyticFunctionOverPartitionsBuilder Ntile(ExprValue value) => AnalyticFunction("NTILE", value);
        public static AnalyticFunctionOverPartitionsBuilder CumeDist() => AnalyticFunction("CUME_DIST");
        public static AnalyticFunctionOverPartitionsBuilder PercentRank() => AnalyticFunction("PERCENT_RANK");

        public static AnalyticFunctionOverPartitionsFrameBuilder FirstValue(ExprValue expr) => AnalyticFunctionFrame("FIRST_VALUE", expr);
        public static AnalyticFunctionOverPartitionsFrameBuilder LastValue(ExprValue expr) => AnalyticFunctionFrame("LAST_VALUE", expr);
        public static AnalyticFunctionOverPartitionsBuilder Lag(ExprValue expr) => AnalyticFunction("LAG", expr);
        public static AnalyticFunctionOverPartitionsBuilder Lag(ExprValue expr, ExprValue? offset, ExprValue? defaultValue = null)
        {
            List<ExprValue> arguments = new List<ExprValue>(3) {expr};

            if (!ReferenceEquals(offset,null) || !ReferenceEquals(defaultValue, null))
            {
                arguments.Add(offset ?? Null);
                if (!ReferenceEquals(defaultValue, null))
                {
                    arguments.Add(defaultValue);
                }
            }

            return new AnalyticFunctionOverPartitionsBuilder("LAG", arguments);
        }

        public static AnalyticFunctionOverPartitionsBuilder Lead(ExprValue expr) => AnalyticFunction("LEAD", expr);
        public static AnalyticFunctionOverPartitionsBuilder Lead(ExprValue expr, ExprValue? offset, ExprValue? defaultValue = null)
        {
            List<ExprValue> arguments = new List<ExprValue>(3) {expr};

            if (!ReferenceEquals(offset,null) || !ReferenceEquals(defaultValue, null))
            {
                arguments.Add(offset ?? Null);
                if (!ReferenceEquals(defaultValue, null))
                {
                    arguments.Add(defaultValue);
                }
            }

            return new AnalyticFunctionOverPartitionsBuilder("LEAD", arguments);
        }

        //Known scalar functions

        public static ExprFuncIsNull IsNull(ExprValue test, ExprValue alt) => new ExprFuncIsNull(test, alt);

        public static ExprFuncCoalesce Coalesce(ExprValue test, ExprValue alt, params ExprValue[] rest) 
            => new ExprFuncCoalesce(test, Helpers.Combine(alt, rest));

        public static ExprGetDate GetDate()=> ExprGetDate.Instance;

        public static ExprGetUtcDate GetUtcDate()=> ExprGetUtcDate.Instance;

        public static ExprDateAdd DateAdd(DateAddDatePart datePart, int number, ExprValue date) 
            => new ExprDateAdd(datePart, number, date);

        public static ExprDateDiff DateDiff(DateDiffDatePart datePart, ExprValue startDate, ExprValue endDate)
            => new ExprDateDiff(datePart, startDate, endDate);

        public readonly struct AnalyticFunctionOverPartitionsBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            internal AnalyticFunctionOverPartitionsBuilder(string name, IReadOnlyList<ExprValue>? arguments)
            {
                this._name = name;
                this._arguments = arguments;
            }

            public ExprAnalyticFunction OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(null, new ExprOrderBy(Helpers.Combine(item, rest)), null));

            public AnalyticFunctionOverOrderByBuilder OverPartitionBy(ExprValue item, params ExprValue[] rest) 
                => new AnalyticFunctionOverOrderByBuilder(this._name, this._arguments, Helpers.Combine(item, rest));
        }

        public readonly struct AggregateOverFunctionOrderByBuilder
        {
            private readonly ExprAggregateFunction _function;

            private readonly IReadOnlyList<ExprValue> _partitions;

            public AggregateOverFunctionOrderByBuilder(ExprAggregateFunction function, IReadOnlyList<ExprValue> partitions)
            {
                this._function = function;
                this._partitions = partitions;
            }

            public ExprAggregateOverFunction OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, new ExprOrderBy(Helpers.Combine(item, rest)), null));

            public ExprAggregateOverFunction OrderBy(ExprOrderBy order) =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, order, null));

            public ExprAggregateOverFunction NoOrderBy() =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, null, null));
        }

        public readonly struct AnalyticFunctionOverOrderByBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            private readonly IReadOnlyList<ExprValue> _partitions;

            internal AnalyticFunctionOverOrderByBuilder(string name, IReadOnlyList<ExprValue>? arguments, IReadOnlyList<ExprValue> partitions)
            {
                this._name = name;
                this._arguments = arguments;
                this._partitions = partitions;
            }

            public ExprAnalyticFunction OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(this._partitions, new ExprOrderBy(Helpers.Combine(item, rest)), null));
        }

        public readonly struct AnalyticFunctionOverPartitionsFrameBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            internal AnalyticFunctionOverPartitionsFrameBuilder(string name, IReadOnlyList<ExprValue>? arguments)
            {
                this._name = name;
                this._arguments = arguments;
            }

            public AnalyticFunctionOverFrameBuilder OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new AnalyticFunctionOverFrameBuilder(this._name, this._arguments, null, Helpers.Combine(item, rest));

            public AnalyticFunctionOverOrderByFrameBuilder OverPartitionBy(ExprValue item, params ExprValue[] rest) 
                => new AnalyticFunctionOverOrderByFrameBuilder(this._name, this._arguments, Helpers.Combine(item, rest));
        }

        public readonly struct AnalyticFunctionOverOrderByFrameBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            private readonly IReadOnlyList<ExprValue> _partitions;

            internal AnalyticFunctionOverOrderByFrameBuilder(string name, IReadOnlyList<ExprValue>? arguments, IReadOnlyList<ExprValue> partitions)
            {
                this._name = name;
                this._arguments = arguments;
                this._partitions = partitions;
            }

            public AnalyticFunctionOverFrameBuilder OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new AnalyticFunctionOverFrameBuilder(this._name, this._arguments, this._partitions, Helpers.Combine(item, rest));
        }

        public readonly struct AnalyticFunctionOverFrameBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            private readonly IReadOnlyList<ExprValue>? _partitions;

            private readonly IReadOnlyList<ExprOrderByItem> _orderBy;

            public AnalyticFunctionOverFrameBuilder(string name, IReadOnlyList<ExprValue>? arguments, IReadOnlyList<ExprValue>? partitions, IReadOnlyList<ExprOrderByItem> orderBy)
            {
                this._name = name;
                this._arguments = arguments;
                this._partitions = partitions;
                this._orderBy = orderBy;
            }

            public ExprAnalyticFunction FrameClause(FrameBorder start, FrameBorder? end) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(this._partitions, new ExprOrderBy(this._orderBy), new ExprFrameClause(start.BuildExpression(), end?.BuildExpression())));

            public ExprAnalyticFunction FrameClauseEmpty() =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(this._partitions, new ExprOrderBy(this._orderBy), null));
        }

        public readonly struct FrameBorder
        {
            private readonly ExprFrameBorder? _exprFrameBorder;

            private FrameBorder(ExprFrameBorder exprFrameBorder)
            {
                this._exprFrameBorder = exprFrameBorder;
            }

            internal ExprFrameBorder BuildExpression() 
                => this._exprFrameBorder ?? ExprCurrentRowFrameBorder.Instance;

            public static readonly FrameBorder UnboundedPreceding 
                = new FrameBorder(new ExprUnboundedFrameBorder(FrameBorderDirection.Preceding));

            public static readonly FrameBorder UnboundedFollowing
                = new FrameBorder(new ExprUnboundedFrameBorder(FrameBorderDirection.Following));

            public static readonly FrameBorder CurrentRow
                = new FrameBorder(ExprCurrentRowFrameBorder.Instance);

            public static FrameBorder Preceding(ExprValue value)
                => new FrameBorder(new ExprValueFrameBorder(value, FrameBorderDirection.Preceding));

            public static FrameBorder Following(ExprValue value)
                => new FrameBorder(new ExprValueFrameBorder(value, FrameBorderDirection.Following));
        }
    }
}