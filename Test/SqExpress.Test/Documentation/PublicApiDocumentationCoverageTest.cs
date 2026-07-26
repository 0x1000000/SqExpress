using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        [Test]
        [Explicit("Enable after the handwritten public API documentation migration is complete.")]
        public void HandwrittenExternallyVisibleDeclarations_HaveXmlDocumentation()
        {
            AssertDocumentation(static declaration => !HasDocumentation(declaration), "Undocumented handwritten public API");
        }

        [Test]
        public void DocumentedHandwrittenExternallyVisibleDeclarations_HaveCompleteXmlDocumentation()
        {
            AssertDocumentation(
                static declaration => HasDocumentation(declaration) && GetDocumentationProblems(declaration).Count > 0,
                "Incomplete handwritten public API documentation");
        }

        [Test]
        public void ExprExtensionExternallyVisibleDeclarations_HaveXmlDocumentation()
        {
            AssertDocumentation(
                static declaration => !HasDocumentation(declaration),
                "Undocumented ExprExtension public API",
                static path => string.Equals(Path.GetFileName(path), "ExprExtension.cs", StringComparison.Ordinal));
        }

        private static void AssertDocumentation(
            Func<MemberDeclarationSyntax, bool> shouldReport,
            string assertionMessage,
            Func<string, bool>? shouldScanFile = null)
        {
            var missing = new List<string>();

            foreach (var file in Directory.EnumerateFiles(GetSqExpressProjectDirectory(), "*.cs", SearchOption.AllDirectories)
                         .Where(static path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                             && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
            {
                if (shouldScanFile != null && !shouldScanFile(file))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(source, path: file);
                var root = tree.GetCompilationUnitRoot();
                var generatedRegions = GetGeneratedRegions(source, tree);

                foreach (var declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
                {
                    if (!IsDocumentable(declaration)
                        || IsInsideGeneratedRegion(declaration.SpanStart, generatedRegions))
                    {
                        continue;
                    }

                    if (shouldReport(declaration))
                    {
                        var problems = GetDocumentationProblems(declaration);
                        missing.Add(
                            $"{Path.GetRelativePath(GetSqExpressProjectDirectory(), file)}:{tree.GetLineSpan(declaration.Span).StartLinePosition.Line + 1} " +
                            $"{GetDeclarationName(declaration)}" +
                            (problems.Count == 0 ? null : $": {string.Join(", ", problems)}"));
                    }
                }
            }

            Assert.That(missing, Is.Empty, assertionMessage + ":\n" + string.Join("\n", missing));
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
                or EnumMemberDeclarationSyntax
                or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax;
        }

        private static bool HasExternalAccessibility(MemberDeclarationSyntax declaration)
        {
            if (declaration is EnumMemberDeclarationSyntax)
            {
                return declaration.Parent?.Parent is EnumDeclarationSyntax enumDeclaration
                    && IsExternallyVisibleType(enumDeclaration);
            }

            if (!HasPublicOrProtectedModifier(declaration) && !IsImplicitlyPublicInterfaceMember(declaration))
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

        private static bool IsImplicitlyPublicInterfaceMember(MemberDeclarationSyntax declaration)
            => declaration.Parent is InterfaceDeclarationSyntax
                && !declaration.Modifiers.Any(SyntaxKind.PrivateKeyword)
                && !declaration.Modifiers.Any(SyntaxKind.InternalKeyword);

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

        private static IReadOnlyList<string> GetDocumentationProblems(MemberDeclarationSyntax declaration)
        {
            var result = new List<string>();
            var documentation = declaration.GetLeadingTrivia()
                .FirstOrDefault(static trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            if (documentation == default)
            {
                result.Add("missing documentation");
                return result;
            }

            var text = documentation.ToFullString();
            if (text.Contains("<inheritdoc", StringComparison.Ordinal))
            {
                return result;
            }

            var summary = GetElementContent(text, "summary");
            if (string.IsNullOrWhiteSpace(summary))
            {
                result.Add("missing or empty <summary>");
            }

            foreach (var parameter in GetParameters(declaration))
            {
                if (!HasNamedElement(text, "param", parameter))
                {
                    result.Add($"missing <param name=\"{parameter}\">");
                }
            }

            foreach (var typeParameter in GetTypeParameters(declaration))
            {
                if (!HasNamedElement(text, "typeparam", typeParameter))
                {
                    result.Add($"missing <typeparam name=\"{typeParameter}\">");
                }
            }

            if (RequiresReturns(declaration) && string.IsNullOrWhiteSpace(GetElementContent(text, "returns")))
            {
                result.Add("missing or empty <returns>");
            }

            return result;
        }

        private static bool HasDocumentation(MemberDeclarationSyntax declaration)
            => declaration.GetLeadingTrivia().Any(static trivia =>
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

        private static IReadOnlyList<string> GetParameters(MemberDeclarationSyntax declaration)
            => declaration switch
            {
                BaseMethodDeclarationSyntax method => method.ParameterList.Parameters.Select(static p => p.Identifier.ValueText).ToArray(),
                DelegateDeclarationSyntax @delegate => @delegate.ParameterList.Parameters.Select(static p => p.Identifier.ValueText).ToArray(),
                IndexerDeclarationSyntax indexer => indexer.ParameterList.Parameters.Select(static p => p.Identifier.ValueText).ToArray(),
                _ => Array.Empty<string>()
            };

        private static IReadOnlyList<string> GetTypeParameters(MemberDeclarationSyntax declaration)
            => declaration switch
            {
                TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Select(static p => p.Identifier.ValueText).ToArray() ?? Array.Empty<string>(),
                MethodDeclarationSyntax method => method.TypeParameterList?.Parameters.Select(static p => p.Identifier.ValueText).ToArray() ?? Array.Empty<string>(),
                DelegateDeclarationSyntax @delegate => @delegate.TypeParameterList?.Parameters.Select(static p => p.Identifier.ValueText).ToArray() ?? Array.Empty<string>(),
                _ => Array.Empty<string>()
            };

        private static bool RequiresReturns(MemberDeclarationSyntax declaration)
            => declaration switch
            {
                MethodDeclarationSyntax method => !IsVoid(method.ReturnType),
                OperatorDeclarationSyntax => true,
                ConversionOperatorDeclarationSyntax => true,
                DelegateDeclarationSyntax @delegate => !IsVoid(@delegate.ReturnType),
                _ => false
            };

        private static bool IsVoid(TypeSyntax returnType)
            => returnType is PredefinedTypeSyntax predefined
                && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);

        private static bool HasNamedElement(string documentation, string elementName, string name)
            => Regex.IsMatch(
                documentation,
                $@"<{elementName}\s+name\s*=\s*[""']{Regex.Escape(name)}[""'][^>]*>\s*\S[\s\S]*?</{elementName}>",
                RegexOptions.CultureInvariant);

        private static string? GetElementContent(string documentation, string elementName)
        {
            var match = Regex.Match(
                documentation,
                $@"<{elementName}[^>]*>([\s\S]*?)</{elementName}>",
                RegexOptions.CultureInvariant);
            return match.Success ? Regex.Replace(match.Groups[1].Value, @"\s+", " ").Trim() : null;
        }

        private static string GetDeclarationName(MemberDeclarationSyntax declaration)
            => declaration switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
                DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                IndexerDeclarationSyntax => "this[]",
                EventDeclarationSyntax @event => @event.Identifier.ValueText,
                EventFieldDeclarationSyntax eventField => string.Join(", ", eventField.Declaration.Variables.Select(static v => v.Identifier.ValueText)),
                FieldDeclarationSyntax field => string.Join(", ", field.Declaration.Variables.Select(static v => v.Identifier.ValueText)),
                EnumMemberDeclarationSyntax enumMember => enumMember.Identifier.ValueText,
                OperatorDeclarationSyntax @operator => $"operator {@operator.OperatorToken.ValueText}",
                ConversionOperatorDeclarationSyntax conversion => $"{conversion.ImplicitOrExplicitKeyword.ValueText} operator",
                _ => declaration.Kind().ToString()
            };

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
