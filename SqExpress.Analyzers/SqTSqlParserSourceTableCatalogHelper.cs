using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqExpress.Analyzers
{
    internal static class SqTSqlParserSourceTableCatalogHelper
    {
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
            if (namedType.TypeKind is TypeKind.Class && !namedType.IsAbstract && DerivesFromTableBase(namedType))
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
                && string.Equals(i.Parameters[0].Type.ToDisplayString(), "SqExpress.Alias", StringComparison.Ordinal));

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
                if (string.Equals(current.ToDisplayString(), "SqExpress.TableBase", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>> ToReadOnly(
            IDictionary<string, List<SourceTableInfo>> byKey)
            => byKey.ToDictionary(i => i.Key, i => (IReadOnlyList<SourceTableInfo>)i.Value, StringComparer.OrdinalIgnoreCase);

        private static string BuildTableKey(string? schema, string tableName)
            => (schema ?? string.Empty) + "." + tableName;

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
