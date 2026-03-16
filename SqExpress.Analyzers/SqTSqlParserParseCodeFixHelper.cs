using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.SqlParser;
using SqExpress.SqlTranspiler;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using RoslynStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax;

namespace SqExpress.Analyzers
{
    internal static partial class SqTSqlParserParseCodeFixHelper
    {
        private static readonly string[] RequiredNamespaces =
        {
            "System",
            "System.Collections.Generic",
            "SqExpress",
            "SqExpress.Syntax",
            "SqExpress.Syntax.Expressions",
            "SqExpress.Syntax.Functions.Known",
            "SqExpress.Syntax.Names",
            "SqExpress.Syntax.Select",
            "SqExpress.Syntax.Type",
            "SqExpress.Syntax.Value"
        };

        public static async Task<SqTSqlParserParseCodeFixPlan?> TryCreatePlanAsync(
            Document document,
            SemanticModel semanticModel,
            SqTSqlParserInvocation match,
            CancellationToken cancellationToken)
        {
            if (!TryFindReplacementRoot(match.Invocation, semanticModel, cancellationToken, out var replacementRoot))
            {
                return null;
            }

            if (replacementRoot.FirstAncestorOrSelf<RoslynStatementSyntax>() is not RoslynStatementSyntax anchorStatement)
            {
                return null;
            }

            if (replacementRoot.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not TypeDeclarationSyntax containingType)
            {
                return null;
            }

            if (match.Invocation.ArgumentList.Arguments.Count > 2)
            {
                return null;
            }

            if (!TryParseExpectedTables(match.SqlText, out var expectedTables, out var _))
            {
                return null;
            }

            var sourceCatalog = await BuildSourceTableCatalogAsync(document.Project.Solution, cancellationToken).ConfigureAwait(false);
            if (!TryResolveExpectedTableBindings(
                    semanticModel,
                    match,
                    expectedTables,
                    anchorStatement,
                    sourceCatalog,
                    cancellationToken,
                    out var tableDeclarations,
                    out var inlineBindings,
                    out var requiredNamespaces,
                    out var _))
            {
                return null;
            }

            var queryVariableName = MakeUniqueLocalName(anchorStatement, "expr");
            SqExpressSqlInlineTranspileResult inline;
            try
            {
                inline = new SqExpressSqlTranspiler().TranspileInline(
                    match.SqlText,
                    inlineBindings,
                    new SqExpressSqlTranspilerOptions
                    {
                        QueryVariableName = queryVariableName
                    });
            }
            catch
            {
                return null;
            }

            var parameterOverrides = CollectParameterOverrides(replacementRoot, match.Invocation, semanticModel, cancellationToken);
            var insertedStatements = new List<RoslynStatementSyntax>(tableDeclarations.Count + inline.Parameters.Count + inline.LocalDeclarations.Count);
            insertedStatements.AddRange(tableDeclarations);
            var dictionaryBackedParameters = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in inline.Parameters)
            {
                if (parameterOverrides.TryGetValue(parameter.ParameterName, out var parameterExpression))
                {
                    insertedStatements.Add(SyntaxFactory.ParseStatement("var " + parameter.VariableName + " = " + RenderParameterOverride(parameterExpression, parameter.IsList) + ";"));
                }
                else if (parameterOverrides.TryGetValue("*", out var dictionaryExpression))
                {
                    insertedStatements.Add(SyntaxFactory.ParseStatement("var " + parameter.VariableName + " = " + RenderParameterOverride(dictionaryExpression, parameter.IsList) + "[" + ToCSharpStringLiteral(parameter.ParameterName) + "];"));
                    dictionaryBackedParameters[parameter.VariableName] = parameter.IsList;
                }
                else
                {
                    insertedStatements.Add(SyntaxFactory.ParseStatement(parameter.DefaultDeclaration));
                }
            }

            foreach (var localDeclaration in inline.LocalDeclarations)
            {
                insertedStatements.Add(SyntaxFactory.ParseStatement(RewriteDictionaryParameterUsage(localDeclaration, dictionaryBackedParameters)));
            }

