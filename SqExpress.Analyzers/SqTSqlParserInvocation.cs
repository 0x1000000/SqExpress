using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

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
            if (!TryResolveSqlText(sqlArgument, invocation, semanticModel, cancellationToken, out var sqlText))
            {
                return false;
            }

            result = new SqTSqlParserInvocation(invocation, method.Name, sqlText);
            return true;
        }

        private static bool TryResolveSqlText(
            ExpressionSyntax sqlArgument,
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out string sqlText)
        {
            sqlText = string.Empty;

            var constant = semanticModel.GetConstantValue(sqlArgument, cancellationToken);
            if (constant.HasValue && constant.Value is string constantSql)
            {
                sqlText = constantSql;
                return true;
            }

            if (sqlArgument is not IdentifierNameSyntax identifier
                || semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol is not ILocalSymbol localSymbol)
            {
                return false;
            }

            if (invocation.FirstAncestorOrSelf<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>() is not Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax currentStatement
                || currentStatement.Parent is not BlockSyntax block)
            {
                return false;
            }

            ExpressionSyntax? lastAssignedExpression = null;
            foreach (var statement in block.Statements)
            {
                if (statement == currentStatement)
                {
                    break;
                }

                if (TryGetAssignedExpression(statement, localSymbol, semanticModel, cancellationToken, out var assignedExpression))
                {
                    lastAssignedExpression = assignedExpression;
                }
            }

            if (lastAssignedExpression == null)
            {
                return false;
            }

            var assignedConstant = semanticModel.GetConstantValue(lastAssignedExpression, cancellationToken);
            if (!assignedConstant.HasValue || assignedConstant.Value is not string assignedSql)
            {
                return false;
            }

            sqlText = assignedSql;
            return true;
        }

        private static bool TryGetAssignedExpression(
            Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax statement,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax? assignedExpression)
        {
            assignedExpression = null;

            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    foreach (var variable in localDeclaration.Declaration.Variables)
                    {
                        if (!string.Equals(variable.Identifier.ValueText, localSymbol.Name, System.StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var declaredSymbol = semanticModel.GetDeclaredSymbol(variable, cancellationToken);
                        if (!SymbolEqualityComparer.Default.Equals(declaredSymbol, localSymbol))
                        {
                            continue;
                        }

                        assignedExpression = variable.Initializer?.Value;
                        return assignedExpression != null;
                    }

                    return false;
                case ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
                    when assignment.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression):
                    if (assignment.Left is not IdentifierNameSyntax leftIdentifier)
                    {
                        return false;
                    }

                    var assignedSymbol = semanticModel.GetSymbolInfo(leftIdentifier, cancellationToken).Symbol;
                    if (!SymbolEqualityComparer.Default.Equals(assignedSymbol, localSymbol))
                    {
                        return false;
                    }

                    assignedExpression = assignment.Right;
                    return true;
                default:
                    return false;
            }
        }
    }
}
