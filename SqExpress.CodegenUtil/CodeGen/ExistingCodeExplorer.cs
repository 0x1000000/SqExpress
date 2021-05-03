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

        public static IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> FindTableDescriptors(string path, IFileSystem fileSystem)
        {
            var result = new Dictionary<TableRef, ClassDeclarationSyntax>();
            foreach (var cdPath in EnumerateSyntaxTrees(path, fileSystem).ExploreTableDescriptors())
            {
                if (cdPath.TableRef != null && !result.ContainsKey(cdPath.TableRef))
                {
                    result.Add(cdPath.TableRef, cdPath.ClassDeclaration);
                }
            }

            return result;
        }

        public static IEnumerable<AttributeSyntax> EnumerateTableDescriptorsModelAttributes(string path, IFileSystem fileSystem)
        {
            foreach (var cdPath in EnumerateSyntaxTrees(path, fileSystem).ExploreTableDescriptors())
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

        private static IEnumerable<SyntaxTree> EnumerateSyntaxTrees(string path, IFileSystem fileSystem)
        {
            if (!fileSystem.DirectoryExists(path))
            {
                throw new SqExpressCodeGenException($"Directory \"{path}\" does not exits");
            }

            var files = fileSystem.EnumerateFiles(
                path,
                "*.cs",
                SearchOption.AllDirectories);


            return files.Select(f => CSharpSyntaxTree.ParseText(fileSystem.ReadAllText(f)));
        }

        private static IEnumerable<CdPath> ExploreTableDescriptors(this IEnumerable<SyntaxTree> fileNodes)
        {
            foreach (var nodePath in fileNodes)
            {

                var classes = nodePath.GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(cd => (Class: cd, BaseTypeKind: SyntaxHelpers.GetTableClassKind(cd)))
                    .Where(p => p.BaseTypeKind != null);

                foreach (var tuple in classes)
                {
                    ConstructorInitializerSyntax? baseConstCall = tuple.Class
                        .DescendantNodes()
                        .OfType<ConstructorInitializerSyntax>()
                        .FirstOrDefault(c =>
                            c.Kind() == SyntaxKind.BaseConstructorInitializer);

                    BaseTypeKindTag baseTypeKindTag = tuple.BaseTypeKind!.Value;

                    if (baseTypeKindTag == BaseTypeKindTag.DerivedTableBase)
                    {
                        yield return new CdPath(tuple.Class, baseTypeKindTag, null);
                    }
                    else if (baseConstCall != null)
                    {
                        string schema;
                        string tableName;

                        if (baseTypeKindTag == BaseTypeKindTag.TableBase)
                        {
                            if (baseConstCall.ArgumentList.Arguments.Count != 3)
                            {
                                continue;
                            }

                            if (baseConstCall.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax slSh 
                                && slSh.Kind() == SyntaxKind.StringLiteralExpression)
                            {
                                schema = slSh.Token.ValueText;
                            }
                            else
                            {
                                continue;
                            }

                            if (baseConstCall.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax slDb 
                                && slDb.Kind() == SyntaxKind.StringLiteralExpression)
                            {
                                tableName = slDb.Token.ValueText;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (baseTypeKindTag == BaseTypeKindTag.TempTableBase)
                        {
                            if (baseConstCall.ArgumentList.Arguments.Count != 2)
                            {
                                continue;
                            }
                            schema = "";
                            if (baseConstCall.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax slSh
                                && slSh.Kind() == SyntaxKind.StringLiteralExpression)
                            {
                                tableName = slSh.Token.ValueText;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            throw new SqExpressCodeGenException($"Unknown base type kind: '{baseTypeKindTag}'");
                        }

                        TableRef tableRef = new TableRef(schema, tableName);

                        yield return new CdPath(tuple.Class, baseTypeKindTag, tableRef);
                    }
                    else
                    {
                        throw new SqExpressCodeGenException($"Unexpected base type kind: '{baseTypeKindTag}' (with empty base constructor)");
                    }
                }
            }
        }

        private readonly struct CdPath
        {
            public readonly ClassDeclarationSyntax ClassDeclaration;
            public readonly BaseTypeKindTag KindTag;
            public readonly TableRef? TableRef;

            public CdPath(ClassDeclarationSyntax classDeclaration, BaseTypeKindTag kindTag, TableRef? tableRef)
            {
                this.ClassDeclaration = classDeclaration;
                this.KindTag = kindTag;
                this.TableRef = tableRef;
            }
        }
    }
}