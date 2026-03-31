using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.TableDeclarationAttributes;

namespace SqExpress.Analyzers
{
    internal static class SqTSqlParserSourceTableCatalogHelper
    {
        private static readonly string TableDescriptorAttributeName = typeof(TableDescriptorAttribute).FullName!;
        private static readonly string TempTableDescriptorAttributeName = typeof(TempTableDescriptorAttribute).FullName!;
        private static readonly string ColumnAttributeBaseName = typeof(TableColumnAttributeBase).FullName!;

        public static IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>> BuildSourceTableCatalog(
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var byKey = new Dictionary<string, List<SourceTableInfo>>(StringComparer.OrdinalIgnoreCase);
            AppendSourceTableCatalog(compilation, cancellationToken, byKey);
            return ToReadOnly(byKey);
        }

        private static void AppendSourceTableCatalog(
            Compilation compilation,
            CancellationToken cancellationToken,
            IDictionary<string, List<SourceTableInfo>> byKey)
        {
            var visitedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
            VisitAssembly(compilation.Assembly, compilation, cancellationToken, byKey, visitedAssemblies);
            foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                VisitAssembly(assembly, compilation, cancellationToken, byKey, visitedAssemblies);
            }
        }

        private static void VisitAssembly(
            IAssemblySymbol assemblySymbol,
            Compilation compilation,
            CancellationToken cancellationToken,
            IDictionary<string, List<SourceTableInfo>> byKey,
            ISet<IAssemblySymbol> visitedAssemblies)
        {
            if (!visitedAssemblies.Add(assemblySymbol))
            {
                return;
            }

            VisitNamespace(assemblySymbol.GlobalNamespace, compilation, cancellationToken, byKey);
            foreach (var referencedAssembly in assemblySymbol.Modules.SelectMany(i => i.ReferencedAssemblySymbols))
            {
                VisitAssembly(referencedAssembly, compilation, cancellationToken, byKey, visitedAssemblies);
            }
        }

        private static void VisitNamespace(
            INamespaceSymbol namespaceSymbol,
            Compilation compilation,
            CancellationToken cancellationToken,
            IDictionary<string, List<SourceTableInfo>> byKey)
        {
            foreach (var member in namespaceSymbol.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (member is INamespaceSymbol nestedNamespace)
                {
                    VisitNamespace(nestedNamespace, compilation, cancellationToken, byKey);
                }
                else if (member is INamedTypeSymbol namedType)
                {
                    VisitNamedType(namedType, compilation, cancellationToken, byKey);
                }
            }
        }

        private static void VisitNamedType(
            INamedTypeSymbol namedType,
            Compilation compilation,
            CancellationToken cancellationToken,
            IDictionary<string, List<SourceTableInfo>> byKey)
        {
            if (namedType.TypeKind is TypeKind.Class
                && !namedType.IsAbstract
                && (DerivesFromTableBase(namedType) || HasTableDeclarationAttributes(namedType)))
            {
                if (TryCreateSourceTableInfo(namedType, compilation, cancellationToken, out var info))
                {
                    if (!byKey.TryGetValue(info.TableKey, out var items))
                    {
                        items = new List<SourceTableInfo>();
                        byKey[info.TableKey] = items;
                    }

                    if (!items.Any(i => string.Equals(i.TypeName, info.TypeName, StringComparison.Ordinal)))
                    {
                        items.Add(info);
                    }
                }
            }

            foreach (var nested in namedType.GetTypeMembers())
            {
                VisitNamedType(nested, compilation, cancellationToken, byKey);
            }
        }

        private static bool TryCreateSourceTableInfo(
            INamedTypeSymbol namedType,
            Compilation compilation,
            CancellationToken cancellationToken,
            out SourceTableInfo info)
        {
            info = default;

            if (TryCreateSourceTableInfoFromAttributes(namedType, out info))
            {
                return true;
            }

            var constructors = namedType.InstanceConstructors
                .Where(i => i.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            string? tableKey = null;
            var columnsByName = new Dictionary<string, SourceColumnInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var constructor in constructors)
            {
                if (TryResolveTableInfoFromConstructor(constructor, compilation, cancellationToken, out var resolvedKey))
                {
                    tableKey ??= resolvedKey;
                    CollectColumnsFromConstructor(constructor, cancellationToken, columnsByName, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));
                }
            }

