using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using SqExpress.Analyzers.Diagnostics;

namespace SqExpress.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SqJsonPathAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.InvalidJsonPath);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
            return;
        if (context.SemanticModel.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation)
            return;
        foreach (var argument in operation.Arguments)
        {
            if (argument.Parameter?.Type.Name != "SqJsonPath" || argument.Parameter.Type.ContainingNamespace.ToDisplayString() != "SqExpress")
                continue;
            if (!TryGetConstantString(argument.Syntax is ArgumentSyntax syntax ? syntax.Expression : argument.Syntax, context, out var path))
                continue;
            if (!TryValidate(path, out var position, out var expected))
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidJsonPath, argument.Syntax.GetLocation(), position, expected));
        }
    }

    private static bool TryGetConstantString(SyntaxNode expression, SyntaxNodeAnalysisContext context, out string value)
    {
        var constant = context.SemanticModel.GetConstantValue(expression, context.CancellationToken);
        if (constant.HasValue && constant.Value is string text)
        {
            value = text;
            return true;
        }

        if (expression is CastExpressionSyntax cast)
            return TryGetConstantString(cast.Expression, context, out value);
        if (expression is ParenthesizedExpressionSyntax parenthesized)
            return TryGetConstantString(parenthesized.Expression, context, out value);

        value = string.Empty;
        return false;
    }

    private static bool TryValidate(string path, out int position, out string expected)
    {
        position = 0; expected = "expected '$'";
        if (path.Length == 0 || path[0] != '$') return false;
        position = 1;
        while (position < path.Length)
        {
            if (path[position] == '.')
            {
                position++;
                if (position >= path.Length) { expected = "expected a property name"; return false; }
                if (path[position] == '"')
                {
                    position++;
                    var any = false;
                    while (position < path.Length && path[position] != '"')
                    {
                        any = true;
                        if (path[position++] == '\\') { if (position >= path.Length) { expected = "expected an escaped character"; return false; } position++; }
                    }
                    if (position >= path.Length) { expected = "expected a closing quote"; return false; }
                    if (!any) { expected = "expected a property name"; return false; }
                    position++;
                }
                else
                {
                    if (!(char.IsLetter(path[position]) || path[position] == '_')) { expected = "expected a property name"; return false; }
                    while (++position < path.Length && (char.IsLetterOrDigit(path[position]) || path[position] == '_')) { }
                }
            }
            else if (path[position] == '[')
            {
                position++;
                var start = position;
                while (position < path.Length && char.IsDigit(path[position])) position++;
                if (position == start) { expected = "expected a nonnegative array index"; return false; }
                if (position >= path.Length || path[position] != ']') { expected = "expected ']'"; return false; }
                position++;
            }
            else { expected = "expected '.' or '['"; return false; }
        }
        return true;
    }
}
