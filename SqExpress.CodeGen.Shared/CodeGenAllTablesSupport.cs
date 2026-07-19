using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGen.Shared
{
    internal static class CodeGenAllTablesSupport
    {
        private const string AllTablesClassName = "AllTables";

        public static CompilationUnitSyntax Generate(
            string existingFilePath,
            IReadOnlyList<TableModel> tables,
            string defaultNamespace,
            string tablePrefix,
            IFileSystem fileSystem,
            IReadOnlyDictionary<TableRef, string>? tableNamespaces = null,
            IReadOnlyDictionary<TableRef, string>? schemaSegments = null)
        {
            CompilationUnitSyntax? modifiedUnit = null;
            if (fileSystem.FileExists(existingFilePath))
            {
                var tree = CSharpSyntaxTree.ParseText(fileSystem.ReadAllText(existingFilePath));
                var existingClassSyntax = tree
                    .GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(static f => f.Identifier.ValueText == AllTablesClassName);

                if (existingClassSyntax != null)
                {
                    modifiedUnit = existingClassSyntax.FindParentOrDefault<CompilationUnitSyntax>()
                                   ?? throw new InvalidOperationException($"Could not find compilation unit for {existingClassSyntax.Identifier.ValueText}");

                    modifiedUnit = modifiedUnit.ReplaceNode(existingClassSyntax, GenerateAllTableList(tables, tablePrefix, existingClassSyntax, tableNamespaces, schemaSegments));
                }
            }

            return EnsureUsings(modifiedUnit ?? SyntaxFactory.CompilationUnit()
                    .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(defaultNamespace))
                        .AddMembers(GenerateAllTableList(tables, tablePrefix, null, tableNamespaces, schemaSegments))))
                .NormalizeWhitespace();
        }

        private static ClassDeclarationSyntax GenerateAllTableList(
            IReadOnlyList<TableModel> tables,
            string tablePrefix,
            ClassDeclarationSyntax? oldClass,
            IReadOnlyDictionary<TableRef, string>? tableNamespaces,
            IReadOnlyDictionary<TableRef, string>? schemaSegments)
        {
            return SyntaxFactory.ClassDeclaration(AllTablesClassName)
                .WithModifiers(oldClass?.Modifiers ?? CodeGenSyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                .AddMembers(GenerateMethods(tables, tablePrefix, tableNamespaces, schemaSegments))
                .NormalizeWhitespace();
        }

        private static MemberDeclarationSyntax[] GenerateMethods(
            IReadOnlyList<TableModel> tables,
            string tablePrefix,
            IReadOnlyDictionary<TableRef, string>? tableNamespaces,
            IReadOnlyDictionary<TableRef, string>? schemaSegments)
        {
            var result = new List<MemberDeclarationSyntax>(tables.Count * 2 + 4);

            var aliasType = SyntaxFactory.IdentifierName(nameof(Alias));
            var tableBaseType = SyntaxFactory.IdentifierName(nameof(TableBase));

            var arrayItems = tables.Select(t => SyntaxFactory.IdentifierName(GetMethodName(t, tablePrefix, schemaSegments)).Invoke(aliasType.MemberAccess(nameof(Alias.Empty))));
            var arrayType = SyntaxFactory.ArrayType(
                tableBaseType,
                new SyntaxList<ArrayRankSpecifierSyntax>(new[]
                {
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.Token(SyntaxKind.OpenBracketToken),
                        new SeparatedSyntaxList<ExpressionSyntax>(),
                        SyntaxFactory.Token(SyntaxKind.CloseBracketToken))
                }));
            var array = SyntaxFactory.ArrayCreationExpression(
                arrayType,
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    new SeparatedSyntaxList<ExpressionSyntax>().AddRange(arrayItems)));

            result.Add(
                SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.GenericName("IReadOnlyList")
                                    .AddTypeArgumentListArguments(tableBaseType))
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator("StaticList")
                                    .WithInitializer(
                                        SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(nameof(Array)),
                                                    SyntaxFactory.IdentifierName(nameof(Array.AsReadOnly)))
                                                .Invoke(SyntaxFactory.IdentifierName("BuildAllTableList").Invoke())))))
                    .WithModifiers(CodeGenSyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword, SyntaxKind.ReadOnlyKeyword)));

            result.Add(
                SyntaxFactory.MethodDeclaration(arrayType, "BuildAllTableList")
                    .WithModifiers(CodeGenSyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(array))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            var aliasedArrayItems = tables.Select(t => SyntaxFactory.IdentifierName(GetMethodName(t, tablePrefix, schemaSegments)).Invoke());
            var aliasedArray = SyntaxFactory.ArrayCreationExpression(
                arrayType,
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    new SeparatedSyntaxList<ExpressionSyntax>().AddRange(aliasedArrayItems)));

            result.Add(
                SyntaxFactory.MethodDeclaration(arrayType, "BuildAllAliasedTableList")
                    .WithModifiers(CodeGenSyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(aliasedArray))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            foreach (var table in tables)
            {
                const string aliasParamName = "alias";
                var typeName = GetTypeName(table, tableNamespaces);
                var typeSyntax = SyntaxFactory.ParseTypeName(typeName);
                var methodName = GetMethodName(table, tablePrefix, schemaSegments);

                result.Add(
                    SyntaxFactory.MethodDeclaration(typeSyntax, methodName)
                        .WithModifiers(CodeGenSyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                        .AddParameterListParameters(CodeGenSyntaxHelpers.FuncParameter(aliasParamName, nameof(Alias)))
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                            SyntaxFactory.ObjectCreationExpression(
                                typeSyntax,
                                CodeGenSyntaxHelpers.ArgumentList(SyntaxFactory.IdentifierName(aliasParamName)),
                                null)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

                result.Add(
                    SyntaxFactory.MethodDeclaration(typeSyntax, methodName)
                        .WithModifiers(CodeGenSyntaxHelpers.Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                            SyntaxFactory.ObjectCreationExpression(
                                typeSyntax,
                                CodeGenSyntaxHelpers.ArgumentList(aliasType.MemberAccess(nameof(Alias.Auto))),
                                null)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            }

            return result.ToArray();

            static string GetMethodName(
                TableModel table,
                string prefix,
                IReadOnlyDictionary<TableRef, string>? schemaSegments)
            {
                var name = table.Name;
                if (!string.IsNullOrEmpty(prefix))
                {
                    name = name.Substring(prefix.Length);
                }

                return "Get" + (schemaSegments != null ? schemaSegments[table.DbName] : string.Empty) + name;
            }

            static string GetTypeName(TableModel table, IReadOnlyDictionary<TableRef, string>? tableNamespaces)
            {
                if (tableNamespaces == null || !tableNamespaces.TryGetValue(table.DbName, out var tableNamespace) || string.IsNullOrEmpty(tableNamespace))
                {
                    return table.Name;
                }

                return "global::" + tableNamespace + "." + table.Name;
            }
        }

        private static CompilationUnitSyntax EnsureUsings(CompilationUnitSyntax compilationUnit)
        {
            return AddUsingIfMissing(
                AddUsingIfMissing(
                    AddUsingIfMissing(compilationUnit, nameof(System)),
                    "System.Collections.Generic"),
                nameof(SqExpress));
        }

        private static CompilationUnitSyntax AddUsingIfMissing(CompilationUnitSyntax compilationUnit, string namespaceName)
        {
            if (compilationUnit.Usings.Any(u => string.Equals(u.Name?.ToString(), namespaceName, StringComparison.Ordinal)))
            {
                return compilationUnit;
            }

            return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName)));
        }
    }
}
