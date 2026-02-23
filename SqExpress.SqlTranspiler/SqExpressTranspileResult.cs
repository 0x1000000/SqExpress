using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
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
            var queryDeclaration = root.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .FirstOrDefault(i =>
                    i.Declaration.Variables.Count == 1
                    && i.Declaration.Variables[0].Initializer?.Value is InvocationExpressionSyntax);

            if (queryDeclaration == null)
            {
                return root.ToFullString();
            }

            var initializer = (InvocationExpressionSyntax)queryDeclaration.Declaration.Variables[0].Initializer!.Value;
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