            var nestedTypes = new List<MemberDeclarationSyntax>(inline.NestedTypeDeclarations.Count);
            foreach (var nestedType in inline.NestedTypeDeclarations)
            {
                if (SyntaxFactory.ParseMemberDeclaration(nestedType) is not TypeDeclarationSyntax parsedType)
                {
                    return null;
                }

                nestedTypes.Add(parsedType.WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.SealedKeyword))));
            }

            return new SqTSqlParserParseCodeFixPlan(
                replacementRoot,
                anchorStatement,
                containingType,
                SyntaxFactory.IdentifierName(queryVariableName),
                insertedStatements,
                nestedTypes,
                RequiredNamespaces.Concat(requiredNamespaces),
                "SqExpress.SqQueryBuilder");
        }

        public static bool TryGetConversionFailureMessage(
            SemanticModel semanticModel,
            SqTSqlParserInvocation match,
            CancellationToken cancellationToken,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (match.Invocation.ArgumentList.Arguments.Count > 2)
            {
                failureMessage = "SqTSqlParser.Parse conversion supports up to two arguments.";
                return true;
            }

            if (!TryParseExpectedTables(match.SqlText, out var expectedTables, out failureMessage))
            {
                return true;
            }

            if (!TryResolveExpectedTableBindings(
                    semanticModel,
                    match,
                    expectedTables,
                    anchorStatement: null,
                    BuildSourceTableCatalog(semanticModel.Compilation, cancellationToken),
                    cancellationToken,
                    out var _,
                    out var _,
                    out var _,
                    out failureMessage))
            {
                return true;
            }

            return false;
        }

        public static CompilationUnitSyntax AddRequiredUsings(
            CompilationUnitSyntax root,
            ImmutableArray<string> namespaces,
            string staticUsing)
        {
            var existing = new HashSet<string>(
                root.Usings
                    .Where(i => i.StaticKeyword.IsKind(SyntaxKind.None))
                    .Select(i => i.Name?.ToString() ?? string.Empty),
                StringComparer.Ordinal);

            foreach (var item in namespaces)
            {
                if (existing.Add(item))
                {
                    root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(item)));
                }
            }

            var hasStatic = root.Usings.Any(i =>
                i.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                && string.Equals(i.Name?.ToString(), staticUsing, StringComparison.Ordinal));
            if (!hasStatic)
            {
                root = root.AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(staticUsing))
                        .WithStaticKeyword(SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
            }

            return root;
        }

        private static bool TryParseExpectedTables(
            string sqlText,
            out IReadOnlyList<ExpectedTableInfo> expectedTables,
            out string failureMessage)
        {
            expectedTables = Array.Empty<ExpectedTableInfo>();
            failureMessage = string.Empty;

            if (!SqTSqlParser.TryParse(sqlText, out IExpr? parsedExpr, out IReadOnlyList<SqExpress.DbMetadata.SqTable>? _, out string? parseError))
            {
                failureMessage = "Could not parse SQL. " + (parseError ?? "Unknown parser error.");
                return false;
            }

            expectedTables = CollectExpectedTables(parsedExpr!);
            return true;
        }

        private static bool TryResolveExpectedTableBindings(
            SemanticModel semanticModel,
            SqTSqlParserInvocation match,
            IReadOnlyList<ExpectedTableInfo> expectedTables,
            RoslynStatementSyntax? anchorStatement,
            IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>> sourceCatalog,
            CancellationToken cancellationToken,
            out IReadOnlyList<RoslynStatementSyntax> tableDeclarations,
            out IReadOnlyList<SqExpressSqlInlineTableBinding> inlineBindings,
            out IReadOnlyList<string> requiredNamespaces,
            out string failureMessage)
        {
            tableDeclarations = Array.Empty<RoslynStatementSyntax>();
            inlineBindings = Array.Empty<SqExpressSqlInlineTableBinding>();
            requiredNamespaces = Array.Empty<string>();
            failureMessage = string.Empty;

            if (expectedTables.Count < 1)
            {
                return true;
            }

            if (TryCreateSourceCatalogBindings(expectedTables, sourceCatalog, anchorStatement, out tableDeclarations, out inlineBindings, out requiredNamespaces, out failureMessage))
            {
                return true;
            }

            if (match.Invocation.ArgumentList.Arguments.Count >= 2
                && TryResolveProvidedTables(
                    match.Invocation.ArgumentList.Arguments[1].Expression,
                    semanticModel,
                    cancellationToken,
                    out var providedTables)
                && TryCreateInlineBindings(expectedTables, providedTables, anchorStatement, out tableDeclarations, out inlineBindings))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(failureMessage))
            {
                failureMessage = "Could not resolve SQL tables to source-visible SqExpress table classes.";
            }

            return false;
        }

        private static bool TryFindReplacementRoot(
            InvocationExpressionSyntax parseInvocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax replacementRoot)
        {
            replacementRoot = parseInvocation;
            while (replacementRoot.Parent is MemberAccessExpressionSyntax memberAccess
                   && memberAccess.Expression == replacementRoot
                   && memberAccess.Parent is InvocationExpressionSyntax invocation
                   && IsWithParamsInvocation(invocation, semanticModel, cancellationToken))
            {
                replacementRoot = invocation;
            }

            return true;
        }

        private static bool IsWithParamsInvocation(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                return false;
            }

            return string.Equals(method.Name, "WithParams", StringComparison.Ordinal)
                   && string.Equals(method.ContainingType?.ToDisplayString(), "SqExpress.ExprExtension", StringComparison.Ordinal);
        }

        private static Dictionary<string, ExpressionSyntax> CollectParameterOverrides(
            ExpressionSyntax replacementRoot,
            InvocationExpressionSyntax parseInvocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, ExpressionSyntax>(StringComparer.OrdinalIgnoreCase);
            ExpressionSyntax current = parseInvocation;
            while (current.Parent is MemberAccessExpressionSyntax memberAccess
                   && memberAccess.Expression == current
                   && memberAccess.Parent is InvocationExpressionSyntax invocation
                   && IsWithParamsInvocation(invocation, semanticModel, cancellationToken))
            {
                ApplyWithParamsOverride(invocation, semanticModel, cancellationToken, result);
                current = invocation;

                if (ReferenceEquals(current, replacementRoot))
                {
                    break;
                }
            }

            return result;
        }

        private static void ApplyWithParamsOverride(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            Dictionary<string, ExpressionSyntax> result)
        {
            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count < 1)
            {
                return;
            }

            if (arguments.Count == 1 && IsDictionaryLike(arguments[0].Expression, semanticModel, cancellationToken))
            {
                result["*"] = arguments[0].Expression;
                return;
            }

            if (arguments.Count == 2
                && TryGetConstantString(arguments[0].Expression, semanticModel, cancellationToken, out var paramName))
            {
                result[NormalizeParameterName(paramName)] = arguments[1].Expression;
                return;
            }

            if (arguments.Count == 1 && TryEnumerateTupleArguments(arguments[0].Expression, out var tupleArgs))
            {
                foreach (var tuple in tupleArgs)
                {
                    if (TryGetConstantString(tuple.NameExpression, semanticModel, cancellationToken, out paramName))
                    {
                        result[NormalizeParameterName(paramName)] = tuple.ValueExpression;
                    }
                }

                return;
            }

            foreach (var argument in arguments)
            {
                if (argument.Expression is TupleExpressionSyntax tuple
                    && tuple.Arguments.Count == 2
                    && TryGetConstantString(tuple.Arguments[0].Expression, semanticModel, cancellationToken, out var tupleParamName))
                {
                    result[NormalizeParameterName(tupleParamName)] = tuple.Arguments[1].Expression;
                }
            }
        }

        private static string RenderParameterOverride(ExpressionSyntax expression, bool isList)
        {
            if (isList && expression is CollectionExpressionSyntax collection)
            {
                var elements = collection.Elements
                    .OfType<ExpressionElementSyntax>()
                    .Select(i => i.Expression.ToString())
                    .ToList();
                return "new[] {" + string.Join(", ", elements) + "}";
            }

            return expression.ToString();
        }

        private static bool TryEnumerateTupleArguments(
            ExpressionSyntax expression,
            out IReadOnlyList<TupleArgumentInfo> tuples)
        {
            switch (expression)
            {
                case CollectionExpressionSyntax collection:
                    tuples = collection.Elements
                        .OfType<ExpressionElementSyntax>()
                        .Select(i => i.Expression)
                        .OfType<TupleExpressionSyntax>()
                        .Where(i => i.Arguments.Count == 2)
                        .Select(i => new TupleArgumentInfo(i.Arguments[0].Expression, i.Arguments[1].Expression))
                        .ToList();
                    return tuples.Count > 0;
                case ImplicitArrayCreationExpressionSyntax implicitArray:
                    tuples = implicitArray.Initializer.Expressions
                        .OfType<TupleExpressionSyntax>()
                        .Where(i => i.Arguments.Count == 2)
                        .Select(i => new TupleArgumentInfo(i.Arguments[0].Expression, i.Arguments[1].Expression))
                        .ToList();
                    return tuples.Count > 0;
                case ArrayCreationExpressionSyntax arrayCreation when arrayCreation.Initializer != null:
                    tuples = arrayCreation.Initializer.Expressions
                        .OfType<TupleExpressionSyntax>()
                        .Where(i => i.Arguments.Count == 2)
                        .Select(i => new TupleArgumentInfo(i.Arguments[0].Expression, i.Arguments[1].Expression))
                        .ToList();
                    return tuples.Count > 0;
                default:
                    tuples = Array.Empty<TupleArgumentInfo>();
                    return false;
            }
        }

        private static bool IsDictionaryLike(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var expressionModel = GetSemanticModelForNode(semanticModel, expression);
            var type = expressionModel.GetTypeInfo(expression, cancellationToken).ConvertedType;
            if (type == null)
            {
                return false;
            }

            return type.AllInterfaces.Any(i => string.Equals(i.ToDisplayString(), "System.Collections.Generic.IReadOnlyDictionary<string, SqExpress.ParamValue>", StringComparison.Ordinal))
                   || string.Equals(type.ToDisplayString(), "System.Collections.Generic.IReadOnlyDictionary<string, SqExpress.ParamValue>", StringComparison.Ordinal)
                   || string.Equals(type.ToDisplayString(), "System.Collections.Generic.Dictionary<string, SqExpress.ParamValue>", StringComparison.Ordinal);
        }

        private static IReadOnlyList<ExpectedTableInfo> CollectExpectedTables(IExpr expr)
        {
            var result = new List<ExpectedTableInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in expr.SyntaxTree().DescendantsAndSelf().OfType<ExprTable>())
            {
                var alias = table.Alias != null
                    ? GetAliasName(table.Alias.Alias)
                    : ToCamelCaseIdentifier(table.FullName.AsExprTableFullName().TableName.Name, "t");
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                var tableKey = GetTableKey(table.FullName.AsExprTableFullName());
                if (seen.Add(alias + "|" + tableKey))
                {
                    result.Add(new ExpectedTableInfo(alias, tableKey));
                }
            }

            return result;
        }

        private static bool TryResolveProvidedTables(
            ExpressionSyntax tablesExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out IReadOnlyList<ProvidedTableInfo> tables)
        {
            tables = Array.Empty<ProvidedTableInfo>();

            if (!TryExtractTableExpressions(tablesExpression, semanticModel, cancellationToken, out var itemExpressions))
            {
                return false;
            }

            var result = new List<ProvidedTableInfo>(itemExpressions.Count);
            foreach (var item in itemExpressions)
            {
                if (!TryResolveProvidedTable(item, semanticModel, cancellationToken, out var provided))
                {
                    return false;
                }

                result.Add(provided);
            }

            tables = result;
            return true;
        }

        private static bool TryExtractTableExpressions(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out IReadOnlyList<ExpressionSyntax> itemExpressions)
        {
            switch (expression)
            {
                case CollectionExpressionSyntax collection:
                    itemExpressions = collection.Elements.OfType<ExpressionElementSyntax>().Select(i => i.Expression).ToList();
                    return true;
                case ImplicitArrayCreationExpressionSyntax implicitArray:
                    itemExpressions = implicitArray.Initializer.Expressions.ToList();
                    return true;
                case ArrayCreationExpressionSyntax arrayCreation when arrayCreation.Initializer != null:
                    itemExpressions = arrayCreation.Initializer.Expressions.ToList();
                    return true;
                case IdentifierNameSyntax:
                case MemberAccessExpressionSyntax:
                    if (!TryResolveSymbolInitializer(expression, semanticModel, cancellationToken, out var initializer))
                    {
                        itemExpressions = Array.Empty<ExpressionSyntax>();
                        return false;
                    }

                    return TryExtractTableExpressions(initializer, semanticModel, cancellationToken, out itemExpressions);
                case InvocationExpressionSyntax invocation:
                    if (!TryResolveInvocationResultExpression(invocation, semanticModel, cancellationToken, out var returnedExpression))
                    {
                        itemExpressions = Array.Empty<ExpressionSyntax>();
                        return false;
                    }

                    return TryExtractTableExpressions(returnedExpression, semanticModel, cancellationToken, out itemExpressions);
                default:
                    itemExpressions = Array.Empty<ExpressionSyntax>();
                    return false;
            }
        }

        private static bool TryResolveProvidedTable(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ProvidedTableInfo provided)
        {
            provided = default!;

            var expressionModel = GetSemanticModelForNode(semanticModel, expression);
            var type = expressionModel.GetTypeInfo(expression, cancellationToken).Type;
            if (type == null || !DerivesFromTableBase(type))
            {
                return false;
            }

            if (!TryResolveTableConstruction(expression, expressionModel, cancellationToken, out var construction))
            {
                return false;
            }

            string? variableReference = null;
            string? initializerExpression = null;
            if (expression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
            {
                variableReference = expression.ToString();
            }
            else
            {
                if (!TryResolveTableInitializerExpression(expression, semanticModel, cancellationToken, out var tableInitializer))
                {
                    return false;
                }

                initializerExpression = tableInitializer.ToString();
            }

            provided = new ProvidedTableInfo(
                variableReference,
                initializerExpression,
                type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                construction.TableKey,
                construction.Alias);
            return true;
        }

        private static bool TryResolveTableConstruction(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out TableConstructionInfo info)
        {
            info = default!;

            if (!TryResolveTableInitializerExpression(expression, semanticModel, cancellationToken, out var initializer))
            {
                return false;
            }

            if (initializer is not ObjectCreationExpressionSyntax objectCreation)
            {
                return false;
            }

            var objectCreationModel = GetSemanticModelForNode(semanticModel, objectCreation);
            if (objectCreationModel.GetSymbolInfo(objectCreation, cancellationToken).Symbol is not IMethodSymbol constructor)
            {
                return false;
            }

            if (!TryResolveTableInfoFromConstructor(constructor, objectCreationModel.Compilation, cancellationToken, out var tableKey))
            {
                return false;
            }

            info = new TableConstructionInfo(tableKey, TryResolveAliasArgument(objectCreation, constructor, objectCreationModel, cancellationToken));
            return true;
        }

        private static bool TryResolveTableInitializerExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax initializer)
        {
            initializer = null!;

            switch (expression)
            {
                case ObjectCreationExpressionSyntax objectCreation:
                    initializer = objectCreation;
                    return true;
                case IdentifierNameSyntax:
                case MemberAccessExpressionSyntax:
                    if (!TryResolveSymbolInitializer(expression, semanticModel, cancellationToken, out var symbolInitializer))
                    {
                        return false;
                    }

                    return TryResolveTableInitializerExpression(symbolInitializer, semanticModel, cancellationToken, out initializer);
                case InvocationExpressionSyntax invocation:
                    if (!TryResolveInvocationResultExpression(invocation, semanticModel, cancellationToken, out var returnedExpression))
                    {
                        return false;
                    }

                    return TryResolveTableInitializerExpression(returnedExpression, semanticModel, cancellationToken, out initializer);
                default:
                    return false;
            }
        }

        private static bool TryResolveSymbolInitializer(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax initializer)
        {
            initializer = null!;

            var expressionModel = GetSemanticModelForNode(semanticModel, expression);
            var symbol = expressionModel.GetSymbolInfo(expression, cancellationToken).Symbol;
            switch (symbol)
            {
                case ILocalSymbol localSymbol:
                    initializer = localSymbol.DeclaringSyntaxReferences
                        .Select(i => i.GetSyntax(cancellationToken))
                        .OfType<VariableDeclaratorSyntax>()
                        .Select(i => i.Initializer?.Value)
                        .FirstOrDefault(i => i != null)!;
                    return initializer != null;
                case IFieldSymbol fieldSymbol:
                    initializer = fieldSymbol.DeclaringSyntaxReferences
                        .Select(i => i.GetSyntax(cancellationToken))
                        .OfType<VariableDeclaratorSyntax>()
                        .Select(i => i.Initializer?.Value)
                        .FirstOrDefault(i => i != null)!;
                    return initializer != null;
                case IPropertySymbol propertySymbol:
                    foreach (var syntaxRef in propertySymbol.DeclaringSyntaxReferences)
                    {
                        var syntax = syntaxRef.GetSyntax(cancellationToken);
                        switch (syntax)
                        {
                            case PropertyDeclarationSyntax property when property.ExpressionBody != null:
                                initializer = property.ExpressionBody.Expression;
                                return true;
                            case PropertyDeclarationSyntax property when property.Initializer != null:
                                initializer = property.Initializer.Value;
                                return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool TryResolveInvocationResultExpression(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax expression)
        {
            expression = null!;

            var invocationModel = GetSemanticModelForNode(semanticModel, invocation);
            if (invocationModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            if (methodSymbol.Parameters.Length != invocation.ArgumentList.Arguments.Count)
            {
                return false;
            }

            if (methodSymbol.Parameters.Length > 0)
            {
                return false;
            }

            foreach (var syntaxRef in methodSymbol.DeclaringSyntaxReferences)
            {
                switch (syntaxRef.GetSyntax(cancellationToken))
                {
                    case MethodDeclarationSyntax methodDeclaration when methodDeclaration.ExpressionBody != null:
                        expression = methodDeclaration.ExpressionBody.Expression;
                        return true;
                    case MethodDeclarationSyntax methodDeclaration when methodDeclaration.Body != null:
                    {
                        var returns = methodDeclaration.Body.Statements.OfType<ReturnStatementSyntax>().ToList();
                        if (returns.Count == 1 && returns[0].Expression != null)
                        {
                            expression = returns[0].Expression!;
                            return true;
                        }

                        break;
                    }
                }
            }

            return false;
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

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

            if (syntax.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
            {
                if (semanticModel.GetSymbolInfo(syntax.Initializer, cancellationToken).Symbol is not IMethodSymbol chainedConstructor)
                {
                    return false;
                }

                return TryResolveTableInfoFromConstructorCore(chainedConstructor, compilation, cancellationToken, out schema, out tableName);
            }

            if (!syntax.Initializer.IsKind(SyntaxKind.BaseConstructorInitializer))
            {
                return false;
            }

            if (semanticModel.GetSymbolInfo(syntax.Initializer, cancellationToken).Symbol is not IMethodSymbol baseConstructor)
            {
                return false;
            }

            if (!string.Equals(baseConstructor.ContainingType.ToDisplayString(), "SqExpress.TableBase", StringComparison.Ordinal))
            {
                return false;
            }

            var args = syntax.Initializer.ArgumentList?.Arguments;
            if (args == null)
            {
                return false;
            }

            if (args.Value.Count >= 3
                && TryGetConstantString(args.Value[0].Expression, semanticModel, cancellationToken, out schema)
                && TryGetConstantString(args.Value[1].Expression, semanticModel, cancellationToken, out tableName))
            {
                return true;
            }

            if (args.Value.Count >= 4
                && TryGetConstantString(args.Value[1].Expression, semanticModel, cancellationToken, out schema)
                && TryGetConstantString(args.Value[2].Expression, semanticModel, cancellationToken, out tableName))
            {
                return true;
            }

            return false;
        }

        private static string? TryResolveAliasArgument(
            ObjectCreationExpressionSyntax objectCreation,
            IMethodSymbol constructor,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (objectCreation.ArgumentList == null)
            {
                return null;
            }

            for (var index = 0; index < objectCreation.ArgumentList.Arguments.Count && index < constructor.Parameters.Length; index++)
            {
                var parameter = constructor.Parameters[index];
                if (!string.Equals(parameter.Type.ToDisplayString(), "SqExpress.Alias", StringComparison.Ordinal))
                {
                    continue;
                }

                return TryGetConstantString(objectCreation.ArgumentList.Arguments[index].Expression, semanticModel, cancellationToken, out var alias)
                    ? alias
                    : null;
            }

            return null;
        }

        private static bool TryCreateInlineBindings(
            IReadOnlyList<ExpectedTableInfo> expectedTables,
            IReadOnlyList<ProvidedTableInfo> providedTables,
            RoslynStatementSyntax? anchorStatement,
            out IReadOnlyList<RoslynStatementSyntax> tableDeclarations,
            out IReadOnlyList<SqExpressSqlInlineTableBinding> inlineBindings)
        {
            tableDeclarations = Array.Empty<RoslynStatementSyntax>();
            inlineBindings = Array.Empty<SqExpressSqlInlineTableBinding>();

            if (expectedTables.GroupBy(i => i.TableKey, StringComparer.OrdinalIgnoreCase).Any(i => i.Count() > 1))
            {
                return false;
            }

            var providedByKey = providedTables
                .GroupBy(i => i.TableKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i.Key, i => i.ToList(), StringComparer.OrdinalIgnoreCase);

            var declarations = new List<RoslynStatementSyntax>();
            var result = new List<SqExpressSqlInlineTableBinding>(expectedTables.Count);
            foreach (var expected in expectedTables)
            {
                if (!providedByKey.TryGetValue(expected.TableKey, out var candidates) || candidates.Count != 1)
                {
                    return false;
                }

                var provided = candidates[0];
                var variableName = provided.VariableReference;
                if (string.IsNullOrWhiteSpace(variableName))
                {
                    if (string.IsNullOrWhiteSpace(provided.InitializerExpression))
                    {
                        return false;
                    }

                    if (anchorStatement == null)
                    {
                        return false;
                    }

                    variableName = MakeUniqueLocalName(anchorStatement, ToCamelCaseIdentifier(expected.Alias, "t"));
                    declarations.Add(SyntaxFactory.ParseStatement("var " + variableName + " = " + provided.InitializerExpression + ";"));
                }

                if (string.IsNullOrWhiteSpace(variableName))
                {
                    return false;
                }

                var resolvedVariableName = variableName!;
                result.Add(new SqExpressSqlInlineTableBinding(expected.TableKey, expected.Alias, resolvedVariableName, provided.TypeName));
            }

            tableDeclarations = declarations;
            inlineBindings = result;
            return true;
        }

        private static bool TryCreateSourceCatalogBindings(
            IReadOnlyList<ExpectedTableInfo> expectedTables,
            IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>> sourceCatalog,
            RoslynStatementSyntax? anchorStatement,
            out IReadOnlyList<RoslynStatementSyntax> tableDeclarations,
            out IReadOnlyList<SqExpressSqlInlineTableBinding> inlineBindings,
            out IReadOnlyList<string> requiredNamespaces,
            out string failureMessage)
        {
            tableDeclarations = Array.Empty<RoslynStatementSyntax>();
            inlineBindings = Array.Empty<SqExpressSqlInlineTableBinding>();
            requiredNamespaces = Array.Empty<string>();
            failureMessage = string.Empty;

            if (expectedTables.GroupBy(i => i.TableKey, StringComparer.OrdinalIgnoreCase).Any(i => i.Count() > 1))
            {
                failureMessage = "Cannot convert SQL with multiple references to the same table because a unique source table binding cannot be inferred.";
                return false;
            }

            var declarations = new List<RoslynStatementSyntax>(expectedTables.Count);
            var bindings = new List<SqExpressSqlInlineTableBinding>(expectedTables.Count);
            var usedNamespaces = new HashSet<string>(StringComparer.Ordinal);
            var usedNames = anchorStatement != null
                ? new HashSet<string>(
                    anchorStatement.FirstAncestorOrSelf<BlockSyntax>() != null
                        ? anchorStatement.FirstAncestorOrSelf<BlockSyntax>()!
                            .DescendantNodes()
                            .OfType<VariableDeclaratorSyntax>()
                            .Select(i => i.Identifier.ValueText)
                        : Enumerable.Empty<string>(),
                    StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            foreach (var expected in expectedTables)
            {
                if (!sourceCatalog.TryGetValue(expected.TableKey, out var candidates) || candidates.Count < 1)
                {
                    failureMessage = "No SqExpress table class found for SQL table " + FormatTableKey(expected.TableKey) + ".";
                    return false;
                }

                if (candidates.Count > 1)
                {
                    failureMessage = "Multiple SqExpress table classes map to SQL table " + FormatTableKey(expected.TableKey) + ": "
                                     + string.Join(", ", candidates.Select(i => i.SimpleTypeName).OrderBy(i => i, StringComparer.OrdinalIgnoreCase)) + ".";
                    return false;
                }

                var candidate = candidates[0];
                if (!candidate.SupportsParameterlessConstructor && !candidate.SupportsAliasConstructor)
                {
                    failureMessage = "SqExpress table class " + candidate.SimpleTypeName + " was found for SQL table "
                                     + FormatTableKey(expected.TableKey) + ", but no supported constructor is available.";
                    return false;
                }

                var variableName = MakeUniqueLocalName(usedNames, candidate.VariableBaseName);
                var descriptorTypeName = candidate.PreferredTypeName;
                var creationExpression = candidate.SupportsAliasConstructor
                    ? "new " + descriptorTypeName + "(" + ToCSharpStringLiteral(expected.Alias) + ")"
                    : "new " + descriptorTypeName + "()";
                declarations.Add(SyntaxFactory.ParseStatement("var " + variableName + " = " + creationExpression + ";"));
                bindings.Add(new SqExpressSqlInlineTableBinding(expected.TableKey, expected.Alias, variableName, descriptorTypeName));
                if (!string.IsNullOrWhiteSpace(candidate.NamespaceName))
                {
                    usedNamespaces.Add(candidate.NamespaceName!);
                }
            }

            tableDeclarations = declarations;
            inlineBindings = bindings;
            requiredNamespaces = usedNamespaces.OrderBy(i => i, StringComparer.Ordinal).ToArray();
            return true;
        }

        private static async Task<IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>>> BuildSourceTableCatalogAsync(
            Solution solution,
            CancellationToken cancellationToken)
        {
            var byKey = new Dictionary<string, List<SourceTableInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in solution.Projects.Where(i => i.Language == LanguageNames.CSharp))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation == null)
                {
                    continue;
                }

                VisitNamespace(compilation.Assembly.GlobalNamespace, compilation, cancellationToken, byKey);
            }

            return byKey.ToDictionary(i => i.Key, i => (IReadOnlyList<SourceTableInfo>)i.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<SourceTableInfo>> BuildSourceTableCatalog(
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var byKey = new Dictionary<string, List<SourceTableInfo>>(StringComparer.OrdinalIgnoreCase);
            VisitNamespace(compilation.Assembly.GlobalNamespace, compilation, cancellationToken, byKey);
            return byKey.ToDictionary(i => i.Key, i => (IReadOnlyList<SourceTableInfo>)i.Value, StringComparer.OrdinalIgnoreCase);
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

                    items.Add(info);
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
            info = default!;

            var constructors = namedType.InstanceConstructors
                .Where(i => i.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            string? tableKey = null;
            foreach (var constructor in constructors)
            {
                if (TryResolveTableInfoFromConstructor(constructor, compilation, cancellationToken, out var resolvedKey))
                {
                    tableKey = resolvedKey;
                    break;
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

            var resolvedTableKey = tableKey!;
            info = new SourceTableInfo(
                resolvedTableKey,
                namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                namedType.Name,
                namedType.ContainingNamespace?.IsGlobalNamespace == false ? namedType.ContainingNamespace.ToDisplayString() : null,
                ToCamelCaseIdentifier(namedType.Name, "table"),
                supportsParameterlessConstructor,
                supportsAliasConstructor);
            return true;
        }

        private static bool TryGetConstantString(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out string value)
        {
            value = string.Empty;
            var constant = semanticModel.GetConstantValue(expression, cancellationToken);
            if (!constant.HasValue || constant.Value is not string stringValue)
            {
                return false;
            }

            value = stringValue;
            return true;
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

        private static string MakeUniqueLocalName(RoslynStatementSyntax anchorStatement, string baseName)
        {
            var usedNames = anchorStatement.FirstAncestorOrSelf<BlockSyntax>() != null
                ? new HashSet<string>(
                    anchorStatement.FirstAncestorOrSelf<BlockSyntax>()!
                        .DescendantNodes()
                        .OfType<VariableDeclaratorSyntax>()
                        .Select(i => i.Identifier.ValueText),
                    StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            var name = baseName;
            var index = 1;
            while (!usedNames.Add(name))
            {
                name = baseName + index.ToString(CultureInfo.InvariantCulture);
                index++;
            }

            return name;
        }

        private static string MakeUniqueLocalName(ISet<string> usedNames, string baseName)
        {
            var name = baseName;
            var index = 1;
            while (!usedNames.Add(name))
            {
                name = baseName + index.ToString(CultureInfo.InvariantCulture);
                index++;
            }

            return name;
        }

        private static string GetAliasName(IExprAlias alias)
        {
            return alias switch
            {
                ExprAlias exprAlias => exprAlias.Name,
                ExprAliasGuid exprAliasGuid => "A" + Math.Abs(exprAliasGuid.Id.GetHashCode()).ToString(CultureInfo.InvariantCulture),
                _ => "A0"
            };
        }

        private static string GetTableKey(ExprTableFullName fullName)
        {
            return BuildTableKey(fullName.DbSchema?.Schema.Name, fullName.TableName.Name);
        }

        private static string BuildTableKey(string? schema, string tableName)
        {
            return (schema ?? string.Empty) + "." + tableName;
        }

        private static SemanticModel GetSemanticModelForNode(SemanticModel semanticModel, SyntaxNode node)
        {
            if (node.SyntaxTree == semanticModel.SyntaxTree)
            {
                return semanticModel;
            }

            return semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
        }

        private static string FormatTableKey(string tableKey)
        {
            var parts = tableKey.Split(new[] { '.' }, 2);
            return parts.Length == 2
                ? "[" + parts[0] + "].[" + parts[1] + "]"
                : "[" + tableKey + "]";
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

        private static string NormalizeParameterName(string name)
        {
            return string.IsNullOrEmpty(name) ? string.Empty : name.TrimStart('@');
        }

        private static string ToCSharpStringLiteral(string value)
        {
            return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(value))
                .ToFullString();
        }

        private static string RewriteDictionaryParameterUsage(string code, IReadOnlyDictionary<string, bool> dictionaryBackedParameters)
        {
            var result = code;
            foreach (var pair in dictionaryBackedParameters)
            {
                var replacement = pair.Key + (pair.Value ? ".AsList" : ".AsSingle");
                result = Regex.Replace(result, "\\b" + Regex.Escape(pair.Key) + "\\b", replacement);
            }

            return result;
        }

        private readonly struct ExpectedTableInfo
        {
            public ExpectedTableInfo(string alias, string tableKey)
            {
                this.Alias = alias;
                this.TableKey = tableKey;
            }

            public string Alias { get; }

            public string TableKey { get; }
        }

        private readonly struct ProvidedTableInfo
        {
            public ProvidedTableInfo(string? variableReference, string? initializerExpression, string typeName, string tableKey, string? alias)
            {
                this.VariableReference = variableReference;
                this.InitializerExpression = initializerExpression;
                this.TypeName = typeName;
                this.TableKey = tableKey;
                this.Alias = alias;
            }

            public string? VariableReference { get; }

            public string? InitializerExpression { get; }

            public string TypeName { get; }

            public string TableKey { get; }

            public string? Alias { get; }
        }

        private readonly struct TableConstructionInfo
        {
            public TableConstructionInfo(string tableKey, string? alias)
            {
                this.TableKey = tableKey;
                this.Alias = alias;
            }

            public string TableKey { get; }

            public string? Alias { get; }
        }

        private readonly struct SourceTableInfo
        {
            public SourceTableInfo(
                string tableKey,
                string typeName,
                string simpleTypeName,
                string? namespaceName,
                string variableBaseName,
                bool supportsParameterlessConstructor,
                bool supportsAliasConstructor)
            {
                this.TableKey = tableKey;
                this.TypeName = typeName;
                this.SimpleTypeName = simpleTypeName;
                this.NamespaceName = namespaceName;
                this.VariableBaseName = variableBaseName;
                this.SupportsParameterlessConstructor = supportsParameterlessConstructor;
                this.SupportsAliasConstructor = supportsAliasConstructor;
            }

            public string TableKey { get; }

            public string TypeName { get; }

            public string SimpleTypeName { get; }

            public string? NamespaceName { get; }

            public string VariableBaseName { get; }

            public bool SupportsParameterlessConstructor { get; }

            public bool SupportsAliasConstructor { get; }

            public string PreferredTypeName => this.SimpleTypeName;
        }

        private readonly struct TupleArgumentInfo
        {
            public TupleArgumentInfo(ExpressionSyntax nameExpression, ExpressionSyntax valueExpression)
            {
                this.NameExpression = nameExpression;
                this.ValueExpression = valueExpression;
            }

            public ExpressionSyntax NameExpression { get; }

            public ExpressionSyntax ValueExpression { get; }
        }
    }

    internal sealed class SqTSqlParserParseCodeFixPlan
    {
        public SqTSqlParserParseCodeFixPlan(
            ExpressionSyntax replacementRoot,
            RoslynStatementSyntax anchorStatement,
            TypeDeclarationSyntax containingType,
            ExpressionSyntax replacementExpression,
            IReadOnlyList<RoslynStatementSyntax> insertedStatements,
            IReadOnlyList<MemberDeclarationSyntax> nestedTypes,
            IEnumerable<string> requiredNamespaces,
            string requiredStaticUsing)
        {
            this.ReplacementRoot = replacementRoot;
            this.AnchorStatement = anchorStatement;
            this.ContainingType = containingType;
            this.ReplacementExpression = replacementExpression;
            this.InsertedStatements = insertedStatements;
            this.NestedTypes = nestedTypes;
            this.RequiredNamespaces = requiredNamespaces.ToImmutableArray();
            this.RequiredStaticUsing = requiredStaticUsing;
        }

        public ExpressionSyntax ReplacementRoot { get; }

        public RoslynStatementSyntax AnchorStatement { get; }

        public TypeDeclarationSyntax ContainingType { get; }

        public ExpressionSyntax ReplacementExpression { get; }

        public IReadOnlyList<RoslynStatementSyntax> InsertedStatements { get; }

        public IReadOnlyList<MemberDeclarationSyntax> NestedTypes { get; }

        public ImmutableArray<string> RequiredNamespaces { get; }

        public string RequiredStaticUsing { get; }
    }
}
