using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal class TableListClassGenerator
    {
        public static CompilationUnitSyntax Generate(IReadOnlyList<TableModel> tables, string defaultNamespace, string tablePrefix)
        {
            return SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(SqExpress))))
                .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(defaultNamespace))
                    .AddMembers(GenerateAllTableList(tables, tablePrefix)))
                .NormalizeWhitespace();
        }

        private static MemberDeclarationSyntax GenerateAllTableList(IReadOnlyList<TableModel> tables, string tablePrefix)
        {
            return SyntaxFactory.ClassDeclaration("AllTables")
                .WithModifiers(SyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                .AddMembers(GenerateMethods(tables, tablePrefix))
                .NormalizeWhitespace();
        }

        private static MemberDeclarationSyntax[] GenerateMethods(IReadOnlyList<TableModel> tables, string tablePrefix)
        {
            var result = new List<MemberDeclarationSyntax>(tables.Count*2 + 2);

            var identifierAliasType = SyntaxFactory.IdentifierName(nameof(Alias));

            var arrayItems = tables.Select(t=> SyntaxFactory.IdentifierName(GetMethodName(t,tablePrefix)).Invoke(identifierAliasType.MemberAccess(nameof(Alias.Empty))));
            var arrayType = SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(nameof(TableBase)),
                new SyntaxList<ArrayRankSpecifierSyntax>(new[]
                {
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.Token(SyntaxKind.OpenBracketToken),
                        new SeparatedSyntaxList<ExpressionSyntax>(),
                        SyntaxFactory.Token(SyntaxKind.CloseBracketToken))
                }));
            var array = SyntaxFactory.ArrayCreationExpression(
                arrayType,
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                    new SeparatedSyntaxList<ExpressionSyntax>().AddRange(arrayItems))
            );

            result.Add(
                SyntaxFactory.MethodDeclaration(arrayType, "BuildAllTableList")
                    .WithModifiers(SyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                        array
                        ))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            foreach (var t in tables)
            {
                var aliasParamName = "alias";

                result.Add(SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(t.Name), GetMethodName(t, tablePrefix))
                    .WithModifiers(SyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(SyntaxHelpers.FuncParameter(aliasParamName, nameof(Alias)))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(t.Name),
                            SyntaxHelpers.ArgumentList(SyntaxFactory.IdentifierName(aliasParamName)),
                            null)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

                result.Add(SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(t.Name), GetMethodName(t, tablePrefix))
                    .WithModifiers(SyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(t.Name),
                            SyntaxHelpers.ArgumentList(identifierAliasType.MemberAccess(nameof(Alias.Auto))),
                            null)))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            }

            return result.ToArray();

            static string GetMethodName(TableModel t, string tablePrefix)
            {
                var name = t.Name;
                if (!string.IsNullOrEmpty(tablePrefix))
                {
                    name = name.Substring(tablePrefix.Length);
                }

                return "Get" + name;
            }
        }
    }
}