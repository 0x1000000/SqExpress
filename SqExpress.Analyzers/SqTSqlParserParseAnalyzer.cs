using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SqExpress.Analyzers.Diagnostics;

namespace SqExpress.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SqTSqlParserParseAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                DiagnosticDescriptors.ConvertSqTSqlParserParseCall,
                DiagnosticDescriptors.SqTSqlParserParseHasInvalidSql,
                DiagnosticDescriptors.SqTSqlParserParseExistingTablesMismatch,
                DiagnosticDescriptors.SqTSqlParserParseExistingColumnsMismatch);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            if (!SqTSqlParserInvocation.TryCreate(invocation, context.SemanticModel, context.CancellationToken, out var match))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ConvertSqTSqlParserParseCall,
                    match.Invocation.GetLocation(),
                    match.MethodName));

            if (SqTSqlParserParseDiagnosticHelper.TryGetSqlParseFailureMessage(match, out var parseFailureMessage))
            {
                var target = match.Invocation.ArgumentList.Arguments.FirstOrDefault() ?? (SyntaxNode)match.Invocation;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.SqTSqlParserParseHasInvalidSql,
                        target.GetLocation(),
                        parseFailureMessage));
                return;
            }

            if (SqTSqlParserParseDiagnosticHelper.TryGetDiscoveredTablesFailureMessage(
                    context.SemanticModel,
                    match,
                    context.CancellationToken,
                    out var tableFailureMessage))
            {
                var target = match.Invocation.ArgumentList.Arguments.FirstOrDefault() ?? (SyntaxNode)match.Invocation;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.SqTSqlParserParseExistingTablesMismatch,
                        target.GetLocation(),
                        tableFailureMessage));
                return;
            }

            if (SqTSqlParserParseDiagnosticHelper.TryGetDiscoveredColumnsFailureMessage(
                    context.SemanticModel,
                    match,
                    context.CancellationToken,
                    out var columnFailureMessage))
            {
                var target = match.Invocation.ArgumentList.Arguments.FirstOrDefault() ?? (SyntaxNode)match.Invocation;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.SqTSqlParserParseExistingColumnsMismatch,
                        target.GetLocation(),
                        columnFailureMessage));
            }
        }
    }
}
