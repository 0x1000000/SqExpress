using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.Analyzers.Diagnostics;
using SqExpress.CodeGen.Shared;
using SqExpress.TableDecalationAttributes;

namespace SqExpress.Analyzers
{
    public sealed partial class TableDescriptorSourceGenerator
    {
        private static TableDescriptorCandidate? CreateCandidate(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclaration ||
                context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            var allAttributes = classSymbol.GetAttributes();
            var tableDescriptorAttribute = allAttributes.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == TableDescriptorAttributeName);
            var tempTableDescriptorAttribute = allAttributes.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == TempTableDescriptorAttributeName);
            var derivedTableDescriptorAttribute = allAttributes.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == DerivedTableDescriptorAttributeName);
            var hasDerivedColumnAttributes = allAttributes.Any(static a => InheritsFrom(a.AttributeClass, DerivedColumnAttributeBaseName));

            if (tableDescriptorAttribute == null && tempTableDescriptorAttribute == null && derivedTableDescriptorAttribute == null && !hasDerivedColumnAttributes)
            {
                return null;
            }

            var activeDescriptorAttribute = tableDescriptorAttribute ?? tempTableDescriptorAttribute ?? derivedTableDescriptorAttribute!;
            var tableKind = derivedTableDescriptorAttribute != null || hasDerivedColumnAttributes
                ? CodeGenTableKind.DerivedTable
                : tempTableDescriptorAttribute != null
                    ? CodeGenTableKind.TempTable
                    : CodeGenTableKind.Table;

            var columns = ImmutableArray.CreateBuilder<CodeGenColumnModel>();
            var indexes = ImmutableArray.CreateBuilder<CodeGenIndexModel>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            var descriptorCount = (tableDescriptorAttribute != null ? 1 : 0)
                                + (tempTableDescriptorAttribute != null ? 1 : 0)
                                + (derivedTableDescriptorAttribute != null ? 1 : 0);
            if (tableDescriptorAttribute != null && tempTableDescriptorAttribute != null)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorAndTempTableDescriptorAreMutuallyExclusive, GetAttributeLocation(tableDescriptorAttribute, classSymbol), classSymbol.Name));
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorAndTempTableDescriptorAreMutuallyExclusive, GetAttributeLocation(tempTableDescriptorAttribute, classSymbol), classSymbol.Name));
            }
            else if (descriptorCount > 1)
            {
                foreach (var descriptorAttribute in allAttributes.Where(static a =>
                             a.AttributeClass?.ToDisplayString() == TableDescriptorAttributeName ||
                             a.AttributeClass?.ToDisplayString() == TempTableDescriptorAttributeName ||
                             a.AttributeClass?.ToDisplayString() == DerivedTableDescriptorAttributeName))
                {
                    diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorAttributesAreMutuallyExclusive, GetAttributeLocation(descriptorAttribute, classSymbol), classSymbol.Name));
                }
            }

            if (classSymbol.TypeKind != TypeKind.Class)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBeClass, classSymbol, classSymbol.Name));
            }

            if (classSymbol.DeclaringSyntaxReferences.All(static r => r.GetSyntax() is not Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax c || !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBePartial, classSymbol, classSymbol.Name));
            }

            var containingTypes = GetContainingTypes(classSymbol);
            if (classSymbol.ContainingType != null && containingTypes.IsDefault)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBeTopLevel, classSymbol, classSymbol.Name));
            }

            if (classSymbol.Arity != 0)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustBeNonGeneric, classSymbol, classSymbol.Name));
            }

            if (classSymbol.BaseType != null &&
                classSymbol.BaseType.SpecialType != SpecialType.System_Object &&
                classSymbol.BaseType.ToDisplayString() != typeof(TableBase).FullName)
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorMustNotSpecifyBaseType, classSymbol, classSymbol.Name));
            }

            string? databaseName = null;
            string? schemaName = null;
            var tableName = classSymbol.Name;
            if (activeDescriptorAttribute != null &&
                !TryReadTableIdentity(activeDescriptorAttribute, tableKind, classSymbol.Name, out databaseName, out schemaName, out tableName))
            {
                diagnostics.Add(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorHasInvalidDeclaration, GetAttributeLocation(activeDescriptorAttribute, classSymbol), classSymbol.Name));
            }

            var tableAttributeLocation = activeDescriptorAttribute != null
                ? GetAttributeLocation(activeDescriptorAttribute, classSymbol)
                : classSymbol.Locations.FirstOrDefault(static l => l.IsInSource) ?? Location.None;
            var columnLocationsBySqlName = ImmutableDictionary.CreateBuilder<string, ImmutableArray<Location>.Builder>(StringComparer.OrdinalIgnoreCase);
            var propertyLocationsByName = ImmutableDictionary.CreateBuilder<string, ImmutableArray<Location>.Builder>(StringComparer.Ordinal);
            var indexLocations = ImmutableArray.CreateBuilder<IndexAttributeLocation>();

            foreach (var attribute in classSymbol.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass == null)
                {
                    continue;
                }

                var attributeTypeName = attributeClass.ToDisplayString();
                if (attributeTypeName == TableDescriptorAttributeName || attributeTypeName == TempTableDescriptorAttributeName || attributeTypeName == DerivedTableDescriptorAttributeName)
                {
                    continue;
                }

                if (InheritsFrom(attributeClass, ColumnAttributeBaseName))
                {
                    var attributeLocation = GetAttributeLocation(attribute, classSymbol);
                    if (derivedTableDescriptorAttribute != null)
                    {
                        diagnostics.Add(CreateDiagnostic(
                            DiagnosticDescriptors.ColumnAttributeIsNotCompatibleWithDescriptor,
                            attributeLocation,
                            TrimAttributeSuffix(attributeClass.Name),
                            classSymbol.Name,
                            nameof(DerivedTableDescriptorAttribute).Replace(nameof(Attribute), string.Empty)));
                        continue;
                    }

                    if (TryReadColumnDescriptor(attribute, classSymbol, out var columnDescriptor, out var defaultValueDiagnostic))
                    {
                        columns.Add(columnDescriptor);
                        AddLocation(columnLocationsBySqlName, columnDescriptor.SqlName, attributeLocation);
                        AddLocation(propertyLocationsByName, string.IsNullOrWhiteSpace(columnDescriptor.PropertyName) ? CodeGenTableDescriptorSupport.ToIdentifier(columnDescriptor.SqlName) : columnDescriptor.PropertyName!, attributeLocation);
                    }
                    else if (defaultValueDiagnostic != null)
                    {
                        diagnostics.Add(defaultValueDiagnostic);
                    }

                    continue;
                }

                if (InheritsFrom(attributeClass, DerivedColumnAttributeBaseName))
                {
                    var attributeLocation = GetAttributeLocation(attribute, classSymbol);
                    if (derivedTableDescriptorAttribute == null)
                    {
                        if (tableDescriptorAttribute != null || tempTableDescriptorAttribute != null)
                        {
                            diagnostics.Add(CreateDiagnostic(
                                DiagnosticDescriptors.ColumnAttributeIsNotCompatibleWithDescriptor,
                                attributeLocation,
                                TrimAttributeSuffix(attributeClass.Name),
                                classSymbol.Name,
                                tableDescriptorAttribute != null
                                    ? nameof(TableDescriptorAttribute).Replace(nameof(Attribute), string.Empty)
                                    : nameof(TempTableDescriptorAttribute).Replace(nameof(Attribute), string.Empty)));
                        }
                        else
                        {
                            diagnostics.Add(CreateDiagnostic(
                                DiagnosticDescriptors.DerivedColumnRequiresDerivedTableDescriptor,
                                attributeLocation,
                                TrimAttributeSuffix(attributeClass.Name),
                                classSymbol.Name));
                        }

                        continue;
                    }

                    if (TryReadColumnDescriptor(attribute, classSymbol, out var columnDescriptor, out var defaultValueDiagnostic))
                    {
                        columns.Add(columnDescriptor);
                        AddLocation(columnLocationsBySqlName, columnDescriptor.SqlName, attributeLocation);
                        AddLocation(propertyLocationsByName, string.IsNullOrWhiteSpace(columnDescriptor.PropertyName) ? CodeGenTableDescriptorSupport.ToIdentifier(columnDescriptor.SqlName) : columnDescriptor.PropertyName!, attributeLocation);
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
                    indexLocations.Add(new IndexAttributeLocation(
                        GetAttributeLocation(attribute, classSymbol),
                        indexDescriptor.Columns.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
                        indexDescriptor.DescendingColumns.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)));
                }
            }

            var model = new CodeGenTableModel(
                tableKind,
                databaseName,
                schemaName,
                tableName,
                classSymbol.Name,
                classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString(),
                classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                columns.ToImmutable(),
                indexes.ToImmutable(),
                GetDescriptorSqModelName(activeDescriptorAttribute, tableKind),
                MapAccessibility(classSymbol.DeclaredAccessibility),
                containingTypes.IsDefault ? ImmutableArray<CodeGenContainingTypeModel>.Empty : containingTypes);

            return new TableDescriptorCandidate(
                classSymbol,
                model,
                diagnostics.ToImmutable(),
                tableAttributeLocation,
                columnLocationsBySqlName.ToImmutableDictionary(static p => p.Key, static p => p.Value.ToImmutable(), StringComparer.OrdinalIgnoreCase),
                propertyLocationsByName.ToImmutableDictionary(static p => p.Key, static p => p.Value.ToImmutable(), StringComparer.Ordinal),
                indexLocations.ToImmutable());
        }

        private static bool TryReadTableIdentity(AttributeData attribute, CodeGenTableKind tableKind, string className, out string? databaseName, out string? schemaName, out string tableName)
        {
            databaseName = null;
            schemaName = null;
            tableName = string.Empty;

            if (tableKind == CodeGenTableKind.DerivedTable)
            {
                tableName = className;
                return attribute.ConstructorArguments.Length == 0;
            }

            if (tableKind == CodeGenTableKind.TempTable)
            {
                tableName = attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string ?? string.Empty : string.Empty;
                return !string.IsNullOrWhiteSpace(tableName);
            }

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

            var defaultValue = GetNamedString(attribute, nameof(TableColumnAttributeBase.DefaultValue));
            if (!TryInferDefaultValue(kind.Value, defaultValue, out var defaultValueKind, out var normalizedDefaultValue))
            {
                diagnostic = CreateDiagnostic(
                    DiagnosticDescriptors.TableDescriptorInvalidDefaultValue,
                    GetAttributeLocation(attribute, classSymbol),
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
                GetNamedString(attribute, nameof(TableColumnAttributeBase.PropertyName)),
                GetNamedBool(attribute, nameof(TableColumnAttributeBase.Pk)),
                GetNamedBool(attribute, nameof(TableColumnAttributeBase.Identity)),
                GetNamedString(attribute, nameof(TableColumnAttributeBase.FkDatabase)),
                GetNamedString(attribute, nameof(TableColumnAttributeBase.FkSchema)),
                GetNamedString(attribute, nameof(TableColumnAttributeBase.FkTable)),
                GetNamedString(attribute, nameof(TableColumnAttributeBase.FkColumn)),
                defaultValueKind,
                normalizedDefaultValue,
                GetNamedBool(attribute, nameof(StringColumnAttributeBase.Unicode)),
                GetNamedNullableInt(attribute, nameof(StringColumnAttributeBase.MaxLength)),
                GetNamedBool(attribute, nameof(StringColumnAttributeBase.FixedLength)),
                GetNamedBool(attribute, nameof(StringColumnAttributeBase.Text)),
                GetNamedInt(attribute, nameof(DecimalColumnAttributeBase.Precision)),
                GetNamedInt(attribute, nameof(DecimalColumnAttributeBase.Scale)),
                GetNamedBool(attribute, nameof(DateTimeColumnAttributeBase.IsDate)),
                GetNamedString(attribute, nameof(TableColumnAttributeBase.SqModels)),
                GetNamedTypeName(attribute, nameof(TableColumnAttributeBase.SqModelCast)));
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
                GetNamedArray(attribute, nameof(IndexAttribute.DescendingColumns)),
                GetNamedString(attribute, nameof(IndexAttribute.Name)),
                GetNamedBool(attribute, nameof(IndexAttribute.Unique)),
                GetNamedBool(attribute, nameof(IndexAttribute.Clustered)));
            return true;
        }

        private static CodeGenColumnKind? TryMapColumnKind(string? attributeName)
        {
            return attributeName switch
            {
                nameof(BooleanColumnAttribute) => CodeGenColumnKind.Boolean,
                nameof(NullableBooleanColumnAttribute) => CodeGenColumnKind.NullableBoolean,
                nameof(ByteColumnAttribute) => CodeGenColumnKind.Byte,
                nameof(NullableByteColumnAttribute) => CodeGenColumnKind.NullableByte,
                nameof(ByteArrayColumnAttribute) => CodeGenColumnKind.ByteArray,
                nameof(NullableByteArrayColumnAttribute) => CodeGenColumnKind.NullableByteArray,
                nameof(Int16ColumnAttribute) => CodeGenColumnKind.Int16,
                nameof(NullableInt16ColumnAttribute) => CodeGenColumnKind.NullableInt16,
                nameof(Int32ColumnAttribute) => CodeGenColumnKind.Int32,
                nameof(NullableInt32ColumnAttribute) => CodeGenColumnKind.NullableInt32,
                nameof(Int64ColumnAttribute) => CodeGenColumnKind.Int64,
                nameof(NullableInt64ColumnAttribute) => CodeGenColumnKind.NullableInt64,
                nameof(DoubleColumnAttribute) => CodeGenColumnKind.Double,
                nameof(NullableDoubleColumnAttribute) => CodeGenColumnKind.NullableDouble,
                nameof(DecimalColumnAttribute) => CodeGenColumnKind.Decimal,
                nameof(NullableDecimalColumnAttribute) => CodeGenColumnKind.NullableDecimal,
                nameof(DateTimeColumnAttribute) => CodeGenColumnKind.DateTime,
                nameof(NullableDateTimeColumnAttribute) => CodeGenColumnKind.NullableDateTime,
                nameof(DateTimeOffsetColumnAttribute) => CodeGenColumnKind.DateTimeOffset,
                nameof(NullableDateTimeOffsetColumnAttribute) => CodeGenColumnKind.NullableDateTimeOffset,
                nameof(GuidColumnAttribute) => CodeGenColumnKind.Guid,
                nameof(NullableGuidColumnAttribute) => CodeGenColumnKind.NullableGuid,
                nameof(StringColumnAttribute) => CodeGenColumnKind.String,
                nameof(NullableStringColumnAttribute) => CodeGenColumnKind.NullableString,
                nameof(XmlColumnAttribute) => CodeGenColumnKind.Xml,
                nameof(NullableXmlColumnAttribute) => CodeGenColumnKind.NullableXml,
                nameof(DerivedBooleanColumnAttribute) => CodeGenColumnKind.Boolean,
                nameof(DerivedNullableBooleanColumnAttribute) => CodeGenColumnKind.NullableBoolean,
                nameof(DerivedByteColumnAttribute) => CodeGenColumnKind.Byte,
                nameof(DerivedNullableByteColumnAttribute) => CodeGenColumnKind.NullableByte,
                nameof(DerivedByteArrayColumnAttribute) => CodeGenColumnKind.ByteArray,
                nameof(DerivedNullableByteArrayColumnAttribute) => CodeGenColumnKind.NullableByteArray,
                nameof(DerivedInt16ColumnAttribute) => CodeGenColumnKind.Int16,
                nameof(DerivedNullableInt16ColumnAttribute) => CodeGenColumnKind.NullableInt16,
                nameof(DerivedInt32ColumnAttribute) => CodeGenColumnKind.Int32,
                nameof(DerivedNullableInt32ColumnAttribute) => CodeGenColumnKind.NullableInt32,
                nameof(DerivedInt64ColumnAttribute) => CodeGenColumnKind.Int64,
                nameof(DerivedNullableInt64ColumnAttribute) => CodeGenColumnKind.NullableInt64,
                nameof(DerivedDoubleColumnAttribute) => CodeGenColumnKind.Double,
                nameof(DerivedNullableDoubleColumnAttribute) => CodeGenColumnKind.NullableDouble,
                nameof(DerivedDecimalColumnAttribute) => CodeGenColumnKind.Decimal,
                nameof(DerivedNullableDecimalColumnAttribute) => CodeGenColumnKind.NullableDecimal,
                nameof(DerivedDateTimeColumnAttribute) => CodeGenColumnKind.DateTime,
                nameof(DerivedNullableDateTimeColumnAttribute) => CodeGenColumnKind.NullableDateTime,
                nameof(DerivedDateTimeOffsetColumnAttribute) => CodeGenColumnKind.DateTimeOffset,
                nameof(DerivedNullableDateTimeOffsetColumnAttribute) => CodeGenColumnKind.NullableDateTimeOffset,
                nameof(DerivedGuidColumnAttribute) => CodeGenColumnKind.Guid,
                nameof(DerivedNullableGuidColumnAttribute) => CodeGenColumnKind.NullableGuid,
                nameof(DerivedStringColumnAttribute) => CodeGenColumnKind.String,
                nameof(DerivedNullableStringColumnAttribute) => CodeGenColumnKind.NullableString,
                nameof(DerivedXmlColumnAttribute) => CodeGenColumnKind.Xml,
                nameof(DerivedNullableXmlColumnAttribute) => CodeGenColumnKind.NullableXml,
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

        private static ImmutableArray<CodeGenContainingTypeModel> GetContainingTypes(INamedTypeSymbol classSymbol)
        {
            if (classSymbol.ContainingType == null)
            {
                return ImmutableArray<CodeGenContainingTypeModel>.Empty;
            }

            var stack = new Stack<CodeGenContainingTypeModel>();
            var current = classSymbol.ContainingType;
            while (current != null)
            {
                if (current.TypeKind != TypeKind.Class || !IsPartial(current))
                {
                    return default;
                }

                stack.Push(new CodeGenContainingTypeModel(current.Name, MapAccessibility(current.DeclaredAccessibility)));
                current = current.ContainingType;
            }

            return stack.ToImmutableArray();
        }

        private static bool IsPartial(INamedTypeSymbol typeSymbol)
            => typeSymbol.DeclaringSyntaxReferences.Any(static r =>
                r.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax t && t.Modifiers.Any(SyntaxKind.PartialKeyword));

        private static CodeGenAccessibility MapAccessibility(Accessibility accessibility)
            => accessibility switch
            {
                Accessibility.Public => CodeGenAccessibility.Public,
                Accessibility.Internal => CodeGenAccessibility.Internal,
                Accessibility.Private => CodeGenAccessibility.Private,
                Accessibility.Protected => CodeGenAccessibility.Protected,
                Accessibility.ProtectedAndInternal => CodeGenAccessibility.PrivateProtected,
                Accessibility.ProtectedOrInternal => CodeGenAccessibility.ProtectedInternal,
                _ => CodeGenAccessibility.None
            };

        private static string? GetNamedString(AttributeData attribute, string name)
            => attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as string;

        private static string? GetDescriptorSqModelName(AttributeData? attribute, CodeGenTableKind tableKind)
            => attribute == null
                ? null
                : tableKind == CodeGenTableKind.DerivedTable
                ? GetNamedString(attribute, nameof(DerivedTableDescriptorAttribute.SqModel))
                : GetNamedString(attribute, nameof(TableDescriptorAttribute.SqModel));

        private static bool GetNamedBool(AttributeData attribute, string name)
            => attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as bool? ?? false;

        private static int GetNamedInt(AttributeData attribute, string name)
            => attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as int? ?? 0;

        private static bool TryInferDefaultValue(CodeGenColumnKind columnKind, string? value, out CodeGenDefaultValueKind defaultValueKind, out string? normalizedValue)
        {
            defaultValueKind = CodeGenDefaultValueKind.None;
            normalizedValue = value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (string.Equals(value, "$null", StringComparison.OrdinalIgnoreCase))
            {
                defaultValueKind = CodeGenDefaultValueKind.Null;
                normalizedValue = null;
                return true;
            }

            if (string.Equals(value, "$utcNow", StringComparison.OrdinalIgnoreCase))
            {
                defaultValueKind = CodeGenDefaultValueKind.UtcNow;
                normalizedValue = null;
                return columnKind is CodeGenColumnKind.DateTime or CodeGenColumnKind.NullableDateTime or CodeGenColumnKind.DateTimeOffset or CodeGenColumnKind.NullableDateTimeOffset;
            }

            if (string.Equals(value, "$now", StringComparison.OrdinalIgnoreCase))
            {
                defaultValueKind = CodeGenDefaultValueKind.Now;
                normalizedValue = null;
                return columnKind is CodeGenColumnKind.DateTime or CodeGenColumnKind.NullableDateTime or CodeGenColumnKind.DateTimeOffset or CodeGenColumnKind.NullableDateTimeOffset;
            }

            if (TryParseRawDefaultValue(value!, out var isRawToken, out var rawValue))
            {
                defaultValueKind = CodeGenDefaultValueKind.RawSql;
                normalizedValue = rawValue;
                return true;
            }

            if (isRawToken)
            {
                return false;
            }

            switch (columnKind)
            {
                case CodeGenColumnKind.Boolean:
                case CodeGenColumnKind.NullableBoolean:
                    if (bool.TryParse(value, out var boolValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Boolean;
                        normalizedValue = boolValue ? "true" : "false";
                        return true;
                    }

                    if (value == "0" || value == "1")
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Boolean;
                        normalizedValue = value;
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Byte:
                case CodeGenColumnKind.NullableByte:
                    if (byte.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var byteValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Byte;
                        normalizedValue = byteValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Int16:
                case CodeGenColumnKind.NullableInt16:
                    if (short.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var shortValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Int16;
                        normalizedValue = shortValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Int32:
                case CodeGenColumnKind.NullableInt32:
                    if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var intValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Int32;
                        normalizedValue = intValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Int64:
                case CodeGenColumnKind.NullableInt64:
                    if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var longValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Int64;
                        normalizedValue = longValue.ToString();
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Decimal:
                case CodeGenColumnKind.NullableDecimal:
                    if (decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Decimal;
                        normalizedValue = decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Double:
                case CodeGenColumnKind.NullableDouble:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Double;
                        normalizedValue = doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.Guid:
                case CodeGenColumnKind.NullableGuid:
                    if (Guid.TryParse(value, out var guidValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.Guid;
                        normalizedValue = guidValue.ToString("D");
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.DateTime:
                case CodeGenColumnKind.NullableDateTime:
                    if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTimeValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.DateTime;
                        normalizedValue = dateTimeValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.DateTimeOffset:
                case CodeGenColumnKind.NullableDateTimeOffset:
                    if (DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTimeOffsetValue))
                    {
                        defaultValueKind = CodeGenDefaultValueKind.DateTimeOffset;
                        normalizedValue = dateTimeOffsetValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }

                    return false;
                case CodeGenColumnKind.String:
                case CodeGenColumnKind.NullableString:
                case CodeGenColumnKind.Xml:
                case CodeGenColumnKind.NullableXml:
                    defaultValueKind = CodeGenDefaultValueKind.String;
                    normalizedValue = value;
                    return true;
                default:
                    return false;
            }
        }

        private static string GetColumnKindDisplayName(CodeGenColumnKind columnKind)
            => columnKind switch
            {
                CodeGenColumnKind.Boolean => TrimAttributeSuffix(nameof(BooleanColumnAttribute)),
                CodeGenColumnKind.NullableBoolean => TrimAttributeSuffix(nameof(NullableBooleanColumnAttribute)),
                CodeGenColumnKind.Byte => TrimAttributeSuffix(nameof(ByteColumnAttribute)),
                CodeGenColumnKind.NullableByte => TrimAttributeSuffix(nameof(NullableByteColumnAttribute)),
                CodeGenColumnKind.ByteArray => TrimAttributeSuffix(nameof(ByteArrayColumnAttribute)),
                CodeGenColumnKind.NullableByteArray => TrimAttributeSuffix(nameof(NullableByteArrayColumnAttribute)),
                CodeGenColumnKind.Int16 => TrimAttributeSuffix(nameof(Int16ColumnAttribute)),
                CodeGenColumnKind.NullableInt16 => TrimAttributeSuffix(nameof(NullableInt16ColumnAttribute)),
                CodeGenColumnKind.Int32 => TrimAttributeSuffix(nameof(Int32ColumnAttribute)),
                CodeGenColumnKind.NullableInt32 => TrimAttributeSuffix(nameof(NullableInt32ColumnAttribute)),
                CodeGenColumnKind.Int64 => TrimAttributeSuffix(nameof(Int64ColumnAttribute)),
                CodeGenColumnKind.NullableInt64 => TrimAttributeSuffix(nameof(NullableInt64ColumnAttribute)),
                CodeGenColumnKind.Double => TrimAttributeSuffix(nameof(DoubleColumnAttribute)),
                CodeGenColumnKind.NullableDouble => TrimAttributeSuffix(nameof(NullableDoubleColumnAttribute)),
                CodeGenColumnKind.Decimal => TrimAttributeSuffix(nameof(DecimalColumnAttribute)),
                CodeGenColumnKind.NullableDecimal => TrimAttributeSuffix(nameof(NullableDecimalColumnAttribute)),
                CodeGenColumnKind.DateTime => TrimAttributeSuffix(nameof(DateTimeColumnAttribute)),
                CodeGenColumnKind.NullableDateTime => TrimAttributeSuffix(nameof(NullableDateTimeColumnAttribute)),
                CodeGenColumnKind.DateTimeOffset => TrimAttributeSuffix(nameof(DateTimeOffsetColumnAttribute)),
                CodeGenColumnKind.NullableDateTimeOffset => TrimAttributeSuffix(nameof(NullableDateTimeOffsetColumnAttribute)),
                CodeGenColumnKind.Guid => TrimAttributeSuffix(nameof(GuidColumnAttribute)),
                CodeGenColumnKind.NullableGuid => TrimAttributeSuffix(nameof(NullableGuidColumnAttribute)),
                CodeGenColumnKind.String => TrimAttributeSuffix(nameof(StringColumnAttribute)),
                CodeGenColumnKind.NullableString => TrimAttributeSuffix(nameof(NullableStringColumnAttribute)),
                CodeGenColumnKind.Xml => TrimAttributeSuffix(nameof(XmlColumnAttribute)),
                CodeGenColumnKind.NullableXml => TrimAttributeSuffix(nameof(NullableXmlColumnAttribute)),
                _ => columnKind.ToString()
            };

        private static string TrimAttributeSuffix(string attributeTypeName)
            => attributeTypeName.EndsWith(nameof(Attribute), StringComparison.Ordinal)
                ? attributeTypeName.Substring(0, attributeTypeName.Length - nameof(Attribute).Length)
                : attributeTypeName;

        private static string GetSupportedPredefinedValuesText(CodeGenColumnKind columnKind)
        {
            if (columnKind is CodeGenColumnKind.DateTime or CodeGenColumnKind.NullableDateTime or CodeGenColumnKind.DateTimeOffset or CodeGenColumnKind.NullableDateTimeOffset)
            {
                return "$null, $utcNow, $now, $raw(...)";
            }

            return "$null, $raw(...)";
        }

        private static bool TryParseRawDefaultValue(string value, out bool isRawToken, out string? rawValue)
        {
            isRawToken = value.StartsWith("$raw", StringComparison.OrdinalIgnoreCase);
            rawValue = null;

            if (!isRawToken)
            {
                return false;
            }

            if (!value.StartsWith("$raw(", StringComparison.OrdinalIgnoreCase) || !value.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            rawValue = value.Substring(5, value.Length - 6);
            return true;
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

        private static string? GetNamedTypeName(AttributeData attribute, string name)
        {
            var value = attribute.NamedArguments.FirstOrDefault(i => i.Key == name).Value.Value as ITypeSymbol;
            return value?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
            => CreateDiagnostic(descriptor, symbol.Locations.FirstOrDefault(static l => l.IsInSource) ?? Location.None, args);

        private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] args)
            => Diagnostic.Create(descriptor, location, args);

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

        private static Location GetAttributeLocation(AttributeData attributeData, ISymbol fallbackSymbol)
        {
            if (attributeData.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
            {
                return attributeSyntax.GetLocation();
            }

            return fallbackSymbol.Locations.FirstOrDefault(static l => l.IsInSource) ?? Location.None;
        }

        private static void AddLocation(
            ImmutableDictionary<string, ImmutableArray<Location>.Builder>.Builder map,
            string key,
            Location location)
        {
            if (!map.TryGetValue(key, out var builder))
            {
                builder = ImmutableArray.CreateBuilder<Location>();
                map[key] = builder;
            }

            builder.Add(location);
        }

        private sealed class TableDescriptorCandidate
        {
            public TableDescriptorCandidate(
                INamedTypeSymbol symbol,
                CodeGenTableModel model,
                ImmutableArray<Diagnostic> diagnostics,
                Location tableAttributeLocation,
                ImmutableDictionary<string, ImmutableArray<Location>> columnLocationsBySqlName,
                ImmutableDictionary<string, ImmutableArray<Location>> propertyLocationsByName,
                ImmutableArray<IndexAttributeLocation> indexLocations)
            {
                this.Symbol = symbol;
                this.Model = model;
                this.Diagnostics = diagnostics;
                this.TableAttributeLocation = tableAttributeLocation;
                this.ColumnLocationsBySqlName = columnLocationsBySqlName;
                this.PropertyLocationsByName = propertyLocationsByName;
                this.IndexLocations = indexLocations;
            }

            public INamedTypeSymbol Symbol { get; }

            public CodeGenTableModel Model { get; }

            public ImmutableArray<Diagnostic> Diagnostics { get; }

            public Location TableAttributeLocation { get; }

            public ImmutableDictionary<string, ImmutableArray<Location>> ColumnLocationsBySqlName { get; }

            public ImmutableDictionary<string, ImmutableArray<Location>> PropertyLocationsByName { get; }

            public ImmutableArray<IndexAttributeLocation> IndexLocations { get; }
        }

        private readonly struct IndexAttributeLocation
        {
            public IndexAttributeLocation(
                Location location,
                ImmutableHashSet<string> columns,
                ImmutableHashSet<string> descendingColumns)
            {
                this.Location = location;
                this.Columns = columns;
                this.DescendingColumns = descendingColumns;
            }

            public Location Location { get; }

            public ImmutableHashSet<string> Columns { get; }

            public ImmutableHashSet<string> DescendingColumns { get; }
        }
    }
}
