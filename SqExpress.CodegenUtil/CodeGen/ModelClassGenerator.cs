using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model.SqModel;
using SqExpress.QueryBuilders.RecordSetter;
using static SqExpress.CodeGenUtil.CodeGen.SyntaxHelpers;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal class ModelClassGenerator
    {
        private const string MethodNameGetColumns = "GetColumns";
        private const string MethodNameGetMapping = "GetMapping";
        private const string MethodNameGetUpdateKeyMapping = "GetUpdateKeyMapping";
        private const string MethodNameGetUpdateMapping = "GetUpdateMapping";
        private const string MethodNameRead = "Read";

        private static readonly HashSet<string> AllMethods = new HashSet<string>
        {
            MethodNameGetColumns,
            MethodNameGetMapping,
            MethodNameGetUpdateKeyMapping,
            MethodNameGetUpdateMapping,
            MethodNameRead
        };

        public static CompilationUnitSyntax Generate(SqModelMeta meta, string defaultNamespace, string existingFilePath, out bool existing)
        {
            CompilationUnitSyntax result;
            ClassDeclarationSyntax? existingClass = null;

            existing = false;

            if (File.Exists(existingFilePath))
            {
                existing = true;
                var tClass = CSharpSyntaxTree.ParseText(File.ReadAllText(existingFilePath));

                existingClass = tClass.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(cd => cd.Identifier.ValueText == meta.Name);
            }

            if (existingClass != null)
            {
                result = existingClass.FindParentOrDefault<CompilationUnitSyntax>() ?? throw new SqExpressCodeGenException($"Could not find compilation unit in \"{existingFilePath}\"");

                result = result.ReplaceNode(existingClass, GenerateClass(meta, existingClass));
            }
            else
            {
                var namespaces =
                    new[] {
                            nameof(System),
                            nameof(SqExpress),
                            $"{nameof(SqExpress)}.{nameof(SqExpress.QueryBuilders)}.{nameof(SqExpress.QueryBuilders.RecordSetter)}"
                        }
                        .Concat(meta.Properties.SelectMany(p => p.Column)
                            .Select(c => c.TableRef.TableTypeNameSpace)
                            .Where(n => n != defaultNamespace))
                        .Distinct()
                        .ToList();


                result = SyntaxFactory.CompilationUnit()
                    .AddUsings(namespaces.Select(n => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(n))).ToArray())
                    .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(defaultNamespace))
                        .AddMembers(GenerateClass(meta, null)));
            }

            return result.NormalizeWhitespace();
        }

        private static ClassDeclarationSyntax GenerateClass(SqModelMeta meta, ClassDeclarationSyntax? existingClass)
        {
            ClassDeclarationSyntax result;
            MemberDeclarationSyntax[]? oldMembers = null;
            Dictionary<string,SyntaxList<AttributeListSyntax>>? oldAttributes = null;
            if (existingClass != null)
            {
                result = existingClass;

                oldMembers = result.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(md =>
                    {
                        if (md is ConstructorDeclarationSyntax)
                        {
                            return false;
                        }

                        if (md is IncompleteMemberSyntax)
                        {
                            return false;
                        }

                        if (md is PropertyDeclarationSyntax p)
                        {
                            if (meta.Properties.Any(mp => mp.Name == p.Identifier.ValueText))
                            {
                                if (p.AttributeLists.Count > 0)
                                {
                                    oldAttributes ??= new Dictionary<string, SyntaxList<AttributeListSyntax>>();
                                    oldAttributes.Add(p.Identifier.ValueText, p.AttributeLists);
                                }
                                return false;
                            }
                        }

                        if (md is MethodDeclarationSyntax method)
                        {
                            var name = method.Identifier.ValueText;

                            if (name.StartsWith("With") || AllMethods.Contains(name))
                            {

                                return false;
                            }

                        }

                        return true;
                    })
                    .ToArray();

                result = result.RemoveNodes(result.DescendantNodes().OfType<MemberDeclarationSyntax>(), SyntaxRemoveOptions.KeepNoTrivia)!;

            }
            else
            {
                result = SyntaxFactory.ClassDeclaration(meta.Name)
                    .WithModifiers(Modifiers(SyntaxKind.PublicKeyword));
            }

            result = result
                .AddMembers(Constructors(meta)
                    .Concat(GenerateStaticFactory(meta))
                    .Concat(Properties(meta, oldAttributes))
                    .Concat(GenerateGetColumns(meta))
                    .Concat(GenerateMapping(meta))
                    .Concat(GenerateWithModifiers(meta))
                    .ToArray());

            if (oldMembers != null && oldMembers.Length > 0)
            {
                result = result.AddMembers(oldMembers);
            }

            return result;
        }

        public static IEnumerable<MemberDeclarationSyntax> Properties(SqModelMeta meta, IReadOnlyDictionary<string, SyntaxList<AttributeListSyntax>>? oldAttributes)
        {
            return meta.Properties.Select(p =>
            {
                var res = SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.ParseTypeName(p.FinalType),
                        p.Name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    );
                if (oldAttributes != null && oldAttributes.TryGetValue(p.Name, out var attributeList))
                {
                    res = res.WithAttributeLists(attributeList);
                }

                return res;
            });
        }

        public static MemberDeclarationSyntax[] Constructors(SqModelMeta meta)
        {
            var constructor = SyntaxFactory.ConstructorDeclaration(meta.Name)
                .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(meta.Properties.Select(p=> FuncParameter(p.Name.FirstToLower(), p.FinalType)).ToArray())
                .WithBody(SyntaxFactory.Block(GenerateConstructorAssignments(meta)));

            return new MemberDeclarationSyntax[] {constructor};
        }

        private static IEnumerable<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax> GenerateConstructorAssignments(SqModelMeta meta)
        {
            return meta.Properties.Select(p =>
                SyntaxFactory.ExpressionStatement(AssignmentThis(p.Name,
                    SyntaxFactory.IdentifierName(p.Name.FirstToLower()))));
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateStaticFactory(SqModelMeta meta)
        {
            return meta.Properties.First()
                .Column.Select(tableColumn => SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.ParseTypeName(meta.Name), MethodNameRead)
                    .WithModifiers(Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(FuncParameter("record", nameof(ISqDataRecordReader)))
                    .AddParameterListParameters(FuncParameter("table", tableColumn.TableRef.TableTypeName))
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.Token(SyntaxKind.NewKeyword),
                            SyntaxFactory.ParseTypeName(meta.Name),
                            ArgumentList(meta.Properties.Select(p =>
                                {
                                    ExpressionSyntax invocation = MemberAccess("table", p.Column.First().ColumnName)
                                        .MemberAccess("Read")
                                        .Invoke(SyntaxFactory.ParseName("record"));

                                    if (p.CastType != null)
                                    {
                                        invocation = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(p.CastType), invocation);
                                    }
                                    return new NamedArgument(p.Name.FirstToLower(),
                                        invocation);
                                })
                                .ToArray()),
                            null)))));
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateGetColumns(SqModelMeta meta)
        {
            return meta.Properties.First()
                .Column.Select(tableColumn =>
                {

                    var arrayItems = meta.Properties.Select(p => p.Column.First())
                        .Select(p => SyntaxFactory.IdentifierName("table").MemberAccess(p.ColumnName));
                    var arrayType = SyntaxFactory.ArrayType(
                        SyntaxFactory.IdentifierName(nameof(TableColumn)),
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

                    return SyntaxFactory
                        .MethodDeclaration(arrayType, MethodNameGetColumns)
                        .WithModifiers(Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                        .AddParameterListParameters(FuncParameter("table", tableColumn.TableRef.TableTypeName))
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                            array
                        )));

                });
        }

        public static MemberDeclarationSyntax[] GenerateMapping(SqModelMeta meta)
        {

            var pkCount = meta.Properties.Count(i => i.IsPrimaryKey);

            if (pkCount > 0 && pkCount < meta.Properties.Count)
            {
                return new []
                {
                    MethodDeclarationSyntax(meta, MethodNameGetMapping, null),
                    MethodDeclarationSyntax(meta, MethodNameGetUpdateKeyMapping, true),
                    MethodDeclarationSyntax(meta, MethodNameGetUpdateMapping, false)
                };
            }
            else
            {
                return new [] { MethodDeclarationSyntax(meta, MethodNameGetMapping, null) };
            }


            static MemberDeclarationSyntax MethodDeclarationSyntax(SqModelMeta sqModelMeta, string name, bool? pkFilter)
            {
                var setter = SyntaxFactory.IdentifierName("s");
                ExpressionSyntax chain = setter;

                foreach (var metaProperty in sqModelMeta.Properties.Where(p => pkFilter.HasValue? p.IsPrimaryKey == pkFilter.Value : !p.IsIdentity))
                {
                    var col = setter.MemberAccess(nameof(IDataMapSetter<object, object>.Target))
                        .MemberAccess(metaProperty.Column.First().ColumnName);
                    ExpressionSyntax prop = setter.MemberAccess(nameof(IDataMapSetter<object, object>.Source))
                        .MemberAccess(metaProperty.Name);
                    if (metaProperty.CastType != null)
                    {
                        prop = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(metaProperty.Type), prop);
                    }

                    chain = chain.MemberAccess("Set").Invoke(col, prop);
                }


                var methodDeclarationSyntax = SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.ParseTypeName(nameof(IRecordSetterNext)), name)
                    .WithModifiers(Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(FuncParameter("s",
                        $"{nameof(IDataMapSetter<object, object>)}<{sqModelMeta.Properties.First().Column.First().TableRef.TableTypeName},{sqModelMeta.Name}>"))
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(chain)));
                return methodDeclarationSyntax;
            }
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateWithModifiers(SqModelMeta meta)
        {
            return meta.Properties.Select(p =>
            {

                var args = meta.Properties.Select(subP =>
                        new NamedArgument(subP.Name.FirstToLower(),
                            p == subP
                                ? SyntaxFactory.IdentifierName(subP.Name.FirstToLower())
                                : MemberAccessThis(subP.Name)
                        ))
                    .ToArray();

                return SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.ParseTypeName(meta.Name), $"With{p.Name}")
                    .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(FuncParameter(p.Name.FirstToLower(), p.FinalType))
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.Token(SyntaxKind.NewKeyword),
                            SyntaxFactory.ParseTypeName(meta.Name),
                            ArgumentList(args),
                            initializer: null)

                    )));
            });

        }
    }
}