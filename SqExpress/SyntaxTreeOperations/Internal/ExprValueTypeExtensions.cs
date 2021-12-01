using SqExpress.Syntax.Value;

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal static class ExprValueTypeExtensions
    {
        public static ExprValueTypeDetails GetTypeDetails(this ExprValue exprValue)
        {
            return exprValue
                .Accept(
                    ExprValueTypeAnalyzer<ExprValueTypeDetails, object?>.Instance,
                    new ExprValueTypeAnalyzerCtx<ExprValueTypeDetails, object?>(
                        null,
                        ExprValueTypeDetailsVisitor.Instance));
        }

        public static bool? IsNullValue(this ExprValue exprValue)
        {
            return exprValue
                .Accept(
                    ExprValueTypeAnalyzer<bool?, object?>.Instance,
                    new ExprValueTypeAnalyzerCtx<bool?, object?>(
                        null,
                        ExprValueTypeIsNullVisitor.Instance));
        }
    }
}