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
        /// <summary>Builds an aggregate call whose function name is supplied by the caller.</summary>
        /// <remarks>The name is emitted as a system-function identifier and is not translated between dialects. Prefer a known helper such as <see cref="Count"/> or <see cref="Sum"/> when available.</remarks>
        /// <param name="name">The aggregate function name, without parentheses.</param>
        /// <param name="distinct">Whether to apply <c>DISTINCT</c> to the argument.</param>
        /// <param name="expression">The value aggregated across the current SQL group.</param>
        /// <returns>An aggregate expression that can also be converted to a window function with <see cref="Over(ExprAggregateFunction,IReadOnlyList{ExprValue}?,ExprOrderBy?)"/>.</returns>
        public static ExprAggregateFunction AggregateFunction(string name, bool distinct, ExprValue expression)
            =>new ExprAggregateFunction(distinct, new ExprFunctionName(true, name), expression);

        /// <summary>Starts a caller-named analytic function and requires its window specification to be completed.</summary>
        /// <remarks>The function name is emitted directly and is not translated between dialects.</remarks>
        /// <param name="name">The analytic function name, without parentheses.</param>
        /// <param name="argument">The first function argument.</param>
        /// <param name="rest">Additional function arguments in call order.</param>
        /// <returns>A builder for selecting window partitions and ordering.</returns>
        public static AnalyticFunctionOverPartitionsBuilder AnalyticFunction(string name, ExprValue argument, params ExprValue[] rest)
            =>new AnalyticFunctionOverPartitionsBuilder(name, Helpers.Combine(argument, rest));

        /// <summary>Starts a caller-named, argumentless analytic function and requires its window specification to be completed.</summary>
        /// <remarks>The function name is emitted directly and is not translated between dialects.</remarks>
        /// <param name="name">The analytic function name, without parentheses.</param>
        /// <returns>A builder for selecting window partitions and ordering.</returns>
        public static AnalyticFunctionOverPartitionsBuilder AnalyticFunction(string name)
            =>new AnalyticFunctionOverPartitionsBuilder(name, null);

        /// <summary>Starts a caller-named analytic function whose window can include an explicit frame clause.</summary>
        /// <remarks>The function name is emitted directly and is not translated between dialects.</remarks>
        /// <param name="name">The analytic function name, without parentheses.</param>
        /// <param name="argument">The first function argument.</param>
        /// <param name="rest">Additional function arguments in call order.</param>
        /// <returns>A builder for partitions, ordering, and a window frame.</returns>
        public static AnalyticFunctionOverPartitionsFrameBuilder AnalyticFunctionFrame(string name, ExprValue argument, params ExprValue[] rest)
            =>new AnalyticFunctionOverPartitionsFrameBuilder(name, Helpers.Combine(argument, rest));

        /// <summary>Builds a complete caller-named analytic-function call from an existing window specification.</summary>
        /// <remarks>This low-level overload bypasses the staged window builders. The function name is not translated between dialects.</remarks>
        /// <param name="name">The analytic function name, without parentheses.</param>
        /// <param name="arguments">Arguments in call order, or <see langword="null"/> for an argumentless call.</param>
        /// <param name="over">The partitions, ordering, and optional frame used by the <c>OVER</c> clause.</param>
        /// <returns>A complete analytic-function expression.</returns>
        public static ExprAnalyticFunction AnalyticFunction(string name, IReadOnlyList<ExprValue>? arguments, ExprOver over)
            =>new ExprAnalyticFunction(new ExprFunctionName(true, name), arguments, over);

        /// <summary>Builds an unqualified scalar-function call whose name and arguments are supplied by the caller.</summary>
        /// <remarks>The function is treated as a database/system function and is emitted without dialect translation.</remarks>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="arguments">Arguments in call order, or <see langword="null"/> for an argumentless call.</param>
        /// <returns>An unqualified scalar-function expression.</returns>
        public static ExprScalarFunction ScalarFunctionSys(string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(null, new ExprFunctionName(true, name), arguments);

        /// <summary>Builds an unqualified scalar-function call from one or more arguments.</summary>
        /// <remarks>The function name is emitted without dialect translation.</remarks>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="argument1">The first argument.</param>
        /// <param name="rest">Additional arguments in call order.</param>
        /// <returns>An unqualified scalar-function expression.</returns>
        public static ExprScalarFunction ScalarFunctionSys(string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(null, new ExprFunctionName(true, name), Helpers.Combine(argument1, rest));

        /// <summary>Builds a schema-qualified call to a user-defined scalar function.</summary>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="arguments">Arguments in call order, or <see langword="null"/> for an argumentless call.</param>
        /// <returns>A schema-qualified scalar-function expression.</returns>
        public static ExprScalarFunction ScalarFunctionCustom(string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Builds a schema-qualified call to a user-defined scalar function from one or more arguments.</summary>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="argument1">The first argument.</param>
        /// <param name="rest">Additional arguments in call order.</param>
        /// <returns>A schema-qualified scalar-function expression.</returns>
        public static ExprScalarFunction ScalarFunctionCustom(string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Builds a database- and schema-qualified call to a user-defined scalar function.</summary>
        /// <param name="databaseName">The database containing the function.</param>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="arguments">Arguments in call order, or <see langword="null"/> for an argumentless call.</param>
        /// <returns>A fully qualified scalar-function expression.</returns>
        public static ExprScalarFunction ScalarFunctionDbCustom(string databaseName, string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprScalarFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Builds a database- and schema-qualified call to a user-defined scalar function from one or more arguments.</summary>
        /// <param name="databaseName">The database containing the function.</param>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="argument1">The first argument.</param>
        /// <param name="rest">Additional arguments in call order.</param>
        /// <returns>A fully qualified scalar-function expression.</returns>
        public static ExprScalarFunction ScalarFunctionDbCustom(string databaseName, string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprScalarFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Builds an unqualified table-valued-function call whose name is supplied by the caller.</summary>
        /// <remarks>The function name is emitted without dialect translation.</remarks>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="arguments">Arguments in call order, or <see langword="null"/> for an argumentless call.</param>
        /// <returns>An unqualified table-function expression suitable for a query source.</returns>
        public static ExprTableFunction TableFunctionSys(string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(null, new ExprFunctionName(true, name), arguments);

        /// <summary>Builds an unqualified table-valued-function call from one or more arguments.</summary>
        /// <remarks>The function name is emitted without dialect translation.</remarks>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="argument1">The first argument.</param>
        /// <param name="rest">Additional arguments in call order.</param>
        /// <returns>An unqualified table-function expression suitable for a query source.</returns>
        public static ExprTableFunction TableFunctionSys(string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(null, new ExprFunctionName(true, name), Helpers.Combine(argument1, rest));

        /// <summary>Builds a schema-qualified call to a user-defined table-valued function.</summary>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="arguments">Arguments in call order, or <see langword="null"/> for an argumentless call.</param>
        /// <returns>A schema-qualified table-function expression suitable for a query source.</returns>
        public static ExprTableFunction TableFunctionCustom(string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Builds a schema-qualified call to a user-defined table-valued function from one or more arguments.</summary>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="argument1">The first argument.</param>
        /// <param name="rest">Additional arguments in call order.</param>
        /// <returns>A schema-qualified table-function expression suitable for a query source.</returns>
        public static ExprTableFunction TableFunctionCustom(string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(new ExprDbSchema(null, new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Builds a database- and schema-qualified call to a user-defined table-valued function.</summary>
        /// <param name="databaseName">The database containing the function.</param>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="arguments">Arguments in call order, or <see langword="null"/> for an argumentless call.</param>
        /// <returns>A fully qualified table-function expression suitable for a query source.</returns>
        public static ExprTableFunction TableFunctionDbCustom(string databaseName, string schemaName, string name, IReadOnlyList<ExprValue>? arguments = null)
            =>new ExprTableFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), arguments);

        /// <summary>Builds a database- and schema-qualified call to a user-defined table-valued function from one or more arguments.</summary>
        /// <param name="databaseName">The database containing the function.</param>
        /// <param name="schemaName">The schema containing the function.</param>
        /// <param name="name">The function name, without parentheses.</param>
        /// <param name="argument1">The first argument.</param>
        /// <param name="rest">Additional arguments in call order.</param>
        /// <returns>A fully qualified table-function expression suitable for a query source.</returns>
        public static ExprTableFunction TableFunctionDbCustom(string databaseName, string schemaName, string name, ExprValue argument1, params ExprValue[] rest)
            =>new ExprTableFunction(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schemaName)), new ExprFunctionName(false, name), Helpers.Combine(argument1, rest));

        /// <summary>Applies an <c>OVER</c> clause to an aggregate, optionally partitioning and ordering its window.</summary>
        /// <param name="function">The aggregate to evaluate as a window function.</param>
        /// <param name="partitions">Partition expressions, or <see langword="null"/> for a single result set partition.</param>
        /// <param name="order">Window ordering, or <see langword="null"/> when the aggregate does not require it.</param>
        /// <returns>The aggregate with a complete window specification.</returns>
        public static ExprAggregateOverFunction Over(this ExprAggregateFunction function, IReadOnlyList<ExprValue>? partitions = null, ExprOrderBy? order = null)
            => new ExprAggregateOverFunction(function, new ExprOver(partitions, order, null));

        /// <summary>Applies an ordered, unpartitioned <c>OVER</c> clause to an aggregate.</summary>
        /// <param name="function">The aggregate to evaluate as a window function.</param>
        /// <param name="item">The first window-ordering item.</param>
        /// <param name="rest">Additional window-ordering items.</param>
        /// <returns>The aggregate with an ordered window specification.</returns>
        public static ExprAggregateOverFunction OverOrderBy(this ExprAggregateFunction function, ExprOrderByItem item, params ExprOrderByItem[] rest) 
            => new ExprAggregateOverFunction(function, new ExprOver(null, new ExprOrderBy(Helpers.Combine(item, rest)), null));

        /// <summary>Applies an existing ordered, unpartitioned <c>OVER</c> clause to an aggregate.</summary>
        /// <param name="function">The aggregate to evaluate as a window function.</param>
        /// <param name="orderBy">The complete window ordering.</param>
        /// <returns>The aggregate with an ordered window specification.</returns>
        public static ExprAggregateOverFunction OverOrderBy(this ExprAggregateFunction function, ExprOrderBy orderBy)
            => new ExprAggregateOverFunction(function, new ExprOver(null, orderBy, null));

        /// <summary>Partitions an aggregate window and advances to optional ordering.</summary>
        /// <param name="function">The aggregate to evaluate as a window function.</param>
        /// <param name="item">The first partition expression.</param>
        /// <param name="rest">Additional partition expressions.</param>
        /// <returns>A builder that can add ordering or finish the unordered window.</returns>
        public static AggregateOverFunctionOrderByBuilder OverPartitionBy(this ExprAggregateFunction function, ExprValue item, params ExprValue[] rest)
            => new AggregateOverFunctionOrderByBuilder(function, Helpers.Combine(item, rest));

        /// <summary>Partitions an aggregate window with an existing expression list and advances to optional ordering.</summary>
        /// <param name="function">The aggregate to evaluate as a window function.</param>
        /// <param name="partition">The complete partition-expression list.</param>
        /// <returns>A builder that can add ordering or finish the unordered window.</returns>
        public static AggregateOverFunctionOrderByBuilder OverPartitionBy(this ExprAggregateFunction function, IReadOnlyList<ExprValue> partition)
            => new AggregateOverFunctionOrderByBuilder(function, partition);


        //Known agg and analytic functions

        /// <summary>Counts rows in each SQL group by emitting <c>COUNT(1)</c>.</summary>
        /// <returns>An aggregate that can be selected directly or converted to a window function.</returns>
        public static ExprAggregateFunction CountOne() => AggregateFunction("COUNT", false, Literal(1));
        /// <summary>Counts the non-<c>NULL</c> values of an expression in each SQL group.</summary>
        /// <param name="expression">The expression whose non-<c>NULL</c> values are counted.</param>
        /// <returns>A <c>COUNT(expression)</c> aggregate.</returns>
        public static ExprAggregateFunction Count(ExprValue expression) => AggregateFunction("COUNT", false, expression);
        /// <summary>Counts distinct non-<c>NULL</c> values of an expression in each SQL group.</summary>
        /// <param name="expression">The expression whose unique non-<c>NULL</c> values are counted.</param>
        /// <returns>A <c>COUNT(DISTINCT expression)</c> aggregate.</returns>
        public static ExprAggregateFunction CountDistinct(ExprValue expression) => AggregateFunction("COUNT", true, expression);

        /// <summary>Builds the legacy partition-only analytic <c>COUNT</c> form.</summary>
        /// <param name="expression">The expression whose non-<c>NULL</c> values are counted.</param>
        /// <param name="partitions">Optional expressions defining independent window partitions.</param>
        /// <returns>A complete analytic <c>COUNT</c> expression.</returns>
        [Obsolete($"Use {nameof(Count)}().{nameof(Over)}() instead.")]
        public static ExprAnalyticFunction CountOver(ExprValue expression,params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ expression }, new ExprOver(partitions.Length == 0 ? null : partitions, null, null));
        /// <summary>Builds the legacy partition-only analytic <c>COUNT(1)</c> form.</summary>
        /// <param name="partitions">Optional expressions defining independent window partitions.</param>
        /// <returns>A complete analytic <c>COUNT(1)</c> expression.</returns>
        [Obsolete($"Use {nameof(CountOne)}().{nameof(Over)}() instead.")]
        public static ExprAnalyticFunction CountOneOver(params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ Literal(1) }, new ExprOver(partitions.Length == 0 ? null : partitions, null, null));

        /// <summary>Selects the minimum non-<c>NULL</c> value in each SQL group.</summary>
        /// <param name="expression">The expression to compare.</param>
        /// <returns>A <c>MIN(expression)</c> aggregate.</returns>
        public static ExprAggregateFunction Min(ExprValue expression)         => AggregateFunction("MIN", false, expression);
        /// <summary>Selects the minimum value after duplicate values are removed.</summary>
        /// <param name="expression">The expression to compare.</param>
        /// <returns>A <c>MIN(DISTINCT expression)</c> aggregate.</returns>
        public static ExprAggregateFunction MinDistinct(ExprValue expression) => AggregateFunction("MIN", true, expression);

        /// <summary>Selects the maximum non-<c>NULL</c> value in each SQL group.</summary>
        /// <param name="expression">The expression to compare.</param>
        /// <returns>A <c>MAX(expression)</c> aggregate.</returns>
        public static ExprAggregateFunction Max(ExprValue expression)         => AggregateFunction("MAX", false, expression);
        /// <summary>Selects the maximum value after duplicate values are removed.</summary>
        /// <param name="expression">The expression to compare.</param>
        /// <returns>A <c>MAX(DISTINCT expression)</c> aggregate.</returns>
        public static ExprAggregateFunction MaxDistinct(ExprValue expression) => AggregateFunction("MAX", true, expression);

        /// <summary>Sums the non-<c>NULL</c> values of an expression in each SQL group.</summary>
        /// <param name="expression">The numeric expression to total.</param>
        /// <returns>A <c>SUM(expression)</c> aggregate.</returns>
        public static ExprAggregateFunction Sum(ExprValue expression)         => AggregateFunction("SUM", false, expression);
        /// <summary>Sums unique non-<c>NULL</c> values of an expression in each SQL group.</summary>
        /// <param name="expression">The numeric expression to total after duplicates are removed.</param>
        /// <returns>A <c>SUM(DISTINCT expression)</c> aggregate.</returns>
        public static ExprAggregateFunction SumDistinct(ExprValue expression) => AggregateFunction("SUM", true, expression);

        /// <summary>Calculates the database average of non-<c>NULL</c> values in each SQL group.</summary>
        /// <param name="expression">The numeric expression to average.</param>
        /// <returns>An <c>AVG(expression)</c> aggregate.</returns>
        public static ExprAggregateFunction Avg(ExprValue expression)         => AggregateFunction("AVG", false, expression);
        /// <summary>Calculates the database average after duplicate values are removed.</summary>
        /// <param name="expression">The numeric expression to average.</param>
        /// <returns>An <c>AVG(DISTINCT expression)</c> aggregate.</returns>
        public static ExprAggregateFunction AvgDistinct(ExprValue expression) => AggregateFunction("AVG", true, expression);

        /// <summary>Assigns a sequential number to each row according to a required window ordering.</summary>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder RowNumber() => AnalyticFunction("ROW_NUMBER");
        /// <summary>Ranks rows according to a required window ordering, leaving gaps after ties.</summary>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder Rank() => AnalyticFunction("RANK");
        /// <summary>Ranks rows according to a required window ordering without gaps after ties.</summary>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder DenseRank() => AnalyticFunction("DENSE_RANK");
        /// <summary>Distributes ordered rows among a requested number of numbered groups.</summary>
        /// <param name="value">The SQL expression specifying the number of groups.</param>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder Ntile(ExprValue value) => AnalyticFunction("NTILE", value);
        /// <summary>Calculates cumulative row distribution within an ordered window.</summary>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder CumeDist() => AnalyticFunction("CUME_DIST");
        /// <summary>Calculates relative rank within an ordered window.</summary>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder PercentRank() => AnalyticFunction("PERCENT_RANK");

        /// <summary>Selects the first value in an ordered window frame.</summary>
        /// <param name="expr">The expression evaluated at the first row of the frame.</param>
        /// <returns>A builder for partitions, ordering, and an optional frame clause.</returns>
        public static AnalyticFunctionOverPartitionsFrameBuilder FirstValue(ExprValue expr) => AnalyticFunctionFrame("FIRST_VALUE", expr);
        /// <summary>Selects the last value in an ordered window frame.</summary>
        /// <param name="expr">The expression evaluated at the last row of the frame.</param>
        /// <returns>A builder for partitions, ordering, and an optional frame clause.</returns>
        public static AnalyticFunctionOverPartitionsFrameBuilder LastValue(ExprValue expr) => AnalyticFunctionFrame("LAST_VALUE", expr);
        /// <summary>Reads an expression from a preceding row in an ordered window.</summary>
        /// <param name="expr">The expression read from the preceding row.</param>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder Lag(ExprValue expr) => AnalyticFunction("LAG", expr);
        /// <summary>Reads an expression from a preceding row with an optional offset and fallback value.</summary>
        /// <param name="expr">The expression read from the preceding row.</param>
        /// <param name="offset">The number of rows to look backward, or <see langword="null"/> to omit the argument unless a default is supplied.</param>
        /// <param name="defaultValue">The value returned when the target row is outside the window, or <see langword="null"/> to use the database default.</param>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
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

        /// <summary>Reads an expression from a following row in an ordered window.</summary>
        /// <param name="expr">The expression read from the following row.</param>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
        public static AnalyticFunctionOverPartitionsBuilder Lead(ExprValue expr) => AnalyticFunction("LEAD", expr);
        /// <summary>Reads an expression from a following row with an optional offset and fallback value.</summary>
        /// <param name="expr">The expression read from the following row.</param>
        /// <param name="offset">The number of rows to look forward, or <see langword="null"/> to omit the argument unless a default is supplied.</param>
        /// <param name="defaultValue">The value returned when the target row is outside the window, or <see langword="null"/> to use the database default.</param>
        /// <returns>A builder requiring the <c>OVER</c> ordering and optional partitions.</returns>
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

        /// <summary>
        /// Produces the first expression unless it is SQL <c>NULL</c>, in which case the alternative is produced.
        /// </summary>
        /// <remarks>Exports as <c>ISNULL</c> for T-SQL and as the equivalent <c>COALESCE</c> expression for other supported dialects.</remarks>
        /// <param name="test">The expression to test for SQL <c>NULL</c>.</param>
        /// <param name="alt">The expression used when <paramref name="test"/> is SQL <c>NULL</c>.</param>
        /// <returns>A portable null-replacement expression.</returns>
        public static ExprFuncIsNull IsNull(ExprValue test, ExprValue alt) => new ExprFuncIsNull(test, alt);

        /// <summary>Produces the first non-<c>NULL</c> value from an ordered set of SQL expressions.</summary>
        /// <param name="test">The first expression to evaluate.</param>
        /// <param name="alt">The second expression to evaluate.</param>
        /// <param name="rest">Additional alternatives evaluated from left to right.</param>
        /// <returns>A <c>COALESCE</c> expression containing all supplied values.</returns>
        public static ExprFuncCoalesce Coalesce(ExprValue test, ExprValue alt, params ExprValue[] rest) 
            => new ExprFuncCoalesce(test, Helpers.Combine(alt, rest));

        /// <summary>Obtains the database server's current date and time using the target dialect's native expression.</summary>
        /// <remarks>The precise clock, time-zone interpretation, and return precision are determined by the database.</remarks>
        /// <returns>A portable current-date/time expression.</returns>
        public static ExprGetDate GetDate()=> ExprGetDate.Instance;

        /// <summary>Obtains the database server's current UTC date and time using the target dialect's native expression.</summary>
        /// <remarks>The return precision is determined by the database.</remarks>
        /// <returns>A portable current-UTC-date/time expression.</returns>
        public static ExprGetUtcDate GetUtcDate()=> ExprGetUtcDate.Instance;

        /// <summary>Produces SQL <c>NULL</c> when two expressions compare equal; otherwise produces the first expression.</summary>
        /// <param name="left">The value returned when the expressions are not equal.</param>
        /// <param name="right">The value compared with <paramref name="left"/>.</param>
        /// <returns>A portable <c>NULLIF</c> expression rendered for the selected database dialect.</returns>
        public static ExprPortableScalarFunction NullIf(ExprValue left, ExprValue right)
            => new ExprPortableScalarFunction(PortableScalarFunction.NullIf, new[] { left, right });

        /// <summary>Calculates the absolute value using the selected database dialect's scalar function.</summary>
        /// <param name="value">The numeric expression whose magnitude is required.</param>
        /// <returns>A portable absolute-value expression.</returns>
        public static ExprPortableScalarFunction Abs(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Abs, new[] { value });

        /// <summary>Converts text to lowercase using the selected database dialect's scalar function.</summary>
        /// <param name="value">The string expression to convert.</param>
        /// <returns>A portable lowercase expression.</returns>
        public static ExprPortableScalarFunction Lower(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Lower, new[] { value });

        /// <summary>Converts text to uppercase using the selected database dialect's scalar function.</summary>
        /// <param name="value">The string expression to convert.</param>
        /// <returns>A portable uppercase expression.</returns>
        public static ExprPortableScalarFunction Upper(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Upper, new[] { value });

        /// <summary>Removes leading and trailing whitespace using the selected database dialect's scalar function or polyfill.</summary>
        /// <param name="value">The string expression to trim.</param>
        /// <returns>A portable trim expression.</returns>
        public static ExprPortableScalarFunction Trim(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Trim, new[] { value });

        /// <summary>Removes leading whitespace using the selected database dialect's scalar function.</summary>
        /// <param name="value">The string expression to trim.</param>
        /// <returns>A portable leading-trim expression.</returns>
        public static ExprPortableScalarFunction LTrim(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.LTrim, new[] { value });

        /// <summary>Removes trailing whitespace using the selected database dialect's scalar function.</summary>
        /// <param name="value">The string expression to trim.</param>
        /// <returns>A portable trailing-trim expression.</returns>
        public static ExprPortableScalarFunction RTrim(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.RTrim, new[] { value });

        /// <summary>Replaces occurrences of a search value using the selected database dialect's string function.</summary>
        /// <param name="value">The string expression to search.</param>
        /// <param name="search">The substring to replace.</param>
        /// <param name="replacement">The replacement text.</param>
        /// <returns>A portable string-replacement expression.</returns>
        public static ExprPortableScalarFunction Replace(ExprValue value, ExprValue search, ExprValue replacement)
            => new ExprPortableScalarFunction(PortableScalarFunction.Replace, new[] { value, search, replacement });

        /// <summary>Extracts a substring using the selected database dialect's indexing and function syntax.</summary>
        /// <param name="value">The string expression from which to extract characters.</param>
        /// <param name="start">The database-level starting position.</param>
        /// <param name="length">The number of characters to return.</param>
        /// <returns>A portable substring expression.</returns>
        public static ExprPortableScalarFunction Substring(ExprValue value, ExprValue start, ExprValue length)
            => new ExprPortableScalarFunction(PortableScalarFunction.Substring, new[] { value, start, length });

        /// <summary>Rounds a numeric expression to the requested precision using the selected database dialect.</summary>
        /// <param name="value">The numeric expression to round.</param>
        /// <param name="precision">The number of fractional decimal places; database rules govern negative values.</param>
        /// <returns>A portable numeric-rounding expression.</returns>
        public static ExprPortableScalarFunction Round(ExprValue value, ExprValue precision)
            => new ExprPortableScalarFunction(PortableScalarFunction.Round, new[] { value, precision });

        /// <summary>Returns the greatest integral value not greater than the supplied numeric expression.</summary>
        /// <param name="value">The numeric expression to round downward.</param>
        /// <returns>A portable floor expression rendered for the selected database dialect.</returns>
        public static ExprPortableScalarFunction Floor(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Floor, new[] { value });

        /// <summary>Returns the smallest integral value not less than the supplied numeric expression.</summary>
        /// <param name="value">The numeric expression to round upward.</param>
        /// <returns>A portable ceiling expression rendered for the selected database dialect.</returns>
        public static ExprPortableScalarFunction Ceiling(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Ceiling, new[] { value });

        /// <summary>Concatenates one or more expressions through the target database's <c>CONCAT</c> function.</summary>
        /// <param name="first">The first expression to concatenate.</param>
        /// <param name="rest">The remaining expressions in concatenation order.</param>
        /// <returns>A scalar-function expression containing the supplied arguments.</returns>
        public static ExprScalarFunction Concat(ExprValue first, params ExprValue[] rest)
            => ScalarFunctionSys("CONCAT", first, rest);

        /// <summary>Counts characters using the selected dialect's character-length semantics.</summary>
        /// <remarks>This is character length rather than encoded byte length; use <see cref="DataLength"/> for storage length.</remarks>
        /// <param name="value">The string expression to measure.</param>
        /// <returns>A portable character-length expression.</returns>
        public static ExprPortableScalarFunction Len(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Len, new[] { value });

        /// <summary>Obtains the storage length of a value using the selected database dialect's byte-length operation.</summary>
        /// <param name="value">The expression whose encoded or binary storage length is required.</param>
        /// <returns>A portable data-length expression.</returns>
        public static ExprPortableScalarFunction DataLength(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.DataLen, new[] { value });

        /// <summary>Extracts the year component using the selected database dialect's date-part expression.</summary>
        /// <param name="value">The date or date/time expression to inspect.</param>
        /// <returns>A portable year-extraction expression.</returns>
        public static ExprPortableScalarFunction Year(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Year, new[] { value });

        /// <summary>Extracts the month component using the selected database dialect's date-part expression.</summary>
        /// <param name="value">The date or date/time expression to inspect.</param>
        /// <returns>A portable month-extraction expression.</returns>
        public static ExprPortableScalarFunction Month(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Month, new[] { value });

        /// <summary>Extracts the day-of-month component using the selected database dialect's date-part expression.</summary>
        /// <param name="value">The date or date/time expression to inspect.</param>
        /// <returns>A portable day-extraction expression.</returns>
        public static ExprPortableScalarFunction Day(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Day, new[] { value });

        /// <summary>Extracts the hour component using the selected database dialect's date-part expression.</summary>
        /// <param name="value">The date/time expression to inspect.</param>
        /// <returns>A portable hour-extraction expression.</returns>
        public static ExprPortableScalarFunction Hour(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Hour, new[] { value });

        /// <summary>Extracts the minute component using the selected database dialect's date-part expression.</summary>
        /// <param name="value">The date/time expression to inspect.</param>
        /// <returns>A portable minute-extraction expression.</returns>
        public static ExprPortableScalarFunction Minute(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Minute, new[] { value });

        /// <summary>Extracts the second component using the selected database dialect's date-part expression.</summary>
        /// <param name="value">The date/time expression to inspect.</param>
        /// <returns>A portable second-extraction expression.</returns>
        public static ExprPortableScalarFunction Second(ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.Second, new[] { value });

        /// <summary>Finds the position of one value within another using the selected database dialect's search function.</summary>
        /// <remarks>The returned position follows SQL/database indexing conventions rather than .NET zero-based indexing.</remarks>
        /// <param name="searchValue">The value to locate.</param>
        /// <param name="value">The expression to search.</param>
        /// <returns>A portable search-position expression.</returns>
        public static ExprPortableScalarFunction IndexOf(ExprValue searchValue, ExprValue value)
            => new ExprPortableScalarFunction(PortableScalarFunction.IndexOf, new[] { searchValue, value });

        /// <summary>Returns the requested number of leftmost characters using a native function or dialect polyfill.</summary>
        /// <param name="value">The string expression to read.</param>
        /// <param name="length">The number of characters to return.</param>
        /// <returns>A portable left-substring expression.</returns>
        public static ExprPortableScalarFunction Left(ExprValue value, ExprValue length)
            => new ExprPortableScalarFunction(PortableScalarFunction.Left, new[] { value, length });

        /// <summary>Returns the requested number of rightmost characters using a native function or dialect polyfill.</summary>
        /// <param name="value">The string expression to read.</param>
        /// <param name="length">The number of characters to return.</param>
        /// <returns>A portable right-substring expression.</returns>
        public static ExprPortableScalarFunction Right(ExprValue value, ExprValue length)
            => new ExprPortableScalarFunction(PortableScalarFunction.Right, new[] { value, length });

        /// <summary>Repeats a value the requested number of times using a native function or dialect polyfill.</summary>
        /// <param name="value">The expression to repeat.</param>
        /// <param name="count">The number of repetitions.</param>
        /// <returns>A portable repetition expression.</returns>
        public static ExprPortableScalarFunction Repeat(ExprValue value, ExprValue count)
            => new ExprPortableScalarFunction(PortableScalarFunction.Repeat, new[] { value, count });

        /// <summary>Adds a calendar or time interval using the selected database dialect's date arithmetic or equivalent polyfill.</summary>
        /// <remarks>Positive values move forward and negative values move backward. Database date range and precision rules still apply.</remarks>
        /// <param name="datePart">The unit in which <paramref name="number"/> is expressed.</param>
        /// <param name="number">The signed number of units to add.</param>
        /// <param name="date">The date or date/time expression to adjust.</param>
        /// <returns>A portable date-add expression interpreted by each supported SQL exporter.</returns>
        public static ExprDateAdd DateAdd(DateAddDatePart datePart, int number, ExprValue date) 
            => new ExprDateAdd(datePart, number, date);

        /// <summary>Calculates a date-part boundary difference using native dialect syntax or an equivalent polyfill.</summary>
        /// <remarks>The result follows SQL boundary-counting semantics for the requested part; it is not a .NET <see cref="TimeSpan"/> duration.</remarks>
        /// <param name="datePart">The boundaries to count.</param>
        /// <param name="startDate">The beginning date or date/time expression.</param>
        /// <param name="endDate">The ending date or date/time expression.</param>
        /// <returns>A portable date-difference expression interpreted by each supported SQL exporter.</returns>
        public static ExprDateDiff DateDiff(DateDiffDatePart datePart, ExprValue startDate, ExprValue endDate)
            => new ExprDateDiff(datePart, startDate, endDate);

        /// <summary>Adds years using the selected database dialect's date arithmetic or equivalent polyfill.</summary>
        /// <param name="number">The signed number of years to add.</param>
        /// <param name="date">The date or date/time expression to adjust.</param>
        /// <returns>A portable year-addition expression.</returns>
        public static ExprDateAdd AddYears(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Year, number, date);

        /// <summary>Adds months using the selected database dialect's date arithmetic or equivalent polyfill.</summary>
        /// <param name="number">The signed number of months to add.</param>
        /// <param name="date">The date or date/time expression to adjust.</param>
        /// <returns>A portable month-addition expression.</returns>
        public static ExprDateAdd AddMonths(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Month, number, date);

        /// <summary>Adds days using the selected database dialect's date arithmetic or equivalent polyfill.</summary>
        /// <param name="number">The signed number of days to add.</param>
        /// <param name="date">The date or date/time expression to adjust.</param>
        /// <returns>A portable day-addition expression interpreted by the selected SQL exporter.</returns>
        public static ExprDateAdd AddDays(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Day, number, date);

        /// <summary>Adds hours using the selected database dialect's date arithmetic or equivalent polyfill.</summary>
        /// <param name="number">The signed number of hours to add.</param>
        /// <param name="date">The date/time expression to adjust.</param>
        /// <returns>A portable hour-addition expression.</returns>
        public static ExprDateAdd AddHours(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Hour, number, date);

        /// <summary>Adds minutes using the selected database dialect's date arithmetic or equivalent polyfill.</summary>
        /// <param name="number">The signed number of minutes to add.</param>
        /// <param name="date">The date/time expression to adjust.</param>
        /// <returns>A portable minute-addition expression.</returns>
        public static ExprDateAdd AddMinutes(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Minute, number, date);

        /// <summary>Adds seconds using the selected database dialect's date arithmetic or equivalent polyfill.</summary>
        /// <param name="number">The signed number of seconds to add.</param>
        /// <param name="date">The date/time expression to adjust.</param>
        /// <returns>A portable second-addition expression.</returns>
        public static ExprDateAdd AddSeconds(int number, ExprValue date)
            => DateAdd(DateAddDatePart.Second, number, date);

        /// <summary>Counts year boundaries using native dialect syntax or an equivalent polyfill.</summary>
        /// <param name="startDate">The beginning date or date/time expression.</param>
        /// <param name="endDate">The ending date or date/time expression.</param>
        /// <returns>A portable year-boundary difference expression.</returns>
        public static ExprDateDiff DiffYears(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Year, startDate, endDate);

        /// <summary>Counts month boundaries using native dialect syntax or an equivalent polyfill.</summary>
        /// <param name="startDate">The beginning date or date/time expression.</param>
        /// <param name="endDate">The ending date or date/time expression.</param>
        /// <returns>A portable month-boundary difference expression.</returns>
        public static ExprDateDiff DiffMonths(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Month, startDate, endDate);

        /// <summary>Counts day boundaries using native dialect syntax or an equivalent polyfill.</summary>
        /// <param name="startDate">The beginning date or date/time expression.</param>
        /// <param name="endDate">The ending date or date/time expression.</param>
        /// <returns>A portable day-boundary difference expression.</returns>
        public static ExprDateDiff DiffDays(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Day, startDate, endDate);

        /// <summary>Counts hour boundaries using native dialect syntax or an equivalent polyfill.</summary>
        /// <param name="startDate">The beginning date/time expression.</param>
        /// <param name="endDate">The ending date/time expression.</param>
        /// <returns>A portable hour-boundary difference expression.</returns>
        public static ExprDateDiff DiffHours(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Hour, startDate, endDate);

        /// <summary>Counts minute boundaries using native dialect syntax or an equivalent polyfill.</summary>
        /// <param name="startDate">The beginning date/time expression.</param>
        /// <param name="endDate">The ending date/time expression.</param>
        /// <returns>A portable minute-boundary difference expression.</returns>
        public static ExprDateDiff DiffMinutes(ExprValue startDate, ExprValue endDate)
            => DateDiff(DateDiffDatePart.Minute, startDate, endDate);

        /// <summary>Counts second boundaries using native dialect syntax or an equivalent polyfill.</summary>
        /// <param name="startDate">The beginning date/time expression.</param>
        /// <param name="endDate">The ending date/time expression.</param>
        /// <returns>A portable second-boundary difference expression.</returns>
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

            /// <summary>Completes the analytic function with an ordered, unpartitioned window.</summary>
            /// <param name="item">The first window-ordering item.</param>
            /// <param name="rest">Additional window-ordering items.</param>
            /// <returns>A complete analytic-function expression.</returns>
            public ExprAnalyticFunction OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(null, new ExprOrderBy(Helpers.Combine(item, rest)), null));

            /// <summary>Partitions the analytic window and advances to its required ordering stage.</summary>
            /// <param name="item">The first partition expression.</param>
            /// <param name="rest">Additional partition expressions.</param>
            /// <returns>A builder requiring one or more window-ordering items.</returns>
            public AnalyticFunctionOverOrderByBuilder OverPartitionBy(ExprValue item, params ExprValue[] rest) 
                => new AnalyticFunctionOverOrderByBuilder(this._name, this._arguments, Helpers.Combine(item, rest));
        }

        /// <summary>Selects ordering for a partitioned aggregate window function.</summary>
        public readonly struct AggregateOverFunctionOrderByBuilder
        {
            private readonly ExprAggregateFunction _function;

            private readonly IReadOnlyList<ExprValue> _partitions;

            /// <summary>Initializes the ordering stage for an aggregate and partition list.</summary>
            /// <param name="function">The aggregate evaluated by the window.</param>
            /// <param name="partitions">The expressions defining independent window partitions.</param>
            public AggregateOverFunctionOrderByBuilder(ExprAggregateFunction function, IReadOnlyList<ExprValue> partitions)
            {
                this._function = function;
                this._partitions = partitions;
            }

            /// <summary>Completes the aggregate window with one or more ordering items.</summary>
            /// <param name="item">The first window-ordering item.</param>
            /// <param name="rest">Additional window-ordering items.</param>
            /// <returns>The aggregate with a partitioned and ordered <c>OVER</c> clause.</returns>
            public ExprAggregateOverFunction OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, new ExprOrderBy(Helpers.Combine(item, rest)), null));

            /// <summary>Completes the aggregate window with an existing ordering.</summary>
            /// <param name="order">The complete window ordering.</param>
            /// <returns>The aggregate with a partitioned and ordered <c>OVER</c> clause.</returns>
            public ExprAggregateOverFunction OrderBy(ExprOrderBy order) =>
                new ExprAggregateOverFunction(this._function, new ExprOver(this._partitions, order, null));

            /// <summary>Completes the partitioned aggregate window without an ordering clause.</summary>
            /// <returns>The aggregate with a partition-only <c>OVER</c> clause.</returns>
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

            /// <summary>Completes the partitioned analytic function with its required ordering.</summary>
            /// <param name="item">The first window-ordering item.</param>
            /// <param name="rest">Additional window-ordering items.</param>
            /// <returns>A complete analytic-function expression.</returns>
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

            /// <summary>Adds ordering to an unpartitioned window and advances to the frame-clause stage.</summary>
            /// <param name="item">The first window-ordering item.</param>
            /// <param name="rest">Additional window-ordering items.</param>
            /// <returns>A builder for choosing or omitting the frame clause.</returns>
            public AnalyticFunctionOverFrameBuilder OverOrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest) =>
                new AnalyticFunctionOverFrameBuilder(this._name, this._arguments, null, Helpers.Combine(item, rest));

            /// <summary>Adds window partitions and advances to the required ordering stage.</summary>
            /// <param name="item">The first partition expression.</param>
            /// <param name="rest">Additional partition expressions.</param>
            /// <returns>A builder requiring one or more window-ordering items.</returns>
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

            /// <summary>Adds ordering to the partitioned window and advances to the frame-clause stage.</summary>
            /// <param name="item">The first window-ordering item.</param>
            /// <param name="rest">Additional window-ordering items.</param>
            /// <returns>A builder for choosing or omitting the frame clause.</returns>
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
            /// <param name="name">The analytic function name.</param>
            /// <param name="arguments">Function arguments, or <see langword="null"/> for none.</param>
            /// <param name="partitions">Partition expressions, or <see langword="null"/> for an unpartitioned window.</param>
            /// <param name="orderBy">The required window ordering.</param>
            public AnalyticFunctionOverFrameBuilder(string name, IReadOnlyList<ExprValue>? arguments, IReadOnlyList<ExprValue>? partitions, IReadOnlyList<ExprOrderByItem> orderBy)
            {
                this._name = name;
                this._arguments = arguments;
                this._partitions = partitions;
                this._orderBy = orderBy;
            }

            /// <summary>Completes the analytic function with a window frame.</summary>
            /// <param name="start">The frame's starting boundary.</param>
            /// <param name="end">The ending boundary, or <see langword="null"/> for the single-boundary form.</param>
            /// <returns>A complete analytic-function expression with an explicit frame clause.</returns>
            public ExprAnalyticFunction FrameClause(FrameBorder start, FrameBorder? end) =>
                new ExprAnalyticFunction(new ExprFunctionName(true, this._name), this._arguments, new ExprOver(this._partitions, new ExprOrderBy(this._orderBy), new ExprFrameClause(start.BuildExpression(), end?.BuildExpression())));

            /// <summary>Completes the analytic function without an explicit frame clause.</summary>
            /// <returns>A complete analytic-function expression using the database's default frame.</returns>
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

            /// <summary>Creates a frame boundary positioned a value-dependent number of rows or range units before the current row.</summary>
            /// <param name="value">The boundary offset interpreted by the containing frame clause.</param>
            /// <returns>A preceding frame boundary.</returns>
            public static FrameBorder Preceding(ExprValue value)
                => new FrameBorder(new ExprValueFrameBorder(value, FrameBorderDirection.Preceding));

            /// <summary>Creates a frame boundary positioned a value-dependent number of rows or range units after the current row.</summary>
            /// <param name="value">The boundary offset interpreted by the containing frame clause.</param>
            /// <returns>A following frame boundary.</returns>
            public static FrameBorder Following(ExprValue value)
                => new FrameBorder(new ExprValueFrameBorder(value, FrameBorderDirection.Following));
        }
    }
}
