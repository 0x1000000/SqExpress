using System.Collections.Generic;
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
        public static CompilationUnitSyntax Generate(SqModelMeta meta, string defaultNamespace)
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


            return SyntaxFactory.CompilationUnit()
                .AddUsings(namespaces.Select(n => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(n))).ToArray())
                .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(defaultNamespace))
                    .AddMembers(GenerateClass(meta)))
                .NormalizeWhitespace();
        }

        private static ClassDeclarationSyntax GenerateClass(SqModelMeta meta)
        {
            return SyntaxFactory.ClassDeclaration(meta.Name)
                .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
                .AddMembers(Constructors(meta)
                    .Concat(GenerateStaticFactory(meta))
                    .Concat(Properties(meta))
                    .Concat(GenerateGetColumns(meta))
                    .Concat(GenerateMapping(meta))
                    .ToArray());
        }

        public static IEnumerable<MemberDeclarationSyntax> Properties(SqModelMeta meta)
        {
            return meta.Properties.Select(p => SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(p.FinalType),
                    p.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                ));
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
                    .MethodDeclaration(SyntaxFactory.ParseTypeName(meta.Name), "Read")
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
                        .MethodDeclaration(arrayType, "GetColumns")
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
                    MethodDeclarationSyntax(meta, "GetMapping", null),
                    MethodDeclarationSyntax(meta, "GetUpdateKeyMapping", true),
                    MethodDeclarationSyntax(meta, "GetUpdateMapping", false)
                };
            }
            else
            {
                return new [] { MethodDeclarationSyntax(meta, "GetMapping", null) };
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
    }
}