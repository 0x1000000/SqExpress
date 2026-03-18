using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SqExpress;
using SqExpress.Analyzers.Diagnostics;

namespace SqExpress.Analyzers
{
    public sealed partial class TableDescriptorSourceGenerator
    {
        private static TableDescriptorCandidate? CreateCandidate(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            var tableDescriptorAttribute = context.Attributes.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == TableDescriptorAttributeName);
            if (tableDescriptorAttribute == null)
            {
                return null;
            }

            var columns = ImmutableArray.CreateBuilder<ColumnDescriptor>();
            var indexes = ImmutableArray.CreateBuilder<IndexDescriptor>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            if (classSymbol.TypeKind != TypeKind.Class)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBeClass, classSymbol, classSymbol.Name));
            }

            if (classSymbol.DeclaringSyntaxReferences.All(static r => r.GetSyntax() is not Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax c || !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBePartial, classSymbol, classSymbol.Name));
            }

            if (classSymbol.ContainingType != null)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBeTopLevel, classSymbol, classSymbol.Name));
            }

            if (classSymbol.Arity != 0)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBeNonGeneric, classSymbol, classSymbol.Name));
            }

            if (classSymbol.BaseType != null &&
                classSymbol.BaseType.SpecialType != SpecialType.System_Object &&
                classSymbol.BaseType.ToDisplayString() != "SqExpress.TableBase")
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustNotSpecifyBaseType, classSymbol, classSymbol.Name));
            }

            if (!TryReadTableIdentity(tableDescriptorAttribute, out var databaseName, out var schemaName, out var tableName))
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorHasInvalidDeclaration, classSymbol, classSymbol.Name));
            }

            foreach (var attribute in classSymbol.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass == null)
                {
                    continue;
                }

                var attributeTypeName = attributeClass.ToDisplayString();
                if (attributeTypeName == TableDescriptorAttributeName)
                {
                    continue;
                }

                if (InheritsFrom(attributeClass, ColumnAttributeBaseName))
                {
                    if (TryReadColumnDescriptor(attribute, out var columnDescriptor))
                    {
                        columns.Add(columnDescriptor);
                    }

                    continue;
                }

                if (attributeTypeName == IndexAttributeName && TryReadIndexDescriptor(attribute, out var indexDescriptor))
                {
                    indexes.Add(indexDescriptor);
                }
            }

            return new TableDescriptorCandidate(
                classSymbol,
                databaseName,
                schemaName,
                tableName,
                columns.ToImmutable(),
                indexes.ToImmutable(),
                diagnostics.ToImmutable());
        }

        private static ValidationResult ValidateCandidate(
            TableDescriptorCandidate candidate,
            IReadOnlyDictionary<string, TableDescriptorCandidate> allTables)
        {
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            var propertyNamesBySqlName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            var sqlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var column in candidate.Columns)
            {
                if (!sqlNames.Add(column.SqlName))
                {
                    diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDuplicateColumn, candidate.Symbol, column.SqlName, candidate.TableDisplayName));
                    continue;
                }

                var propertyName = string.IsNullOrWhiteSpace(column.PropertyName) ? ToIdentifier(column.SqlName) : column.PropertyName!;
                if (!SyntaxFacts.IsValidIdentifier(propertyName))
                {
                    diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorHasInvalidPropertyName, candidate.Symbol, propertyName, column.SqlName, candidate.Symbol.Name));
                    continue;
                }

                if (!propertyNames.Add(propertyName))
                {
                    diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDuplicatePropertyName, candidate.Symbol, propertyName, candidate.Symbol.Name));
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
                        diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorUnknownIndexColumn, candidate.Symbol, columnName, candidate.TableDisplayName));
                    }
                }

                foreach (var columnName in index.DescendingColumns)
                {
                    if (!propertyNamesBySqlName.ContainsKey(columnName))
                    {
                        diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorUnknownIndexColumn, candidate.Symbol, columnName, candidate.TableDisplayName));
                    }
                    else if (!index.Columns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                    {
                        diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDescendingColumnMustBeIndexed, candidate.Symbol, columnName, candidate.TableDisplayName));
                    }
                }
            }

            foreach (var column in candidate.Columns.Where(static c => !string.IsNullOrWhiteSpace(c.ForeignKeyTable) && !string.IsNullOrWhiteSpace(c.ForeignKeyColumn)))
            {
                var targetKey = BuildTableKey(column.ForeignKeyDatabase, column.ForeignKeySchema ?? candidate.SchemaName, column.ForeignKeyTable!);
                if (!allTables.TryGetValue(targetKey, out var targetTable))
                {
                    diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorForeignKeyTableNotFound, candidate.Symbol, column.ForeignKeyTable!, candidate.TableDisplayName, column.SqlName));
                    continue;
                }

                if (!targetTable.Columns.Any(c => string.Equals(c.SqlName, column.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorForeignKeyColumnNotFound, candidate.Symbol, column.ForeignKeyColumn!, targetTable.TableDisplayName, column.SqlName));
                }
            }

            return new ValidationResult(propertyNamesBySqlName.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase), diagnostics.ToImmutable());
        }

        private static bool TryReadTableIdentity(AttributeData attribute, out string? databaseName, out string? schemaName, out string tableName)
        {
            databaseName = null;
            schemaName = null;
            tableName = string.Empty;

            switch (attribute.ConstructorArguments.Length)
            {
                case 1:
                    tableName = attribute.ConstructorArguments[0].Value as string ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(tableName);
                case 2:
                    schemaName = attribute.ConstructorArguments[0].Value as string;
                    tableName = attribute.ConstructorArguments[1].Value as string ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(schemaName) && !string.IsNullOrWhiteSpace(tableName);
                case 3:
                    databaseName = attribute.ConstructorArguments[0].Value as string;
                    schemaName = attribute.ConstructorArguments[1].Value as string;
                    tableName = attribute.ConstructorArguments[2].Value as string ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(databaseName) && !string.IsNullOrWhiteSpace(schemaName) && !string.IsNullOrWhiteSpace(tableName);
                default:
                    return false;
            }
        }

        private static bool TryReadColumnDescriptor(AttributeData attribute, out ColumnDescriptor columnDescriptor)
        {
            columnDescriptor = default;
            var sqlName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
            if (string.IsNullOrWhiteSpace(sqlName))
            {
                return false;
            }

            var kind = TryMapColumnKind(attribute.AttributeClass?.Name);
            if (!kind.HasValue)
            {
                return false;
            }

            columnDescriptor = new ColumnDescriptor(
                kind.Value,
                sqlName!,
                GetNamedString(attribute, "PropertyName"),
                GetNamedBool(attribute, "Pk"),
                GetNamedBool(attribute, "Identity"),
                GetNamedString(attribute, "FkDatabase"),
                GetNamedString(attribute, "FkSchema"),
                GetNamedString(attribute, "FkTable"),
                GetNamedString(attribute, "FkColumn"),
                GetNamedEnumValue<TableColumnDefaultValueKind>(attribute, "DefaultValueKind"),
                GetNamedString(attribute, "DefaultValue"),
                GetNamedBool(attribute, "Unicode"),
                GetNamedNullableInt(attribute, "MaxLength"),
                GetNamedBool(attribute, "FixedLength"),
                GetNamedBool(attribute, "Text"),
                GetNamedInt(attribute, "Precision"),
                GetNamedInt(attribute, "Scale"),
                GetNamedBool(attribute, "IsDate"));
            return true;
        }

        private static bool TryReadIndexDescriptor(AttributeData attribute, out IndexDescriptor descriptor)
        {
            descriptor = default;
            if (attribute.ConstructorArguments.Length == 0)
            {
                return false;
            }

            var columns = new List<string>();
            foreach (var constructorArgument in attribute.ConstructorArguments)
            {
                if (constructorArgument.Kind == TypedConstantKind.Array)
                {
                    columns.AddRange(constructorArgument.Values.Select(static i => i.Value as string).Where(static i => !string.IsNullOrWhiteSpace(i))!);
                }
                else if (constructorArgument.Value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    columns.Add(stringValue);
                }
            }

            if (columns.Count == 0)
            {
                return false;
            }

            descriptor = new IndexDescriptor(
                columns.ToImmutableArray(),
                GetNamedArray(attribute, "DescendingColumns"),
                GetNamedString(attribute, "Name"),
                GetNamedBool(attribute, "Unique"),
                GetNamedBool(attribute, "Clustered"));
            return true;
        }

        private static ColumnKind? TryMapColumnKind(string? attributeName)
        {
            return attributeName switch
            {
                "BooleanColumnAttribute" => ColumnKind.Boolean,
                "NullableBooleanColumnAttribute" => ColumnKind.NullableBoolean,
                "ByteColumnAttribute" => ColumnKind.Byte,
                "NullableByteColumnAttribute" => ColumnKind.NullableByte,
                "ByteArrayColumnAttribute" => ColumnKind.ByteArray,
                "NullableByteArrayColumnAttribute" => ColumnKind.NullableByteArray,
                "Int16ColumnAttribute" => ColumnKind.Int16,
                "NullableInt16ColumnAttribute" => ColumnKind.NullableInt16,
                "Int32ColumnAttribute" => ColumnKind.Int32,
                "NullableInt32ColumnAttribute" => ColumnKind.NullableInt32,
                "Int64ColumnAttribute" => ColumnKind.Int64,
                "NullableInt64ColumnAttribute" => ColumnKind.NullableInt64,
                "DoubleColumnAttribute" => ColumnKind.Double,
                "NullableDoubleColumnAttribute" => ColumnKind.NullableDouble,
                "DecimalColumnAttribute" => ColumnKind.Decimal,
                "NullableDecimalColumnAttribute" => ColumnKind.NullableDecimal,
                "DateTimeColumnAttribute" => ColumnKind.DateTime,
                "NullableDateTimeColumnAttribute" => ColumnKind.NullableDateTime,
                "DateTimeOffsetColumnAttribute" => ColumnKind.DateTimeOffset,
                "NullableDateTimeOffsetColumnAttribute" => ColumnKind.NullableDateTimeOffset,
                "GuidColumnAttribute" => ColumnKind.Guid,
                "NullableGuidColumnAttribute" => ColumnKind.NullableGuid,
                "StringColumnAttribute" => ColumnKind.String,
                "NullableStringColumnAttribute" => ColumnKind.NullableString,
                "XmlColumnAttribute" => ColumnKind.Xml,
                "NullableXmlColumnAttribute" => ColumnKind.NullableXml,
                _ => null
            };
        }

        private static bool InheritsFrom(INamedTypeSymbol? symbol, string metadataName)
        {
            while (symbol != null)
            {
                if (symbol.ToDisplayString() == metadataName)
                {
                    return true;
                }

                symbol = symbol.BaseType;
            }

            return false;
        }

        private static string? GetNamedString(AttributeData attribute, string name)
            => attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as string;

        private static bool GetNamedBool(AttributeData attribute, string name)
            => attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as bool? ?? false;

        private static int GetNamedInt(AttributeData attribute, string name)
            => attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as int? ?? 0;

        private static int? GetNamedNullableInt(AttributeData attribute, string name)
        {
            var value = attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as int?;
            return value.HasValue && value.Value >= 0 ? value : null;
        }

        private static ImmutableArray<string> GetNamedArray(AttributeData attribute, string name)
        {
            var argument = attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value;
            if (argument.Kind != TypedConstantKind.Array)
            {
                return ImmutableArray<string>.Empty;
            }

            return argument.Values.Select(static i => i.Value as string).Where(static i => !string.IsNullOrWhiteSpace(i)).Cast<string>().ToImmutableArray();
        }

        private static TEnum GetNamedEnumValue<TEnum>(AttributeData attribute, string name) where TEnum : struct, Enum
        {
            var value = attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value;
            return value is int intValue ? (TEnum)Enum.ToObject(typeof(TEnum), intValue) : default;
        }

        private static string ToIdentifier(string value)
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

        private static string BuildTableKey(string? databaseName, string? schemaName, string tableName)
            => string.IsNullOrEmpty(databaseName) ? $"[{schemaName ?? string.Empty}].[{tableName}]" : $"[{databaseName}].[{schemaName ?? string.Empty}].[{tableName}]";

        private static string Literal(string? value)
            => SymbolDisplay.FormatLiteral(value ?? string.Empty, quote: true);

        private static string RenderBool(bool value)
            => value ? "true" : "false";

        private static string RenderNullableInt(int? value)
            => value?.ToString(CultureInfo.InvariantCulture) ?? "null";

        private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
        {
            var location = symbol.Locations.FirstOrDefault(static l => l.IsInSource) ?? Location.None;
            return Diagnostic.Create(descriptor, location, args);
        }

        private readonly struct ValidationResult
        {
            public ValidationResult(ImmutableDictionary<string, string> propertyNamesBySqlName, ImmutableArray<Diagnostic> diagnostics)
            {
                this.PropertyNamesBySqlName = propertyNamesBySqlName;
                this.Diagnostics = diagnostics;
            }

            public ImmutableDictionary<string, string> PropertyNamesBySqlName { get; }

            public ImmutableArray<Diagnostic> Diagnostics { get; }
        }

        private sealed class TableDescriptorCandidate
        {
            public TableDescriptorCandidate(
                INamedTypeSymbol symbol,
                string? databaseName,
                string? schemaName,
                string tableName,
                ImmutableArray<ColumnDescriptor> columns,
                ImmutableArray<IndexDescriptor> indexes,
                ImmutableArray<Diagnostic> diagnostics)
            {
                this.Symbol = symbol;
                this.DatabaseName = databaseName;
                this.SchemaName = schemaName;
                this.TableName = tableName;
                this.Columns = columns;
                this.Indexes = indexes;
                this.Diagnostics = diagnostics;
            }

            public INamedTypeSymbol Symbol { get; }

            public string? DatabaseName { get; }

            public string? SchemaName { get; }

            public string TableName { get; }

            public ImmutableArray<ColumnDescriptor> Columns { get; }

            public ImmutableArray<IndexDescriptor> Indexes { get; }

            public ImmutableArray<Diagnostic> Diagnostics { get; }

            public string TableKey => BuildTableKey(this.DatabaseName, this.SchemaName, this.TableName);

            public string TableDisplayName => this.TableKey;
        }

        private readonly struct IndexDescriptor
        {
            public IndexDescriptor(ImmutableArray<string> columns, ImmutableArray<string> descendingColumns, string? name, bool isUnique, bool isClustered)
            {
                this.Columns = columns;
                this.DescendingColumns = descendingColumns;
                this.Name = name;
                this.IsUnique = isUnique;
                this.IsClustered = isClustered;
            }

            public ImmutableArray<string> Columns { get; }

            public ImmutableArray<string> DescendingColumns { get; }

            public string? Name { get; }

            public bool IsUnique { get; }

            public bool IsClustered { get; }
        }

        private readonly struct ColumnDescriptor
        {
            public ColumnDescriptor(
                ColumnKind kind,
                string sqlName,
                string? propertyName,
                bool isPrimaryKey,
                bool isIdentity,
                string? foreignKeyDatabase,
                string? foreignKeySchema,
                string? foreignKeyTable,
                string? foreignKeyColumn,
                TableColumnDefaultValueKind defaultValueKind,
                string? defaultValue,
                bool isUnicode,
                int? maxLength,
                bool isFixedLength,
                bool isText,
                int precision,
                int scale,
                bool isDate)
            {
                this.Kind = kind;
                this.SqlName = sqlName;
                this.PropertyName = propertyName;
                this.IsPrimaryKey = isPrimaryKey;
                this.IsIdentity = isIdentity;
                this.ForeignKeyDatabase = foreignKeyDatabase;
                this.ForeignKeySchema = foreignKeySchema;
                this.ForeignKeyTable = foreignKeyTable;
                this.ForeignKeyColumn = foreignKeyColumn;
                this.DefaultValueKind = defaultValueKind;
                this.DefaultValue = defaultValue;
                this.IsUnicode = isUnicode;
                this.MaxLength = maxLength;
                this.IsFixedLength = isFixedLength;
                this.IsText = isText;
                this.Precision = precision;
                this.Scale = scale;
                this.IsDate = isDate;
            }

            public ColumnKind Kind { get; }

            public string SqlName { get; }

            public string? PropertyName { get; }

            public bool IsPrimaryKey { get; }

            public bool IsIdentity { get; }

            public string? ForeignKeyDatabase { get; }

            public string? ForeignKeySchema { get; }

            public string? ForeignKeyTable { get; }

            public string? ForeignKeyColumn { get; }

            public TableColumnDefaultValueKind DefaultValueKind { get; }

            public string? DefaultValue { get; }

            public bool IsUnicode { get; }

            public int? MaxLength { get; }

            public bool IsFixedLength { get; }

            public bool IsText { get; }

            public int Precision { get; }

            public int Scale { get; }

            public bool IsDate { get; }
        }

        private enum ColumnKind
        {
            Boolean,
            NullableBoolean,
            Byte,
            NullableByte,
            ByteArray,
            NullableByteArray,
            Int16,
            NullableInt16,
            Int32,
            NullableInt32,
            Int64,
            NullableInt64,
            Double,
            NullableDouble,
            Decimal,
            NullableDecimal,
            DateTime,
            NullableDateTime,
            DateTimeOffset,
            NullableDateTimeOffset,
            Guid,
            NullableGuid,
            String,
            NullableString,
            Xml,
            NullableXml
        }
    }
}
