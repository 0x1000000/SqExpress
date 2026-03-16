using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqExpress.Analyzers
{
    internal readonly struct SqTSqlParserInvocation
    {
        public SqTSqlParserInvocation(InvocationExpressionSyntax invocation, string methodName, string sqlText)
        {
            this.Invocation = invocation;
            this.MethodName = methodName;
            this.SqlText = sqlText;
        }

        public InvocationExpressionSyntax Invocation { get; }

        public string MethodName { get; }

        public string SqlText { get; }

        public static bool TryCreate(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out SqTSqlParserInvocation result)
        {
            result = default;

            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                return false;
            }

            if (method.Name != "Parse")
            {
                return false;
            }

            if (method.ContainingType.Name != "SqTSqlParser"
                || method.ContainingNamespace.ToDisplayString() != "SqExpress.SqlParser")
            {
                return false;
            }

            if (invocation.ArgumentList.Arguments.Count < 1)
            {
                return false;
            }

            var sqlArgument = invocation.ArgumentList.Arguments[0].Expression;
            var constant = semanticModel.GetConstantValue(sqlArgument, cancellationToken);
            if (!constant.HasValue || constant.Value is not string sqlText)
            {
                return false;
            }

            result = new SqTSqlParserInvocation(invocation, method.Name, sqlText);
            return true;
        }
    }
}
