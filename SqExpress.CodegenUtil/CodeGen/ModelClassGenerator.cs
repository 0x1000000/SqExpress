using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model.SqModel;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Names;
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
        private const string MethodNameReadOrdinal = "ReadOrdinal";
        private const string ReaderClassSuffix = "Reader";
        private const string MethodNameGetReader = "GetReader";
        private const string UpdaterClassSuffix = "Updater";
        private const string MethodNameGetUpdater = "GetUpdater";

        private static readonly HashSet<string> AllMethods = new HashSet<string>
        {
            MethodNameGetColumns,
            MethodNameGetMapping,
            MethodNameGetUpdateKeyMapping,
            MethodNameGetUpdateMapping,
            MethodNameRead,
            MethodNameReadOrdinal,
            MethodNameGetReader,
            MethodNameGetUpdater
        };

        public static CompilationUnitSyntax Generate(SqModelMeta meta, string defaultNamespace, string existingFilePath, bool rwClasses, ModelType modelType, IFileSystem fileSystem, out bool existing)
        {
            CompilationUnitSyntax result;
            TypeDeclarationSyntax? existingClass = null;

            existing = false;

            if (fileSystem.FileExists(existingFilePath))
            {
                existing = true;
                var tClass = CSharpSyntaxTree.ParseText(fileSystem.ReadAllText(existingFilePath));

                existingClass = tClass.GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault(cd => cd.Identifier.ValueText == meta.Name);
            }

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

            if (rwClasses || ExtractTableRefs(meta).Any(tr => tr.BaseTypeKindTag == BaseTypeKindTag.DerivedTableBase))
            {
                namespaces.Add($"{nameof(SqExpress)}.{nameof(SqExpress.Syntax)}.{nameof(SqExpress.Syntax.Names)}");
                namespaces.Add($"{nameof(System)}.{nameof(System.Collections)}.{nameof(System.Collections.Generic)}");
            }


            if (existingClass != null)
            {
                result = existingClass.FindParentOrDefault<CompilationUnitSyntax>() ?? throw new SqExpressCodeGenException($"Could not find compilation unit in \"{existingFilePath}\"");

                foreach (var usingDirectiveSyntax in result.Usings)
                {
                    var existingUsing = usingDirectiveSyntax.Name.ToFullString();
                    var index = namespaces.IndexOf(existingUsing);
                    if (index >= 0)
                    {
                        namespaces.RemoveAt(index);
                    }
                }

                if (namespaces.Count > 0)
                {
                    result = result.AddUsings(namespaces
                        .Select(n => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(n)))
                        .ToArray());
                }

                var oldClass = existingClass;

                if (oldClass is ClassDeclarationSyntax classDeclaration && modelType == ModelType.Record)
                {
                    oldClass = SyntaxFactory.RecordDeclaration(classDeclaration.AttributeLists,
                        classDeclaration.Modifiers,
                        SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                        classDeclaration.Identifier,
                        classDeclaration.TypeParameterList,
                        null,
                        classDeclaration.BaseList,
                        classDeclaration.ConstraintClauses,
                        SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                        classDeclaration.Members, 
                        SyntaxFactory.Token(SyntaxKind.CloseBraceToken), 
                        SyntaxFactory.Token(SyntaxKind.None));
                }

                result = result.ReplaceNode(existingClass, GenerateClass(meta, rwClasses, modelType, oldClass));
            }
            else
            {
                result = SyntaxFactory.CompilationUnit()
                    .AddUsings(namespaces.Select(n => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(n))).ToArray())
                    .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(defaultNamespace))
                        .AddMembers(GenerateClass(meta, rwClasses, modelType, null)));
            }

            return result.NormalizeWhitespace();
        }

        private static TypeDeclarationSyntax GenerateClass(SqModelMeta meta, bool rwClasses, ModelType modelType, TypeDeclarationSyntax? existingClass)
        {
            TypeDeclarationSyntax result;
            MemberDeclarationSyntax[]? oldMembers = null;
            Dictionary<string,SyntaxList<AttributeListSyntax>>? oldAttributes = null;
            if (existingClass != null)
            {
                result = existingClass;

                oldMembers = result.Members
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

                            if (name.StartsWith("With") || AllMethods.Contains(name) || name.StartsWith(MethodNameGetReader + "For") || name.StartsWith(MethodNameGetUpdater + "For"))
                            {
                                return false;
                            }
                        }

                        if (md is ClassDeclarationSyntax classDeclaration)
                        {
                            var name = classDeclaration.Identifier.ValueText;

                            if (name == meta.Name + ReaderClassSuffix || name.StartsWith(meta.Name + ReaderClassSuffix + "For"))
                            {
                                return false;
                            }
                            if (name == meta.Name + UpdaterClassSuffix || name.StartsWith(meta.Name + UpdaterClassSuffix + "For"))
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
                result = (modelType == ModelType.Record
                        ? (TypeDeclarationSyntax)SyntaxFactory
                            .RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), meta.Name)
                            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                        : SyntaxFactory.ClassDeclaration(meta.Name))
                    .WithModifiers(existingClass?.Modifiers ?? Modifiers(SyntaxKind.PublicKeyword));
            }

            result = result
                .AddMembers(Constructors(meta)
                    .Concat(GenerateStaticFactory(meta))
                    .Concat(rwClasses ? GenerateOrdinalStaticFactory(meta) : Array.Empty<MemberDeclarationSyntax>())
                    .Concat(Properties(meta, oldAttributes))
                    .Concat(modelType == ModelType.ImmutableClass ? GenerateWithModifiers(meta) : Array.Empty<MemberDeclarationSyntax>())
                    .Concat(GenerateGetColumns(meta))
                    .Concat(GenerateMapping(meta))
                    .Concat(rwClasses ? GenerateReaderClass(meta): Array.Empty<MemberDeclarationSyntax>())
                    .Concat(rwClasses ? GenerateWriterClass(meta) : Array.Empty<MemberDeclarationSyntax>())
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
            return ExtractTableRefs(meta).Select(tableRef => SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.ParseTypeName(meta.Name), MethodNameRead)
                    .WithModifiers(Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(FuncParameter("record", nameof(ISqDataRecordReader)))
                    .AddParameterListParameters(FuncParameter("table", ExtractTableTypeName(meta, tableRef)))
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

        public static IEnumerable<MemberDeclarationSyntax> GenerateOrdinalStaticFactory(SqModelMeta meta)
        {
            return ExtractTableRefs(meta).Select(tableRef => SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.ParseTypeName(meta.Name), MethodNameReadOrdinal)
                    .WithModifiers(Modifiers(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(FuncParameter("record", nameof(ISqDataRecordReader)))
                    .AddParameterListParameters(FuncParameter("table", ExtractTableTypeName(meta, tableRef)))
                    .AddParameterListParameters(FuncParameter("offset", "int"))
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.Token(SyntaxKind.NewKeyword),
                            SyntaxFactory.ParseTypeName(meta.Name),
                            ArgumentList(meta.Properties.Select((p,index) =>
                                {
                                    ExpressionSyntax invocation = MemberAccess("table", p.Column.First().ColumnName)
                                        .MemberAccess("Read")
                                        .Invoke(
                                            SyntaxFactory.ParseName("record"),
                                            index == 0 
                                                ? SyntaxFactory.IdentifierName("offset")
                                                : SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, SyntaxFactory.IdentifierName("offset"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(index))));

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
            return ExtractTableRefs(meta).Select(tableRef =>
                {
                    string columnTypeName = ExtractTableColumnTypeName(tableRef);
                    var arrayItems = meta.Properties.Select(p => p.Column.First())
                        .Select(p => SyntaxFactory.IdentifierName("table").MemberAccess(p.ColumnName));
                    var arrayType = SyntaxFactory.ArrayType(
                        SyntaxFactory.IdentifierName(columnTypeName),
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
                        .AddParameterListParameters(FuncParameter("table", ExtractTableTypeName(meta, tableRef)))
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                            array
                        )));

                });
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateMapping(SqModelMeta meta)
        {
            return ExtractTableRefs(meta).SelectMany(tr => GenerateMapping(meta, tr));
        }

        public static MemberDeclarationSyntax[] GenerateMapping(SqModelMeta meta, SqModelTableRef tableRef)
        {
            if (!HasUpdater(tableRef))
            {
                return Array.Empty<MemberDeclarationSyntax>();
            }

            if (meta.HasPk())
            {
                return new []
                {
                    MethodDeclarationSyntax(meta, tableRef, MethodNameGetMapping, null),
                    MethodDeclarationSyntax(meta, tableRef,MethodNameGetUpdateKeyMapping, true),
                    MethodDeclarationSyntax(meta, tableRef,MethodNameGetUpdateMapping, false)
                };
            }
            else
            {
                return new [] { MethodDeclarationSyntax(meta, tableRef, MethodNameGetMapping, null) };
            }


            static MemberDeclarationSyntax MethodDeclarationSyntax(SqModelMeta sqModelMeta, SqModelTableRef tableRef, string name, bool? pkFilter)
            {
                var setter = SyntaxFactory.IdentifierName("s");
                ExpressionSyntax chain = setter;

                foreach (var metaProperty in sqModelMeta.Properties.Where(p => pkFilter.HasValue? p.IsPrimaryKey == pkFilter.Value : !p.IsIdentity))
                {
                    var col = setter.MemberAccess(nameof(IDataMapSetter<object, object>.Target))
                        .MemberAccess(metaProperty.Column.First(c=>c.TableRef.Equals(tableRef)).ColumnName);
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
                        $"{nameof(IDataMapSetter<object, object>)}<{ExtractTableTypeName(sqModelMeta, tableRef)},{sqModelMeta.Name}>"))
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

        public static IEnumerable<MemberDeclarationSyntax> GenerateReaderClass(SqModelMeta meta)
        {
            return ExtractTableRefs(meta, out var addName).SelectMany(tableRef => GenerateReaderClass(meta, tableRef, addName));
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateReaderClass(SqModelMeta meta, SqModelTableRef tableRef, bool addName)
        {
            string tableType = ExtractTableTypeName(meta, tableRef);
            var className = meta.Name + ReaderClassSuffix;
            if (addName)
            {
                className += $"For{tableRef.TableTypeName}";
            }
            var classType = SyntaxFactory.ParseTypeName(className);

            var baseInterface = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(nameof(ISqModelReader<object, object>)))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.IdentifierName(meta.Name),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.IdentifierName(tableType)
                            })));
            //Instance
            var instance = SyntaxFactory.PropertyDeclaration(
                    classType,
                    SyntaxFactory.Identifier("Instance"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))
                .WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ObjectCreationExpression(classType)
                            .WithArgumentList(SyntaxFactory.ArgumentList())))
                .WithSemicolonToken(
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            //GetColumns
            var getColumns = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier(nameof(IReadOnlyList<object>)))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    SyntaxFactory.IdentifierName(nameof(ExprColumn))
                                }))),
                    SyntaxFactory.Identifier(MethodNameGetColumns))
                .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(baseInterface))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Parameter(
                                    SyntaxFactory.Identifier("table"))
                                .WithType(
                                    SyntaxFactory.IdentifierName(tableType)))))
                .WithBody(
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(meta.Name),
                                            SyntaxFactory.IdentifierName(nameof(ISqModelReader<object, object>.GetColumns))))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.IdentifierName("table")))))))));

            //Read
            var read = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.IdentifierName(meta.Name),
                    SyntaxFactory.Identifier(MethodNameRead))
                .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(baseInterface))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList<ParameterSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("record"))
                                    .WithType(
                                        SyntaxFactory.IdentifierName(nameof(ISqDataRecordReader))),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("table"))
                                    .WithType(
                                        SyntaxFactory.IdentifierName(tableType))
                            })))
                .WithBody(
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(meta.Name),
                                            SyntaxFactory.IdentifierName(MethodNameRead)))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(
                                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                new SyntaxNodeOrToken[]
                                                {
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName("record")),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName("table"))
                                                })))))));
            //ReadOrdinal
            var readOrdinal = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.IdentifierName(meta.Name),
                    SyntaxFactory.Identifier(MethodNameReadOrdinal))
                .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(baseInterface))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList<ParameterSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("record"))
                                    .WithType(
                                        SyntaxFactory.IdentifierName(nameof(ISqDataRecordReader))),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("table"))
                                    .WithType(
                                        SyntaxFactory.IdentifierName(tableType)),                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("offset"))
                                    .WithType(
                                        SyntaxFactory.IdentifierName("int"))
                            })))
                .WithBody(
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(meta.Name),
                                            SyntaxFactory.IdentifierName(MethodNameReadOrdinal)))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(
                                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                new SyntaxNodeOrToken[]
                                                {
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName("record")),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName("table")),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName("offset"))
                                                })))))));


            var readerClassDeclaration = SyntaxFactory.ClassDeclaration(className)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithBaseList(SyntaxFactory.BaseList().AddTypes(SyntaxFactory.SimpleBaseType(baseInterface)))
                .AddMembers(instance, getColumns, read, readOrdinal);

            var getReader = SyntaxFactory.MethodDeclaration(baseInterface, addName ? $"{MethodNameGetReader}For{tableRef.TableTypeName}" : MethodNameGetReader)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddBodyStatements(SyntaxFactory.ReturnStatement(MemberAccess(className, "Instance")));

            return new MemberDeclarationSyntax[] {getReader, readerClassDeclaration};
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateWriterClass(SqModelMeta meta)
        {
            return ExtractTableRefs(meta, out var addName).SelectMany(tableRef => GenerateWriterClass(meta, tableRef, addName));
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateWriterClass(SqModelMeta meta, SqModelTableRef tableRef, bool addName)
        {
            if (!HasUpdater(tableRef))
            {
                return Array.Empty<MemberDeclarationSyntax>();
            }

            var tableType = ExtractTableTypeName(meta, tableRef);
            var className = meta.Name + UpdaterClassSuffix;
            if (addName)
            {
                className += $"For{tableRef.TableTypeName}";
            }
            var classType = SyntaxFactory.ParseTypeName(className);

            bool hasPk = meta.HasPk();

            var baseInterfaceKeyLess = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(nameof(ISqModelUpdater<object, object>)))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.IdentifierName(meta.Name),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.IdentifierName(tableType)
                            })));

            var baseInterfaceKey = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(nameof(ISqModelUpdaterKey<object, object>)))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.IdentifierName(meta.Name),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.IdentifierName(tableType)
                            })));
            var baseInterface = hasPk ? baseInterfaceKey : baseInterfaceKeyLess;

            //Instance
            var instance = SyntaxFactory.PropertyDeclaration(
                    classType,
                    SyntaxFactory.Identifier("Instance"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))
                .WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ObjectCreationExpression(classType)
                            .WithArgumentList(SyntaxFactory.ArgumentList())))
                .WithSemicolonToken(
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken));


            //GetMapping
            var dataMapSetterName = "dataMapSetter";

            var parameterDataMapperSetter = SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier(dataMapSetterName))
                .WithType(
                    SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(nameof(IDataMapSetter<object, object>)))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SeparatedList<TypeSyntax>(
                                    new SyntaxNodeOrToken[]{
                                        SyntaxFactory.IdentifierName(tableType),
                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                        SyntaxFactory.IdentifierName(meta.Name)}))));


            var updaterClassDeclaration = SyntaxFactory.ClassDeclaration(className)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithBaseList(SyntaxFactory.BaseList().AddTypes(SyntaxFactory.SimpleBaseType(baseInterface)))
                .AddMembers(
                    instance,
                    GetMapping(baseInterfaceKeyLess, MethodNameGetMapping));

            if (hasPk)
            {
                updaterClassDeclaration = updaterClassDeclaration.AddMembers(
                    GetMapping(baseInterfaceKey, MethodNameGetUpdateKeyMapping),
                    GetMapping(baseInterfaceKey, MethodNameGetUpdateMapping));
            }

            MethodDeclarationSyntax GetMapping(GenericNameSyntax bi, string s)
            {
                return SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName(nameof(IRecordSetterNext)),
                        SyntaxFactory.Identifier(s))
                    .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(bi))
                    .WithParameterList(
                        SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameterDataMapperSetter)))
                    .WithBody(
                        SyntaxFactory.Block(
                            SyntaxFactory.SingletonList<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>(
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(meta.Name),
                                                SyntaxFactory.IdentifierName(s)))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName(dataMapSetterName)))))))));
            }

            var getUpdater = SyntaxFactory.MethodDeclaration(baseInterface, addName ? $"{MethodNameGetUpdater}For{tableRef.TableTypeName}" : MethodNameGetUpdater)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddBodyStatements(SyntaxFactory.ReturnStatement(MemberAccess(className, "Instance")));

            return new MemberDeclarationSyntax[] { getUpdater, updaterClassDeclaration };
        }

        private static string ExtractTableTypeName(SqModelMeta meta, SqModelTableRef tableRef)
        {
            string tableType = tableRef.TableTypeName;
            if (tableType == meta.Name)
            {
                tableType = $"{tableRef.TableTypeNameSpace}.{tableType}";
            }
            return tableType;
        }

        private static IEnumerable<SqModelTableRef> ExtractTableRefs(SqModelMeta meta) 
            => ExtractTableRefs(meta, out _);

        private static IEnumerable<SqModelTableRef> ExtractTableRefs(SqModelMeta meta, out bool multi)
        {
            var first = meta.Properties.First();
            multi = first.Column.Count > 1;
            return first.Column.Select(c => c.TableRef);
        }

        private static string ExtractTableColumnTypeName(SqModelTableRef tableRef)
            => tableRef.BaseTypeKindTag.Switch(
                tableBaseRes: nameof(TableColumn), 
                tempTableBaseRes: nameof(TableColumn), 
                derivedTableBaseRes: nameof(ExprColumn));

        private static bool HasUpdater(SqModelTableRef tableRef)
            => tableRef.BaseTypeKindTag.Switch(
                tableBaseRes: true,
                tempTableBaseRes: true,
                derivedTableBaseRes: false);
    }
}