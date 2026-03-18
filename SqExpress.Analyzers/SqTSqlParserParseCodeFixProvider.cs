using System;
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
        private const string SqexErrorPrefix = "#error SQEX:";

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

            var scopeNode = GetErrorScopeNode(invocation);
            if (scopeNode == null)
            {
                return document;
            }

            var invocationAnnotation = new SyntaxAnnotation("sqex", "invocation");
            var scopeAnnotation = new SyntaxAnnotation("sqex", "scope");
            var trackedRoot = root.TrackNodes(invocation, scopeNode);
            var currentInvocation = trackedRoot.GetCurrentNode(invocation);
            var currentScope = trackedRoot.GetCurrentNode(scopeNode);
            if (currentInvocation == null || currentScope == null)
            {
                return document;
            }

            var annotatedScope = currentScope
                .ReplaceNode(currentInvocation, currentInvocation.WithAdditionalAnnotations(invocationAnnotation))
                .WithAdditionalAnnotations(scopeAnnotation);
            trackedRoot = trackedRoot.ReplaceNode(currentScope, annotatedScope);

            currentScope = trackedRoot.GetAnnotatedNodes(scopeAnnotation).FirstOrDefault();
            if (currentScope == null)
            {
                return document;
            }

            var cleanedRoot = RemoveSqexErrors(trackedRoot, currentScope);
            var cleanedInvocation = cleanedRoot.GetAnnotatedNodes(invocationAnnotation).OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (cleanedInvocation == null)
            {
                return document;
            }

            document = document.WithSyntaxRoot(cleanedRoot);
            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            cleanedInvocation = root.GetAnnotatedNodes(invocationAnnotation).OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (cleanedInvocation == null)
            {
                return document;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return document;
            }

            if (!SqTSqlParserInvocation.TryCreate(cleanedInvocation, semanticModel, cancellationToken, out var match))
            {
                return document;
            }

            var anchorStatement = cleanedInvocation.FirstAncestorOrSelf<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>();
            var plan = await SqTSqlParserParseCodeFixHelper.TryCreatePlanAsync(document, semanticModel, match, cancellationToken).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (plan != null)
            {
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
            }
            else if (anchorStatement != null)
            {
                var failureMessage = await SqTSqlParserParseCodeFixHelper.GetConversionFailureMessageAsync(document, semanticModel, match, cancellationToken).ConfigureAwait(false);
                InsertSqexError(editor, anchorStatement, failureMessage);
            }

            var changedRoot = editor.GetChangedRoot();
            if (plan != null && changedRoot is CompilationUnitSyntax compilationUnit)
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

        private static SyntaxNode? GetErrorScopeNode(SyntaxNode node)
            => (SyntaxNode?)node.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>()
               ?? (SyntaxNode?)node.FirstAncestorOrSelf<AccessorDeclarationSyntax>()
               ?? (SyntaxNode?)node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>()
               ?? (SyntaxNode?)node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();

        private static SyntaxNode RemoveSqexErrors(SyntaxNode root, SyntaxNode scopeNode)
        {
            var tokensToReplace = scopeNode.DescendantTokens(descendIntoTrivia: true)
                .Where(i => !FilterSqexErrorTrivia(i.LeadingTrivia).Equals(i.LeadingTrivia)
                            || !FilterSqexErrorTrivia(i.TrailingTrivia).Equals(i.TrailingTrivia))
                .ToList();

            if (tokensToReplace.Count < 1)
            {
                return root;
            }

            return root.ReplaceTokens(
                tokensToReplace,
                (original, _) => original
                    .WithLeadingTrivia(FilterSqexErrorTrivia(original.LeadingTrivia))
                    .WithTrailingTrivia(FilterSqexErrorTrivia(original.TrailingTrivia)));
        }

        private static SyntaxTriviaList FilterSqexErrorTrivia(SyntaxTriviaList triviaList)
        {
            if (triviaList.Count < 1)
            {
                return triviaList;
            }

            var builder = new SyntaxTriviaList();
            for (var i = 0; i < triviaList.Count; i++)
            {
                var trivia = triviaList[i];
                if (IsSqexErrorTrivia(trivia))
                {
                    if (i + 1 < triviaList.Count && triviaList[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        i++;
                    }

                    continue;
                }

                builder = builder.Add(trivia);
            }

            return builder;
        }

        private static bool IsSqexErrorTrivia(SyntaxTrivia trivia)
            => trivia.GetStructure() is ErrorDirectiveTriviaSyntax errorDirective
               && errorDirective.ToFullString().TrimStart().StartsWith(SqexErrorPrefix, StringComparison.Ordinal);

        private static void InsertSqexError(DocumentEditor editor, Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax anchorStatement, string failureMessage)
        {
            var message = SanitizeSqexErrorMessage(failureMessage);
            var directiveTrivia = SyntaxFactory.ParseLeadingTrivia(SqexErrorPrefix + " " + message + "\r\n");
            editor.ReplaceNode(
                anchorStatement,
                anchorStatement.WithLeadingTrivia(directiveTrivia.Concat(anchorStatement.GetLeadingTrivia())));
        }

        private static string SanitizeSqexErrorMessage(string failureMessage)
        {
            var normalized = failureMessage
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "Unknown conversion error.";
            }

            return "Could not convert SQL to SqExpress: " + normalized;
        }
    }
}
