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
        public static ExprFuncCoalesce Coalesce(ExprValue test, ExprValue alt, params ExprValue[] rest) => new ExprFuncCoalesce(test, Helpers.Combine(alt, rest));
        public static ExprGetDate GetDate()=> ExprGetDate.Instance;
        public static ExprGetUtcDate GetUtcDate()=> ExprGetUtcDate.Instance;
    }
}