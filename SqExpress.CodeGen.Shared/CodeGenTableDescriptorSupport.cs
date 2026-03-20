using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGen.Shared
{
    public static class CodeGenTableDescriptorSupport
    {
        public static string BuildTableKey(string? databaseName, string? schemaName, string tableName)
            => string.IsNullOrEmpty(databaseName) ? $"[{schemaName ?? string.Empty}].[{tableName}]" : $"[{databaseName}].[{schemaName ?? string.Empty}].[{tableName}]";

        public static string ToIdentifier(string value)
        {
            var parts = value
                .Split(new[] { ' ', '-', '.', '/', '\\', ':', ';', ',', '(', ')', '[', ']', '{', '}', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ToPascalCasePart)
                .Where(static i => i.Length > 0)
                .ToArray();

            var result = string.Concat(parts);
            if (string.IsNullOrEmpty(result))
            {
                result = "Column";
            }

            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            return result;
        }

        public static CodeGenValidationResult Validate(
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables)
        {
            var issues = ImmutableArray.CreateBuilder<CodeGenValidationIssue>();
            var propertyNamesBySqlName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            var sqlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var column in candidate.Columns)
            {
                if (!sqlNames.Add(column.SqlName))
                {
                    issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.DuplicateColumn, column.SqlName, candidate.TableDisplayName));
                    continue;
                }

                var propertyName = string.IsNullOrWhiteSpace(column.PropertyName) ? ToIdentifier(column.SqlName) : column.PropertyName!;
                if (!IsValidIdentifier(propertyName))
                {
                    issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.InvalidPropertyName, propertyName, candidate.TableDisplayName, column.SqlName));
                    continue;
                }

                if (!propertyNames.Add(propertyName))
                {
                    issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.DuplicatePropertyName, propertyName, candidate.TableDisplayName));
                    continue;
                }

                propertyNamesBySqlName[column.SqlName] = propertyName;
            }

            foreach (var index in candidate.Indexes)
            {
                foreach (var columnName in index.Columns)
                {
                    if (!propertyNamesBySqlName.ContainsKey(columnName))
                    {
                        issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.UnknownIndexColumn, columnName, candidate.TableDisplayName));
                    }
                }

                foreach (var columnName in index.DescendingColumns)
                {
                    if (!propertyNamesBySqlName.ContainsKey(columnName))
                    {
                        issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.UnknownIndexColumn, columnName, candidate.TableDisplayName));
                    }
                    else if (!index.Columns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                    {
                        issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.DescendingColumnMustBeIndexed, columnName, candidate.TableDisplayName));
                    }
                }
            }

            foreach (var column in candidate.Columns.Where(static c => !string.IsNullOrWhiteSpace(c.ForeignKeyTable) && !string.IsNullOrWhiteSpace(c.ForeignKeyColumn)))
            {
                var targetKey = BuildTableKey(column.ForeignKeyDatabase, column.ForeignKeySchema ?? candidate.SchemaName, column.ForeignKeyTable!);
                if (!allTables.TryGetValue(targetKey, out var targetTable))
                {
                    issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.ForeignKeyTableNotFound, column.ForeignKeyTable!, candidate.TableDisplayName, column.SqlName));
                    continue;
                }

                if (!targetTable.Columns.Any(c => string.Equals(c.SqlName, column.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(new CodeGenValidationIssue(CodeGenValidationIssueKind.ForeignKeyColumnNotFound, column.ForeignKeyColumn!, targetTable.TableDisplayName, column.SqlName));
                }
            }

            return new CodeGenValidationResult(propertyNamesBySqlName.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase), issues.ToImmutable());
        }

        public static CompilationUnitSyntax GenerateTableDescriptor(
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables,
            CodeGenTableDescriptorRenderOptions? options = null,
            CompilationUnitSyntax? existingCompilationUnit = null)
        {
            var validation = Validate(candidate, allTables);
            return GenerateTableDescriptor(candidate, validation, allTables, options, existingCompilationUnit);
        }

        public static CompilationUnitSyntax GenerateTableDescriptor(
            CodeGenTableModel candidate,
            CodeGenValidationResult validation,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables,
            CodeGenTableDescriptorRenderOptions? options = null,
            CompilationUnitSyntax? existingCompilationUnit = null)
        {
            if (validation.Issues.Length > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Issues.Select(FormatValidationIssue)));
            }

            if (existingCompilationUnit == null)
            {
                return CreateTableDescriptorSyntax(candidate, validation.PropertyNamesBySqlName, allTables, options);
            }

            return UpdateExistingTableDescriptor(existingCompilationUnit, candidate, validation.PropertyNamesBySqlName, allTables);
        }

        internal static CompilationUnitSyntax GenerateTableDescriptor(
            TableModel table,
            IReadOnlyDictionary<TableRef, TableModel> allTables,
            string defaultNamespace,
            IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> existingCode,
            out bool existing)
        {
            var allCodeGenTables = allTables.Values
                .Select(t => ToCodeGenTableModel(t, defaultNamespace))
                .ToDictionary(static t => t.TableKey, static t => t, StringComparer.OrdinalIgnoreCase);
            var candidate = allCodeGenTables[BuildTableKey(databaseName: null, schemaName: table.DbName.Schema, tableName: table.DbName.Name)];

            if (existingCode.TryGetValue(table.DbName, out var existingClass))
            {
                existing = true;
                var existingCompilationUnit = existingClass.SyntaxTree.GetCompilationUnitRoot();
                return GenerateTableDescriptor(candidate, allCodeGenTables, CodeGenTableDescriptorRenderOptions.PublicPartial, existingCompilationUnit);
            }

            existing = false;
            return GenerateTableDescriptor(candidate, allCodeGenTables, CodeGenTableDescriptorRenderOptions.PublicPartial);
        }

        public static string GetHintName(CodeGenTableModel candidate)
        {
            var typeName = string.IsNullOrEmpty(candidate.Namespace)
                ? candidate.ClassName
                : candidate.Namespace + "." + candidate.ClassName;
            return $"{typeName.Replace('<', '_').Replace('>', '_').Replace('.', '_')}.TableDescriptor.g.cs";
        }

        public static CompilationUnitSyntax GenerateTableDeclaration(
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables,
            CompilationUnitSyntax? existingCompilationUnit = null)
        {
            var validation = Validate(candidate, allTables);
            if (validation.Issues.Length > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Issues.Select(FormatValidationIssue)));
            }

            if (existingCompilationUnit == null)
            {
                return CreateTableDeclarationSyntax(candidate, validation.PropertyNamesBySqlName);
            }

            return UpdateExistingTableDeclaration(existingCompilationUnit, candidate, validation.PropertyNamesBySqlName);
        }

        internal static CompilationUnitSyntax GenerateTableDeclaration(
            TableModel table,
            IReadOnlyDictionary<TableRef, TableModel> allTables,
            string defaultNamespace,
            CompilationUnitSyntax? existingCompilationUnit = null)
        {
            var allCodeGenTables = allTables.Values
                .Select(t => ToCodeGenTableModel(t, defaultNamespace))
                .ToDictionary(static t => t.TableKey, static t => t, StringComparer.OrdinalIgnoreCase);
            var candidate = allCodeGenTables[BuildTableKey(databaseName: null, schemaName: table.DbName.Schema, tableName: table.DbName.Name)];
            return GenerateTableDeclaration(candidate, allCodeGenTables, existingCompilationUnit);
        }

        private static CompilationUnitSyntax CreateTableDescriptorSyntax(
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, string> propertyNamesBySqlName,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables,
            CodeGenTableDescriptorRenderOptions? options)
        {
            options ??= CodeGenTableDescriptorRenderOptions.Analyzer;

            var classMembers = new MemberDeclarationSyntax[]
                {
                    RenderEmptyConstructor(candidate),
                    RenderMainConstructor(candidate, propertyNamesBySqlName, allTables)
                }
                .Concat(RenderProperties(candidate, propertyNamesBySqlName))
                .ToArray();

            var classDeclaration = SyntaxFactory.ClassDeclaration(candidate.ClassName)
                .WithModifiers(GetClassModifiers(options))
                .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                    SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("TableBase")))))
                .AddMembers(classMembers);

            MemberDeclarationSyntax container = classDeclaration;
            if (!string.IsNullOrEmpty(candidate.Namespace))
            {
                container = SyntaxFactory.NamespaceDeclaration(QualifiedName(candidate.Namespace!))
                    .AddMembers(classDeclaration);
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(
                    SyntaxFactory.UsingDirective(QualifiedName("SqExpress")),
                    SyntaxFactory.UsingDirective(QualifiedName("SqExpress.Syntax.Type")))
                .AddMembers(container);

            if (options.IncludeNullableEnable)
            {
                compilationUnit = compilationUnit.WithLeadingTrivia(
                    SyntaxFactory.TriviaList(
                        CreateAutoGeneratedTrivia(options.IncludeAutoGeneratedHeader),
                        SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.EnableKeyword), true)),
                        SyntaxFactory.ElasticCarriageReturnLineFeed));
            }
            else if (options.IncludeAutoGeneratedHeader)
            {
                compilationUnit = compilationUnit.WithLeadingTrivia(SyntaxFactory.TriviaList(CreateAutoGeneratedTrivia(true)));
            }

            return compilationUnit.NormalizeWhitespace();
        }

        private static CompilationUnitSyntax CreateTableDeclarationSyntax(
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, string> propertyNamesBySqlName)
        {
            var classDeclaration = SyntaxFactory.ClassDeclaration(candidate.ClassName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithAttributeLists(SyntaxFactory.List(RenderTableDeclarationAttributes(candidate, propertyNamesBySqlName)))
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

            MemberDeclarationSyntax container = classDeclaration;
            if (!string.IsNullOrEmpty(candidate.Namespace))
            {
                container = SyntaxFactory.NamespaceDeclaration(QualifiedName(candidate.Namespace!))
                    .AddMembers(classDeclaration);
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(QualifiedName("SqExpress.TableDecalationAttributes")))
                .AddMembers(container);

            compilationUnit = compilationUnit.WithLeadingTrivia(
                SyntaxFactory.TriviaList(
                    CreateAutoGeneratedTrivia(true),
                    SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.EnableKeyword), true)),
                    SyntaxFactory.ElasticCarriageReturnLineFeed));

            return compilationUnit.NormalizeWhitespace();
        }

        private static ConstructorDeclarationSyntax RenderEmptyConstructor(CodeGenTableModel candidate)
        {
            return SyntaxFactory.ConstructorDeclaration(candidate.ClassName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithInitializer(SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.ThisConstructorInitializer,
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                        NamedArgument("alias", MemberAccess(QualifiedName("SqExpress.Alias"), "Auto"))))))
                .WithBody(SyntaxFactory.Block());
        }

        private static ConstructorDeclarationSyntax RenderMainConstructor(
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, string> propertyNamesBySqlName,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables)
        {
            var statements = new List<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>();

            foreach (var column in candidate.Columns)
            {
                statements.Add(SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccess(SyntaxFactory.ThisExpression(), propertyNamesBySqlName[column.SqlName]),
                        SyntaxFactory.InvocationExpression(
                            MemberAccess(SyntaxFactory.ThisExpression(), RenderCreateMethodName(column)),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(RenderCreateMethodArguments(column, candidate, allTables)))))));
            }

            foreach (var index in candidate.Indexes)
            {
                statements.Add(SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        MemberAccess(SyntaxFactory.ThisExpression(), RenderIndexMethodName(index)),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(RenderIndexArguments(index, propertyNamesBySqlName))))));
            }

            return SyntaxFactory.ConstructorDeclaration(candidate.ClassName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("alias"))
                        .WithType(SyntaxFactory.IdentifierName("Alias")))
                .WithInitializer(SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(RenderBaseConstructorArguments(candidate)))))
                .WithBody(SyntaxFactory.Block(statements));
        }

        private static PropertyDeclarationSyntax[] RenderProperties(CodeGenTableModel candidate, IReadOnlyDictionary<string, string> propertyNamesBySqlName)
        {
            return candidate.Columns
                .Select(column => SyntaxFactory.PropertyDeclaration(
                        RenderPropertyType(column),
                        SyntaxFactory.Identifier(propertyNamesBySqlName[column.SqlName]))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))))
                .ToArray();
        }

        private static SyntaxTokenList GetClassModifiers(CodeGenTableDescriptorRenderOptions options)
        {
            var tokens = new List<SyntaxToken>();
            if (options.IsPublic)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }

            if (options.IsPartial)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }

            return SyntaxFactory.TokenList(tokens);
        }

        private static SyntaxTrivia CreateAutoGeneratedTrivia(bool includeAutoGeneratedHeader)
            => includeAutoGeneratedHeader
                ? SyntaxFactory.Comment("// <auto-generated/>")
                : default;

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (!(char.IsLetter(value[0]) || value[0] == '_'))
            {
                return false;
            }

            for (var i = 1; i < value.Length; i++)
            {
                if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToPascalCasePart(string value)
        {
            var chars = value.Where(char.IsLetterOrDigit).ToArray();
            if (chars.Length == 0)
            {
                return string.Empty;
            }

            var text = new string(chars);
            return text.Length == 1 ? char.ToUpperInvariant(text[0]).ToString() : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        private static IEnumerable<ArgumentSyntax> RenderBaseConstructorArguments(CodeGenTableModel candidate)
        {
            if (!string.IsNullOrEmpty(candidate.DatabaseName))
            {
                yield return SyntaxFactory.Argument(Literal(candidate.DatabaseName));
                yield return SyntaxFactory.Argument(Literal(candidate.SchemaName));
                yield return SyntaxFactory.Argument(Literal(candidate.TableName));
                yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName("alias"));
                yield break;
            }

            yield return SyntaxFactory.Argument(candidate.SchemaName != null ? Literal(candidate.SchemaName) : SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
            yield return SyntaxFactory.Argument(Literal(candidate.TableName));
            yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName("alias"));
        }

        private static IEnumerable<AttributeListSyntax> RenderTableDeclarationAttributes(
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, string> propertyNamesBySqlName)
        {
            yield return SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("TableDescriptor"))
                        .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(RenderTableDescriptorAttributeArguments(candidate))))));

            foreach (var column in candidate.Columns)
            {
                yield return SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(RenderColumnAttributeName(column)))
                            .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(RenderColumnAttributeArguments(candidate, column, propertyNamesBySqlName[column.SqlName]))))));
            }

            foreach (var index in candidate.Indexes)
            {
                yield return SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Index"))
                            .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(RenderIndexAttributeArguments(index))))));
            }
        }

        private static IEnumerable<AttributeArgumentSyntax> RenderTableDescriptorAttributeArguments(CodeGenTableModel candidate)
        {
            if (!string.IsNullOrEmpty(candidate.DatabaseName))
            {
                yield return SyntaxFactory.AttributeArgument(Literal(candidate.DatabaseName));
                yield return SyntaxFactory.AttributeArgument(Literal(candidate.SchemaName));
                yield return SyntaxFactory.AttributeArgument(Literal(candidate.TableName));
                yield break;
            }

            if (!string.IsNullOrEmpty(candidate.SchemaName))
            {
                yield return SyntaxFactory.AttributeArgument(Literal(candidate.SchemaName));
            }

            yield return SyntaxFactory.AttributeArgument(Literal(candidate.TableName));
        }

        private static IEnumerable<AttributeArgumentSyntax> RenderColumnAttributeArguments(CodeGenTableModel candidate, CodeGenColumnModel column, string propertyName)
        {
            yield return SyntaxFactory.AttributeArgument(Literal(column.SqlName));

            var inferredPropertyName = ToIdentifier(column.SqlName);
            if (!string.Equals(propertyName, inferredPropertyName, StringComparison.Ordinal))
            {
                yield return NamedAttributeArgument("PropertyName", Literal(propertyName));
            }

            if (column.IsPrimaryKey)
            {
                yield return NamedAttributeArgument("Pk", BoolLiteral(true));
            }

            if (column.IsIdentity)
            {
                yield return NamedAttributeArgument("Identity", BoolLiteral(true));
            }

            if (!string.IsNullOrWhiteSpace(column.ForeignKeyDatabase))
            {
                yield return NamedAttributeArgument("FkDatabase", Literal(column.ForeignKeyDatabase));
            }

            if (!string.IsNullOrWhiteSpace(column.ForeignKeySchema) &&
                !string.Equals(column.ForeignKeySchema, candidate.SchemaName, StringComparison.OrdinalIgnoreCase))
            {
                yield return NamedAttributeArgument("FkSchema", Literal(column.ForeignKeySchema));
            }

            if (!string.IsNullOrWhiteSpace(column.ForeignKeyTable))
            {
                yield return NamedAttributeArgument("FkTable", Literal(column.ForeignKeyTable));
            }

            if (!string.IsNullOrWhiteSpace(column.ForeignKeyColumn))
            {
                yield return NamedAttributeArgument("FkColumn", Literal(column.ForeignKeyColumn));
            }

            if ((column.Kind == CodeGenColumnKind.String || column.Kind == CodeGenColumnKind.NullableString) && column.IsUnicode)
            {
                yield return NamedAttributeArgument("Unicode", BoolLiteral(true));
            }

            if (column.MaxLength.HasValue)
            {
                yield return NamedAttributeArgument("MaxLength", NumericLiteral(column.MaxLength.Value));
            }

            if (column.IsFixedLength)
            {
                yield return NamedAttributeArgument("FixedLength", BoolLiteral(true));
            }

            if ((column.Kind == CodeGenColumnKind.String || column.Kind == CodeGenColumnKind.NullableString) && column.IsText)
            {
                yield return NamedAttributeArgument("Text", BoolLiteral(true));
            }

            if (column.Kind == CodeGenColumnKind.Decimal || column.Kind == CodeGenColumnKind.NullableDecimal)
            {
                yield return NamedAttributeArgument("Precision", NumericLiteral(column.Precision));
                yield return NamedAttributeArgument("Scale", NumericLiteral(column.Scale));
            }

            if ((column.Kind == CodeGenColumnKind.DateTime || column.Kind == CodeGenColumnKind.NullableDateTime) && column.IsDate)
            {
                yield return NamedAttributeArgument("IsDate", BoolLiteral(true));
            }

            var defaultValue = RenderDefaultValueAttributeValue(column);
            if (defaultValue != null)
            {
                yield return NamedAttributeArgument("DefaultValue", Literal(defaultValue));
            }
        }

        private static IEnumerable<AttributeArgumentSyntax> RenderIndexAttributeArguments(CodeGenIndexModel index)
        {
            if (index.Columns.Length > 0)
            {
                yield return SyntaxFactory.AttributeArgument(Literal(index.Columns[0]));
                for (var i = 1; i < index.Columns.Length; i++)
                {
                    yield return SyntaxFactory.AttributeArgument(Literal(index.Columns[i]));
                }
            }

            if (!string.IsNullOrWhiteSpace(index.Name))
            {
                yield return NamedAttributeArgument("Name", Literal(index.Name));
            }

            if (index.IsUnique)
            {
                yield return NamedAttributeArgument("Unique", BoolLiteral(true));
            }

            if (index.IsClustered)
            {
                yield return NamedAttributeArgument("Clustered", BoolLiteral(true));
            }

            if (index.DescendingColumns.Length > 0)
            {
                yield return NamedAttributeArgument(
                    "DescendingColumns",
                    SyntaxFactory.ArrayCreationExpression(
                        SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                            .AddRankSpecifiers(
                                SyntaxFactory.ArrayRankSpecifier(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))))
                        .WithInitializer(
                            SyntaxFactory.InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList<ExpressionSyntax>(index.DescendingColumns.Select(Literal)))));
            }
        }

        private static IEnumerable<ArgumentSyntax> RenderCreateMethodArguments(
            CodeGenColumnModel column,
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables)
        {
            var columnMeta = RenderColumnMeta(column, candidate, allTables);
            switch (column.Kind)
            {
                case CodeGenColumnKind.String:
                case CodeGenColumnKind.NullableString:
                    if (column.IsFixedLength)
                    {
                        return new[]
                        {
                            NamedArgument("name", Literal(column.SqlName)),
                            NamedArgument("size", NullableIntLiteral(column.MaxLength)),
                            NamedArgument("isUnicode", BoolLiteral(column.IsUnicode)),
                            NamedArgument("columnMeta", columnMeta)
                        };
                    }

                    return new[]
                    {
                        NamedArgument("name", Literal(column.SqlName)),
                        NamedArgument("size", NullableIntLiteral(column.MaxLength)),
                        NamedArgument("isUnicode", BoolLiteral(column.IsUnicode)),
                        NamedArgument("isText", BoolLiteral(column.IsText)),
                        NamedArgument("columnMeta", columnMeta)
                    };
                case CodeGenColumnKind.ByteArray:
                case CodeGenColumnKind.NullableByteArray:
                    return new[]
                    {
                        SyntaxFactory.Argument(Literal(column.SqlName)),
                        SyntaxFactory.Argument(NullableIntLiteral(column.MaxLength)),
                        SyntaxFactory.Argument(columnMeta)
                    };
                case CodeGenColumnKind.Decimal:
                case CodeGenColumnKind.NullableDecimal:
                    return new[]
                    {
                        SyntaxFactory.Argument(Literal(column.SqlName)),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("DecimalPrecisionScale"))
                                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                                {
                                    NamedArgument("precision", NumericLiteral(column.Precision)),
                                    NamedArgument("scale", NumericLiteral(column.Scale))
                                })))),
                        SyntaxFactory.Argument(columnMeta)
                    };
                case CodeGenColumnKind.DateTime:
                case CodeGenColumnKind.NullableDateTime:
                    return new[]
                    {
                        SyntaxFactory.Argument(Literal(column.SqlName)),
                        SyntaxFactory.Argument(BoolLiteral(column.IsDate)),
                        SyntaxFactory.Argument(columnMeta)
                    };
                default:
                    return new[]
                    {
                        SyntaxFactory.Argument(Literal(column.SqlName)),
                        SyntaxFactory.Argument(columnMeta)
                    };
            }
        }

        private static ExpressionSyntax RenderColumnMeta(
            CodeGenColumnModel column,
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables)
        {
            ExpressionSyntax? current = null;

            void Append(SimpleNameSyntax methodName, params ArgumentSyntax[] arguments)
            {
                var host = current ?? (ExpressionSyntax)SyntaxFactory.IdentifierName("ColumnMeta");
                current = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, host, methodName),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
            }

            if (column.IsPrimaryKey)
            {
                Append(SyntaxFactory.IdentifierName("PrimaryKey"));
            }

            if (column.IsIdentity)
            {
                Append(SyntaxFactory.IdentifierName("Identity"));
            }

            if (column.DefaultValueKind != 0)
            {
                Append(SyntaxFactory.IdentifierName("DefaultValue"), SyntaxFactory.Argument(RenderDefaultValue(column)));
            }

            if (!string.IsNullOrWhiteSpace(column.ForeignKeyTable) && !string.IsNullOrWhiteSpace(column.ForeignKeyColumn))
            {
                var targetKey = BuildTableKey(column.ForeignKeyDatabase, column.ForeignKeySchema ?? candidate.SchemaName, column.ForeignKeyTable!);
                if (allTables.TryGetValue(targetKey, out var targetTable))
                {
                    var targetPropertyName = targetTable.Columns
                        .Where(c => string.Equals(c.SqlName, column.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase))
                        .Select(c => string.IsNullOrWhiteSpace(c.PropertyName) ? ToIdentifier(c.SqlName) : c.PropertyName!)
                        .First();

                    Append(
                        SyntaxFactory.GenericName("ForeignKey")
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(RenderForeignKeyTargetType(candidate, targetTable)))),
                        SyntaxFactory.Argument(
                            SyntaxFactory.SimpleLambdaExpression(
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("t")),
                                MemberAccess(SyntaxFactory.IdentifierName("t"), targetPropertyName))));
                }
            }

            return current ?? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        private static ExpressionSyntax RenderDefaultValue(CodeGenColumnModel column)
        {
            switch (column.DefaultValueKind)
            {
                case 1:
                    return SyntaxFactory.InvocationExpression(
                        MemberAccess(SyntaxFactory.IdentifierName("SqQueryBuilder"), "UnsafeValue"),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(Literal(column.DefaultValue ?? string.Empty)))));
                case 2:
                    return MemberAccess(SyntaxFactory.IdentifierName("SqQueryBuilder"), "Null");
                case 3:
                    return int.TryParse(column.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)
                        ? NumericLiteral(intValue)
                        : NumericLiteral(0);
                case 4:
                    if (bool.TryParse(column.DefaultValue, out var boolValue))
                    {
                        return BoolLiteral(boolValue);
                    }

                    if (column.DefaultValue == "0" || column.DefaultValue == "1")
                    {
                        return BoolLiteral(column.DefaultValue == "1");
                    }

                    return BoolLiteral(false);
                case 5:
                    return Literal(column.DefaultValue ?? string.Empty);
                case 6:
                    return SyntaxFactory.InvocationExpression(MemberAccess(SyntaxFactory.IdentifierName("SqQueryBuilder"), "GetUtcDate"));
                case 7:
                    return SyntaxFactory.InvocationExpression(MemberAccess(SyntaxFactory.IdentifierName("SqQueryBuilder"), "GetDate"));
                case 8:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword)),
                        NumericLiteral(byte.TryParse(column.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteValue) ? byteValue : 0));
                case 9:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword)),
                        NumericLiteral(short.TryParse(column.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortValue) ? shortValue : 0));
                case 10:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(long.TryParse(column.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue) ? longValue : 0L));
                case 11:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(decimal.TryParse(column.DefaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue) ? decimalValue : 0M));
                case 12:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(double.TryParse(column.DefaultValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue) ? doubleValue : 0D));
                case 13:
                    return SyntaxFactory.ObjectCreationExpression(QualifiedName("System.Guid"))
                        .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(Literal(column.DefaultValue ?? string.Empty)))));
                case 14:
                    return SyntaxFactory.InvocationExpression(
                        MemberAccess(QualifiedName("System.DateTime"), "Parse"),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(Literal(column.DefaultValue ?? string.Empty)),
                            SyntaxFactory.Argument(MemberAccess(QualifiedName("System.Globalization.CultureInfo"), "InvariantCulture")),
                            SyntaxFactory.Argument(MemberAccess(QualifiedName("System.Globalization.DateTimeStyles"), "RoundtripKind"))
                        })));
                case 15:
                    return SyntaxFactory.InvocationExpression(
                        MemberAccess(QualifiedName("System.DateTimeOffset"), "Parse"),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(Literal(column.DefaultValue ?? string.Empty)),
                            SyntaxFactory.Argument(MemberAccess(QualifiedName("System.Globalization.CultureInfo"), "InvariantCulture")),
                            SyntaxFactory.Argument(MemberAccess(QualifiedName("System.Globalization.DateTimeStyles"), "RoundtripKind"))
                        })));
                default:
                    return MemberAccess(SyntaxFactory.IdentifierName("SqQueryBuilder"), "Null");
            }
        }

        private static TypeSyntax RenderForeignKeyTargetType(CodeGenTableModel candidate, CodeGenTableModel targetTable)
        {
            if (string.Equals(candidate.Namespace, targetTable.Namespace, StringComparison.Ordinal))
            {
                return SyntaxFactory.IdentifierName(targetTable.ClassName);
            }

            return string.IsNullOrEmpty(targetTable.FullyQualifiedTypeName)
                ? SyntaxFactory.IdentifierName(targetTable.ClassName)
                : QualifiedName(targetTable.FullyQualifiedTypeName);
        }

        private static string RenderIndexMethodName(CodeGenIndexModel index)
        {
            if (index.IsUnique)
            {
                return index.IsClustered ? "AddUniqueClusteredIndex" : "AddUniqueIndex";
            }

            return index.IsClustered ? "AddClusteredIndex" : "AddIndex";
        }

        private static IEnumerable<ArgumentSyntax> RenderIndexArguments(CodeGenIndexModel index, IReadOnlyDictionary<string, string> propertyNamesBySqlName)
        {
            if (!string.IsNullOrWhiteSpace(index.Name))
            {
                yield return SyntaxFactory.Argument(Literal(index.Name));
            }

            foreach (var column in index.Columns)
            {
                yield return SyntaxFactory.Argument(index.DescendingColumns.Contains(column, StringComparer.OrdinalIgnoreCase)
                    ? SyntaxFactory.InvocationExpression(
                        MemberAccess(SyntaxFactory.IdentifierName("IndexMetaColumn"), "Desc"),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(MemberAccess(SyntaxFactory.ThisExpression(), propertyNamesBySqlName[column])))))
                    : MemberAccess(SyntaxFactory.ThisExpression(), propertyNamesBySqlName[column]));
            }
        }

        private static TypeSyntax RenderPropertyType(CodeGenColumnModel column)
        {
            switch (column.Kind)
            {
                case CodeGenColumnKind.Boolean: return SyntaxFactory.IdentifierName("BooleanTableColumn");
                case CodeGenColumnKind.NullableBoolean: return SyntaxFactory.IdentifierName("NullableBooleanTableColumn");
                case CodeGenColumnKind.Byte: return SyntaxFactory.IdentifierName("ByteTableColumn");
                case CodeGenColumnKind.NullableByte: return SyntaxFactory.IdentifierName("NullableByteTableColumn");
                case CodeGenColumnKind.ByteArray: return SyntaxFactory.IdentifierName("ByteArrayTableColumn");
                case CodeGenColumnKind.NullableByteArray: return SyntaxFactory.IdentifierName("NullableByteArrayTableColumn");
                case CodeGenColumnKind.Int16: return SyntaxFactory.IdentifierName("Int16TableColumn");
                case CodeGenColumnKind.NullableInt16: return SyntaxFactory.IdentifierName("NullableInt16TableColumn");
                case CodeGenColumnKind.Int32: return SyntaxFactory.IdentifierName("Int32TableColumn");
                case CodeGenColumnKind.NullableInt32: return SyntaxFactory.IdentifierName("NullableInt32TableColumn");
                case CodeGenColumnKind.Int64: return SyntaxFactory.IdentifierName("Int64TableColumn");
                case CodeGenColumnKind.NullableInt64: return SyntaxFactory.IdentifierName("NullableInt64TableColumn");
                case CodeGenColumnKind.Double: return SyntaxFactory.IdentifierName("DoubleTableColumn");
                case CodeGenColumnKind.NullableDouble: return SyntaxFactory.IdentifierName("NullableDoubleTableColumn");
                case CodeGenColumnKind.Decimal: return SyntaxFactory.IdentifierName("DecimalTableColumn");
                case CodeGenColumnKind.NullableDecimal: return SyntaxFactory.IdentifierName("NullableDecimalTableColumn");
                case CodeGenColumnKind.DateTime: return SyntaxFactory.IdentifierName("DateTimeTableColumn");
                case CodeGenColumnKind.NullableDateTime: return SyntaxFactory.IdentifierName("NullableDateTimeTableColumn");
                case CodeGenColumnKind.DateTimeOffset: return SyntaxFactory.IdentifierName("DateTimeOffsetTableColumn");
                case CodeGenColumnKind.NullableDateTimeOffset: return SyntaxFactory.IdentifierName("NullableDateTimeOffsetTableColumn");
                case CodeGenColumnKind.Guid: return SyntaxFactory.IdentifierName("GuidTableColumn");
                case CodeGenColumnKind.NullableGuid: return SyntaxFactory.IdentifierName("NullableGuidTableColumn");
                case CodeGenColumnKind.String: return SyntaxFactory.IdentifierName("StringTableColumn");
                case CodeGenColumnKind.NullableString: return SyntaxFactory.IdentifierName("NullableStringTableColumn");
                case CodeGenColumnKind.Xml: return SyntaxFactory.IdentifierName("StringTableColumn");
                case CodeGenColumnKind.NullableXml: return SyntaxFactory.IdentifierName("NullableStringTableColumn");
                default: throw new ArgumentOutOfRangeException(nameof(column.Kind), column.Kind, null);
            }
        }

        private static string RenderCreateMethodName(CodeGenColumnModel column)
        {
            switch (column.Kind)
            {
                case CodeGenColumnKind.Boolean: return "CreateBooleanColumn";
                case CodeGenColumnKind.NullableBoolean: return "CreateNullableBooleanColumn";
                case CodeGenColumnKind.Byte: return "CreateByteColumn";
                case CodeGenColumnKind.NullableByte: return "CreateNullableByteColumn";
                case CodeGenColumnKind.ByteArray: return column.IsFixedLength ? "CreateFixedSizeByteArrayColumn" : "CreateByteArrayColumn";
                case CodeGenColumnKind.NullableByteArray: return column.IsFixedLength ? "CreateNullableFixedSizeByteArrayColumn" : "CreateNullableByteArrayColumn";
                case CodeGenColumnKind.Int16: return "CreateInt16Column";
                case CodeGenColumnKind.NullableInt16: return "CreateNullableInt16Column";
                case CodeGenColumnKind.Int32: return "CreateInt32Column";
                case CodeGenColumnKind.NullableInt32: return "CreateNullableInt32Column";
                case CodeGenColumnKind.Int64: return "CreateInt64Column";
                case CodeGenColumnKind.NullableInt64: return "CreateNullableInt64Column";
                case CodeGenColumnKind.Double: return "CreateDoubleColumn";
                case CodeGenColumnKind.NullableDouble: return "CreateNullableDoubleColumn";
                case CodeGenColumnKind.Decimal: return "CreateDecimalColumn";
                case CodeGenColumnKind.NullableDecimal: return "CreateNullableDecimalColumn";
                case CodeGenColumnKind.DateTime: return "CreateDateTimeColumn";
                case CodeGenColumnKind.NullableDateTime: return "CreateNullableDateTimeColumn";
                case CodeGenColumnKind.DateTimeOffset: return "CreateDateTimeOffsetColumn";
                case CodeGenColumnKind.NullableDateTimeOffset: return "CreateNullableDateTimeOffsetColumn";
                case CodeGenColumnKind.Guid: return "CreateGuidColumn";
                case CodeGenColumnKind.NullableGuid: return "CreateNullableGuidColumn";
                case CodeGenColumnKind.String: return column.IsFixedLength ? "CreateFixedSizeStringColumn" : "CreateStringColumn";
                case CodeGenColumnKind.NullableString: return column.IsFixedLength ? "CreateNullableFixedSizeStringColumn" : "CreateNullableStringColumn";
                case CodeGenColumnKind.Xml: return "CreateXmlColumn";
                case CodeGenColumnKind.NullableXml: return "CreateNullableXmlColumn";
                default: throw new ArgumentOutOfRangeException(nameof(column.Kind), column.Kind, null);
            }
        }

        private static string RenderColumnAttributeName(CodeGenColumnModel column)
        {
            switch (column.Kind)
            {
                case CodeGenColumnKind.Boolean: return "BooleanColumn";
                case CodeGenColumnKind.NullableBoolean: return "NullableBooleanColumn";
                case CodeGenColumnKind.Byte: return "ByteColumn";
                case CodeGenColumnKind.NullableByte: return "NullableByteColumn";
                case CodeGenColumnKind.ByteArray: return "ByteArrayColumn";
                case CodeGenColumnKind.NullableByteArray: return "NullableByteArrayColumn";
                case CodeGenColumnKind.Int16: return "Int16Column";
                case CodeGenColumnKind.NullableInt16: return "NullableInt16Column";
                case CodeGenColumnKind.Int32: return "Int32Column";
                case CodeGenColumnKind.NullableInt32: return "NullableInt32Column";
                case CodeGenColumnKind.Int64: return "Int64Column";
                case CodeGenColumnKind.NullableInt64: return "NullableInt64Column";
                case CodeGenColumnKind.Double: return "DoubleColumn";
                case CodeGenColumnKind.NullableDouble: return "NullableDoubleColumn";
                case CodeGenColumnKind.Decimal: return "DecimalColumn";
                case CodeGenColumnKind.NullableDecimal: return "NullableDecimalColumn";
                case CodeGenColumnKind.DateTime: return "DateTimeColumn";
                case CodeGenColumnKind.NullableDateTime: return "NullableDateTimeColumn";
                case CodeGenColumnKind.DateTimeOffset: return "DateTimeOffsetColumn";
                case CodeGenColumnKind.NullableDateTimeOffset: return "NullableDateTimeOffsetColumn";
                case CodeGenColumnKind.Guid: return "GuidColumn";
                case CodeGenColumnKind.NullableGuid: return "NullableGuidColumn";
                case CodeGenColumnKind.String: return "StringColumn";
                case CodeGenColumnKind.NullableString: return "NullableStringColumn";
                case CodeGenColumnKind.Xml: return "XmlColumn";
                case CodeGenColumnKind.NullableXml: return "NullableXmlColumn";
                default: throw new ArgumentOutOfRangeException(nameof(column.Kind), column.Kind, null);
            }
        }

        private static string? RenderDefaultValueAttributeValue(CodeGenColumnModel column)
        {
            switch (column.DefaultValueKind)
            {
                case 0:
                    return null;
                case 1:
                    return null;
                case 2:
                    return "$null";
                case 6:
                    return "$utcNow";
                case 7:
                    return "$now";
                default:
                    return column.DefaultValue;
            }
        }

        private static NameSyntax QualifiedName(string dottedName, bool global = false)
        {
            if (dottedName.StartsWith("global::", StringComparison.Ordinal))
            {
                dottedName = dottedName.Substring("global::".Length);
                global = true;
            }

            var parts = dottedName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                throw new ArgumentException("Qualified name cannot be empty.", nameof(dottedName));
            }

            NameSyntax current = SyntaxFactory.IdentifierName(parts[0]);
            if (global)
            {
                current = SyntaxFactory.AliasQualifiedName(
                    SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                    SyntaxFactory.IdentifierName(parts[0]));
            }

            for (var i = 1; i < parts.Length; i++)
            {
                current = SyntaxFactory.QualifiedName(current, SyntaxFactory.IdentifierName(parts[i]));
            }

            return current;
        }

        private static NameSyntax GlobalName(params string[] parts)
        {
            if (parts.Length == 0)
            {
                throw new ArgumentException("Global name requires at least one part.", nameof(parts));
            }

            return QualifiedName(string.Join(".", parts), global: true);
        }

        private static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expression, string member)
            => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, SyntaxFactory.IdentifierName(member));

        private static ArgumentSyntax NamedArgument(string name, ExpressionSyntax expression)
            => SyntaxFactory.Argument(SyntaxFactory.NameColon(name), default, expression);

        private static AttributeArgumentSyntax NamedAttributeArgument(string name, ExpressionSyntax expression)
            => SyntaxFactory.AttributeArgument(expression)
                .WithNameEquals(SyntaxFactory.NameEquals(name));

        private static LiteralExpressionSyntax Literal(string? value)
            => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value ?? string.Empty));

        private static LiteralExpressionSyntax NumericLiteral(int value)
            => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));

        private static LiteralExpressionSyntax BoolLiteral(bool value)
            => SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);

        private static ExpressionSyntax NullableIntLiteral(int? value)
            => value.HasValue ? NumericLiteral(value.Value) : SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        private static CompilationUnitSyntax UpdateExistingTableDescriptor(
            CompilationUnitSyntax compilationUnit,
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, string> propertyNamesBySqlName,
            IReadOnlyDictionary<string, CodeGenTableModel> allTables)
        {
            var classDeclaration = compilationUnit.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.ValueText == candidate.ClassName)
                ?? throw new InvalidOperationException($"Could not find class declaration for {candidate.ClassName}");

            var mainConstructor = classDeclaration.DescendantNodesAndSelf()
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(c =>
                    c.ParameterList.Parameters.Count == 1 &&
                    c.ParameterList.Parameters[0].Type?.ToString() == "Alias" &&
                    c.Initializer != null &&
                    c.Initializer.Kind() == SyntaxKind.BaseConstructorInitializer);

            var newClass = classDeclaration;
            var constructorDeclaration = RenderMainConstructor(candidate, propertyNamesBySqlName, allTables);

            if (mainConstructor != null)
            {
                newClass = newClass.ReplaceNode(mainConstructor, constructorDeclaration);
            }

            var otherBaseConstructors = newClass.DescendantNodesAndSelf()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c =>
                    !c.IsEquivalentTo(constructorDeclaration) &&
                    c.Initializer != null &&
                    c.Initializer.Kind() == SyntaxKind.BaseConstructorInitializer)
                .ToList();

            if (otherBaseConstructors.Count > 0)
            {
                newClass = newClass.RemoveNodes(otherBaseConstructors, SyntaxRemoveOptions.KeepNoTrivia)!;
            }

            var columnsByPropertyName = candidate.Columns.ToDictionary(
                c => propertyNamesBySqlName[c.SqlName],
                StringComparer.Ordinal);
            var replacements = new Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax>();

            foreach (var oldProperty in newClass.DescendantNodes()
                         .OfType<PropertyDeclarationSyntax>()
                         .Where(p => columnsByPropertyName.ContainsKey(p.Identifier.ValueText)))
            {
                var columnModel = columnsByPropertyName[oldProperty.Identifier.ValueText];
                columnsByPropertyName.Remove(oldProperty.Identifier.ValueText);

                var newProperty = SyntaxFactory.PropertyDeclaration(
                        RenderPropertyType(columnModel),
                        oldProperty.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                    .WithAttributeLists(oldProperty.AttributeLists);

                replacements.Add(oldProperty, newProperty);
            }

            if (replacements.Count > 0)
            {
                newClass = newClass.ReplaceNodes(replacements.Keys, (o, _) => replacements[o]);
            }

            if (columnsByPropertyName.Count > 0)
            {
                newClass = newClass.AddMembers(columnsByPropertyName.Values
                    .Select(c => SyntaxFactory.PropertyDeclaration(
                            RenderPropertyType(c),
                            SyntaxFactory.Identifier(propertyNamesBySqlName[c.SqlName]))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))))
                    .Cast<MemberDeclarationSyntax>()
                    .ToArray());
            }

            return compilationUnit.ReplaceNode(classDeclaration, newClass).NormalizeWhitespace();
        }

        private static CompilationUnitSyntax UpdateExistingTableDeclaration(
            CompilationUnitSyntax compilationUnit,
            CodeGenTableModel candidate,
            IReadOnlyDictionary<string, string> propertyNamesBySqlName)
        {
            var classDeclaration = compilationUnit.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.ValueText == candidate.ClassName);

            if (classDeclaration == null)
            {
                return CreateTableDeclarationSyntax(candidate, propertyNamesBySqlName);
            }

            var updatedClass = classDeclaration
                .WithAttributeLists(SyntaxFactory.List(RenderTableDeclarationAttributes(candidate, propertyNamesBySqlName)));

            return EnsureUsingDirective(
                    compilationUnit.ReplaceNode(classDeclaration, updatedClass),
                    "SqExpress.TableDecalationAttributes")
                .NormalizeWhitespace();
        }

        private static CodeGenTableModel ToCodeGenTableModel(TableModel table, string defaultNamespace)
        {
            var typeNamespace = string.IsNullOrEmpty(defaultNamespace) ? null : defaultNamespace;
            var fullyQualifiedTypeName = string.IsNullOrEmpty(typeNamespace)
                ? table.Name
                : typeNamespace + "." + table.Name;

            return new CodeGenTableModel(
                databaseName: null,
                schemaName: table.DbName.Schema,
                tableName: table.DbName.Name,
                className: table.Name,
                @namespace: typeNamespace,
                fullyQualifiedTypeName: fullyQualifiedTypeName,
                columns: table.Columns.Select(ToCodeGenColumnModel).ToImmutableArray(),
                indexes: table.Indexes.Select(ToCodeGenIndexModel).ToImmutableArray());
        }

        private static CodeGenColumnModel ToCodeGenColumnModel(ColumnModel column)
        {
            var foreignKey = column.Fk?.FirstOrDefault();

            return new CodeGenColumnModel(
                kind: ToCodeGenColumnKind(column.ColumnType),
                sqlName: column.DbName.Name,
                propertyName: column.Name,
                isPrimaryKey: column.Pk.HasValue,
                isIdentity: column.Identity,
                foreignKeyDatabase: null,
                foreignKeySchema: foreignKey?.Schema,
                foreignKeyTable: foreignKey?.TableName,
                foreignKeyColumn: foreignKey?.Name,
                defaultValueKind: ToDefaultValueKind(column.DefaultValue),
                defaultValue: column.DefaultValue?.RawValue,
                isUnicode: column.ColumnType is StringColumnType stringType && stringType.IsUnicode,
                maxLength: column.ColumnType switch
                {
                    StringColumnType s => s.Size,
                    ByteArrayColumnType b => b.Size,
                    _ => null
                },
                isFixedLength: column.ColumnType switch
                {
                    StringColumnType s => s.IsFixed,
                    ByteArrayColumnType b => b.IsFixed,
                    _ => false
                },
                isText: column.ColumnType is StringColumnType textType && textType.IsText,
                precision: column.ColumnType is DecimalColumnType precisionType ? precisionType.Precision : 0,
                scale: column.ColumnType is DecimalColumnType scaleType ? scaleType.Scale : 0,
                isDate: column.ColumnType is DateTimeColumnType dateTimeType && dateTimeType.IsDate);
        }

        private static CodeGenIndexModel ToCodeGenIndexModel(IndexModel index)
        {
            return new CodeGenIndexModel(
                columns: index.Columns.Select(static c => c.DbName.Name).ToImmutableArray(),
                descendingColumns: index.Columns.Where(static c => c.IsDescending).Select(static c => c.DbName.Name).ToImmutableArray(),
                name: null,
                isUnique: index.IsUnique,
                isClustered: index.IsClustered);
        }

        private static int ToDefaultValueKind(DefaultValue? defaultValue)
        {
            if (!defaultValue.HasValue)
            {
                return 0;
            }

            return defaultValue.Value.Type switch
            {
                DefaultValueType.Raw => 1,
                DefaultValueType.Null => 2,
                DefaultValueType.Integer => 3,
                DefaultValueType.Bool => 4,
                DefaultValueType.String => 5,
                DefaultValueType.GetUtcDate => 6,
                _ => 0
            };
        }

        private static CodeGenColumnKind ToCodeGenColumnKind(ColumnType columnType)
        {
            return columnType switch
            {
                BooleanColumnType { IsNullable: false } => CodeGenColumnKind.Boolean,
                BooleanColumnType => CodeGenColumnKind.NullableBoolean,
                ByteColumnType { IsNullable: false } => CodeGenColumnKind.Byte,
                ByteColumnType => CodeGenColumnKind.NullableByte,
                ByteArrayColumnType { IsNullable: false } => CodeGenColumnKind.ByteArray,
                ByteArrayColumnType => CodeGenColumnKind.NullableByteArray,
                Int16ColumnType { IsNullable: false } => CodeGenColumnKind.Int16,
                Int16ColumnType => CodeGenColumnKind.NullableInt16,
                Int32ColumnType { IsNullable: false } => CodeGenColumnKind.Int32,
                Int32ColumnType => CodeGenColumnKind.NullableInt32,
                Int64ColumnType { IsNullable: false } => CodeGenColumnKind.Int64,
                Int64ColumnType => CodeGenColumnKind.NullableInt64,
                DoubleColumnType { IsNullable: false } => CodeGenColumnKind.Double,
                DoubleColumnType => CodeGenColumnKind.NullableDouble,
                DecimalColumnType { IsNullable: false } => CodeGenColumnKind.Decimal,
                DecimalColumnType => CodeGenColumnKind.NullableDecimal,
                DateTimeColumnType { IsNullable: false } => CodeGenColumnKind.DateTime,
                DateTimeColumnType => CodeGenColumnKind.NullableDateTime,
                DateTimeOffsetColumnType { IsNullable: false } => CodeGenColumnKind.DateTimeOffset,
                DateTimeOffsetColumnType => CodeGenColumnKind.NullableDateTimeOffset,
                GuidColumnType { IsNullable: false } => CodeGenColumnKind.Guid,
                GuidColumnType => CodeGenColumnKind.NullableGuid,
                StringColumnType { IsNullable: false } => CodeGenColumnKind.String,
                StringColumnType => CodeGenColumnKind.NullableString,
                XmlColumnType { IsNullable: false } => CodeGenColumnKind.Xml,
                XmlColumnType => CodeGenColumnKind.NullableXml,
                _ => throw new InvalidOperationException($"Unsupported column type: {columnType.GetType().Name}")
            };
        }

        private static string FormatValidationIssue(CodeGenValidationIssue issue)
        {
            return issue.Kind switch
            {
                CodeGenValidationIssueKind.DuplicateColumn => $"Duplicate column \"{issue.Subject}\" in table {issue.TableDisplayName}.",
                CodeGenValidationIssueKind.InvalidPropertyName => $"Invalid property name \"{issue.Subject}\" for column \"{issue.RelatedValue}\" in table {issue.TableDisplayName}.",
                CodeGenValidationIssueKind.DuplicatePropertyName => $"Duplicate property name \"{issue.Subject}\" in table {issue.TableDisplayName}.",
                CodeGenValidationIssueKind.UnknownIndexColumn => $"Could not find index column \"{issue.Subject}\" in table {issue.TableDisplayName}.",
                CodeGenValidationIssueKind.DescendingColumnMustBeIndexed => $"Descending column \"{issue.Subject}\" must also be included in the index for table {issue.TableDisplayName}.",
                CodeGenValidationIssueKind.ForeignKeyTableNotFound => $"Could not find foreign key table \"{issue.Subject}\" referenced by column \"{issue.RelatedValue}\" in table {issue.TableDisplayName}.",
                CodeGenValidationIssueKind.ForeignKeyColumnNotFound => $"Could not find foreign key column \"{issue.Subject}\" referenced by column \"{issue.RelatedValue}\" in table {issue.TableDisplayName}.",
                _ => $"Unknown validation issue \"{issue.Kind}\" for table {issue.TableDisplayName}."
            };
        }

        private static CompilationUnitSyntax EnsureUsingDirective(CompilationUnitSyntax compilationUnit, string namespaceName)
        {
            if (compilationUnit.Usings.Any(u => string.Equals(u.Name?.ToString(), namespaceName, StringComparison.Ordinal)))
            {
                return compilationUnit;
            }

            return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(QualifiedName(namespaceName)));
        }
    }
}
