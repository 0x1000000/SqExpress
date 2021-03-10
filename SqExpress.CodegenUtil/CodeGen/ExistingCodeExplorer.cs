using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal static class ExistingCodeExplorer
    {
        private static readonly string SqModelAttributeName = nameof(SqModelAttribute)
            .Substring(0, nameof(SqModelAttribute).Length - nameof(Attribute).Length);

        public static IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> FindTableDescriptors(string path)
        {
            var result = new Dictionary<TableRef, ClassDeclarationSyntax>();
            foreach (var cdPath in EnumerateSyntaxTrees(path).ExploreTableDescriptors())
            {
                if (!result.ContainsKey(cdPath.TableRef))
                {
                    result.Add(cdPath.TableRef, cdPath.ClassDeclaration);
                }
            }

            return result;
        }

        public static IEnumerable<AttributeSyntax> EnumerateTableDescriptorsModelAttributes(string path)
        {
            foreach (var cdPath in EnumerateSyntaxTrees(path).ExploreTableDescriptors())
            {

                var attributes = cdPath.ClassDeclaration.DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .SelectMany(p => p.DescendantNodes())
                    .OfType<AttributeSyntax>();

                foreach (var attr in attributes)
                {
                    var name = attr.Name.ToString();

                    if (name.EndsWith(SqModelAttributeName) || name.EndsWith(nameof(SqModelAttribute)))
                    {
                        yield return attr;
                    }

                }
            }
        }

        private static IEnumerable<SyntaxTree> EnumerateSyntaxTrees(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new SqExpressCodeGenException($"Directory \"{path}\" does not exits");
            }

            var files = Directory.EnumerateFiles(
                path,
                "*.cs",
                SearchOption.AllDirectories);


            return files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)));
        }

        private static IEnumerable<CdPath> ExploreTableDescriptors(this IEnumerable<SyntaxTree> fileNodes)
        {
            foreach (var nodePath in fileNodes)
            {

                var classes = nodePath.GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(cd =>
                        cd.BaseList?.DescendantNodesAndSelf()
                            .OfType<BaseTypeSyntax>()
                            .Any(b => b.Type.ToString().EndsWith(nameof(TableBase))) ??
                        false);

                foreach (var classDeclarationSyntax in classes)
                {
                    var baseConstCall = classDeclarationSyntax
                        .DescendantNodes()
                        .OfType<ConstructorInitializerSyntax>()
                        .FirstOrDefault(c =>
                            c.Kind() == SyntaxKind.BaseConstructorInitializer && c.ArgumentList.Arguments.Count == 3);

                    if (baseConstCall != null)
                    {
                        string schema;
                        string tableName;
                        if (baseConstCall.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax slSh &&
                            slSh.Kind() == SyntaxKind.StringLiteralExpression)
                        {
                            schema = slSh.Token.ValueText;
                        }
                        else
                        {
                            continue;
                        }

                        if (baseConstCall.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax slDb &&
                            slDb.Kind() == SyntaxKind.StringLiteralExpression)
                        {
                            tableName = slDb.Token.ValueText;
                        }
                        else
                        {
                            continue;
                        }

                        TableRef tableRef = new TableRef(schema, tableName);

                        yield return new CdPath(classDeclarationSyntax, nodePath.FilePath, tableRef);
                    }
                }
            }
        }


        private readonly struct CdPath
        {
            public readonly ClassDeclarationSyntax ClassDeclaration;
            public readonly string Path;
            public readonly TableRef TableRef;

            public CdPath(ClassDeclarationSyntax classDeclaration, string path, TableRef tableRef)
            {
                this.ClassDeclaration = classDeclaration;
                this.Path = path;
                this.TableRef = tableRef;
            }
        }
    }

}