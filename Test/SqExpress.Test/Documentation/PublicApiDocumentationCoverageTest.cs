using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace SqExpress.Test.Documentation
{
    [TestFixture]
    public class PublicApiDocumentationCoverageTest
    {
        /// <summary>
        /// This is an explicit migration gate while the existing handwritten public surface is documented in batches.
        /// Remove <see cref="ExplicitAttribute"/> after the migration is complete so new undocumented public API
        /// fails the regular test suite.
        /// </summary>
        [Test]
        [Explicit("Enable after the handwritten public API documentation migration is complete.")]
        public void HandwrittenExternallyVisibleDeclarations_HaveXmlDocumentation()
        {
            var missing = new List<string>();

            foreach (var file in Directory.EnumerateFiles(GetSqExpressProjectDirectory(), "*.cs", SearchOption.AllDirectories)
                         .Where(static path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                             && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
            {
                var source = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(source, path: file);
                var root = tree.GetCompilationUnitRoot();
                var generatedRegions = GetGeneratedRegions(source, tree);

                foreach (var declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
                {
                    if (!IsDocumentable(declaration)
                        || IsInsideGeneratedRegion(declaration.SpanStart, generatedRegions)
                        || HasDocumentation(declaration))
                    {
                        continue;
                    }

                    missing.Add($"{Path.GetRelativePath(GetSqExpressProjectDirectory(), file)}:{tree.GetLineSpan(declaration.Span).StartLinePosition.Line + 1} {declaration.Kind()}");
                }
            }

            Assert.That(missing, Is.Empty, "Undocumented handwritten public API:\n" + string.Join("\n", missing));
        }

        private static bool IsDocumentable(MemberDeclarationSyntax declaration)
        {
            if (!HasExternalAccessibility(declaration))
            {
                return false;
            }

            return declaration is BaseTypeDeclarationSyntax
                or DelegateDeclarationSyntax
                or MethodDeclarationSyntax
                or ConstructorDeclarationSyntax
                or PropertyDeclarationSyntax
                or IndexerDeclarationSyntax
                or EventDeclarationSyntax
                or EventFieldDeclarationSyntax
                or FieldDeclarationSyntax
                or EnumMemberDeclarationSyntax;
        }

        private static bool HasExternalAccessibility(MemberDeclarationSyntax declaration)
        {
            if (declaration is EnumMemberDeclarationSyntax)
            {
                return declaration.Parent?.Parent is EnumDeclarationSyntax enumDeclaration
                    && IsExternallyVisibleType(enumDeclaration);
            }

            if (!HasPublicOrProtectedModifier(declaration))
            {
                return false;
            }

            for (SyntaxNode? current = declaration.Parent; current != null; current = current.Parent)
            {
                if (current is BaseTypeDeclarationSyntax type && !IsExternallyVisibleType(type))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsExternallyVisibleType(BaseTypeDeclarationSyntax declaration)
        {
            if (declaration.Parent is CompilationUnitSyntax or NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax)
            {
                return declaration.Modifiers.Any(SyntaxKind.PublicKeyword);
            }

            return HasPublicOrProtectedModifier(declaration);
        }

        private static bool HasPublicOrProtectedModifier(MemberDeclarationSyntax declaration)
            => declaration.Modifiers.Any(SyntaxKind.PublicKeyword)
                || declaration.Modifiers.Any(SyntaxKind.ProtectedKeyword);

        private static bool HasDocumentation(MemberDeclarationSyntax declaration)
        {
            var documentation = declaration.GetLeadingTrivia()
                .FirstOrDefault(static trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            if (documentation == default)
            {
                return false;
            }

            var text = documentation.ToFullString();
            return text.Contains("<inheritdoc", StringComparison.Ordinal)
                || text.Contains("<summary>", StringComparison.Ordinal);
        }

        private static IReadOnlyList<TextSpan> GetGeneratedRegions(string source, SyntaxTree tree)
        {
            var regions = new List<TextSpan>();
            var start = 0;

            while (true)
            {
                var startIndex = source.IndexOf("//CodeGenStart", start, StringComparison.Ordinal);
                if (startIndex < 0)
                {
                    return regions;
                }

                var endIndex = source.IndexOf("//CodeGenEnd", startIndex, StringComparison.Ordinal);
                Assert.That(endIndex, Is.GreaterThanOrEqualTo(0), $"Unterminated generated region in '{tree.FilePath}'.");
                var endLine = source.IndexOf('\n', endIndex);
                regions.Add(TextSpan.FromBounds(startIndex, endLine < 0 ? source.Length : endLine));
                start = endIndex + "//CodeGenEnd".Length;
            }
        }

        private static bool IsInsideGeneratedRegion(int position, IReadOnlyList<TextSpan> generatedRegions)
            => generatedRegions.Any(region => region.Contains(position));

        private static string GetSqExpressProjectDirectory()
            => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "SqExpress"));
    }
}
