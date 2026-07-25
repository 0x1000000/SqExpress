using System;
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
        /// <summary>Creates a named aggregate function over an expression.</summary>
        public static ExprAggregateFunction AggregateFunction(string name, bool distinct, ExprValue expression)
            =>new ExprAggregateFunction(distinct, new ExprFunctionName(true, name), expression);

        /// <summary>Starts a named analytic function with one or more arguments.</summary>
        public static AnalyticFunctionOverPartitionsBuilder AnalyticFunction(string name, ExprValue argument, params ExprValue[] rest)
            =>new AnalyticFunctionOverPartitionsBuilder(name, Helpers.Combine(argument, rest));

        /// <summary>Starts a named analytic function without arguments.</summary>
        public static AnalyticFunctionOverPartitionsBuilder AnalyticFunction(string name)
            =>new AnalyticFunctionOverPartitionsBuilder(name, null);

        /// <summary>Starts a named analytic function that supports a window frame clause.</summary>
        public static AnalyticFunctionOverPartitionsFrameBuilder AnalyticFunctionFrame(string name, ExprValue argument, params ExprValue[] rest)
            =>new AnalyticFunctionOverPartitionsFrameBuilder(name, Helpers.Combine(argument, rest));

        /// <summary>Creates a named analytic function with an explicit window specification.</summary>
        public static ExprAnalyticFunction AnalyticFunction(string name, IReadOnlyList<ExprValue>? arguments, ExprOver over)
            =>new ExprAnalyticFunction(new ExprFunctionName(true, name), arguments, over);

        /// <summary>Creates an unqualified system scalar-function call.</summary>
        public static ExprScalarFunction ScalarFunctionSys(string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(null, new ExprFunctionName(true, name), arguments);

        /// <summary>Creates an unqualified system scalar-function call with one or more arguments.</summary>
        public static ExprScalarFunction ScalarFunctionSys(string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(null, new ExprFunctionName(true, name), Helpers.Combine(argument1, rest));

        /// <summary>Creates a schema-qualified custom scalar-function call.</summary>
        public static ExprScalarFunction ScalarFunctionCustom(string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Creates a schema-qualified custom scalar-function call with one or more arguments.</summary>
        public static ExprScalarFunction ScalarFunctionCustom(string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Creates a database- and schema-qualified custom scalar-function call.</summary>
        public static ExprScalarFunction ScalarFunctionDbCustom(string databaseName, string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Creates a database- and schema-qualified custom scalar-function call with arguments.</summary>
        public static ExprScalarFunction ScalarFunctionDbCustom(string databaseName, string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Creates an unqualified system table-function call.</summary>
        public static ExprTableFunction TableFunctionSys(string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(null, new ExprFunctionName(true, name), arguments);

        /// <summary>Creates an unqualified system table-function call with arguments.</summary>
        public static ExprTableFunction TableFunctionSys(string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(null, new ExprFunctionName(true, name), Helpers.Combine(argument1, rest));

        /// <summary>Creates a schema-qualified custom table-function call.</summary>
        public static ExprTableFunction TableFunctionCustom(string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Creates a schema-qualified custom table-function call with arguments.</summary>
        public static ExprTableFunction TableFunctionCustom(string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Creates a database- and schema-qualified custom table-function call.</summary>
        public static ExprTableFunction TableFunctionDbCustom(string databaseName, string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Creates a database- and schema-qualified custom table-function call with arguments.</summary>
        public static ExprTableFunction TableFunctionDbCustom(string databaseName, string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Converts an aggregate to a window function with optional partitioning and ordering.</summary>
        public static ExprAggregateOverFunction Over(this ExprAggregateFunction function, IReadOnlyList<ExprValue>? partitions = null, ExprOrderBy? order = null)
            => new ExprAggregateOverFunction(function, new ExprOver(partitions, order, null));

        /// <summary>Converts an aggregate to an ordered window function.</summary>
        public static ExprAggregateOverFunction OverOrderBy(this ExprAggregateFunction function, ExprOrderByItem item, params ExprOrderByItem[] rest) 
            => new ExprAggregateOverFunction(function, new ExprOver(null, new ExprOrderBy(Helpers.Combine(item, rest)), null));

        /// <summary>Converts an aggregate to a window function using an existing ordering.</summary>
        public static ExprAggregateOverFunction OverOrderBy(this ExprAggregateFunction function, ExprOrderBy orderBy)
            => new ExprAggregateOverFunction(function, new ExprOver(null, orderBy, null));

        /// <summary>Starts a partitioned aggregate window function.</summary>
        public static AggregateOverFunctionOrderByBuilder OverPartitionBy(this ExprAggregateFunction function, ExprValue item, params ExprValue[] rest)
            => new AggregateOverFunctionOrderByBuilder(function, Helpers.Combine(item, rest));

        /// <summary>Starts a partitioned aggregate window function from a partition list.</summary>
        public static AggregateOverFunctionOrderByBuilder OverPartitionBy(this ExprAggregateFunction function, IReadOnlyList<ExprValue> partition)
            => new AggregateOverFunctionOrderByBuilder(function, partition);


        //Known agg and analytic functions

        /// <summary>Creates <c>COUNT(1)</c>.</summary>
        public static ExprAggregateFunction CountOne() => AggregateFunction("COUNT", false, Literal(1));
        /// <summary>Creates <c>COUNT(expression)</c>.</summary>
        public static ExprAggregateFunction Count(ExprValue expression) => AggregateFunction("COUNT", false, expression);
        /// <summary>Creates <c>COUNT(DISTINCT expression)</c>.</summary>
        public static ExprAggregateFunction CountDistinct(ExprValue expression) => AggregateFunction("COUNT", true, expression);

        /// <summary>Creates the legacy analytic <c>COUNT</c> form.</summary>
        [Obsolete($"Use {nameof(Count)}().{nameof(Over)}() instead.")]
        public static ExprAnalyticFunction CountOver(ExprValue expression,params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ expression }, new ExprOver(partitions.Length == 0 ? null : partitions, null, null));
        /// <summary>Creates the legacy analytic <c>COUNT(1)</c> form.</summary>
        [Obsolete($"Use {nameof(CountOne)}().{nameof(Over)}() instead.")]
        public static ExprAnalyticFunction CountOneOver(params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ Literal(1) }, new ExprOver(partitions.Length == 0 ? null : partitions, null, null));

        /// <summary>Creates <c>MIN(expression)</c>.</summary>
        public static ExprAggregateFunction Min(ExprValue expression)         => AggregateFunction("MIN", false, expression);
        /// <summary>Creates <c>MIN(DISTINCT expression)</c>.</summary>
        public static ExprAggregateFunction MinDistinct(ExprValue expression) => AggregateFunction("MIN", true, expression);

        /// <summary>Creates <c>MAX(expression)</c>.</summary>
        public static ExprAggregateFunction Max(ExprValue expression)         => AggregateFunction("MAX", false, expression);
        /// <summary>Creates <c>MAX(DISTINCT expression)</c>.</summary>
        public static ExprAggregateFunction MaxDistinct(ExprValue expression) => AggregateFunction("MAX", true, expression);

        /// <summary>Creates <c>SUM(expression)</c>.</summary>
        public static ExprAggregateFunction Sum(ExprValue expression)         => AggregateFunction("SUM", false, expression);
        /// <summary>Creates <c>SUM(DISTINCT expression)</c>.</summary>
        public static ExprAggregateFunction SumDistinct(ExprValue expression) => AggregateFunction("SUM", true, expression);

        /// <summary>Creates <c>AVG(expression)</c>.</summary>
        public static ExprAggregateFunction Avg(ExprValue expression)         => AggregateFunction("AVG", false, expression);
        /// <summary>Creates <c>AVG(DISTINCT expression)</c>.</summary>
        public static ExprAggregateFunction AvgDistinct(ExprValue expression) => AggregateFunction("AVG", true, expression);

        /// <summary>Starts a <c>ROW_NUMBER</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder RowNumber() => AnalyticFunction("ROW_NUMBER");
        /// <summary>Starts a <c>RANK</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder Rank() => AnalyticFunction("RANK");
        /// <summary>Starts a <c>DENSE_RANK</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder DenseRank() => AnalyticFunction("DENSE_RANK");
        /// <summary>Starts an <c>NTILE</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder Ntile(ExprValue value) => AnalyticFunction("NTILE", value);
        /// <summary>Starts a <c>CUME_DIST</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder CumeDist() => AnalyticFunction("CUME_DIST");
        /// <summary>Starts a <c>PERCENT_RANK</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder PercentRank() => AnalyticFunction("PERCENT_RANK");

        /// <summary>Starts a <c>FIRST_VALUE</c> analytic function with frame support.</summary>
        public static AnalyticFunctionOverPartitionsFrameBuilder FirstValue(ExprValue expr) => AnalyticFunctionFrame("FIRST_VALUE", expr);
        /// <summary>Starts a <c>LAST_VALUE</c> analytic function with frame support.</summary>
        public static AnalyticFunctionOverPartitionsFrameBuilder LastValue(ExprValue expr) => AnalyticFunctionFrame("LAST_VALUE", expr);
        /// <summary>Starts a <c>LAG</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder Lag(ExprValue expr) => AnalyticFunction("LAG", expr);
        /// <summary>Starts a <c>LAG</c> analytic function with optional offset and default value.</summary>
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

        /// <summary>Starts a <c>LEAD</c> analytic function.</summary>
        public static AnalyticFunctionOverPartitionsBuilder Lead(ExprValue expr) => AnalyticFunction("LEAD", expr);
        /// <summary>Starts a <c>LEAD</c> analytic function with optional offset and default value.</summary>
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

        /// <summary>Creates the dialect-appropriate null-replacement function.</summary>
        public static ExprFuncIsNull IsNull(ExprValue test, ExprValue alt) => new ExprFuncIsNull(test, alt);

        /// <summary>Creates a <c>COALESCE</c> expression.</summary>
        public static ExprFuncCoalesce Coalesce(ExprValue test, ExprValue alt, params ExprValue[] rest) 
            => new ExprFuncCoalesce(test, Helpers.Combine(alt, rest));

        /// <summary>Creates the dialect-appropriate current local date and time expression.</summary>
        public static ExprGetDate GetDate()=> ExprGetDate.Instance;

        /// <summary>Creates the dialect-appropriate current UTC date and time expression.</summary>
        public static ExprGetUtcDate GetUtcDate()=> ExprGetUtcDate.Instance;

        /// <summary>Creates a portable <c>NULLIF</c> function call.</summary>
        public static ExprPortableScalarFunction NullIf(ExprValue left, ExprValue right)
            => new ExprPortableScalarFunction(PortableScalarFunction.NullIf, new[] { left, right });

        /// <summary>Creates a portable absolute-value function call.</summary>
        public static ExprPortableScalarFunction Abs(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Abs, new[] { value });

        /// <summary>Creates a portable lowercase function call.</summary>
        public static ExprPortableScalarFunction Lower(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Lower, new[] { value });

        /// <summary>Creates a portable uppercase function call.</summary>
        public static ExprPortableScalarFunction Upper(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Upper, new[] { value });

        /// <summary>Creates a portable trim function call.</summary>
        public static ExprPortableScalarFunction Trim(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Trim, new[] { value });

        /// <summary>Creates a portable leading-whitespace trim function call.</summary>
        public static ExprPortableScalarFunction LTrim(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.LTrim, new[] { value });

        /// <summary>Creates a portable trailing-whitespace trim function call.</summary>
        public static ExprPortableScalarFunction RTrim(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.RTrim, new[] { value });

        /// <summary>Creates a portable string replacement function call.</summary>
        public static ExprPortableScalarFunction Replace(ExprValue value, ExprValue search, ExprValue replacement)
            => new ExprPortableScalarFunction(PortableScalarFunction.Replace, new[] { value, search, replacement });

        /// <summary>Creates a portable substring function call.</summary>
        public static ExprPortableScalarFunction Substring(ExprValue value, ExprValue start, ExprValue length)
            => new ExprPortableScalarFunction(PortableScalarFunction.Substring, new[] { value, start, length });

        /// <summary>Creates a portable numeric rounding function call.</summary>
        public static ExprPortableScalarFunction Round(ExprValue value, ExprValue precision)
            => new ExprPortableScalarFunction(PortableScalarFunction.Round, new[] { value, precision });

        /// <summary>Creates a portable floor function call.</summary>
        public static ExprPortableScalarFunction Floor(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Floor, new[] { value });

        /// <summary>Creates a portable ceiling function call.</summary>
        public static ExprPortableScalarFunction Ceiling(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Ceiling, new[] { value });

        /// <summary>Creates a <c>CONCAT</c> call from one or more expressions.</summary>
        public static ExprScalarFunction Concat(ExprValue first, params ExprValue[] rest)
            => ScalarFunctionSys("CONCAT", first, rest);

        /// <summary>Creates a portable character-length function call.</summary>
        public static ExprPortableScalarFunction Len(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Len, new[] { value });

        /// <summary>Creates a portable data-length function call.</summary>
        public static ExprPortableScalarFunction DataLength(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.DataLen, new[] { value });

        /// <summary>Extracts the year component from a date expression.</summary>
        public static ExprPortableScalarFunction Year(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Year, new[] { value });

        /// <summary>Extracts the month component from a date expression.</summary>
        public static ExprPortableScalarFunction Month(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Month, new[] { value });

        /// <summary>Extracts the day component from a date expression.</summary>
        public static ExprPortableScalarFunction Day(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Day, new[] { value });

        /// <summary>Extracts the hour component from a date/time expression.</summary>
        public static ExprPortableScalarFunction Hour(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Hour, new[] { value });

        /// <summary>Extracts the minute component from a date/time expression.</summary>
        public static ExprPortableScalarFunction Minute(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Minute, new[] { value });

        /// <summary>Extracts the second component from a date/time expression.</summary>
        public static ExprPortableScalarFunction Second(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Second, new[] { value });

        /// <summary>Creates a portable expression that finds a value within another value.</summary>
        public static ExprPortableScalarFunction IndexOf(ExprValue searchValue, ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.IndexOf, new[] { searchValue, value });

        /// <summary>Creates a portable expression returning the leftmost characters.</summary>
        public static ExprPortableScalarFunction Left(ExprValue value, ExprValue length)
            => new ExprPortableScalarFunction(PortableScalarFunction.Left, new[] { value, length });

        /// <summary>Creates a portable expression returning the rightmost characters.</summary>
        public static ExprPortableScalarFunction Right(ExprValue value, ExprValue length)
            => new ExprPortableScalarFunction(PortableScalarFunction.Right, new[] { value, length });

        /// <summary>Creates a portable expression that repeats a value.</summary>
        public static ExprPortableScalarFunction Repeat(ExprValue value, ExprValue count)
            => new ExprPortableScalarFunction(PortableScalarFunction.Repeat, new[] { value, count });

        /// <summary>Adds a specified number of date parts to a date expression.</summary>
        public static ExprDateAdd DateAdd(DateAddDatePart datePart, int number, ExprValue date) 
            => new ExprDateAdd(datePart, number, date);

        /// <summary>Calculates the dialect-appropriate boundary difference between two date expressions.</summary>
        public static ExprDateDiff DateDiff(DateDiffDatePart datePart, ExprValue startDate, ExprValue endDate)
            => new ExprDateDiff(datePart, startDate, endDate);

        /// <summary>Adds years to a date expression.</summary>
        public static ExprDateAdd AddYears(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Year, number, date);

        /// <summary>Adds months to a date expression.</summary>
        public static ExprDateAdd AddMonths(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Month, number, date);

        /// <summary>Adds days to a date expression.</summary>
        public static ExprDateAdd AddDays(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Day, number, date);

        /// <summary>Adds hours to a date expression.</summary>
        public static ExprDateAdd AddHours(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Hour, number, date);

        /// <summary>Adds minutes to a date expression.</summary>
        public static ExprDateAdd AddMinutes(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Minute, number, date);

        /// <summary>Adds seconds to a date expression.</summary>
        public static ExprDateAdd AddSeconds(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Second, number, date);

        /// <summary>Calculates the year-boundary difference between two date expressions.</summary>
        public static ExprDateDiff DiffYears(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Year, startDate, endDate);

        /// <summary>Calculates the month-boundary difference between two date expressions.</summary>
        public static ExprDateDiff DiffMonths(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Month, startDate, endDate);

        /// <summary>Calculates the day-boundary difference between two date expressions.</summary>
        public static ExprDateDiff DiffDays(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Day, startDate, endDate);

        /// <summary>Calculates the hour-boundary difference between two date expressions.</summary>
        public static ExprDateDiff DiffHours(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Hour, startDate, endDate);

        /// <summary>Calculates the minute-boundary difference between two date expressions.</summary>
        public static ExprDateDiff DiffMinutes(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Minute, startDate, endDate);

        /// <summary>Calculates the second-boundary difference between two date expressions.</summary>
        public static ExprDateDiff DiffSeconds(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Second, startDate, endDate);

        /// <summary>Selects partitioning and ordering for an analytic function.</summary>
        public readonly struct AnalyticFunctionOverPartitionsBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            internal AnalyticFunctionOverPartitionsBuilder(string name, IReadOnlyList<ExprValue>? arguments)
            {
                this._name = name;
                this._arguments = arguments;
            }

            /// <summary>Completes the analytic function with an ordered window.</summary>
            public ExprAnalyticFunction OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(null, new ExprOrderBy(Helpers.Combine(item, rest)), null));

            /// <summary>Adds window partitions and advances to the required ordering stage.</summary>
            public AnalyticFunctionOverOrderByBuilder OverPartitionBy(ExprValue item, params ExprValue[] rest) 
                => new AnalyticFunctionOverOrderByBuilder(this._name, this._arguments, Helpers.Combine(item, rest));
        }

        /// <summary>Selects ordering for a partitioned aggregate window function.</summary>
        public readonly struct AggregateOverFunctionOrderByBuilder
        {
            private readonly ExprAggregateFunction _function;

            private readonly IReadOnlyList<ExprValue> _partitions;

            /// <summary>Initializes the ordering stage for an aggregate and partition list.</summary>
            public AggregateOverFunctionOrderByBuilder(ExprAggregateFunction function, IReadOnlyList<ExprValue> partitions)
            {
                this._function = function;
                this._partitions = partitions;
            }

            /// <summary>Completes the aggregate window with one or more order-by items.</summary>
            public ExprAggregateOverFunction OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, new ExprOrderBy(Helpers.Combine(item, rest)), null));

            /// <summary>Completes the aggregate window with an existing ordering.</summary>
            public ExprAggregateOverFunction OrderBy(ExprOrderBy order) =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, order, null));

            /// <summary>Completes the partitioned aggregate window without ordering.</summary>
            public ExprAggregateOverFunction NoOrderBy() =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, null, null));
        }

        /// <summary>Requires ordering after analytic-function partitions have been selected.</summary>
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

            /// <summary>Completes the partitioned analytic function with ordering.</summary>
            public ExprAnalyticFunction OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(this._partitions, new ExprOrderBy(Helpers.Combine(item, rest)), null));
        }

        /// <summary>Selects partitioning and ordering for an analytic function that supports window frames.</summary>
        public readonly struct AnalyticFunctionOverPartitionsFrameBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            internal AnalyticFunctionOverPartitionsFrameBuilder(string name, IReadOnlyList<ExprValue>? arguments)
            {
                this._name = name;
                this._arguments = arguments;
            }

            /// <summary>Adds ordering and advances to the frame-clause stage.</summary>
            public AnalyticFunctionOverFrameBuilder OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new AnalyticFunctionOverFrameBuilder(this._name, this._arguments, null, Helpers.Combine(item, rest));

            /// <summary>Adds partitions and advances to the required ordering stage.</summary>
            public AnalyticFunctionOverOrderByFrameBuilder OverPartitionBy(ExprValue item, params ExprValue[] rest) 
                => new AnalyticFunctionOverOrderByFrameBuilder(this._name, this._arguments, Helpers.Combine(item, rest));
        }

        /// <summary>Requires ordering for a partitioned analytic function with frame support.</summary>
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

            /// <summary>Adds ordering and advances to the frame-clause stage.</summary>
            public AnalyticFunctionOverFrameBuilder OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new AnalyticFunctionOverFrameBuilder(this._name, this._arguments, this._partitions, Helpers.Combine(item, rest));
        }

        /// <summary>Selects or omits the frame clause of an ordered analytic function.</summary>
        public readonly struct AnalyticFunctionOverFrameBuilder
        {
            private readonly string _name;

            private readonly IReadOnlyList<ExprValue>? _arguments;

            private readonly IReadOnlyList<ExprValue>? _partitions;

            private readonly IReadOnlyList<ExprOrderByItem> _orderBy;

            /// <summary>Initializes an analytic frame stage from its function and window components.</summary>
            public AnalyticFunctionOverFrameBuilder(string name, IReadOnlyList<ExprValue>? arguments, IReadOnlyList<ExprValue>? partitions, IReadOnlyList<ExprOrderByItem> orderBy)
            {
                this._name = name;
                this._arguments = arguments;
                this._partitions = partitions;
                this._orderBy = orderBy;
            }

            /// <summary>Completes the analytic function with a window frame.</summary>
            public ExprAnalyticFunction FrameClause(FrameBorder start, FrameBorder? end) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(this._partitions, new ExprOrderBy(this._orderBy), new ExprFrameClause(start.BuildExpression(), end?.BuildExpression())));

            /// <summary>Completes the analytic function without a frame clause.</summary>
            public ExprAnalyticFunction FrameClauseEmpty() =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(this._partitions, new ExprOrderBy(this._orderBy), null));
        }

        /// <summary>Represents a boundary of a SQL window frame.</summary>
        public readonly struct FrameBorder
        {
            private readonly ExprFrameBorder? _exprFrameBorder;

            private FrameBorder(ExprFrameBorder exprFrameBorder)
            {
                this._exprFrameBorder = exprFrameBorder;
            }

            internal ExprFrameBorder BuildExpression() 
                => this._exprFrameBorder ?? ExprCurrentRowFrameBorder.Instance;

            /// <summary>Gets the unbounded-preceding frame boundary.</summary>
            public static readonly FrameBorder UnboundedPreceding 
                = new FrameBorder(new ExprUnboundedFrameBorder(FrameBorderDirection.Preceding));

            /// <summary>Gets the unbounded-following frame boundary.</summary>
            public static readonly FrameBorder UnboundedFollowing
                = new FrameBorder(new ExprUnboundedFrameBorder(FrameBorderDirection.Following));

            /// <summary>Gets the current-row frame boundary.</summary>
            public static readonly FrameBorder CurrentRow
                = new FrameBorder(ExprCurrentRowFrameBorder.Instance);

            /// <summary>Creates a value-based preceding frame boundary.</summary>
            public static FrameBorder Preceding(ExprValue value)
                => new FrameBorder(new ExprValueFrameBorder(value, FrameBorderDirection.Preceding));

            /// <summary>Creates a value-based following frame boundary.</summary>
            public static FrameBorder Following(ExprValue value)
                => new FrameBorder(new ExprValueFrameBorder(value, FrameBorderDirection.Following));
        }
    }
}
