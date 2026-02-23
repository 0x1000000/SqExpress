using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressTranspileResult
    {
        public SqExpressTranspileResult(
            string statementKind,
            CompilationUnitSyntax queryAst,
            CompilationUnitSyntax declarationsAst)
        {
            this.StatementKind = statementKind;
            this.QueryAst = queryAst;
            this.DeclarationsAst = declarationsAst;
            this.QueryCSharpCode = FormatQueryCode(queryAst);
            this.DeclarationsCSharpCode = declarationsAst.ToFullString();
        }

        public string StatementKind { get; }

        public CompilationUnitSyntax QueryAst { get; }

        public CompilationUnitSyntax DeclarationsAst { get; }

        public string QueryCSharpCode { get; }

        public string DeclarationsCSharpCode { get; }

        //Backwards compatibility
        public CompilationUnitSyntax Ast => this.QueryAst;

        //Backwards compatibility
        public string CSharpCode => this.QueryCSharpCode;

        private static string FormatQueryCode(CompilationUnitSyntax queryAst)
        {
            var root = queryAst;
            var queryDeclaration = FindQueryInvocationDeclaration(root);

            if (queryDeclaration == null)
            {
                return root.ToFullString();
            }

            var queryVariableName = queryDeclaration.Declaration.Variables[0].Identifier.ValueText;
            root = WrapLongSelectArguments(root, queryVariableName);
            root = WrapComplexWhereClauses(root, queryVariableName);

            queryDeclaration = FindQueryInvocationDeclaration(root, queryVariableName);
            if (queryDeclaration?.Declaration.Variables[0].Initializer?.Value is not InvocationExpressionSyntax initializer)
            {
                return root.ToFullString();
            }

            var dotTokens = GetFluentDotTokens(initializer);
            if (dotTokens.Count == 0)
            {
                return root.ToFullString();
            }

            var formatted = root.ReplaceTokens(
                dotTokens,
                static (_, token) => token.WithLeadingTrivia(CarriageReturnLineFeed, Whitespace("                ")));

            return formatted.ToFullString();
        }

        private static LocalDeclarationStatementSyntax? FindQueryInvocationDeclaration(CompilationUnitSyntax root, string? preferredVariableName = null)
        {
            var candidates = root.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .Where(i =>
                    i.Declaration.Variables.Count == 1
                    && i.Declaration.Variables[0].Initializer?.Value is InvocationExpressionSyntax)
                .Select(i => new
                {
                    Node = i,
                    VariableName = i.Declaration.Variables[0].Identifier.ValueText,
                    Invocation = (InvocationExpressionSyntax)i.Declaration.Variables[0].Initializer!.Value
                })
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(preferredVariableName))
            {
                var preferred = candidates.FirstOrDefault(i => string.Equals(i.VariableName, preferredVariableName, StringComparison.Ordinal));
                if (preferred != null)
                {
                    return preferred.Node;
                }
            }

            return candidates
                .OrderByDescending(i => GetFluentDotTokens(i.Invocation).Count)
                .First()
                .Node;
        }

        private static CompilationUnitSyntax WrapLongSelectArguments(CompilationUnitSyntax root, string queryVariableName)
        {
            var queryDeclaration = FindQueryInvocationDeclaration(root, queryVariableName);
            if (queryDeclaration?.Declaration.Variables[0].Initializer?.Value is not InvocationExpressionSyntax queryInvocation)
            {
                return root;
            }

            var baseInvocation = GetFluentBaseInvocation(queryInvocation);
            if (baseInvocation == null || !TryGetSelectMethodName(baseInvocation, out var methodName))
            {
                return root;
            }

            if (!methodName.StartsWith("Select", StringComparison.Ordinal))
            {
                return root;
            }

            var args = baseInvocation.ArgumentList.Arguments;
            if (args.Count < 3)
            {
                return root;
            }

            var wrappedArgs = new List<SyntaxNodeOrToken>(args.Count * 2 - 1);
            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (i == 0)
                {
                    arg = arg.WithLeadingTrivia(CarriageReturnLineFeed, Whitespace("                    "));
                }
                else
                {
                    arg = arg.WithLeadingTrivia(default(SyntaxTriviaList));
                }

                wrappedArgs.Add(arg);
                if (i < args.Count - 1)
                {
                    var comma = baseInvocation.ArgumentList.Arguments.GetSeparator(i)
                        .WithTrailingTrivia(CarriageReturnLineFeed, Whitespace("                    "));
                    wrappedArgs.Add(comma);
                }
            }

            var wrappedArgumentList = baseInvocation.ArgumentList.WithArguments(SeparatedList<ArgumentSyntax>(wrappedArgs));
            var wrappedBaseInvocation = baseInvocation.WithArgumentList(wrappedArgumentList);
            return root.ReplaceNode(baseInvocation, wrappedBaseInvocation);
        }

        private static CompilationUnitSyntax WrapComplexWhereClauses(CompilationUnitSyntax root, string queryVariableName)
        {
            var queryDeclaration = FindQueryInvocationDeclaration(root, queryVariableName);
            if (queryDeclaration?.Declaration.Variables[0].Initializer?.Value is not InvocationExpressionSyntax queryInvocation)
            {
                return root;
            }

            var fluentInvocations = GetFluentInvocations(queryInvocation);
            foreach (var invocation in fluentInvocations)
            {
                if (!IsWhereInvocation(invocation) || invocation.ArgumentList.Arguments.Count != 1)
                {
                    continue;
                }

                var arg = invocation.ArgumentList.Arguments[0];
                if (!IsComplexWhereExpression(arg.Expression))
                {
                    continue;
                }

                var expressionWithWrappedOperators = WrapBooleanOperators(arg.Expression);
                var wrappedArg = arg
                    .WithExpression(expressionWithWrappedOperators)
                    .WithLeadingTrivia(CarriageReturnLineFeed, Whitespace("                    "))
                    .WithTrailingTrivia(CarriageReturnLineFeed, Whitespace("                "));
                var wrappedArgumentList = invocation.ArgumentList.WithArguments(SingletonSeparatedList(wrappedArg));
                var wrappedInvocation = invocation.WithArgumentList(wrappedArgumentList);
                root = root.ReplaceNode(invocation, wrappedInvocation);
            }

            return root;
        }

        private static InvocationExpressionSyntax? GetFluentBaseInvocation(InvocationExpressionSyntax expression)
        {
            ExpressionSyntax current = expression;
            while (current is InvocationExpressionSyntax invocation
                   && invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                current = memberAccess.Expression;
            }

            return current as InvocationExpressionSyntax;
        }

        private static bool TryGetSelectMethodName(InvocationExpressionSyntax invocation, out string methodName)
        {
            methodName = string.Empty;
            if (invocation.Expression is IdentifierNameSyntax identifier)
            {
                methodName = identifier.Identifier.ValueText;
                return true;
            }

            return false;
        }

        private static IReadOnlyList<InvocationExpressionSyntax> GetFluentInvocations(InvocationExpressionSyntax expression)
        {
            var result = new List<InvocationExpressionSyntax>();
            ExpressionSyntax current = expression;

            while (current is InvocationExpressionSyntax invocation)
            {
                result.Add(invocation);
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    break;
                }

                current = memberAccess.Expression;
            }

            return result;
        }

        private static bool IsWhereInvocation(InvocationExpressionSyntax invocation)
            => invocation.Expression is MemberAccessExpressionSyntax memberAccess
               && memberAccess.Name is IdentifierNameSyntax name
               && string.Equals(name.Identifier.ValueText, "Where", StringComparison.Ordinal);

        private static bool IsComplexWhereExpression(ExpressionSyntax expression)
        {
            var booleanBinaryCount = expression.DescendantNodesAndSelf()
                .OfType<BinaryExpressionSyntax>()
                .Count(i =>
                    i.IsKind(SyntaxKind.BitwiseAndExpression)
                    || i.IsKind(SyntaxKind.BitwiseOrExpression)
                    || i.IsKind(SyntaxKind.LogicalAndExpression)
                    || i.IsKind(SyntaxKind.LogicalOrExpression));

            if (booleanBinaryCount >= 2 || expression.ToFullString().Length > 100)
            {
                return true;
            }

            return booleanBinaryCount >= 1 && expression.ToFullString().Length > 50;
        }

        private static ExpressionSyntax WrapBooleanOperators(ExpressionSyntax expression)
        {
            var operators = expression.DescendantTokens()
                .Where(i => i.IsKind(SyntaxKind.AmpersandToken) || i.IsKind(SyntaxKind.BarToken))
                .ToList();

            if (operators.Count == 0)
            {
                return expression;
            }

            return expression.ReplaceTokens(
                operators,
                static (_, token) => token
                    .WithLeadingTrivia(CarriageReturnLineFeed, Whitespace("                    "))
                    .WithTrailingTrivia(Whitespace(" ")));
        }

        private static IReadOnlyList<SyntaxToken> GetFluentDotTokens(InvocationExpressionSyntax expression)
        {
            var result = new List<SyntaxToken>();
            ExpressionSyntax current = expression;

            while (current is InvocationExpressionSyntax invocation
                   && invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                result.Add(memberAccess.OperatorToken);
                current = memberAccess.Expression;
            }

            return result;
        }
    }
}
