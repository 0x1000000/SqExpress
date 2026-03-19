using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SqExpress;
using SqExpress.Analyzers.Diagnostics;
using SqExpress.CodeGen.Shared;

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

            var columns = ImmutableArray.CreateBuilder<CodeGenColumnModel>();
            var indexes = ImmutableArray.CreateBuilder<CodeGenIndexModel>();
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
                    if (TryReadColumnDescriptor(attribute, classSymbol, out var columnDescriptor, out var defaultValueDiagnostic))
                    {
                        columns.Add(columnDescriptor);
                    }
                    else if (defaultValueDiagnostic != null)
                    {
                        diagnostics.Add(defaultValueDiagnostic);
                    }

                    continue;
                }

                if (attributeTypeName == IndexAttributeName && TryReadIndexDescriptor(attribute, out var indexDescriptor))
                {
                    indexes.Add(indexDescriptor);
                }
            }

            var model = new CodeGenTableModel(
                databaseName,
                schemaName,
                tableName,
                classSymbol.Name,
                classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString(),
                classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                columns.ToImmutable(),
                indexes.ToImmutable());

            return new TableDescriptorCandidate(classSymbol, model, diagnostics.ToImmutable());
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

        private static bool TryReadColumnDescriptor(AttributeData attribute, INamedTypeSymbol classSymbol, out CodeGenColumnModel columnDescriptor, out Diagnostic? diagnostic)
        {
            columnDescriptor = null!;
            diagnostic = null;
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

            var defaultValue = GetNamedString(attribute, "DefaultValue");
            if (!TryInferDefaultValue(kind.Value, defaultValue, out var defaultValueKind, out var normalizedDefaultValue))
            {
                diagnostic = CreateDiagnostic(
                    DiagnosticDescriptors.TableDescriptorInvalidDefaultValue,
                    classSymbol,
                    defaultValue ?? string.Empty,
                    sqlName!,
                    classSymbol.Name,
                    GetColumnKindDisplayName(kind.Value),
                    GetSupportedPredefinedValuesText(kind.Value));
                return false;
            }

            columnDescriptor = new CodeGenColumnModel(
                kind.Value,
                sqlName!,
                GetNamedString(attribute, "PropertyName"),
                GetNamedBool(attribute, "Pk"),
                GetNamedBool(attribute, "Identity"),
                GetNamedString(attribute, "FkDatabase"),
                GetNamedString(attribute, "FkSchema"),
                GetNamedString(attribute, "FkTable"),
                GetNamedString(attribute, "FkColumn"),
                defaultValueKind,
                normalizedDefaultValue,
                GetNamedBool(attribute, "Unicode"),
                GetNamedNullableInt(attribute, "MaxLength"),
                GetNamedBool(attribute, "FixedLength"),
                GetNamedBool(attribute, "Text"),
                GetNamedInt(attribute, "Precision"),
                GetNamedInt(attribute, "Scale"),
                GetNamedBool(attribute, "IsDate"));
            return true;
        }

        private static bool TryReadIndexDescriptor(AttributeData attribute, out CodeGenIndexModel descriptor)
        {
            descriptor = null!;
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

            descriptor = new CodeGenIndexModel(
                columns.ToImmutableArray(),
                GetNamedArray(attribute, "DescendingColumns"),
                GetNamedString(attribute, "Name"),
                GetNamedBool(attribute, "Unique"),
                GetNamedBool(attribute, "Clustered"));
            return true;
        }

        private static CodeGenColumnKind? TryMapColumnKind(string? attributeName)
        {
            return attributeName switch
            {
                "BooleanColumnAttribute" => CodeGenColumnKind.Boolean,
                "NullableBooleanColumnAttribute" => CodeGenColumnKind.NullableBoolean,
                "ByteColumnAttribute" => CodeGenColumnKind.Byte,
                "NullableByteColumnAttribute" => CodeGenColumnKind.NullableByte,
                "ByteArrayColumnAttribute" => CodeGenColumnKind.ByteArray,
                "NullableByteArrayColumnAttribute" => CodeGenColumnKind.NullableByteArray,
                "Int16ColumnAttribute" => CodeGenColumnKind.Int16,
                "NullableInt16ColumnAttribute" => CodeGenColumnKind.NullableInt16,
                "Int32ColumnAttribute" => CodeGenColumnKind.Int32,
                "NullableInt32ColumnAttribute" => CodeGenColumnKind.NullableInt32,
                "Int64ColumnAttribute" => CodeGenColumnKind.Int64,
                "NullableInt64ColumnAttribute" => CodeGenColumnKind.NullableInt64,
                "DoubleColumnAttribute" => CodeGenColumnKind.Double,
                "NullableDoubleColumnAttribute" => CodeGenColumnKind.NullableDouble,
                "DecimalColumnAttribute" => CodeGenColumnKind.Decimal,
                "NullableDecimalColumnAttribute" => CodeGenColumnKind.NullableDecimal,
                "DateTimeColumnAttribute" => CodeGenColumnKind.DateTime,
                "NullableDateTimeColumnAttribute" => CodeGenColumnKind.NullableDateTime,
                "DateTimeOffsetColumnAttribute" => CodeGenColumnKind.DateTimeOffset,
                "NullableDateTimeOffsetColumnAttribute" => CodeGenColumnKind.NullableDateTimeOffset,
                "GuidColumnAttribute" => CodeGenColumnKind.Guid,
                "NullableGuidColumnAttribute" => CodeGenColumnKind.NullableGuid,
                "StringColumnAttribute" => CodeGenColumnKind.String,
                "NullableStringColumnAttribute" => CodeGenColumnKind.NullableString,
                "XmlColumnAttribute" => CodeGenColumnKind.Xml,
                "NullableXmlColumnAttribute" => CodeGenColumnKind.NullableXml,
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

        private static bool TryInferDefaultValue(CodeGenColumnKind columnKind, string? value, out int defaultValueKind, out string? normalizedValue)
        {
            defaultValueKind = 0;
            normalizedValue = value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (string.Equals(value, "$null", StringComparison.OrdinalIgnoreCase))
            {
                defaultValueKind = 2;
                normalizedValue = null;
                return true;
            }

            if (string.Equals(value, "$utcNow", StringComparison.OrdinalIgnoreCase))
            {
                defaultValueKind = 6;
                normalizedValue = null;
                return columnKind is CodeGenColumnKind.DateTime or CodeGenColumnKind.NullableDateTime or CodeGenColumnKind.DateTimeOffset or CodeGenColumnKind.NullableDateTimeOffset;
            }

            if (string.Equals(value, "$now", StringComparison.OrdinalIgnoreCase))
            {
                defaultValueKind = 7;
                normalizedValue = null;
                return columnKind is CodeGenColumnKind.DateTime or CodeGenColumnKind.NullableDateTime or CodeGenColumnKind.DateTimeOffset or CodeGenColumnKind.NullableDateTimeOffset;
            }

            switch (columnKind)
            {
                case CodeGenColumnKind.Boolean:
                case CodeGenColumnKind.NullableBoolean:
                    if (bool.TryParse(value, out var boolValue))
                    {
                        defaultValueKind = 4;
                        normalizedValue = boolValue ? "true" : "false";
                        return true;
                    }

                    if (value == "0" || value == "1")
                    {
                        defaultValueKind = 4;
                        normalizedValue = value;
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Byte:
                case CodeGenColumnKind.NullableByte:
                    if (byte.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var byteValue))
                    {
                        defaultValueKind = 8;
                        normalizedValue = byteValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Int16:
                case CodeGenColumnKind.NullableInt16:
                    if (short.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var shortValue))
                    {
                        defaultValueKind = 9;
                        normalizedValue = shortValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Int32:
                case CodeGenColumnKind.NullableInt32:
                    if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var intValue))
                    {
                        defaultValueKind = 3;
                        normalizedValue = intValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Int64:
                case CodeGenColumnKind.NullableInt64:
                    if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var longValue))
                    {
                        defaultValueKind = 10;
                        normalizedValue = longValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Decimal:
                case CodeGenColumnKind.NullableDecimal:
                    if (decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
                    {
                        defaultValueKind = 11;
                        normalizedValue = decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Double:
                case CodeGenColumnKind.NullableDouble:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        defaultValueKind = 12;
                        normalizedValue = doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Guid:
                case CodeGenColumnKind.NullableGuid:
                    if (Guid.TryParse(value, out var guidValue))
                    {
                        defaultValueKind = 13;
                        normalizedValue = guidValue.ToString("D");
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.DateTime:
                case CodeGenColumnKind.NullableDateTime:
                    if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTimeValue))
                    {
                        defaultValueKind = 14;
                        normalizedValue = dateTimeValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.DateTimeOffset:
                case CodeGenColumnKind.NullableDateTimeOffset:
                    if (DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTimeOffsetValue))
                    {
                        defaultValueKind = 15;
                        normalizedValue = dateTimeOffsetValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.String:
                case CodeGenColumnKind.NullableString:
                case CodeGenColumnKind.Xml:
                case CodeGenColumnKind.NullableXml:
                    defaultValueKind = 5;
                    normalizedValue = value;
                    return true;
                default:
                    return false;
            }
        }

        private static string GetColumnKindDisplayName(CodeGenColumnKind columnKind)
            => columnKind switch
            {
                CodeGenColumnKind.Boolean => "BooleanColumn",
                CodeGenColumnKind.NullableBoolean => "NullableBooleanColumn",
                CodeGenColumnKind.Byte => "ByteColumn",
                CodeGenColumnKind.NullableByte => "NullableByteColumn",
                CodeGenColumnKind.ByteArray => "ByteArrayColumn",
                CodeGenColumnKind.NullableByteArray => "NullableByteArrayColumn",
                CodeGenColumnKind.Int16 => "Int16Column",
                CodeGenColumnKind.NullableInt16 => "NullableInt16Column",
                CodeGenColumnKind.Int32 => "Int32Column",
                CodeGenColumnKind.NullableInt32 => "NullableInt32Column",
                CodeGenColumnKind.Int64 => "Int64Column",
                CodeGenColumnKind.NullableInt64 => "NullableInt64Column",
                CodeGenColumnKind.Double => "DoubleColumn",
                CodeGenColumnKind.NullableDouble => "NullableDoubleColumn",
                CodeGenColumnKind.Decimal => "DecimalColumn",
                CodeGenColumnKind.NullableDecimal => "NullableDecimalColumn",
                CodeGenColumnKind.DateTime => "DateTimeColumn",
                CodeGenColumnKind.NullableDateTime => "NullableDateTimeColumn",
                CodeGenColumnKind.DateTimeOffset => "DateTimeOffsetColumn",
                CodeGenColumnKind.NullableDateTimeOffset => "NullableDateTimeOffsetColumn",
                CodeGenColumnKind.Guid => "GuidColumn",
                CodeGenColumnKind.NullableGuid => "NullableGuidColumn",
                CodeGenColumnKind.String => "StringColumn",
                CodeGenColumnKind.NullableString => "NullableStringColumn",
                CodeGenColumnKind.Xml => "XmlColumn",
                CodeGenColumnKind.NullableXml => "NullableXmlColumn",
                _ => columnKind.ToString()
            };

        private static string GetSupportedPredefinedValuesText(CodeGenColumnKind columnKind)
        {
            if (columnKind is CodeGenColumnKind.DateTime or CodeGenColumnKind.NullableDateTime or CodeGenColumnKind.DateTimeOffset or CodeGenColumnKind.NullableDateTimeOffset)
            {
                return "$null, $utcNow, $now";
            }

            return "$null";
        }

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

        private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
        {
            var location = symbol.Locations.FirstOrDefault(static l => l.IsInSource) ?? Location.None;
            return Diagnostic.Create(descriptor, location, args);
        }

        private static Diagnostic CreateValidationDiagnostic(CodeGenValidationIssue issue, ISymbol symbol)
        {
            switch (issue.Kind)
            {
                case CodeGenValidationIssueKind.DuplicateColumn:
                    return CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDuplicateColumn, symbol, issue.Subject, issue.TableDisplayName);
                case CodeGenValidationIssueKind.InvalidPropertyName:
                    return CreateDiagnostic(DiagnosticDescriptors.TableDescriptorHasInvalidPropertyName, symbol, issue.Subject, issue.RelatedValue ?? string.Empty, symbol.Name);
                case CodeGenValidationIssueKind.DuplicatePropertyName:
                    return CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDuplicatePropertyName, symbol, issue.Subject, symbol.Name);
                case CodeGenValidationIssueKind.UnknownIndexColumn:
                    return CreateDiagnostic(DiagnosticDescriptors.TableDescriptorUnknownIndexColumn, symbol, issue.Subject, issue.TableDisplayName);
                case CodeGenValidationIssueKind.DescendingColumnMustBeIndexed:
                    return CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDescendingColumnMustBeIndexed, symbol, issue.Subject, issue.TableDisplayName);
                case CodeGenValidationIssueKind.ForeignKeyTableNotFound:
                    return CreateDiagnostic(DiagnosticDescriptors.TableDescriptorForeignKeyTableNotFound, symbol, issue.Subject, issue.TableDisplayName, issue.RelatedValue ?? string.Empty);
                case CodeGenValidationIssueKind.ForeignKeyColumnNotFound:
                    return CreateDiagnostic(DiagnosticDescriptors.TableDescriptorForeignKeyColumnNotFound, symbol, issue.Subject, issue.TableDisplayName, issue.RelatedValue ?? string.Empty);
                default:
                    throw new ArgumentOutOfRangeException(nameof(issue.Kind), issue.Kind, null);
            }
        }

        private sealed class TableDescriptorCandidate
        {
            public TableDescriptorCandidate(INamedTypeSymbol symbol, CodeGenTableModel model, ImmutableArray<Diagnostic> diagnostics)
            {
                this.Symbol = symbol;
                this.Model = model;
                this.Diagnostics = diagnostics;
            }

            public INamedTypeSymbol Symbol { get; }

            public CodeGenTableModel Model { get; }

            public ImmutableArray<Diagnostic> Diagnostics { get; }
        }
    }
}