            if (string.IsNullOrWhiteSpace(tableKey))
            {
                return false;
            }

            var supportsParameterlessConstructor = constructors.Any(i => i.Parameters.Length == 0);
            var supportsAliasConstructor = constructors.Any(i =>
                i.Parameters.Length == 1
                && string.Equals(i.Parameters[0].Type.ToDisplayString(), typeof(Alias).FullName, StringComparison.Ordinal));

            info = new SourceTableInfo(
                tableKey!,
                namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                namedType.Name,
                namedType.ContainingNamespace?.IsGlobalNamespace == false ? namedType.ContainingNamespace.ToDisplayString() : null,
                ToCamelCaseIdentifier(namedType.Name, "table"),
                supportsParameterlessConstructor,
                supportsAliasConstructor,
                columnsByName);
            return true;
        }

        private static bool TryResolveTableInfoFromConstructor(
            IMethodSymbol constructor,
            Compilation compilation,
            CancellationToken cancellationToken,
            out string tableKey)
        {
            tableKey = string.Empty;
            if (!TryResolveTableInfoFromConstructorCore(constructor, compilation, cancellationToken, out var schema, out var tableName))
            {
                return false;
            }

            tableKey = BuildTableKey(schema, tableName);
            return true;
        }

        private static bool TryCreateSourceTableInfoFromAttributes(
            INamedTypeSymbol namedType,
            out SourceTableInfo info)
        {
            info = default;

            var attributes = namedType.GetAttributes();
            var tableDescriptorAttribute = attributes.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == TableDescriptorAttributeName);
            var tempTableDescriptorAttribute = attributes.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == TempTableDescriptorAttributeName);
            var activeDescriptorAttribute = tableDescriptorAttribute ?? tempTableDescriptorAttribute;
            if (activeDescriptorAttribute == null)
            {
                return false;
            }

            var isTempTable = tempTableDescriptorAttribute != null;
            if (!TryReadTableDeclarationAttribute(activeDescriptorAttribute, isTempTable, out var tableKey))
            {
                return false;
            }

            var columnsByName = new Dictionary<string, SourceColumnInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in attributes)
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass == null || !InheritsFrom(attributeClass, ColumnAttributeBaseName))
                {
                    continue;
                }

                if (!TryReadColumnDeclarationAttribute(attribute, out var columnInfo))
                {
                    continue;
                }

                columnsByName[columnInfo.ColumnName] = columnInfo;
            }

            info = new SourceTableInfo(
                tableKey,
                namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                namedType.Name,
                namedType.ContainingNamespace?.IsGlobalNamespace == false ? namedType.ContainingNamespace.ToDisplayString() : null,
                ToCamelCaseIdentifier(namedType.Name, "table"),
                supportsParameterlessConstructor: true,
                supportsAliasConstructor: true,
                columnsByName);
            return true;
        }

        private static bool TryResolveTableInfoFromConstructorCore(
            IMethodSymbol constructor,
            Compilation compilation,
            CancellationToken cancellationToken,
            out string? schema,
            out string tableName)
        {
            schema = null;
            tableName = string.Empty;

            if (constructor.DeclaringSyntaxReferences.Length < 1)
            {
                return false;
            }

            var syntax = constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) as ConstructorDeclarationSyntax;
            if (syntax == null || syntax.Initializer == null)
            {
                return false;
            }

            if (syntax.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
            {
                if (!TryResolveChainedConstructor(constructor, syntax, out var chainedConstructor))
                {
                    return false;
                }

                return TryResolveTableInfoFromConstructorCore(chainedConstructor, compilation, cancellationToken, out schema, out tableName);
            }

            if (!syntax.Initializer.IsKind(SyntaxKind.BaseConstructorInitializer))
            {
                return false;
            }

            var args = syntax.Initializer.ArgumentList?.Arguments;
            if (args == null)
            {
                return false;
            }

            if (args.Value.Count >= 2
                && TryGetConstantNullableString(args.Value[0].Expression, out schema)
                && TryGetConstantString(args.Value[1].Expression, out tableName))
            {
                return true;
            }

            if (args.Value.Count >= 3
                && TryGetConstantNullableString(args.Value[1].Expression, out schema)
                && TryGetConstantString(args.Value[2].Expression, out tableName))
            {
                return true;
            }

            return false;
        }

        private static bool TryResolveChainedConstructor(
            IMethodSymbol constructor,
            ConstructorDeclarationSyntax syntax,
            out IMethodSymbol chainedConstructor)
        {
            chainedConstructor = null!;
            var argCount = syntax.Initializer?.ArgumentList?.Arguments.Count ?? 0;
            var candidates = constructor.ContainingType.InstanceConstructors
                .Where(i => !SymbolEqualityComparer.Default.Equals(i, constructor) && i.Parameters.Length == argCount)
                .ToList();

            if (candidates.Count != 1)
            {
                return false;
            }

            chainedConstructor = candidates[0];
            return true;
        }

        private static void CollectColumnsFromConstructor(
            IMethodSymbol constructor,
            CancellationToken cancellationToken,
            IDictionary<string, SourceColumnInfo> columnsByName,
            ISet<IMethodSymbol> visitedConstructors)
        {
            if (!visitedConstructors.Add(constructor))
            {
                return;
            }

            if (constructor.DeclaringSyntaxReferences.Length < 1)
            {
                return;
            }

            var syntax = constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) as ConstructorDeclarationSyntax;
            if (syntax == null)
            {
                return;
            }

            if (syntax.Initializer != null
                && syntax.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer)
                && TryResolveChainedConstructor(constructor, syntax, out var chainedConstructor))
            {
                CollectColumnsFromConstructor(chainedConstructor, cancellationToken, columnsByName, visitedConstructors);
            }

            if (syntax.ExpressionBody?.Expression is AssignmentExpressionSyntax expressionBodyAssignment
                && TryCreateSourceColumnInfo(constructor.ContainingType, expressionBodyAssignment, cancellationToken, out var expressionBodyColumn))
            {
                columnsByName[expressionBodyColumn.ColumnName] = expressionBodyColumn;
            }

            if (syntax.Body == null)
            {
                return;
            }

            foreach (var assignment in syntax.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (TryCreateSourceColumnInfo(constructor.ContainingType, assignment, cancellationToken, out var column))
                {
                    columnsByName[column.ColumnName] = column;
                }
            }
        }

        private static bool TryCreateSourceColumnInfo(
            INamedTypeSymbol containingType,
            AssignmentExpressionSyntax assignment,
            CancellationToken cancellationToken,
            out SourceColumnInfo info)
        {
            info = default;

            if (!TryGetAssignedMemberName(assignment.Left, out var memberName))
            {
                return false;
            }

            if (!TryGetCreateColumnCallInfo(assignment.Right, out var columnName, out var factoryMethodName))
            {
                return false;
            }

            TryGetDeclaredMemberTypeName(containingType, memberName, cancellationToken, out var declaredTypeName);
            declaredTypeName ??= GetColumnTypeNameFromFactory(factoryMethodName);

            info = new SourceColumnInfo(columnName, memberName, declaredTypeName);
            return true;
        }

        private static bool TryGetAssignedMemberName(ExpressionSyntax expression, out string memberName)
        {
            memberName = string.Empty;

            switch (expression)
            {
                case IdentifierNameSyntax identifier:
                    memberName = identifier.Identifier.ValueText;
                    return !string.IsNullOrWhiteSpace(memberName);
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Expression is ThisExpressionSyntax:
                    memberName = memberAccess.Name.Identifier.ValueText;
                    return !string.IsNullOrWhiteSpace(memberName);
                default:
                    return false;
            }
        }

        private static bool TryGetCreateColumnCallInfo(
            ExpressionSyntax expression,
            out string columnName,
            out string factoryMethodName)
        {
            columnName = string.Empty;
            factoryMethodName = string.Empty;

            if (expression is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            switch (invocation.Expression)
            {
                case IdentifierNameSyntax identifier:
                    factoryMethodName = identifier.Identifier.ValueText;
                    break;
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Expression is ThisExpressionSyntax:
                    factoryMethodName = memberAccess.Name.Identifier.ValueText;
                    break;
                default:
                    return false;
            }

            if (string.IsNullOrWhiteSpace(factoryMethodName)
                || !factoryMethodName.StartsWith("Create", StringComparison.Ordinal)
                || !factoryMethodName.EndsWith("Column", StringComparison.Ordinal)
                || invocation.ArgumentList.Arguments.Count < 1
                || !TryGetConstantString(invocation.ArgumentList.Arguments[0].Expression, out columnName))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetDeclaredMemberTypeName(
            INamedTypeSymbol containingType,
            string memberName,
            CancellationToken cancellationToken,
            out string? declaredTypeName)
        {
            declaredTypeName = null;

            foreach (var syntaxReference in containingType.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax typeDeclaration)
                {
                    continue;
                }

                foreach (var member in typeDeclaration.Members)
                {
                    switch (member)
                    {
                        case PropertyDeclarationSyntax property when string.Equals(property.Identifier.ValueText, memberName, StringComparison.Ordinal):
                            declaredTypeName = property.Type.ToString();
                            return true;
                        case FieldDeclarationSyntax field:
                            foreach (var variable in field.Declaration.Variables)
                            {
                                if (string.Equals(variable.Identifier.ValueText, memberName, StringComparison.Ordinal))
                                {
                                    declaredTypeName = field.Declaration.Type.ToString();
                                    return true;
                                }
                            }

                            break;
                    }
                }
            }

            return false;
        }

        private static string? GetColumnTypeNameFromFactory(string factoryMethodName)
        {
            if (string.IsNullOrWhiteSpace(factoryMethodName)
                || !factoryMethodName.StartsWith("Create", StringComparison.Ordinal)
                || !factoryMethodName.EndsWith("Column", StringComparison.Ordinal))
            {
                return null;
            }

            var core = factoryMethodName.Substring("Create".Length);
            return string.IsNullOrWhiteSpace(core) ? null : core + "TableColumn";
        }

        private static bool TryGetConstantNullableString(
            ExpressionSyntax expression,
            out string? value)
        {
            if (expression.IsKind(SyntaxKind.NullLiteralExpression))
            {
                value = null;
                return true;
            }

            return TryGetConstantString(expression, out value);
        }

        private static bool TryGetConstantString(
            ExpressionSyntax expression,
            out string value)
        {
            value = string.Empty;
            if (expression is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                value = literal.Token.ValueText;
                return true;
            }

            if (expression is InvocationExpressionSyntax invocation
                && invocation.Expression is IdentifierNameSyntax identifier
                && string.Equals(identifier.Identifier.ValueText, "nameof", StringComparison.Ordinal)
                && invocation.ArgumentList.Arguments.Count == 1)
            {
                value = ExtractNameofText(invocation.ArgumentList.Arguments[0].Expression);
                return !string.IsNullOrWhiteSpace(value);
            }

            return false;
        }

        private static string ExtractNameofText(ExpressionSyntax expression)
        {
            return expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                _ => string.Empty
            };
        }

        private static bool DerivesFromTableBase(ITypeSymbol type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.ToDisplayString(), typeof(TableBase).FullName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTableDeclarationAttributes(INamedTypeSymbol namedType)
        {
            foreach (var attribute in namedType.GetAttributes())
            {
                var attributeTypeName = attribute.AttributeClass?.ToDisplayString();
                if (string.Equals(attributeTypeName, TableDescriptorAttributeName, StringComparison.Ordinal)
                    || string.Equals(attributeTypeName, TempTableDescriptorAttributeName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadTableDeclarationAttribute(
            AttributeData attribute,
            bool isTempTable,
            out string tableKey)
        {
            tableKey = string.Empty;

            var args = attribute.ConstructorArguments;
            string? schema = null;
            string? tableName = null;

            if (isTempTable)
            {
                if (args.Length >= 1 && args[0].Value is string tempTableName)
                {
                    tableName = tempTableName;
                }
            }
            else
            {
                if (args.Length >= 1 && args[0].Value is string singleName)
                {
                    if (args.Length == 1)
                    {
                        tableName = singleName;
                    }
                    else if (args.Length >= 2 && args[1].Value is string secondName)
                    {
                        schema = singleName;
                        tableName = secondName;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            tableKey = BuildTableKey(schema, tableName!);
            return true;
        }

        private static bool TryReadColumnDeclarationAttribute(
            AttributeData attribute,
            out SourceColumnInfo info)
        {
            info = default;

            if (attribute.ConstructorArguments.Length < 1 || attribute.ConstructorArguments[0].Value is not string columnName)
            {
                return false;
            }

            var propertyName = GetNamedString(attribute, "PropertyName");
            var memberName = string.IsNullOrWhiteSpace(propertyName) ? ToIdentifier(columnName) : propertyName!;
            var typeName = GetColumnTypeNameFromAttribute(attribute.AttributeClass?.Name);
            info = new SourceColumnInfo(columnName, memberName, typeName);
            return true;
        }

        private static bool InheritsFrom(INamedTypeSymbol type, string baseTypeName)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.ToDisplayString(), baseTypeName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string? GetNamedString(AttributeData attribute, string name)
        {
            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (string.Equals(namedArgument.Key, name, StringComparison.Ordinal) && namedArgument.Value.Value is string value)
                {
                    return value;
                }
            }

            return null;
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

        private static string? GetColumnTypeNameFromAttribute(string? attributeClassName)
        {
            return attributeClassName switch
            {
                "BooleanColumnAttribute" => "BooleanTableColumn",
                "NullableBooleanColumnAttribute" => "NullableBooleanTableColumn",
                "ByteColumnAttribute" => "ByteTableColumn",
                "NullableByteColumnAttribute" => "NullableByteTableColumn",
                "ByteArrayColumnAttribute" => "ByteArrayTableColumn",
                "NullableByteArrayColumnAttribute" => "NullableByteArrayTableColumn",
                "Int16ColumnAttribute" => "Int16TableColumn",
                "NullableInt16ColumnAttribute" => "NullableInt16TableColumn",
                "Int32ColumnAttribute" => "Int32TableColumn",
                "NullableInt32ColumnAttribute" => "NullableInt32TableColumn",
                "Int64ColumnAttribute" => "Int64TableColumn",
                "NullableInt64ColumnAttribute" => "NullableInt64TableColumn",
                "DoubleColumnAttribute" => "DoubleTableColumn",
                "NullableDoubleColumnAttribute" => "NullableDoubleTableColumn",
                "DecimalColumnAttribute" => "DecimalTableColumn",
                "NullableDecimalColumnAttribute" => "NullableDecimalTableColumn",
                "DateTimeColumnAttribute" => "DateTimeTableColumn",
                "NullableDateTimeColumnAttribute" => "NullableDateTimeTableColumn",
                "DateTimeOffsetColumnAttribute" => "DateTimeOffsetTableColumn",
                "NullableDateTimeOffsetColumnAttribute" => "NullableDateTimeOffsetTableColumn",
                "GuidColumnAttribute" => "GuidTableColumn",
                "NullableGuidColumnAttribute" => "NullableGuidTableColumn",
                "StringColumnAttribute" => "StringTableColumn",
                "NullableStringColumnAttribute" => "NullableStringTableColumn",
                "XmlColumnAttribute" => "XmlTableColumn",
                "NullableXmlColumnAttribute" => "NullableXmlTableColumn",
                _ => null
            };
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>> ToReadOnly(
            IDictionary<string, List<SourceTableInfo>> byKey)
            => byKey.ToDictionary(i => i.Key, i => (IReadOnlyList<SourceTableInfo>)i.Value, StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<SourceTableInfo> GetSourceTableMatches(
            IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>> sourceCatalog,
            string expectedTableKey,
            string? defaultSchema)
        {
            if (sourceCatalog.TryGetValue(expectedTableKey, out var directCandidates) && directCandidates.Count > 0)
            {
                return directCandidates;
            }

            if (!TrySplitTableKey(expectedTableKey, out var schema, out var tableName)
                || string.IsNullOrWhiteSpace(schema)
                || !string.Equals(schema, defaultSchema, StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<SourceTableInfo>();
            }

            var unqualifiedKey = BuildTableKey(schema: null, tableName);
            if (sourceCatalog.TryGetValue(unqualifiedKey, out var unqualifiedCandidates) && unqualifiedCandidates.Count > 0)
            {
                return unqualifiedCandidates;
            }

            return Array.Empty<SourceTableInfo>();
        }

        private static string BuildTableKey(string? schema, string tableName)
            => (schema ?? string.Empty) + "." + tableName;

        private static bool TrySplitTableKey(string tableKey, out string? schema, out string tableName)
        {
            schema = null;
            tableName = string.Empty;

            var separatorIndex = tableKey.IndexOf('.');
            if (separatorIndex < 0 || separatorIndex >= tableKey.Length - 1)
            {
                return false;
            }

            schema = separatorIndex == 0 ? null : tableKey.Substring(0, separatorIndex);
            tableName = tableKey.Substring(separatorIndex + 1);
            return !string.IsNullOrWhiteSpace(tableName);
        }

        private static string ToCamelCaseIdentifier(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var parts = value
                .Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => i.Length > 0)
                .ToList();
            if (parts.Count < 1)
            {
                return fallback;
            }

            var first = parts[0];
            var result = char.ToLowerInvariant(first[0]) + first.Substring(1);
            for (var i = 1; i < parts.Count; i++)
            {
                result += char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }

            return SyntaxFacts.IsValidIdentifier(result) ? result : fallback;
        }
    }

    internal readonly struct SourceTableInfo
    {
        public SourceTableInfo(
            string tableKey,
            string typeName,
            string simpleTypeName,
            string? namespaceName,
            string variableBaseName,
            bool supportsParameterlessConstructor,
            bool supportsAliasConstructor,
            IReadOnlyDictionary<string, SourceColumnInfo> columnsByName)
        {
            this.TableKey = tableKey;
            this.TypeName = typeName;
            this.SimpleTypeName = simpleTypeName;
            this.NamespaceName = namespaceName;
            this.VariableBaseName = variableBaseName;
            this.SupportsParameterlessConstructor = supportsParameterlessConstructor;
            this.SupportsAliasConstructor = supportsAliasConstructor;
            this.ColumnsByName = columnsByName;
        }

        public string TableKey { get; }

        public string TypeName { get; }

        public string SimpleTypeName { get; }

        public string? NamespaceName { get; }

        public string VariableBaseName { get; }

        public bool SupportsParameterlessConstructor { get; }

        public bool SupportsAliasConstructor { get; }

        public IReadOnlyDictionary<string, SourceColumnInfo> ColumnsByName { get; }

        public string PreferredTypeName => this.SimpleTypeName;
    }

    internal readonly struct SourceColumnInfo
    {
        public SourceColumnInfo(string columnName, string memberName, string? typeName)
        {
            this.ColumnName = columnName;
            this.MemberName = memberName;
            this.TypeName = typeName;
        }

        public string ColumnName { get; }

        public string MemberName { get; }

        public string? TypeName { get; }
    }
}
