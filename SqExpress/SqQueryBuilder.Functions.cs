using System.Collections.Generic;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress
{
    public static partial class SqQueryBuilder
    {
        public static ExprAggregateFunction AggregateFunction(string name, bool distinct, ExprValue expression)
            =>new ExprAggregateFunction(distinct, new ExprFunctionName(true, name), expression);

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

        //Known agg and analytic functions

        public static ExprAggregateFunction CountOne() => AggregateFunction("COUNT", false, Literal(1));
        public static ExprAggregateFunction Count(ExprValue expression) => AggregateFunction("COUNT", false, expression);
        public static ExprAggregateFunction CountDistinct(ExprValue expression) => AggregateFunction("COUNT", true, expression);

        public static ExprAnalyticFunction CountOver(ExprValue expression,params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ expression }, new ExprOver(partitions.Length == 0 ? null : partitions, null));
        public static ExprAnalyticFunction CountOneOver(params ExprValue[] partitions) => AnalyticFunction("COUNT", new []{ Literal(1) }, new ExprOver(partitions.Length == 0 ? null : partitions, null));

        public static ExprAggregateFunction Min(ExprValue expression)         => AggregateFunction("MIN", false, expression);
        public static ExprAggregateFunction MinDistinct(ExprValue expression) => AggregateFunction("MIN", true, expression);

        public static ExprAggregateFunction Max(ExprValue expression)         => AggregateFunction("MAX", false, expression);
        public static ExprAggregateFunction MaxDistinct(ExprValue expression) => AggregateFunction("MAX", true, expression);

        public static ExprAggregateFunction Sum(ExprValue expression)         => AggregateFunction("SUM", false, expression);
        public static ExprAggregateFunction SumDistinct(ExprValue expression) => AggregateFunction("SUM", true, expression);

        public static ExprAggregateFunction Avg(ExprValue expression)         => AggregateFunction("AVG", false, expression);
        public static ExprAggregateFunction AvgDistinct(ExprValue expression) => AggregateFunction("AVG", true, expression);

        //Known scalar functions

        public static ExprFuncIsNull IsNull(ExprValue test, ExprValue alt) => new ExprFuncIsNull(test, alt);

        public static ExprFuncCoalesce Coalesce(ExprValue test, ExprValue alt, params ExprValue[] rest) 
            => new ExprFuncCoalesce(test, Helpers.Combine(alt, rest));

        public static ExprGetDate GetDate()=> ExprGetDate.Instance;

        public static ExprGetUtcDate GetUtcDate()=> ExprGetUtcDate.Instance;

        public static ExprDateAdd DateAdd(DateAddDatePart datePart, int number, ExprValue date) 
            => new ExprDateAdd(datePart, number, date);
    }
}