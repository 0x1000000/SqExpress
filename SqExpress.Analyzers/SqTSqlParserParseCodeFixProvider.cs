using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace SqExpress.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SqTSqlParserParseCodeFixProvider))]
    [Shared]
    public sealed class SqTSqlParserParseCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create("SQEX001");

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
            {
                return;
            }

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>() ?? node as InvocationExpressionSyntax;
            if (invocation == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            if (!SqTSqlParserInvocation.TryCreate(invocation, semanticModel, context.CancellationToken, out _))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Convert SQL to SqExpress",
                    c => ApplyFixAsync(context.Document, diagnostic.Location.SourceSpan, c),
                    equivalenceKey: "ConvertSqlToSqExpress"),
                diagnostic);
        }

        private static async Task<Document> ApplyFixAsync(Document document, Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var node = root.FindNode(diagnosticSpan);
            var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>() ?? node as InvocationExpressionSyntax;
            if (invocation == null)
            {
                return document;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return document;
            }

            if (!SqTSqlParserInvocation.TryCreate(invocation, semanticModel, cancellationToken, out var match))
            {
                return document;
            }

            var plan = await SqTSqlParserParseCodeFixHelper.TryCreatePlanAsync(document, semanticModel, match, cancellationToken).ConfigureAwait(false);
            if (plan == null)
            {
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(plan.ReplacementRoot, plan.ReplacementExpression.WithAdditionalAnnotations(Formatter.Annotation));

            for (var i = 0; i < plan.InsertedStatements.Count; i++)
            {
                editor.InsertBefore(
                    plan.AnchorStatement,
                    plan.InsertedStatements[i]
                        .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                        .WithAdditionalAnnotations(Formatter.Annotation));
            }

            foreach (var nestedType in plan.NestedTypes)
            {
                editor.AddMember(plan.ContainingType, nestedType.WithAdditionalAnnotations(Formatter.Annotation));
            }

            var changedRoot = editor.GetChangedRoot();
            if (changedRoot is CompilationUnitSyntax compilationUnit)
            {
                changedRoot = SqTSqlParserParseCodeFixHelper.AddRequiredUsings(compilationUnit, plan.RequiredNamespaces, plan.RequiredStaticUsing)
                    .WithAdditionalAnnotations(Formatter.Annotation)
                    .WithAdditionalAnnotations(Simplifier.Annotation);
            }

            var changedDocument = document.WithSyntaxRoot(changedRoot);
            changedDocument = await Simplifier.ReduceAsync(changedDocument, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            changedDocument = await Formatter.FormatAsync(changedDocument, Formatter.Annotation, options: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return changedDocument;
        }
    }
}
