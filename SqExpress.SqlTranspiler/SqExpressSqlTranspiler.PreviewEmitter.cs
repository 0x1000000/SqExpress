using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SqExpress.SqlTranspiler
{
    public sealed partial class SqExpressSqlTranspiler
    {
        private sealed class QueryPreviewBuildSource
        {
            public QueryPreviewBuildSource(string variableName, string className, string alias, string initializationExpression)
            {
                this.VariableName = variableName;
                this.ClassName = className;
                this.Alias = alias;
                this.InitializationExpression = initializationExpression;
            }

            public string VariableName { get; }

            public string ClassName { get; }

            public string Alias { get; }

            public string InitializationExpression { get; }
        }

        private sealed class QueryPreviewBuildModel
        {
            public QueryPreviewBuildModel(
                IReadOnlyList<QueryPreviewBuildSource> outSources,
                IReadOnlyList<QueryPreviewBuildSource> localSources,
                IReadOnlyList<string> parameterDeclarations,
                IReadOnlyList<string> readStatements,
                IReadOnlyList<MemberDeclarationSyntax> nestedTypes,
                string queryExpressionCode)
            {
                this.OutSources = outSources;
                this.LocalSources = localSources;
                this.ParameterDeclarations = parameterDeclarations;
                this.ReadStatements = readStatements;
                this.NestedTypes = nestedTypes;
                this.QueryExpressionCode = queryExpressionCode;
            }

            public IReadOnlyList<QueryPreviewBuildSource> OutSources { get; }

            public IReadOnlyList<QueryPreviewBuildSource> LocalSources { get; }

            public IReadOnlyList<string> ParameterDeclarations { get; }

            public IReadOnlyList<string> ReadStatements { get; }

            public IReadOnlyList<MemberDeclarationSyntax> NestedTypes { get; }

            public string QueryExpressionCode { get; }
        }

        private sealed class QueryPreviewEmitter
        {
            private enum SourceKind
            {
                Table,
                Derived,
                Cte,
                Other
            }

            private sealed class SourceBinding
            {
                public SourceBinding(string alias, string variableName, SourceKind sourceKind, string? className, string initializationExpression)
                {
                    this.Alias = alias;
                    this.VariableName = variableName;
                    this.SourceKind = sourceKind;
                    this.ClassName = className;
                    this.InitializationExpression = initializationExpression;
                }

                public string Alias { get; }

                public string VariableName { get; }

                public SourceKind SourceKind { get; }

                public string? ClassName { get; }

                public string InitializationExpression { get; }
            }

            private sealed class RenderContext
            {
                public RenderContext()
                {
                    this.BindingsByAlias = new Dictionary<string, SourceBinding>(StringComparer.OrdinalIgnoreCase);
                    this.UsedVariableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    this.OutSources = new List<QueryPreviewBuildSource>();
                    this.LocalSources = new List<QueryPreviewBuildSource>();
                }

                public Dictionary<string, SourceBinding> BindingsByAlias { get; }

                public HashSet<string> UsedVariableNames { get; }

                public List<QueryPreviewBuildSource> OutSources { get; }

                public List<QueryPreviewBuildSource> LocalSources { get; }
            }

            private sealed class LeafSource
            {
                public LeafSource(IExprTableSource source)
                {
                    this.Source = source;
                }

                public IExprTableSource Source { get; }
            }

            private readonly IExpr _previewExpr;
            private readonly string _statementKind;
            private readonly SqExpressSqlTranspilerOptions _options;
            private readonly IReadOnlyDictionary<string, string> _classNamesByTableKey;
            private readonly Dictionary<string, Dictionary<string, string>> _columnTypesByClassName;
            private readonly IReadOnlyDictionary<string, ExprValue> _parameterDefaults;
            private readonly IReadOnlyCollection<string> _listParameters;

            private readonly Dictionary<string, string> _tableClassByAlias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> _tableVariableByAlias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> _tableClassByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, string> _cteClassByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> _derivedClassByAlias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private readonly HashSet<string> _usedNestedTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly List<MemberDeclarationSyntax> _nestedTypes = new List<MemberDeclarationSyntax>();

            public QueryPreviewEmitter(
                IExpr previewExpr,
                string statementKind,
                SqExpressSqlTranspilerOptions options,
                IReadOnlyDictionary<string, string> classNamesByTableKey,
                IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> columnTypesByClassName,
                IReadOnlyDictionary<string, ExprValue> parameterDefaults,
                IReadOnlyCollection<string> listParameters)
            {
                this._previewExpr = previewExpr;
                this._statementKind = statementKind;
                this._options = options;
                this._classNamesByTableKey = classNamesByTableKey;
                this._columnTypesByClassName = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                this._parameterDefaults = parameterDefaults;
                this._listParameters = listParameters;

                foreach (var pair in classNamesByTableKey)
                {
                    this._tableClassByKey[pair.Key] = pair.Value;
                }

                foreach (var classPair in columnTypesByClassName)
                {
                    this._columnTypesByClassName[classPair.Key] = new Dictionary<string, string>(classPair.Value, StringComparer.OrdinalIgnoreCase);
                }
            }

            public QueryPreviewBuildModel BuildModel(IReadOnlyList<TableUsage> buildUsages)
            {
                foreach (var usage in buildUsages)
                {
                    if (!string.IsNullOrWhiteSpace(usage.Alias)
                        && !string.IsNullOrWhiteSpace(usage.ClassName))
                    {
                        this._tableClassByAlias[usage.Alias] = usage.ClassName;
                        this._tableVariableByAlias[usage.Alias] = usage.VariableName;
                    }
                }

                this.PrepareNestedTypes();

                var rootContext = new RenderContext();
                this.PreRegisterTopLevelOutSources(rootContext);

                string queryExpressionCode = this.RenderStatement(this._previewExpr, rootContext);
                if (this._options.UseStaticSqQueryBuilderUsing == false)
                {
                    queryExpressionCode = "SqQueryBuilder." + queryExpressionCode;
                }

                var parameterDeclarations = this.BuildParameterDeclarations();
                var readStatements = this.BuildReadStatements(rootContext);

                return new QueryPreviewBuildModel(
                    rootContext.OutSources,
                    rootContext.LocalSources,
                    parameterDeclarations,
                    readStatements,
                    this._nestedTypes,
                    queryExpressionCode);
            }

            private void PrepareNestedTypes()
            {
                var all = this._previewExpr.SyntaxTree().DescendantsAndSelf();
                foreach (var cte in all.OfType<ExprCteQuery>())
                {
                    this.EnsureCteClass(cte);
                }
                foreach (var derived in all.OfType<ExprDerivedTableQuery>())
                {
                    this.EnsureDerivedClass(derived);
                }
            }

            private void PreRegisterTopLevelOutSources(RenderContext context)
            {
                if (!string.Equals(this._statementKind, "SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (this._previewExpr is not IExprQuery query)
                {
                    return;
                }

                var topFrom = GetTopLevelFrom(query);
                if (topFrom is null)
                {
                    return;
                }

                foreach (var leaf in this.ExtractLeafSources(topFrom))
                {
                    if (this.TryCreateBindingForSource(context, leaf.Source, out var binding, isOutPreferred: true))
                    {
                        if (binding is null)
                        {
                            continue;
                        }

                        context.BindingsByAlias[binding.Alias] = binding;
                        if (binding.ClassName is string className
                            && (binding.SourceKind == SourceKind.Table
                                || binding.SourceKind == SourceKind.Cte
                                || binding.SourceKind == SourceKind.Derived))
                        {
                            context.OutSources.Add(
                                new QueryPreviewBuildSource(
                                    binding.VariableName,
                                    className,
                                    binding.Alias,
                                    binding.InitializationExpression));
                        }
                        else
                        {
                            context.LocalSources.Add(
                                new QueryPreviewBuildSource(
                                    binding.VariableName,
                                    binding.ClassName ?? "object",
                                    binding.Alias,
                                    binding.InitializationExpression));
                        }
                    }
                }
            }

            private static IExprTableSource? GetTopLevelFrom(IExprQuery query)
            {
                if (query is ExprSelect exprSelect)
                {
                    return GetTopLevelFrom(exprSelect.SelectQuery);
                }

                if (query is IExprSubQuery subQuery)
                {
                    return GetTopLevelFrom(subQuery);
                }

                return null;
            }

            private static IExprTableSource? GetTopLevelFrom(IExprSubQuery subQuery)
            {
                if (subQuery is ExprQuerySpecification querySpecification)
                {
                    return querySpecification.From;
                }

                if (subQuery is ExprSelectOffsetFetch offsetFetch)
                {
                    return GetTopLevelFrom(offsetFetch.SelectQuery);
                }

                return null;
            }
            private IReadOnlyList<string> BuildParameterDeclarations()
            {
                var result = new List<string>(this._parameterDefaults.Count);
                foreach (var pair in this._parameterDefaults.OrderBy(i => i.Key, StringComparer.Ordinal))
                {
                    var variable = NormalizeParameterName(pair.Key);
                    if (this._listParameters.Contains(pair.Key))
                    {
                        var item = this.RenderListParameterItem(pair.Value);
                        result.Add("var " + variable + " = new[] { " + item + " };");
                        continue;
                    }

                    result.Add("var " + variable + " = " + this.RenderParameterDefaultValue(pair.Value) + ";");
                }

                return result;
            }

            private string RenderListParameterItem(ExprValue value)
            {
                if (value is ExprStringLiteral literal)
                {
                    return "Literal(" + ToCSharpStringLiteral(literal.Value ?? string.Empty) + ")";
                }

                return "Literal(" + this.RenderValue(value, new RenderContext()) + ")";
            }

            private string RenderParameterDefaultValue(ExprValue value)
            {
                switch (value)
                {
                    case ExprStringLiteral literal:
                        return ToCSharpStringLiteral(literal.Value ?? string.Empty);
                    case ExprInt16Literal int16Literal:
                        return int16Literal.Value?.ToString(CultureInfo.InvariantCulture) ?? "0";
                    case ExprInt32Literal int32Literal:
                        return int32Literal.Value?.ToString(CultureInfo.InvariantCulture) ?? "0";
                    case ExprInt64Literal int64Literal:
                        return int64Literal.Value?.ToString(CultureInfo.InvariantCulture) + "L" ?? "0L";
                    case ExprDecimalLiteral decimalLiteral:
                        return (decimalLiteral.Value ?? 0m).ToString(CultureInfo.InvariantCulture) + "M";
                    case ExprDoubleLiteral doubleLiteral:
                        return (doubleLiteral.Value ?? 0d).ToString("R", CultureInfo.InvariantCulture) + "D";
                    case ExprBoolLiteral boolLiteral:
                        return boolLiteral.Value == true ? "true" : "false";
                    case ExprGuidLiteral:
                        return "default(global::System.Guid)";
                    case ExprDateTimeLiteral:
                        return "default(global::System.DateTime)";
                    case ExprDateTimeOffsetLiteral:
                        return "default(global::System.DateTimeOffset)";
                    case ExprByteArrayLiteral:
                        return "global::System.Array.Empty<byte>()";
                    default:
                        return this.RenderValue(value, new RenderContext());
                }
            }

            private IReadOnlyList<string> BuildReadStatements(RenderContext context)
            {
                IExprSubQuery? topSelect = null;
                if (this._previewExpr is ExprSelect select)
                {
                    topSelect = select.SelectQuery;
                }
                else if (this._previewExpr is IExprSubQuery subQuery)
                {
                    topSelect = subQuery;
                }

                if (topSelect is null)
                {
                    return Array.Empty<string>();
                }

                var list = GetTopSelectList(topSelect);
                if (list == null)
                {
                    return Array.Empty<string>();
                }

                var result = new List<string>(list.Count);
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] is ExprAliasedColumn aliasedColumn)
                    {
                        var outputName = ((IExprNamedSelecting)aliasedColumn).OutputName;
                        var varName = ToCamelCaseIdentifier(outputName ?? "c" + (i + 1).ToString(CultureInfo.InvariantCulture), "c");
                        var typedRead = this.TryRenderTypedRead(aliasedColumn.Column, context, aliasedColumn.Alias?.Name);
                        if (typedRead != null)
                        {
                            result.Add("var " + varName + " = " + typedRead + ";");
                        }
                        else
                        {
                            var readName = aliasedColumn.Alias?.Name ?? aliasedColumn.Column.ColumnName.Name;
                            result.Add("var " + varName + " = r.GetValue(r.GetOrdinal(" + ToCSharpStringLiteral(readName) + "));");
                        }
                    }
                    else if (list[i] is ExprColumn column)
                    {
                        var outputName = ((IExprNamedSelecting)column).OutputName;
                        var varName = ToCamelCaseIdentifier(outputName ?? "c" + (i + 1).ToString(CultureInfo.InvariantCulture), "c");
                        var typedRead = this.TryRenderTypedRead(column, context, null);
                        if (typedRead != null)
                        {
                            result.Add("var " + varName + " = " + typedRead + ";");
                        }
                        else
                        {
                            var readName = outputName ?? column.ColumnName.Name;
                            result.Add("var " + varName + " = r.GetValue(r.GetOrdinal(" + ToCSharpStringLiteral(readName) + "));");
                        }
                    }
                    else if (list[i] is ExprAliasedSelecting aliasedSelecting && aliasedSelecting.Value is ExprColumn aliasedColumnValue)
                    {
                        var outputName = ((IExprNamedSelecting)aliasedSelecting).OutputName;
                        var varName = ToCamelCaseIdentifier(outputName ?? "c" + (i + 1).ToString(CultureInfo.InvariantCulture), "c");
                        var typedRead = this.TryRenderTypedRead(aliasedColumnValue, context, aliasedSelecting.Alias.Name);
                        if (typedRead != null)
                        {
                            result.Add("var " + varName + " = " + typedRead + ";");
                        }
                        else
                        {
                            result.Add("var " + varName + " = r.GetValue(r.GetOrdinal(" + ToCSharpStringLiteral(aliasedSelecting.Alias.Name) + "));");
                        }
                    }
                    else if (list[i] is ExprAliasedSelecting anyAliasedSelecting)
                    {
                        var outputName = ((IExprNamedSelecting)anyAliasedSelecting).OutputName;
                        var varName = ToCamelCaseIdentifier(outputName ?? "c" + (i + 1).ToString(CultureInfo.InvariantCulture), "c");
                        result.Add("var " + varName + " = r.GetValue(r.GetOrdinal(" + ToCSharpStringLiteral(anyAliasedSelecting.Alias.Name) + "));");
                    }
                }

                return result;
            }

            private string? TryRenderTypedRead(ExprColumn column, RenderContext context, string? aliasedName)
            {
                if (!this.TryGetColumnType(column, context, out var columnType))
                {
                    return null;
                }

                if (string.Equals(columnType, "ExprColumn", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var colExpr = this.RenderColumn(column, context);
                if (string.IsNullOrWhiteSpace(aliasedName))
                {
                    return colExpr + ".Read(r)";
                }

                return colExpr + ".Read(r, " + ToCSharpStringLiteral(aliasedName) + ")";
            }

            private bool TryGetColumnType(ExprColumn column, RenderContext context, out string columnType)
            {
                columnType = string.Empty;
                if (column.Source is ExprTableAlias tableAlias)
                {
                    var alias = this.GetAliasName(tableAlias.Alias);
                    if (!string.IsNullOrWhiteSpace(alias)
                        && context.BindingsByAlias.TryGetValue(alias!, out var binding)
                        && this.TryGetColumnTypeFromBinding(binding, column.ColumnName.Name, out columnType))
                    {
                        return true;
                    }
                }

                if (context.BindingsByAlias.Count == 1)
                {
                    var single = context.BindingsByAlias.Values.First();
                    if (this.TryGetColumnTypeFromBinding(single, column.ColumnName.Name, out columnType))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool TryGetColumnTypeFromBinding(SourceBinding binding, string columnName, out string columnType)
            {
                columnType = string.Empty;
                if (string.IsNullOrWhiteSpace(binding.ClassName))
                {
                    return false;
                }

                if (!this._columnTypesByClassName.TryGetValue(binding.ClassName!, out var byColumn))
                {
                    return false;
                }

                if (byColumn.TryGetValue(columnName, out var resolvedType))
                {
                    columnType = resolvedType;
                    return true;
                }

                return false;
            }

            private static IReadOnlyList<IExprSelecting>? GetTopSelectList(IExprSubQuery query)
            {
                if (query is ExprQuerySpecification specification)
                {
                    return specification.SelectList;
                }

                if (query is ExprSelectOffsetFetch offsetFetch)
                {
                    return GetTopSelectList(offsetFetch.SelectQuery);
                }

                return null;
            }

            private string RenderStatement(IExpr expr, RenderContext context)
            {
                switch (expr)
                {
                    case ExprDeleteOutput deleteOutput:
                        return this.RenderDelete(deleteOutput.Delete, context);
                    case ExprInsertOutput insertOutput:
                        return this.RenderInsert(insertOutput.Insert, context);
                    case ExprMergeOutput mergeOutput:
                        return this.RenderMerge(mergeOutput, context);
                    case IExprQuery query:
                        return this.RenderQuery(query, context);
                    case ExprUpdate update:
                        return this.RenderUpdate(update, context);
                    case ExprDelete delete:
                        return this.RenderDelete(delete, context);
                    case ExprInsert insert:
                        return this.RenderInsert(insert, context);
                    case ExprIdentityInsert identityInsert:
                        return this.RenderIdentityInsert(identityInsert, context);
                    case ExprMerge merge:
                        return this.RenderMerge(merge, context);
                    default:
                        return "SelectOne().Done()";
                }
            }

            private string RenderQuery(IExprQuery query, RenderContext context)
            {
                if (query is ExprSelect select)
                {
                    var builder = this.RenderSubQueryBuilder(select.SelectQuery, context, useDerivedPropertyAliases: false, derivedPropertyMap: null);
                    builder = this.ApplyOrderBy(builder, select.OrderBy.OrderList, context);
                    return builder + ".Done()";
                }

                if (query is IExprSubQuery subQuery)
                {
                    return this.RenderSubQueryFinal(subQuery, context, useDerivedPropertyAliases: false, derivedPropertyMap: null);
                }

                return "SelectOne().Done()";
            }

            private string RenderUpdate(ExprUpdate expr, RenderContext context)
            {
                var target = this.RenderTableReference(expr.Target, context);
                var builder = "Update(" + target + ")";

                if (expr.Source != null)
                {
                    this.PreBindSourceAliases(expr.Source, context);
                }

                foreach (var set in expr.SetClause)
                {
                    builder += ".Set(" + this.RenderColumn(set.Column, context) + ", " + this.RenderAssigning(set.Value, context) + ")";
                }

                if (expr.Source != null)
                {
                    builder = this.ApplyFromChain(builder, expr.Source, context, updateDeleteFrom: true);
                }

                if (expr.Filter != null)
                {
                    builder += ".Where(" + this.RenderBoolean(expr.Filter, context) + ")";
                }
                else
                {
                    builder += ".All()";
                }

                return builder;
            }

            private string RenderDelete(ExprDelete expr, RenderContext context)
            {
                var target = this.RenderTableReference(expr.Target, context);
                var builder = "Delete(" + target + ")";
                if (expr.Source != null)
                {
                    builder = this.ApplyFromChain(builder, expr.Source, context, updateDeleteFrom: true);
                }

                if (expr.Filter != null)
                {
                    builder += ".Where(" + this.RenderBoolean(expr.Filter, context) + ")";
                }
                else
                {
                    builder += ".All()";
                }

                return builder;
            }

            private string RenderInsert(ExprInsert expr, RenderContext context)
            {
                var targetVariable = this.EnsureTableVariable(expr.Target, context);
                var targetColumns = (expr.TargetColumns ?? Array.Empty<ExprColumnName>())
                    .Select(i => targetVariable + "." + ToPascalCaseIdentifier(i.Name, "Column"))
                    .ToList();

                var builder = "InsertInto(" + targetVariable;
                if (targetColumns.Count > 0)
                {
                    builder += ", " + string.Join(", ", targetColumns);
                }
                builder += ")";

                if (expr.Source is ExprInsertValues values)
                {
                    foreach (var row in values.Items)
                    {
                        builder += ".Values(" + string.Join(", ", row.Items.Select(i => this.RenderAssigning(i, context))) + ")";
                    }
                    builder += ".DoneWithValues()";
                    return builder;
                }

                if (expr.Source is ExprInsertQuery query)
                {
                    builder += ".From(" + this.RenderQuery(query.Query, context) + ")";
                    return builder;
                }

                return builder;
            }

            private string RenderIdentityInsert(ExprIdentityInsert expr, RenderContext context)
            {
                var targetVariable = this.EnsureTableVariable(expr.Insert.Target, context);
                var targetColumns = (expr.Insert.TargetColumns ?? Array.Empty<ExprColumnName>())
                    .Select(i => targetVariable + "." + ToPascalCaseIdentifier(i.Name, "Column"))
                    .ToList();

                var builder = "IdentityInsertInto(" + targetVariable;
                if (targetColumns.Count > 0)
                {
                    builder += ", " + string.Join(", ", targetColumns);
                }
                builder += ")";

                if (expr.Insert.Source is ExprInsertValues values)
                {
                    foreach (var row in values.Items)
                    {
                        builder += ".Values(" + string.Join(", ", row.Items.Select(i => this.RenderAssigning(i, context))) + ")";
                    }
                    builder += ".DoneWithValues()";
                    return builder;
                }

                if (expr.Insert.Source is ExprInsertQuery query)
                {
                    builder += ".From(" + this.RenderQuery(query.Query, context) + ")";
                    return builder;
                }

                return builder;
            }

            private string RenderMerge(ExprMerge expr, RenderContext context)
            {
                var target = this.EnsureTableVariable(expr.TargetTable.FullName, context);
                if (expr.TargetTable.Alias != null)
                {
                    var targetAlias = this.GetAliasName(expr.TargetTable.Alias.Alias);
                    if (!string.IsNullOrWhiteSpace(targetAlias))
                    {
                        context.BindingsByAlias[targetAlias!] = new SourceBinding(
                            targetAlias!,
                            target,
                            SourceKind.Table,
                            this._tableClassByKey.TryGetValue(GetTableKey(expr.TargetTable.FullName.AsExprTableFullName()), out var className)
                                ? className
                                : null,
                            "new " + (this._tableClassByKey.TryGetValue(GetTableKey(expr.TargetTable.FullName.AsExprTableFullName()), out var targetClassName) ? targetClassName : "TableBase") + "(" + ToCSharpStringLiteral(targetAlias!) + ")");
                    }
                }
                var source = this.RenderTableSource(expr.Source, context);

                var builder = "MergeInto(" + target + ", " + source + ").On(" + this.RenderBoolean(expr.On, context) + ")";

                if (expr.WhenMatched is ExprMergeMatchedUpdate matchedUpdate)
                {
                    builder += matchedUpdate.And != null
                        ? ".WhenMatchedAnd(" + this.RenderBoolean(matchedUpdate.And, context) + ").ThenUpdate()"
                        : ".WhenMatched().ThenUpdate()";

                    foreach (var set in matchedUpdate.Set)
                    {
                        builder += ".Set(" + this.RenderColumn(set.Column, context) + ", " + this.RenderAssigning(set.Value, context) + ")";
                    }
                }
                else if (expr.WhenMatched is ExprMergeMatchedDelete matchedDelete)
                {
                    builder += matchedDelete.And != null
                        ? ".WhenMatchedAnd(" + this.RenderBoolean(matchedDelete.And, context) + ").ThenDelete()"
                        : ".WhenMatched().ThenDelete()";
                }

                if (expr.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsert notMatchedInsert)
                {
                    builder += notMatchedInsert.And != null
                        ? ".WhenNotMatchedByTargetAnd(" + this.RenderBoolean(notMatchedInsert.And, context) + ").ThenInsert()"
                        : ".WhenNotMatchedByTarget().ThenInsert()";

                    for (var i = 0; i < notMatchedInsert.Columns.Count && i < notMatchedInsert.Values.Count; i++)
                    {
                        builder += ".Set(CustomColumnFactory.Any(" + ToCSharpStringLiteral(notMatchedInsert.Columns[i].Name) + "), " + this.RenderAssigning(notMatchedInsert.Values[i], context) + ")";
                    }
                }
                else if (expr.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsertDefault notMatchedInsertDefault)
                {
                    builder += notMatchedInsertDefault.And != null
                        ? ".WhenNotMatchedByTargetAnd(" + this.RenderBoolean(notMatchedInsertDefault.And, context) + ").ThenInsertDefaultValues()"
                        : ".WhenNotMatchedByTarget().ThenInsertDefaultValues()";
                }

                if (expr.WhenNotMatchedBySource is ExprMergeMatchedUpdate notMatchedBySourceUpdate)
                {
                    builder += notMatchedBySourceUpdate.And != null
                        ? ".WhenNotMatchedBySourceAnd(" + this.RenderBoolean(notMatchedBySourceUpdate.And, context) + ").ThenUpdate()"
                        : ".WhenNotMatchedBySource().ThenUpdate()";
                    foreach (var set in notMatchedBySourceUpdate.Set)
                    {
                        builder += ".Set(" + this.RenderColumn(set.Column, context) + ", " + this.RenderAssigning(set.Value, context) + ")";
                    }
                }
                else if (expr.WhenNotMatchedBySource is ExprMergeMatchedDelete notMatchedBySourceDelete)
                {
                    builder += notMatchedBySourceDelete.And != null
                        ? ".WhenNotMatchedBySourceAnd(" + this.RenderBoolean(notMatchedBySourceDelete.And, context) + ").ThenDelete()"
                        : ".WhenNotMatchedBySource().ThenDelete()";
                }

                return builder + ".Done()";
            }
            private string RenderSubQueryBuilder(
                IExprSubQuery query,
                RenderContext context,
                bool useDerivedPropertyAliases,
                IReadOnlyDictionary<string, string>? derivedPropertyMap)
            {
                switch (query)
                {
                    case ExprQuerySpecification specification:
                        return this.RenderQuerySpecificationBuilder(specification, context, useDerivedPropertyAliases, derivedPropertyMap);
                    case ExprQueryExpression queryExpression:
                    {
                        var left = this.RenderSubQueryBuilder(queryExpression.Left, context, useDerivedPropertyAliases, derivedPropertyMap);
                        var right = this.RenderSubQueryBuilder(queryExpression.Right, context, useDerivedPropertyAliases, derivedPropertyMap);
                        string op = queryExpression.QueryExpressionType switch
                        {
                            ExprQueryExpressionType.UnionAll => "UnionAll",
                            ExprQueryExpressionType.Union => "Union",
                            ExprQueryExpressionType.Except => "Except",
                            ExprQueryExpressionType.Intersect => "Intersect",
                            _ => "UnionAll"
                        };
                        return left + "." + op + "(" + right + ")";
                    }
                    case ExprSelectOffsetFetch offsetFetch:
                    {
                        var builder = this.RenderSubQueryBuilder(offsetFetch.SelectQuery, context, useDerivedPropertyAliases, derivedPropertyMap);
                        builder = this.ApplyOrderBy(builder, offsetFetch.OrderBy.OrderList, context);
                        if (offsetFetch.OrderBy.OffsetFetch.Fetch is ExprValue fetch)
                        {
                            builder += ".OffsetFetch(" + this.RenderValue(offsetFetch.OrderBy.OffsetFetch.Offset, context) + ", " + this.RenderValue(fetch, context) + ")";
                        }
                        else
                        {
                            builder += ".Offset(" + this.RenderValue(offsetFetch.OrderBy.OffsetFetch.Offset, context) + ")";
                        }
                        return builder;
                    }
                    default:
                        return "SelectOne()";
                }
            }

            private string RenderSubQueryFinal(
                IExprSubQuery query,
                RenderContext context,
                bool useDerivedPropertyAliases,
                IReadOnlyDictionary<string, string>? derivedPropertyMap)
            {
                return this.RenderSubQueryBuilder(query, context, useDerivedPropertyAliases, derivedPropertyMap) + ".Done()";
            }

            private string RenderQuerySpecificationBuilder(
                ExprQuerySpecification query,
                RenderContext context,
                bool useDerivedPropertyAliases,
                IReadOnlyDictionary<string, string>? derivedPropertyMap)
            {
                if (query.From != null)
                {
                    this.PreBindSourceAliases(query.From, context);
                }

                string builder = this.RenderSelectStart(query.SelectList, query.Top, query.Distinct, context, useDerivedPropertyAliases, derivedPropertyMap);

                if (query.From != null)
                {
                    builder = this.ApplyFromChain(builder, query.From, context, updateDeleteFrom: false);
                }

                if (query.Where != null)
                {
                    var whereValue = this.RenderBoolean(query.Where, context);
                    if (query.Where is ExprBooleanAnd || query.Where is ExprBooleanOr)
                    {
                        builder += ".Where(\r\n                    " + whereValue + ")";
                    }
                    else
                    {
                        builder += ".Where(" + whereValue + ")";
                    }
                }

                if (query.GroupBy != null && query.GroupBy.Count > 0)
                {
                    builder += ".GroupBy(" + string.Join(", ", query.GroupBy.Select(i => this.RenderColumn(i, context))) + ")";
                }

                return builder;
            }

            private void PreBindSourceAliases(IExprTableSource source, RenderContext context)
            {
                foreach (var leaf in this.ExtractLeafSources(source))
                {
                    if (!this.TryCreateBindingForSource(context, leaf.Source, out var binding, isOutPreferred: false) || binding == null)
                    {
                        continue;
                    }

                    if (context.BindingsByAlias.ContainsKey(binding.Alias))
                    {
                        continue;
                    }

                    context.BindingsByAlias[binding.Alias] = binding;
                    if (binding.ClassName is string className)
                    {
                        var alreadyOut = context.OutSources.Any(i => string.Equals(i.Alias, binding.Alias, StringComparison.OrdinalIgnoreCase));
                        if (!alreadyOut)
                        {
                            context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, className, binding.Alias, binding.InitializationExpression));
                        }
                    }
                    else
                    {
                        context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, "object", binding.Alias, binding.InitializationExpression));
                    }
                }
            }

            private string ApplyFromChain(string builder, IExprTableSource source, RenderContext context, bool updateDeleteFrom)
            {
                if (source is ExprJoinedTable joined)
                {
                    builder = this.ApplyFromChain(builder, joined.Left, context, updateDeleteFrom);
                    var joinRight = this.RenderTableSource(joined.Right, context);
                    string method = joined.JoinType switch
                    {
                        ExprJoinedTable.ExprJoinType.Inner => "InnerJoin",
                        ExprJoinedTable.ExprJoinType.Left => "LeftJoin",
                        ExprJoinedTable.ExprJoinType.Right => "RightJoin",
                        ExprJoinedTable.ExprJoinType.Full => "FullJoin",
                        _ => "InnerJoin"
                    };
                    builder += "." + method + "(" + joinRight + ", " + this.RenderBoolean(joined.SearchCondition, context) + ")";
                    return builder;
                }

                if (source is ExprCrossedTable crossed)
                {
                    builder = this.ApplyFromChain(builder, crossed.Left, context, updateDeleteFrom);
                    builder += ".CrossJoin(" + this.RenderTableSource(crossed.Right, context) + ")";
                    return builder;
                }

                if (source is ExprLateralCrossedTable lateral)
                {
                    builder = this.ApplyFromChain(builder, lateral.Left, context, updateDeleteFrom);
                    builder += lateral.Outer
                        ? ".OuterApply(" + this.RenderTableSource(lateral.Right, context) + ")"
                        : ".CrossApply(" + this.RenderTableSource(lateral.Right, context) + ")";
                    return builder;
                }

                if (updateDeleteFrom)
                {
                    return builder + ".From(" + this.RenderTableSource(source, context) + ")";
                }

                return builder + ".From(" + this.RenderTableSource(source, context) + ")";
            }

            private string ApplyOrderBy(string builder, IReadOnlyList<ExprOrderByItem> orderItems, RenderContext context)
            {
                if (orderItems.Count < 1)
                {
                    return builder;
                }

                return builder + ".OrderBy(" + string.Join(", ", orderItems.Select(i => this.RenderOrderByItem(i, context))) + ")";
            }

            private string RenderSelectStart(
                IReadOnlyList<IExprSelecting> selectList,
                ExprValue? top,
                bool distinct,
                RenderContext context,
                bool useDerivedPropertyAliases,
                IReadOnlyDictionary<string, string>? derivedPropertyMap)
            {
                var rendered = selectList
                    .Select(i => this.RenderSelecting(i, context, useDerivedPropertyAliases, derivedPropertyMap))
                    .ToList();

                string listText;
                if (rendered.Count > 3)
                {
                    listText = "\r\n                    " + string.Join(",\r\n                    ", rendered);
                    listText += "\r\n                ";
                }
                else
                {
                    listText = string.Join(", ", rendered);
                }

                if (top is null)
                {
                    if (!distinct && rendered.Count == 1 && (rendered[0] == "Literal(1)" || rendered[0] == "1"))
                    {
                        return "SelectOne()";
                    }

                    return distinct
                        ? "SelectDistinct(" + listText + ")"
                        : "Select(" + listText + ")";
                }

                string topValue = this.RenderValue(top, context);
                return distinct
                    ? "SelectTopDistinct(" + topValue + ", " + listText + ")"
                    : "SelectTop(" + topValue + ", " + listText + ")";
            }

            private string RenderSelecting(
                IExprSelecting selecting,
                RenderContext context,
                bool useDerivedPropertyAliases,
                IReadOnlyDictionary<string, string>? derivedPropertyMap)
            {
                switch (selecting)
                {
                    case ExprAliasedColumn aliasedColumn:
                    {
                        var value = this.RenderColumn(aliasedColumn.Column, context);
                        if (aliasedColumn.Alias != null)
                        {
                            if (useDerivedPropertyAliases
                                && derivedPropertyMap != null
                                && derivedPropertyMap.TryGetValue(aliasedColumn.Alias.Name, out var propName)
                                && !string.Equals(aliasedColumn.Alias.Name, aliasedColumn.Column.ColumnName.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                return value + ".As(this." + propName + ")";
                            }

                            return value + ".As(" + ToCSharpStringLiteral(aliasedColumn.Alias.Name) + ")";
                        }

                        return value;
                    }
                    case ExprAliasedSelecting aliasedSelecting:
                    {
                        var rendered = this.RenderSelectingForAlias(aliasedSelecting.Value, context, useDerivedPropertyAliases, derivedPropertyMap);
                        if (useDerivedPropertyAliases
                            && derivedPropertyMap != null
                            && derivedPropertyMap.TryGetValue(aliasedSelecting.Alias.Name, out var propName))
                        {
                            return rendered + ".As(this." + propName + ")";
                        }

                        return rendered + ".As(" + ToCSharpStringLiteral(aliasedSelecting.Alias.Name) + ")";
                    }
                    case ExprAllColumns allColumns:
                        return allColumns.Source == null ? "AllColumns()" : this.RenderColumnSource(allColumns.Source, context) + ".AllColumns()";
                    case ExprAggregateFunction aggregateFunction:
                        return this.RenderAggregateFunction(aggregateFunction, context);
                    case ExprAggregateOverFunction aggregateOverFunction:
                        return this.RenderAggregateOverFunction(aggregateOverFunction, context);
                    case ExprAnalyticFunction analyticFunction:
                        return this.RenderAnalyticFunction(analyticFunction, context);
                    case ExprColumn column:
                        return this.RenderColumn(column, context);
                    case ExprColumnName columnName:
                        return "Column(" + ToCSharpStringLiteral(columnName.Name) + ")";
                    case ExprValue value:
                        return this.RenderValue(value, context);
                    default:
                        return "Literal(1)";
                }
            }

            private string RenderSelectingForAlias(
                IExprSelecting selecting,
                RenderContext context,
                bool useDerivedPropertyAliases,
                IReadOnlyDictionary<string, string>? derivedPropertyMap)
            {
                if (selecting is ExprValue value)
                {
                    if (value is ExprParameter
                        || value is ExprStringLiteral
                        || value is ExprInt16Literal
                        || value is ExprInt32Literal
                        || value is ExprInt64Literal
                        || value is ExprDecimalLiteral
                        || value is ExprDoubleLiteral
                        || value is ExprBoolLiteral
                        || value is ExprDateTimeLiteral
                        || value is ExprDateTimeOffsetLiteral
                        || value is ExprGuidLiteral
                        || value is ExprByteArrayLiteral
                        || value is ExprNull)
                    {
                        return "Literal(" + this.RenderValue(value, context) + ")";
                    }

                    return this.RenderValue(value, context);
                }

                return this.RenderSelecting(selecting, context, useDerivedPropertyAliases, derivedPropertyMap);
            }

            private string RenderTableSource(IExprTableSource source, RenderContext context)
            {
                switch (source)
                {
                    case ExprTable table:
                    {
                        if (table.Alias == null)
                        {
                            return this.EnsureTableVariable(table.FullName, context);
                        }

                        var alias = this.GetAliasName(table.Alias.Alias) ?? "t";
                        var expectedKey = GetTableKey(table.FullName.AsExprTableFullName());
                        var expectedClass = this._tableClassByKey.TryGetValue(expectedKey, out var classByKey)
                            ? classByKey
                            : null;
                        if (!context.BindingsByAlias.TryGetValue(alias, out var binding))
                        {
                            if (this.TryCreateBindingForSource(context, table, out var created, isOutPreferred: false))
                            {
                                if (created is null)
                                {
                                    return this.RenderInlineTable(table.FullName, alias, context);
                                }

                                binding = created;
                                context.BindingsByAlias[alias] = binding;
                                if (binding.ClassName is string className)
                                {
                                    context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, className, binding.Alias, binding.InitializationExpression));
                                }
                            }
                            else
                            {
                                return this.RenderInlineTable(table.FullName, alias, context);
                            }
                        }
                        else if (binding.SourceKind == SourceKind.Table
                                 && expectedClass != null
                                 && !string.Equals(binding.ClassName, expectedClass, StringComparison.Ordinal))
                        {
                            if (this.TryCreateBindingForSource(context, table, out var recreated, isOutPreferred: false))
                            {
                                if (recreated is null)
                                {
                                    return this.RenderInlineTable(table.FullName, alias, context);
                                }

                                binding = recreated;
                                context.BindingsByAlias[alias] = binding;
                                if (binding.ClassName is string recreatedClass)
                                {
                                    context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, recreatedClass, binding.Alias, binding.InitializationExpression));
                                }
                            }
                        }

                        return binding.VariableName;
                    }
                    case ExprDerivedTableQuery derived:
                    {
                        var alias = this.GetAliasName(derived.Alias.Alias) ?? "sq";
                        if (!context.BindingsByAlias.TryGetValue(alias, out var binding))
                        {
                            if (!this.TryCreateBindingForSource(context, derived, out var created, isOutPreferred: false))
                            {
                                throw new SqExpressSqlTranspilerException("Could not resolve derived table source binding.");
                            }
                            if (created is null)
                            {
                                throw new SqExpressSqlTranspilerException("Derived table source binding is null.");
                            }

                            binding = created;
                            context.BindingsByAlias[alias] = binding;
                            if (binding.ClassName is string className)
                            {
                                context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, className, binding.Alias, binding.InitializationExpression));
                            }
                        }
                        return binding.VariableName;
                    }
                    case ExprCteQuery cte:
                    {
                        var alias = cte.Alias != null ? this.GetAliasName(cte.Alias.Alias) ?? cte.Name : cte.Name;
                        if (!context.BindingsByAlias.TryGetValue(alias, out var binding))
                        {
                            if (!this.TryCreateBindingForSource(context, cte, out var created, isOutPreferred: false))
                            {
                                throw new SqExpressSqlTranspilerException("Could not resolve CTE source binding.");
                            }
                            if (created is null)
                            {
                                throw new SqExpressSqlTranspilerException("CTE source binding is null.");
                            }

                            binding = created;
                            context.BindingsByAlias[alias] = binding;
                            if (binding.ClassName is string className)
                            {
                                context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, className, binding.Alias, binding.InitializationExpression));
                            }
                        }
                        return binding.VariableName;
                    }
                    case ExprAliasedTableFunction aliasedFunction:
                    {
                        var alias = this.GetAliasName(aliasedFunction.Alias.Alias) ?? "f";
                        if (!context.BindingsByAlias.TryGetValue(alias, out var binding))
                        {
                            if (this.TryCreateBindingForSource(context, aliasedFunction, out var created, isOutPreferred: false))
                            {
                                if (created is null)
                                {
                                    return this.RenderAliasedTableFunctionInline(aliasedFunction, context);
                                }

                                binding = created;
                                context.BindingsByAlias[alias] = binding;
                                if (binding.ClassName is string className)
                                {
                                    context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, className, binding.Alias, binding.InitializationExpression));
                                }
                            }
                            else
                            {
                                return this.RenderAliasedTableFunctionInline(aliasedFunction, context);
                            }
                        }
                        return binding.VariableName;
                    }
                    case ExprDerivedTableValues values:
                    {
                        var alias = this.GetAliasName(values.Alias.Alias) ?? "v";
                        if (!context.BindingsByAlias.TryGetValue(alias, out var binding))
                        {
                            if (this.TryCreateBindingForSource(context, values, out var created, isOutPreferred: false))
                            {
                                if (created is null)
                                {
                                    return this.RenderDerivedValuesInline(values, context);
                                }

                                binding = created;
                                context.BindingsByAlias[alias] = binding;
                                if (binding.ClassName is string className)
                                {
                                    context.LocalSources.Add(new QueryPreviewBuildSource(binding.VariableName, className, binding.Alias, binding.InitializationExpression));
                                }
                            }
                            else
                            {
                                return this.RenderDerivedValuesInline(values, context);
                            }
                        }
                        return binding.VariableName;
                    }
                    case ExprTableFunction tableFunction:
                        return this.RenderTableFunction(tableFunction, context);
                    default:
                        return "TableAlias(\"t\")";
                }
            }

            private string RenderInlineTable(IExprTableFullName fullName, string? alias, RenderContext context)
            {
                var key = GetTableKey(fullName.AsExprTableFullName());
                if (!this._tableClassByKey.TryGetValue(key, out var className))
                {
                    var fallbackByDefaultSchema = "dbo." + fullName.AsExprTableFullName().TableName.Name;
                    if (!this._tableClassByKey.TryGetValue(fallbackByDefaultSchema, out className))
                    {
                        className = this._tableClassByKey
                            .Where(i => TryParseTableKey(i.Key, out _, out var tableName) && string.Equals(tableName, fullName.AsExprTableFullName().TableName.Name, StringComparison.OrdinalIgnoreCase))
                            .Select(i => i.Value)
                            .FirstOrDefault() ?? "TableBase";
                    }
                }

                if (alias == null)
                {
                    return "new " + className + "()";
                }

                return "new " + className + "(" + ToCSharpStringLiteral(alias) + ")";
            }
            private bool TryCreateBindingForSource(RenderContext context, IExprTableSource source, out SourceBinding? binding, bool isOutPreferred)
            {
                binding = null;
                switch (source)
                {
                    case ExprTable table:
                    {
                        var hasExplicitAlias = table.Alias != null;
                        if (!hasExplicitAlias && isOutPreferred)
                        {
                            return false;
                        }

                        var alias = table.Alias != null
                            ? this.GetAliasName(table.Alias.Alias)
                            : ToCamelCaseIdentifier(table.FullName.AsExprTableFullName().TableName.Name, "t");

                        var key = GetTableKey(table.FullName.AsExprTableFullName());
                        if (!this._tableClassByKey.TryGetValue(key, out var className))
                        {
                            return false;
                        }

                        var variableName = this.GetVariableName(alias!, context);
                        var initExpression = !hasExplicitAlias
                            ? "new " + className + "()"
                            : "new " + className + "(" + ToCSharpStringLiteral(alias!) + ")";
                        binding = new SourceBinding(
                            alias!,
                            variableName,
                            SourceKind.Table,
                            className,
                            initExpression);
                        return true;
                    }
                    case ExprDerivedTableQuery derived:
                    {
                        var alias = this.GetAliasName(derived.Alias.Alias);
                        if (string.IsNullOrWhiteSpace(alias))
                        {
                            return false;
                        }

                        var className = this.EnsureDerivedClass(derived);
                        var variableName = this.GetVariableName(alias!, context);
                        binding = new SourceBinding(
                            alias!,
                            variableName,
                            SourceKind.Derived,
                            className,
                            "new " + className + "(" + ToCSharpStringLiteral(alias!) + ")");
                        return true;
                    }
                    case ExprCteQuery cte:
                    {
                        var alias = cte.Alias != null ? this.GetAliasName(cte.Alias.Alias) ?? cte.Name : cte.Name;
                        var className = this.EnsureCteClass(cte);
                        var variableName = this.GetVariableName(alias, context);
                        binding = new SourceBinding(
                            alias,
                            variableName,
                            SourceKind.Cte,
                            className,
                            "new " + className + "(" + ToCSharpStringLiteral(alias) + ")");
                        return true;
                    }
                    case ExprAliasedTableFunction aliasedFunction:
                    {
                        var alias = this.GetAliasName(aliasedFunction.Alias.Alias);
                        if (string.IsNullOrWhiteSpace(alias))
                        {
                            return false;
                        }
                        var variableName = this.GetVariableName(alias!, context);
                        binding = new SourceBinding(
                            alias!,
                            variableName,
                            SourceKind.Other,
                            "object",
                            this.RenderAliasedTableFunctionInline(aliasedFunction, context));
                        return true;
                    }
                    case ExprDerivedTableValues values:
                    {
                        var alias = this.GetAliasName(values.Alias.Alias);
                        if (string.IsNullOrWhiteSpace(alias))
                        {
                            return false;
                        }
                        var variableName = this.GetVariableName(alias!, context);
                        binding = new SourceBinding(
                            alias!,
                            variableName,
                            SourceKind.Other,
                            "object",
                            this.RenderDerivedValuesInline(values, context));
                        return true;
                    }
                }

                return false;
            }

            private string GetVariableName(string alias, RenderContext context)
            {
                if (this._tableVariableByAlias.TryGetValue(alias, out var suggested))
                {
                    return MakeUniqueVariableName(suggested, context.UsedVariableNames);
                }

                return MakeUniqueVariableName(ToCamelCaseIdentifier(alias, "t"), context.UsedVariableNames);
            }

            private IReadOnlyList<LeafSource> ExtractLeafSources(IExprTableSource source)
            {
                var list = new List<LeafSource>();
                this.ExtractLeafSources(source, list);
                return list;
            }

            private void ExtractLeafSources(IExprTableSource source, List<LeafSource> result)
            {
                switch (source)
                {
                    case ExprJoinedTable joined:
                        this.ExtractLeafSources(joined.Left, result);
                        this.ExtractLeafSources(joined.Right, result);
                        break;
                    case ExprCrossedTable crossed:
                        this.ExtractLeafSources(crossed.Left, result);
                        this.ExtractLeafSources(crossed.Right, result);
                        break;
                    case ExprLateralCrossedTable lateral:
                        this.ExtractLeafSources(lateral.Left, result);
                        this.ExtractLeafSources(lateral.Right, result);
                        break;
                    default:
                        result.Add(new LeafSource(source));
                        break;
                }
            }

            private string RenderTableReference(ExprTable table, RenderContext context)
            {
                if (table.Alias == null)
                {
                    return this.EnsureTableVariable(table.FullName, context);
                }

                return this.RenderTableSource(table, context);
            }

            private string EnsureTableVariable(IExprTableFullName fullName, RenderContext context)
            {
                var key = GetTableKey(fullName.AsExprTableFullName());
                if (!this._tableClassByKey.TryGetValue(key, out var className))
                {
                    var fallbackByDefaultSchema = "dbo." + fullName.AsExprTableFullName().TableName.Name;
                    if (!this._tableClassByKey.TryGetValue(fallbackByDefaultSchema, out className))
                    {
                        className = this._tableClassByKey
                            .Where(i => TryParseTableKey(i.Key, out _, out var tableName) && string.Equals(tableName, fullName.AsExprTableFullName().TableName.Name, StringComparison.OrdinalIgnoreCase))
                            .Select(i => i.Value)
                            .FirstOrDefault() ?? "TableBase";
                    }
                }

                var aliasName = ToCamelCaseIdentifier(fullName.AsExprTableFullName().TableName.Name, "t");
                if (context.BindingsByAlias.TryGetValue(aliasName, out var existing))
                {
                    return existing.VariableName;
                }

                var variable = MakeUniqueVariableName(aliasName, context.UsedVariableNames);
                var binding = new SourceBinding(aliasName, variable, SourceKind.Table, className, "new " + className + "()");
                context.BindingsByAlias[aliasName] = binding;
                context.LocalSources.Add(new QueryPreviewBuildSource(variable, className, aliasName, binding.InitializationExpression));
                return variable;
            }

            private string RenderColumn(ExprColumn column, RenderContext context)
            {
                if (column.Source is ExprTableAlias tableAlias)
                {
                    var alias = this.GetAliasName(tableAlias.Alias);
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        if (!context.BindingsByAlias.TryGetValue(alias!, out var binding))
                        {
                            return "Column(TableAlias(" + ToCSharpStringLiteral(alias!) + "), " + ToCSharpStringLiteral(column.ColumnName.Name) + ")";
                        }

                        if (binding.SourceKind == SourceKind.Other)
                        {
                            return binding.VariableName + ".Column(" + ToCSharpStringLiteral(column.ColumnName.Name) + ")";
                        }

                        return binding.VariableName + "." + ToPascalCaseIdentifier(column.ColumnName.Name, "Column");
                    }
                }

                if (context.BindingsByAlias.Count == 1)
                {
                    var single = context.BindingsByAlias.Values.First();
                    if (single.SourceKind == SourceKind.Table
                        || single.SourceKind == SourceKind.Derived
                        || single.SourceKind == SourceKind.Cte)
                    {
                        return single.VariableName + "." + ToPascalCaseIdentifier(column.ColumnName.Name, "Column");
                    }
                }

                return "Column(" + ToCSharpStringLiteral(column.ColumnName.Name) + ")";
            }

            private string RenderColumnSource(IExprColumnSource source, RenderContext context)
            {
                if (source is ExprTableAlias alias)
                {
                    var aliasName = this.GetAliasName(alias.Alias);
                    if (!string.IsNullOrWhiteSpace(aliasName)
                        && context.BindingsByAlias.TryGetValue(aliasName!, out var binding))
                    {
                        return binding.VariableName;
                    }
                }

                if (source is IExprTableSource tableSource)
                {
                    return this.RenderTableSource(tableSource, context);
                }

                return "TableAlias(\"t\")";
            }

            private string RenderOrderByItem(ExprOrderByItem item, RenderContext context)
            {
                var value = this.RenderValue(item.Value, context);
                return item.Descendant ? "Desc(" + value + ")" : "Asc(" + value + ")";
            }

            private string RenderBoolean(ExprBoolean boolean, RenderContext context)
            {
                switch (boolean)
                {
                    case ExprBooleanAnd and:
                        return "(" + this.RenderBoolean(and.Left, context) + ") & (" + this.RenderBoolean(and.Right, context) + ")";
                    case ExprBooleanOr or:
                        return "(" + this.RenderBoolean(or.Left, context) + ") | (" + this.RenderBoolean(or.Right, context) + ")";
                    case ExprBooleanNot not:
                        return "!(" + this.RenderBoolean(not.Expr, context) + ")";
                    case ExprBooleanEq eq:
                        return this.RenderComparisonValue(eq.Left, eq.Right, context) + " == " + this.RenderComparisonValue(eq.Right, eq.Left, context);
                    case ExprBooleanNotEq notEq:
                        return this.RenderComparisonValue(notEq.Left, notEq.Right, context) + " != " + this.RenderComparisonValue(notEq.Right, notEq.Left, context);
                    case ExprBooleanGt gt:
                        return this.RenderComparisonValue(gt.Left, gt.Right, context) + " > " + this.RenderComparisonValue(gt.Right, gt.Left, context);
                    case ExprBooleanGtEq gtEq:
                        return this.RenderComparisonValue(gtEq.Left, gtEq.Right, context) + " >= " + this.RenderComparisonValue(gtEq.Right, gtEq.Left, context);
                    case ExprBooleanLt lt:
                        return this.RenderComparisonValue(lt.Left, lt.Right, context) + " < " + this.RenderComparisonValue(lt.Right, lt.Left, context);
                    case ExprBooleanLtEq ltEq:
                        return this.RenderComparisonValue(ltEq.Left, ltEq.Right, context) + " <= " + this.RenderComparisonValue(ltEq.Right, ltEq.Left, context);
                    case ExprInSubQuery inSubQuery:
                        return this.RenderValue(inSubQuery.TestExpression, context) + ".In(" + this.RenderSubQueryBuilder(inSubQuery.SubQuery, context, useDerivedPropertyAliases: false, derivedPropertyMap: null) + ")";
                    case ExprInValues inValues:
                    {
                        if (inValues.Items.Count == 1 && inValues.Items[0] is ExprParameter parameter && !string.IsNullOrWhiteSpace(parameter.TagName))
                        {
                            return this.RenderValue(inValues.TestExpression, context) + ".In(" + NormalizeParameterName(parameter.TagName!) + ")";
                        }
                        return this.RenderValue(inValues.TestExpression, context) + ".In(" + string.Join(", ", inValues.Items.Select(i => this.RenderValue(i, context))) + ")";
                    }
                    case ExprExists exists:
                        return "Exists(" + this.RenderSubQueryBuilder(exists.SubQuery, context, useDerivedPropertyAliases: false, derivedPropertyMap: null) + ")";
                    case ExprLike like:
                        return "Like(" + this.RenderValue(like.Test, context) + ", " + this.RenderValue(like.Pattern, context) + ")";
                    case ExprIsNull isNull:
                        return (isNull.Not ? "IsNotNull(" : "IsNull(") + this.RenderValue(isNull.Test, context) + ")";
                    default:
                        return "Literal(1) == Literal(1)";
                }
            }

            private string RenderAssigning(IExprAssigning assigning, RenderContext context)
            {
                if (assigning is ExprDefault)
                {
                    return "Default";
                }

                if (assigning is ExprValue value)
                {
                    return this.RenderValue(value, context);
                }

                return "Default";
            }

            private string RenderComparisonValue(ExprValue value, ExprValue other, RenderContext context)
            {
                if (value is ExprParameter parameter)
                {
                    var name = NormalizeParameterName(parameter.TagName ?? "p");
                    if (other is ExprColumn)
                    {
                        return name;
                    }

                    return "Literal(" + name + ")";
                }

                return this.RenderValue(value, context);
            }

            private string RenderValue(ExprValue value, RenderContext context)
            {
                switch (value)
                {
                    case ExprColumn column:
                        return this.RenderColumn(column, context);
                    case ExprStringLiteral stringLiteral:
                        return ToCSharpStringLiteral(stringLiteral.Value ?? string.Empty);
                    case ExprBoolLiteral boolLiteral:
                        return boolLiteral.Value == true ? "true" : "false";
                    case ExprInt16Literal int16Literal:
                        return (int16Literal.Value ?? 0).ToString(CultureInfo.InvariantCulture);
                    case ExprInt32Literal int32Literal:
                        return (int32Literal.Value ?? 0).ToString(CultureInfo.InvariantCulture);
                    case ExprInt64Literal int64Literal:
                        return (int64Literal.Value ?? 0L).ToString(CultureInfo.InvariantCulture) + "L";
                    case ExprDecimalLiteral decimalLiteral:
                        return (decimalLiteral.Value ?? 0m).ToString(CultureInfo.InvariantCulture) + "m";
                    case ExprDoubleLiteral doubleLiteral:
                        return (doubleLiteral.Value ?? 0d).ToString("R", CultureInfo.InvariantCulture) + "d";
                    case ExprGuidLiteral:
                        return "default(global::System.Guid)";
                    case ExprDateTimeLiteral:
                        return "default(global::System.DateTime)";
                    case ExprDateTimeOffsetLiteral:
                        return "default(global::System.DateTimeOffset)";
                    case ExprByteArrayLiteral:
                        return "global::System.Array.Empty<byte>()";
                    case ExprByteLiteral byteLiteral:
                        return "(byte)" + (byteLiteral.Value ?? (byte)0).ToString(CultureInfo.InvariantCulture);
                    case ExprNull:
                        return "Null";
                    case ExprParameter parameter:
                        return NormalizeParameterName(parameter.TagName ?? "p");
                    case ExprSum sum:
                        return "(" + this.RenderValue(sum.Left, context) + " + " + this.RenderValue(sum.Right, context) + ")";
                    case ExprSub sub:
                        return "(" + this.RenderValue(sub.Left, context) + " - " + this.RenderValue(sub.Right, context) + ")";
                    case ExprMul mul:
                        return "(" + this.RenderValue(mul.Left, context) + " * " + this.RenderValue(mul.Right, context) + ")";
                    case ExprDiv div:
                        return "(" + this.RenderValue(div.Left, context) + " / " + this.RenderValue(div.Right, context) + ")";
                    case ExprModulo modulo:
                        return "(" + this.RenderValue(modulo.Left, context) + " % " + this.RenderValue(modulo.Right, context) + ")";
                    case ExprStringConcat concat:
                        return this.RenderValue(concat.Left, context) + " + " + this.RenderValue(concat.Right, context);
                    case ExprBitwiseAnd bitwiseAnd:
                        return "(" + this.RenderValue(bitwiseAnd.Left, context) + " & " + this.RenderValue(bitwiseAnd.Right, context) + ")";
                    case ExprBitwiseOr bitwiseOr:
                        return "(" + this.RenderValue(bitwiseOr.Left, context) + " | " + this.RenderValue(bitwiseOr.Right, context) + ")";
                    case ExprBitwiseXor bitwiseXor:
                        return "(" + this.RenderValue(bitwiseXor.Left, context) + " ^ " + this.RenderValue(bitwiseXor.Right, context) + ")";
                    case ExprBitwiseNot bitwiseNot:
                        return "~(" + this.RenderValue(bitwiseNot.Value, context) + ")";
                    case ExprValueQuery valueQuery:
                        return "ValueQuery(" + this.RenderSubQueryFinal(valueQuery.Query, context, useDerivedPropertyAliases: false, derivedPropertyMap: null) + ")";
                    case ExprFuncIsNull isNull:
                        return "IsNull(" + this.RenderValue(isNull.Test, context) + ", " + this.RenderValue(isNull.Alt, context) + ")";
                    case ExprFuncCoalesce coalesce:
                        return "Coalesce(" + this.RenderValue(coalesce.Test, context) + ", " + string.Join(", ", coalesce.Alts.Select(i => this.RenderValue(i, context))) + ")";
                    case ExprGetDate:
                        return "GetDate()";
                    case ExprGetUtcDate:
                        return "GetUtcDate()";
                    case ExprDateAdd dateAdd:
                        return "DateAdd(DateAddDatePart." + dateAdd.DatePart + ", " + dateAdd.Number.ToString(CultureInfo.InvariantCulture) + ", " + this.RenderValue(dateAdd.Date, context) + ")";
                    case ExprDateDiff dateDiff:
                        return "DateDiff(DateDiffDatePart." + dateDiff.DatePart + ", " + this.RenderValue(dateDiff.StartDate, context) + ", " + this.RenderValue(dateDiff.EndDate, context) + ")";
                    case ExprCase exprCase:
                        return this.RenderCase(exprCase, context);
                    case ExprCast cast:
                    {
                        string castExpr;
                        if (cast.Expression is ExprParameter parameter)
                        {
                            castExpr = "Literal(" + NormalizeParameterName(parameter.TagName ?? "p") + ")";
                        }
                        else
                        {
                            castExpr = this.RenderSelecting(cast.Expression, context, useDerivedPropertyAliases: false, derivedPropertyMap: null);
                        }

                        return "Cast(" + castExpr + ", " + this.RenderSqlType(cast.SqlType) + ")";
                    }
                    case ExprScalarFunction scalarFunction:
                        return this.RenderScalarFunction(scalarFunction, context);
                    case ExprPortableScalarFunction portable:
                        return this.RenderPortableScalarFunction(portable, context);
                    case ExprUnsafeValue unsafeValue:
                        return "UnsafeValue(" + ToCSharpStringLiteral(unsafeValue.UnsafeValue) + ")";
                    default:
                        return "Literal(1)";
                }
            }

            private string RenderScalarFunction(ExprScalarFunction function, RenderContext context)
            {
                var args = function.Arguments == null || function.Arguments.Count < 1
                    ? string.Empty
                    : ", " + string.Join(", ", function.Arguments.Select(i => this.RenderValue(i, context)));

                if (function.Schema == null)
                {
                    return "ScalarFunctionSys(" + ToCSharpStringLiteral(function.Name.Name) + args + ")";
                }

                return "ScalarFunctionCustom(" + ToCSharpStringLiteral(function.Schema.Schema.Name) + ", " + ToCSharpStringLiteral(function.Name.Name) + args + ")";
            }

            private string RenderPortableScalarFunction(ExprPortableScalarFunction function, RenderContext context)
            {
                var args = function.Arguments == null
                    ? Array.Empty<string>()
                    : function.Arguments.Select(i => this.RenderValue(i, context)).ToArray();

                string functionName = function.PortableFunction switch
                {
                    PortableScalarFunction.Len => "ScalarFunctionSys(\"LEN\"",
                    PortableScalarFunction.DataLen => "ScalarFunctionSys(\"DATALENGTH\"",
                    PortableScalarFunction.Year => "ScalarFunctionSys(\"YEAR\"",
                    PortableScalarFunction.Month => "ScalarFunctionSys(\"MONTH\"",
                    PortableScalarFunction.Day => "ScalarFunctionSys(\"DAY\"",
                    PortableScalarFunction.Hour => "ScalarFunctionSys(\"DATEPART\", \"hour\"",
                    PortableScalarFunction.Minute => "ScalarFunctionSys(\"DATEPART\", \"minute\"",
                    PortableScalarFunction.Second => "ScalarFunctionSys(\"DATEPART\", \"second\"",
                    PortableScalarFunction.IndexOf => "ScalarFunctionSys(\"CHARINDEX\"",
                    PortableScalarFunction.Left => "ScalarFunctionSys(\"LEFT\"",
                    PortableScalarFunction.Right => "ScalarFunctionSys(\"RIGHT\"",
                    PortableScalarFunction.Repeat => "ScalarFunctionSys(\"REPLICATE\"",
                    _ => "ScalarFunctionSys(\"UNKNOWN\""
                };

                if (args.Length < 1)
                {
                    return functionName + ")";
                }

                return functionName + ", " + string.Join(", ", args) + ")";
            }

            private string RenderCase(ExprCase exprCase, RenderContext context)
            {
                var builder = "Case()";
                foreach (var item in exprCase.Cases)
                {
                    builder += ".When(" + this.RenderBoolean(item.Condition, context) + ").Then(" + this.RenderValue(item.Value, context) + ")";
                }

                return builder + ".Else(" + this.RenderValue(exprCase.DefaultValue, context) + ")";
            }

            private string RenderAggregateFunction(ExprAggregateFunction function, RenderContext context)
            {
                var name = function.Name.Name.ToUpperInvariant();
                var value = this.RenderValue(function.Expression, context);

                return name switch
                {
                    "COUNT" when !function.IsDistinct && function.Expression is ExprInt32Literal literal && literal.Value == 1 => "CountOne()",
                    "COUNT" => function.IsDistinct ? "CountDistinct(" + value + ")" : "Count(" + value + ")",
                    "MIN" => function.IsDistinct ? "MinDistinct(" + value + ")" : "Min(" + value + ")",
                    "MAX" => function.IsDistinct ? "MaxDistinct(" + value + ")" : "Max(" + value + ")",
                    "SUM" => function.IsDistinct ? "SumDistinct(" + value + ")" : "Sum(" + value + ")",
                    "AVG" => function.IsDistinct ? "AvgDistinct(" + value + ")" : "Avg(" + value + ")",
                    _ => "AggregateFunction(" + ToCSharpStringLiteral(function.Name.Name) + ", " + (function.IsDistinct ? "true" : "false") + ", " + value + ")"
                };
            }

            private string RenderAggregateOverFunction(ExprAggregateOverFunction function, RenderContext context)
            {
                var result = this.RenderAggregateFunction(function.Function, context);
                result = this.ApplyOver(result, function.Over, context, analyticFrameBuilder: false, supportsNoOrder: true);
                if (function.Over.FrameClause != null)
                {
                    result += ".FrameClause(" + this.RenderFrameBorder(function.Over.FrameClause.Start, context) + ", " + (function.Over.FrameClause.End == null ? "null" : this.RenderFrameBorder(function.Over.FrameClause.End, context)) + ")";
                }
                return result;
            }

            private string RenderAnalyticFunction(ExprAnalyticFunction function, RenderContext context)
            {
                var upper = function.Name.Name.ToUpperInvariant();
                var args = function.Arguments == null
                    ? Array.Empty<string>()
                    : function.Arguments.Select(i => this.RenderValue(i, context)).ToArray();

                string baseBuilder = upper switch
                {
                    "ROW_NUMBER" => "RowNumber()",
                    "RANK" => "Rank()",
                    "DENSE_RANK" => "DenseRank()",
                    "CUME_DIST" => "CumeDist()",
                    "PERCENT_RANK" => "PercentRank()",
                    "COUNT" => args.Length < 1 ? "CountOne()" : "Count(" + args.First() + ")",
                    "MIN" => "Min(" + args.FirstOrDefault() + ")",
                    "MAX" => "Max(" + args.FirstOrDefault() + ")",
                    "SUM" => "Sum(" + args.FirstOrDefault() + ")",
                    "AVG" => "Avg(" + args.FirstOrDefault() + ")",
                    "NTILE" => "Ntile(" + args.FirstOrDefault() + ")",
                    "LAG" => "Lag(" + string.Join(", ", args) + ")",
                    "LEAD" => "Lead(" + string.Join(", ", args) + ")",
                    "FIRST_VALUE" => "FirstValue(" + args.FirstOrDefault() + ")",
                    "LAST_VALUE" => "LastValue(" + args.FirstOrDefault() + ")",
                    _ => args.Length < 1
                        ? "AnalyticFunction(" + ToCSharpStringLiteral(function.Name.Name) + ")"
                        : "AnalyticFunction(" + ToCSharpStringLiteral(function.Name.Name) + ", " + string.Join(", ", args) + ")"
                };

                bool aggregateWindow = upper is "COUNT" or "MIN" or "MAX" or "SUM" or "AVG";
                bool frameBuilder = upper is "FIRST_VALUE" or "LAST_VALUE";
                var result = this.ApplyOver(baseBuilder, function.Over, context, analyticFrameBuilder: frameBuilder, supportsNoOrder: aggregateWindow);
                if (aggregateWindow && function.Over.FrameClause != null)
                {
                    result += ".FrameClause(" + this.RenderFrameBorder(function.Over.FrameClause.Start, context) + ", " + (function.Over.FrameClause.End == null ? "null" : this.RenderFrameBorder(function.Over.FrameClause.End, context)) + ")";
                }
                if (frameBuilder)
                {
                    if (function.Over.FrameClause == null)
                    {
                        result += ".FrameClauseEmpty()";
                    }
                    else
                    {
                        result += ".FrameClause(" + this.RenderFrameBorder(function.Over.FrameClause.Start, context) + ", " + (function.Over.FrameClause.End == null ? "null" : this.RenderFrameBorder(function.Over.FrameClause.End, context)) + ")";
                    }
                }
                return result;
            }

            private string ApplyOver(string baseExpression, ExprOver over, RenderContext context, bool analyticFrameBuilder, bool supportsNoOrder)
            {
                var hasPartitions = over.Partitions != null && over.Partitions.Count > 0;
                var hasOrder = over.OrderBy != null && over.OrderBy.OrderList.Count > 0;

                if (!hasPartitions && !hasOrder)
                {
                    return supportsNoOrder ? baseExpression + ".Over()" : baseExpression;
                }

                if (hasPartitions && hasOrder)
                {
                    string partition = string.Join(", ", over.Partitions!.Select(i => this.RenderValue(i, context)));
                    string order = string.Join(", ", over.OrderBy!.OrderList.Select(i => this.RenderOrderByItem(i, context)));
                    if (analyticFrameBuilder)
                    {
                        return baseExpression + ".OverPartitionBy(" + partition + ").OverOrderBy(" + order + ")";
                    }

                    return baseExpression + ".OverPartitionBy(" + partition + ").OrderBy(" + order + ")";
                }

                if (hasPartitions)
                {
                    string partition = string.Join(", ", over.Partitions!.Select(i => this.RenderValue(i, context)));
                    if (supportsNoOrder)
                    {
                        return baseExpression + ".OverPartitionBy(" + partition + ").NoOrderBy()";
                    }
                    return baseExpression + ".OverPartitionBy(" + partition + ")";
                }

                string orderOnly = string.Join(", ", over.OrderBy!.OrderList.Select(i => this.RenderOrderByItem(i, context)));
                return baseExpression + ".OverOrderBy(" + orderOnly + ")";
            }

            private string RenderFrameBorder(ExprFrameBorder border, RenderContext context)
            {
                switch (border)
                {
                    case ExprCurrentRowFrameBorder:
                        return "FrameBorder.CurrentRow";
                    case ExprUnboundedFrameBorder unbounded:
                        return unbounded.FrameBorderDirection == FrameBorderDirection.Preceding
                            ? "FrameBorder.UnboundedPreceding"
                            : "FrameBorder.UnboundedFollowing";
                    case ExprValueFrameBorder valueBorder:
                    {
                        string value = this.RenderValue(valueBorder.Value, context);
                        return valueBorder.FrameBorderDirection == FrameBorderDirection.Preceding
                            ? "FrameBorder.Preceding(" + value + ")"
                            : "FrameBorder.Following(" + value + ")";
                    }
                    default:
                        return "FrameBorder.CurrentRow";
                }
            }

            private string RenderSqlType(ExprType exprType)
            {
                switch (exprType)
                {
                    case ExprTypeBoolean:
                        return "SqlType.Boolean";
                    case ExprTypeByte:
                        return "SqlType.Byte";
                    case ExprTypeInt16:
                        return "SqlType.Int16";
                    case ExprTypeInt32:
                        return "SqlType.Int32";
                    case ExprTypeInt64:
                        return "SqlType.Int64";
                    case ExprTypeDecimal decimalType:
                    {
                        if (decimalType.PrecisionScale == null)
                        {
                            return "SqlType.Decimal()";
                        }

                        return "SqlType.Decimal(new DecimalPrecisionScale(" + decimalType.PrecisionScale.Value.Precision.ToString(CultureInfo.InvariantCulture) + ", " + (decimalType.PrecisionScale.Value.Scale?.ToString(CultureInfo.InvariantCulture) ?? "null") + "))";
                    }
                    case ExprTypeDouble:
                        return "SqlType.Double";
                    case ExprTypeDateTime dateTime:
                        return "SqlType.DateTime(" + (dateTime.IsDate ? "true" : "false") + ")";
                    case ExprTypeDateTimeOffset:
                        return "SqlType.DateTimeOffset";
                    case ExprTypeGuid:
                        return "SqlType.Guid";
                    case ExprTypeByteArray byteArray:
                        return "SqlType.ByteArray(" + (byteArray.Size?.ToString(CultureInfo.InvariantCulture) ?? "null") + ")";
                    case ExprTypeFixSizeByteArray fixedSizeByteArray:
                        return "SqlType.ByteArrayFixedSize(" + fixedSizeByteArray.Size.ToString(CultureInfo.InvariantCulture) + ")";
                    case ExprTypeString str:
                        return "SqlType.String(" + (str.Size?.ToString(CultureInfo.InvariantCulture) ?? "null") + ", " + (str.IsUnicode ? "true" : "false") + ", " + (str.IsText ? "true" : "false") + ")";
                    case ExprTypeFixSizeString fixedString:
                        return "SqlType.String(" + fixedString.Size.ToString(CultureInfo.InvariantCulture) + ", " + (fixedString.IsUnicode ? "true" : "false") + ", false)";
                    case ExprTypeXml:
                        return "SqlType.String(null, true, true)";
                    default:
                        return "SqlType.String()";
                }
            }

            private string RenderTableFunction(ExprTableFunction function, RenderContext context)
            {
                var args = function.Arguments == null || function.Arguments.Count < 1
                    ? string.Empty
                    : ", " + string.Join(", ", function.Arguments.Select(i => this.RenderValue(i, context)));

                if (function.Schema == null)
                {
                    return "TableFunctionSys(" + ToCSharpStringLiteral(function.Name.Name) + args + ")";
                }

                return "TableFunctionCustom(" + ToCSharpStringLiteral(function.Schema.Schema.Name) + ", " + ToCSharpStringLiteral(function.Name.Name) + args + ")";
            }

            private string RenderAliasedTableFunctionInline(ExprAliasedTableFunction function, RenderContext context)
            {
                var alias = this.GetAliasName(function.Alias.Alias) ?? "f";
                return this.RenderTableFunction(function.Function, context) + ".As(TableAlias(" + ToCSharpStringLiteral(alias) + "))";
            }

            private string RenderDerivedValuesInline(ExprDerivedTableValues values, RenderContext context)
            {
                var rows = values.Values.Items
                    .Select(r => "new global::SqExpress.Syntax.Value.ExprValue[] { " + string.Join(", ", r.Items.Select(i => this.RenderValue(i, context))) + " }")
                    .ToList();
                string alias = this.GetAliasName(values.Alias.Alias) ?? "v";
                var columns = values.Columns.Select(i => ToCSharpStringLiteral(i.Name)).ToList();
                return "Values(new[] { " + string.Join(", ", rows) + " }).As(" + ToCSharpStringLiteral(alias) + (columns.Count > 0 ? ", " + string.Join(", ", columns) : string.Empty) + ")";
            }

            private string EnsureCteClass(ExprCteQuery cte)
            {
                if (this._cteClassByName.TryGetValue(cte.Name, out var existing))
                {
                    return existing;
                }

                var className = this.MakeUniqueNestedTypeName(ToPascalCaseIdentifier(cte.Name, "Cte") + "Cte");
                this._cteClassByName[cte.Name] = className;

                var outputColumns = (cte.Query.GetOutputColumnNames() ?? Array.Empty<string?>())
                    .Select((name, index) => string.IsNullOrWhiteSpace(name) ? "Col" + (index + 1).ToString(CultureInfo.InvariantCulture) : name!)
                    .ToList();
                var usageNames = this.GetCteUsageColumnNames(cte.Name);
                if (usageNames.Count > 0)
                {
                    if (usageNames.Count == outputColumns.Count)
                    {
                        outputColumns = usageNames.ToList();
                    }
                    else if (outputColumns.All(i => i.StartsWith("Col", StringComparison.OrdinalIgnoreCase))
                             && usageNames.Count >= outputColumns.Count)
                    {
                        outputColumns = usageNames.Take(outputColumns.Count).ToList();
                    }
                }

                var properties = string.Join("\r\n", outputColumns.Select(i => "        public ExprColumn " + ToPascalCaseIdentifier(i, "Column") + " { get; }"));
                var ctorAssignments = string.Join("\r\n", outputColumns.Select(i => "            this." + ToPascalCaseIdentifier(i, "Column") + " = this.CreateStringColumn(" + ToCSharpStringLiteral(i) + ");"));

                var createQueryContext = new RenderContext();
                var queryExpr = this.RenderSubQueryFinal(cte.Query, createQueryContext, useDerivedPropertyAliases: false, derivedPropertyMap: null);
                var localDecl = string.Join("\r\n", createQueryContext.LocalSources.Select(i => "            var " + i.VariableName + " = " + i.InitializationExpression + ";"));
                if (localDecl.Length > 0)
                {
                    localDecl += "\r\n";
                }

                var classCode =
                    "public sealed class " + className + " : CteBase\r\n" +
                    "{\r\n" +
                    (properties.Length > 0 ? properties + "\r\n\r\n" : string.Empty) +
                    "    public " + className + "(Alias alias = default) : base(" + ToCSharpStringLiteral(cte.Name) + ", alias)\r\n" +
                    "    {\r\n" +
                    (ctorAssignments.Length > 0 ? ctorAssignments + "\r\n" : string.Empty) +
                    "    }\r\n\r\n" +
                    "    public override IExprSubQuery CreateQuery()\r\n" +
                    "    {\r\n" +
                    localDecl +
                    "        return " + queryExpr + ";\r\n" +
                    "    }\r\n" +
                    "}";

                var member = ParseMemberDeclaration(classCode) ?? throw new SqExpressSqlTranspilerException("Could not parse generated CTE class.");
                this._nestedTypes.Add(member);
                var cteColumnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var outputColumn in outputColumns)
                {
                    cteColumnTypes[outputColumn] = "ExprColumn";
                }
                this._columnTypesByClassName[className] = cteColumnTypes;
                return className;
            }

            private IReadOnlyList<string> GetCteUsageColumnNames(string cteName)
            {
                var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var cte in this._previewExpr.SyntaxTree().DescendantsAndSelf().OfType<ExprCteQuery>())
                {
                    if (!string.Equals(cte.Name, cteName, StringComparison.OrdinalIgnoreCase) || cte.Alias == null)
                    {
                        continue;
                    }

                    aliases.Add(this.GetAliasName(cte.Alias.Alias));
                }

                if (aliases.Count < 1)
                {
                    return Array.Empty<string>();
                }

                var result = new List<string>();
                foreach (var column in this._previewExpr.SyntaxTree().DescendantsAndSelf().OfType<ExprColumn>())
                {
                    if (column.Source is not ExprTableAlias sourceAlias)
                    {
                        continue;
                    }

                    var alias = this.GetAliasName(sourceAlias.Alias);
                    if (!aliases.Contains(alias))
                    {
                        continue;
                    }

                    if (!result.Contains(column.ColumnName.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(column.ColumnName.Name);
                    }
                }

                return result;
            }

            private string EnsureDerivedClass(ExprDerivedTableQuery derived)
            {
                var alias = this.GetAliasName(derived.Alias.Alias) ?? "Sub";
                if (this._derivedClassByAlias.TryGetValue(alias, out var existing))
                {
                    return existing;
                }

                var className = this.MakeUniqueNestedTypeName(ToPascalCaseIdentifier(alias, "Sq") + "SubQuery");
                this._derivedClassByAlias[alias] = className;

                var outputColumns = (derived.Query.GetOutputColumnNames() ?? Array.Empty<string?>()).Select((name, index) => string.IsNullOrWhiteSpace(name) ? "Col" + (index + 1).ToString(CultureInfo.InvariantCulture) : name!).ToList();
                var usageNames = this.GetDerivedUsageColumnNames(alias);
                if (usageNames.Count > 0)
                {
                    if (outputColumns.Count == usageNames.Count)
                    {
                        outputColumns = usageNames.ToList();
                    }
                    else if (outputColumns.All(i => i.StartsWith("Col", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Typical SELECT * case where parser cannot expose concrete output names.
                        outputColumns = usageNames.ToList();
                    }
                }
                var propertyNames = outputColumns.ToDictionary(i => i, i => ToPascalCaseIdentifier(i, "Column"), StringComparer.OrdinalIgnoreCase);

                var createQueryContext = new RenderContext();
                var queryExpr = this.RenderSubQueryFinal(derived.Query, createQueryContext, useDerivedPropertyAliases: true, derivedPropertyMap: propertyNames);
                var selectingList = (derived.Query as ExprQuerySpecification)?.SelectList;
                var propertyTypesByColumn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < outputColumns.Count; index++)
                {
                    var outColumn = outputColumns[index];
                    var resolvedType = this.ResolveDerivedOutputColumnType(selectingList, index, outColumn, createQueryContext);
                    propertyTypesByColumn[outColumn] = resolvedType ?? "ExprColumn";
                }
                var properties = string.Join(
                    "\r\n",
                    outputColumns.Select(i => "        public " + propertyTypesByColumn[i] + " " + propertyNames[i] + " { get; }"));

                var fieldSources = createQueryContext.LocalSources
                    .Where(i =>
                        !string.IsNullOrWhiteSpace(i.ClassName)
                        && !string.Equals(i.ClassName, "object", StringComparison.Ordinal)
                        && this._tableClassByKey.Values.Contains(i.ClassName!, StringComparer.OrdinalIgnoreCase))
                    .GroupBy(i => i.VariableName, StringComparer.OrdinalIgnoreCase)
                    .Select(i => i.First())
                    .ToList();
                var fieldSourceNames = new HashSet<string>(fieldSources.Select(i => i.VariableName), StringComparer.OrdinalIgnoreCase);
                var fields = string.Join(
                    "\r\n",
                    fieldSources.Select(i => "        private readonly " + i.ClassName + " " + i.VariableName + " = " + i.InitializationExpression + ";"));

                var ctorAssignmentsList = new List<string>(outputColumns.Count);
                for (var index = 0; index < outputColumns.Count; index++)
                {
                    var outColumn = outputColumns[index];
                    var propName = propertyNames[outColumn];

                    if (selectingList != null
                        && index < selectingList.Count
                        && this.TryBuildDerivedColumnAssignment(selectingList[index], outColumn, createQueryContext, fieldSourceNames, out var assignmentExpr))
                    {
                        ctorAssignmentsList.Add("            this." + propName + " = " + assignmentExpr + ";");
                    }
                    else
                    {
                        var propType = propertyTypesByColumn[outColumn];
                        ctorAssignmentsList.Add("            this." + propName + " = " + RenderCreateDerivedColumnExpression(propType, outColumn) + ";");
                    }
                }

                var ctorAssignments = string.Join("\r\n", ctorAssignmentsList);
                var locals = string.Join(
                    "\r\n",
                    createQueryContext.LocalSources
                        .Where(i => !fieldSourceNames.Contains(i.VariableName))
                        .Select(i => "            var " + i.VariableName + " = " + i.InitializationExpression + ";"));
                if (locals.Length > 0)
                {
                    locals += "\r\n";
                }

                var classCode =
                    "public sealed class " + className + " : DerivedTableBase\r\n" +
                    "{\r\n" +
                    (fields.Length > 0 ? fields + "\r\n\r\n" : string.Empty) +
                    (properties.Length > 0 ? properties + "\r\n\r\n" : string.Empty) +
                    "    public " + className + "(Alias alias = default) : base(alias)\r\n" +
                    "    {\r\n" +
                    (ctorAssignments.Length > 0 ? ctorAssignments + "\r\n" : string.Empty) +
                    "    }\r\n\r\n" +
                    "    protected override IExprSubQuery CreateQuery()\r\n" +
                    "    {\r\n" +
                    locals +
                    "        return " + queryExpr + ";\r\n" +
                    "    }\r\n" +
                    "}";

                var member = ParseMemberDeclaration(classCode) ?? throw new SqExpressSqlTranspilerException("Could not parse generated derived-table class.");
                this._nestedTypes.Add(member);
                this._columnTypesByClassName[className] = propertyTypesByColumn;
                return className;
            }

            private IReadOnlyList<string> GetDerivedUsageColumnNames(string derivedAlias)
            {
                if (string.IsNullOrWhiteSpace(derivedAlias))
                {
                    return Array.Empty<string>();
                }

                var result = new List<string>();
                foreach (var column in this._previewExpr.SyntaxTree().DescendantsAndSelf().OfType<ExprColumn>())
                {
                    if (column.Source is not ExprTableAlias sourceAlias)
                    {
                        continue;
                    }

                    var alias = this.GetAliasName(sourceAlias.Alias);
                    if (!string.Equals(alias, derivedAlias, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!result.Contains(column.ColumnName.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(column.ColumnName.Name);
                    }
                }

                foreach (var query in this._previewExpr.SyntaxTree().DescendantsAndSelf().OfType<ExprQuerySpecification>())
                {
                    if (query.From == null || !this.IsSingleDerivedAliasSource(query.From, derivedAlias))
                    {
                        continue;
                    }

                    foreach (var selecting in query.SelectList)
                    {
                        if (selecting is ExprColumn directColumn)
                        {
                            if (directColumn.Source == null || this.IsSourceAlias(directColumn.Source, derivedAlias))
                            {
                                if (!result.Contains(directColumn.ColumnName.Name, StringComparer.OrdinalIgnoreCase))
                                {
                                    result.Add(directColumn.ColumnName.Name);
                                }
                            }
                            continue;
                        }

                        if (selecting is ExprAliasedColumn aliasedColumn)
                        {
                            if (aliasedColumn.Column.Source == null || this.IsSourceAlias(aliasedColumn.Column.Source, derivedAlias))
                            {
                                var colName = aliasedColumn.Alias?.Name ?? aliasedColumn.Column.ColumnName.Name;
                                if (!result.Contains(colName, StringComparer.OrdinalIgnoreCase))
                                {
                                    result.Add(colName);
                                }
                            }
                            continue;
                        }

                        if (selecting is ExprAliasedSelecting aliasedSelecting && aliasedSelecting.Value is ExprColumn aliasedValueColumn)
                        {
                            if (aliasedValueColumn.Source == null || this.IsSourceAlias(aliasedValueColumn.Source, derivedAlias))
                            {
                                if (!result.Contains(aliasedSelecting.Alias.Name, StringComparer.OrdinalIgnoreCase))
                                {
                                    result.Add(aliasedSelecting.Alias.Name);
                                }
                            }
                        }
                    }
                }

                return result;
            }

            private bool IsSingleDerivedAliasSource(IExprTableSource source, string alias)
            {
                var leaves = this.ExtractLeafSources(source);
                if (leaves.Count != 1)
                {
                    return false;
                }

                if (leaves[0].Source is not ExprDerivedTableQuery derived)
                {
                    return false;
                }

                var leafAlias = this.GetAliasName(derived.Alias.Alias);
                return string.Equals(leafAlias, alias, StringComparison.OrdinalIgnoreCase);
            }

            private bool IsSourceAlias(IExprColumnSource source, string alias)
            {
                if (source is ExprTableAlias tableAlias)
                {
                    var sourceAlias = this.GetAliasName(tableAlias.Alias);
                    return string.Equals(sourceAlias, alias, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }

            private string? ResolveDerivedOutputColumnType(
                IReadOnlyList<IExprSelecting>? selectingList,
                int index,
                string outputColumn,
                RenderContext context)
            {
                if (selectingList == null || selectingList.Count < 1)
                {
                    return null;
                }

                if (index >= selectingList.Count)
                {
                    if (selectingList.Count == 1 && selectingList[0] is ExprAllColumns allColumnsForExpandedOutput)
                    {
                        if (this.TryResolveAllColumnsDerivedType(allColumnsForExpandedOutput, outputColumn, context, out var expandedType))
                        {
                            return expandedType;
                        }
                    }

                    return null;
                }

                var selecting = selectingList[index];
                ExprColumn? sourceColumn = null;
                if (selecting is ExprColumn directColumn)
                {
                    sourceColumn = directColumn;
                }
                else if (selecting is ExprAliasedColumn aliasedColumn)
                {
                    sourceColumn = aliasedColumn.Column;
                }
                else if (selecting is ExprAliasedSelecting aliasedSelecting && aliasedSelecting.Value is ExprColumn aliasedColumnValue)
                {
                    sourceColumn = aliasedColumnValue;
                }
                else if (selecting is ExprAllColumns allColumns)
                {
                    if (this.TryResolveAllColumnsDerivedType(allColumns, outputColumn, context, out var allColumnsType))
                    {
                        return allColumnsType;
                    }

                    return null;
                }

                if (sourceColumn is null)
                {
                    return null;
                }

                if (this.TryGetColumnType(sourceColumn, context, out var sourceType))
                {
                    return ConvertToCustomColumnType(sourceType);
                }

                return null;
            }

            private bool TryResolveAllColumnsDerivedType(ExprAllColumns allColumns, string outputColumn, RenderContext context, out string type)
            {
                type = string.Empty;

                if (allColumns.Source is ExprTableAlias sourceAlias)
                {
                    var sourceName = this.GetAliasName(sourceAlias.Alias);
                    if (!string.IsNullOrWhiteSpace(sourceName)
                        && context.BindingsByAlias.TryGetValue(sourceName, out var binding)
                        && this.TryGetColumnTypeFromBinding(binding, outputColumn, out type))
                    {
                        type = ConvertToCustomColumnType(type);
                        return true;
                    }
                }

                if (context.BindingsByAlias.Count == 1)
                {
                    var single = context.BindingsByAlias.Values.First();
                    if (this.TryGetColumnTypeFromBinding(single, outputColumn, out type))
                    {
                        type = ConvertToCustomColumnType(type);
                        return true;
                    }
                }

                return false;
            }

            private static string ConvertToCustomColumnType(string sourceType)
            {
                return sourceType switch
                {
                    "BooleanTableColumn" => "BooleanCustomColumn",
                    "NullableBooleanTableColumn" => "NullableBooleanCustomColumn",
                    "ByteTableColumn" => "ByteCustomColumn",
                    "NullableByteTableColumn" => "NullableByteCustomColumn",
                    "ByteArrayTableColumn" => "ByteArrayCustomColumn",
                    "NullableByteArrayTableColumn" => "NullableByteArrayCustomColumn",
                    "Int16TableColumn" => "Int16CustomColumn",
                    "NullableInt16TableColumn" => "NullableInt16CustomColumn",
                    "Int32TableColumn" => "Int32CustomColumn",
                    "NullableInt32TableColumn" => "NullableInt32CustomColumn",
                    "Int64TableColumn" => "Int64CustomColumn",
                    "NullableInt64TableColumn" => "NullableInt64CustomColumn",
                    "DecimalTableColumn" => "DecimalCustomColumn",
                    "NullableDecimalTableColumn" => "NullableDecimalCustomColumn",
                    "DoubleTableColumn" => "DoubleCustomColumn",
                    "NullableDoubleTableColumn" => "NullableDoubleCustomColumn",
                    "DateTimeTableColumn" => "DateTimeCustomColumn",
                    "NullableDateTimeTableColumn" => "NullableDateTimeCustomColumn",
                    "DateTimeOffsetTableColumn" => "DateTimeOffsetCustomColumn",
                    "NullableDateTimeOffsetTableColumn" => "NullableDateTimeOffsetCustomColumn",
                    "GuidTableColumn" => "GuidCustomColumn",
                    "NullableGuidTableColumn" => "NullableGuidCustomColumn",
                    "StringTableColumn" => "StringCustomColumn",
                    "NullableStringTableColumn" => "NullableStringCustomColumn",
                    _ => sourceType
                };
            }

            private static string RenderCreateDerivedColumnExpression(string propertyType, string columnName)
            {
                var name = ToCSharpStringLiteral(columnName);
                return propertyType switch
                {
                    "BooleanCustomColumn" => "this.CreateBooleanColumn(" + name + ")",
                    "NullableBooleanCustomColumn" => "this.CreateNullableBooleanColumn(" + name + ")",
                    "ByteCustomColumn" => "this.CreateByteColumn(" + name + ")",
                    "NullableByteCustomColumn" => "this.CreateNullableByteColumn(" + name + ")",
                    "ByteArrayCustomColumn" => "this.CreateByteArrayColumn(" + name + ")",
                    "NullableByteArrayCustomColumn" => "this.CreateNullableByteArrayColumn(" + name + ")",
                    "Int16CustomColumn" => "this.CreateInt16Column(" + name + ")",
                    "NullableInt16CustomColumn" => "this.CreateNullableInt16Column(" + name + ")",
                    "Int32CustomColumn" => "this.CreateInt32Column(" + name + ")",
                    "NullableInt32CustomColumn" => "this.CreateNullableInt32Column(" + name + ")",
                    "Int64CustomColumn" => "this.CreateInt64Column(" + name + ")",
                    "NullableInt64CustomColumn" => "this.CreateNullableInt64Column(" + name + ")",
                    "DecimalCustomColumn" => "this.CreateDecimalColumn(" + name + ")",
                    "NullableDecimalCustomColumn" => "this.CreateNullableDecimalColumn(" + name + ")",
                    "DoubleCustomColumn" => "this.CreateDoubleColumn(" + name + ")",
                    "NullableDoubleCustomColumn" => "this.CreateNullableDoubleColumn(" + name + ")",
                    "DateTimeCustomColumn" => "this.CreateDateTimeColumn(" + name + ")",
                    "NullableDateTimeCustomColumn" => "this.CreateNullableDateTimeColumn(" + name + ")",
                    "DateTimeOffsetCustomColumn" => "this.CreateDateTimeOffsetColumn(" + name + ")",
                    "NullableDateTimeOffsetCustomColumn" => "this.CreateNullableDateTimeOffsetColumn(" + name + ")",
                    "GuidCustomColumn" => "this.CreateGuidColumn(" + name + ")",
                    "NullableGuidCustomColumn" => "this.CreateNullableGuidColumn(" + name + ")",
                    "NullableStringCustomColumn" => "this.CreateNullableStringColumn(" + name + ")",
                    _ => "this.CreateStringColumn(" + name + ")"
                };
            }

            private bool TryBuildDerivedColumnAssignment(
                IExprSelecting selecting,
                string outputColumnName,
                RenderContext context,
                IReadOnlyCollection<string> fieldSourceNames,
                out string assignmentExpression)
            {
                assignmentExpression = string.Empty;
                ExprColumn? sourceColumn = null;

                if (selecting is ExprColumn directColumn)
                {
                    sourceColumn = directColumn;
                }
                else if (selecting is ExprAliasedColumn aliasedColumn)
                {
                    sourceColumn = aliasedColumn.Column;
                }

                if (sourceColumn is null)
                {
                    return false;
                }

                var sourceExpr = this.RenderColumn(sourceColumn, context);
                var sourcePrefix = string.Empty;
                var dotIndex = sourceExpr.IndexOf('.');
                if (dotIndex > 0)
                {
                    sourcePrefix = sourceExpr.Substring(0, dotIndex);
                }

                if (!string.IsNullOrEmpty(sourcePrefix) && !fieldSourceNames.Contains(sourcePrefix))
                {
                    return false;
                }

                if (sourceExpr.IndexOf('.') > 0 && !sourceExpr.StartsWith("this.", StringComparison.Ordinal))
                {
                    var dot = sourceExpr.IndexOf('.');
                    var prefix = sourceExpr.Substring(0, dot);
                    if (IsSimpleIdentifier(prefix) && fieldSourceNames.Contains(prefix))
                    {
                        sourceExpr = "this." + sourceExpr;
                    }
                }

                if (string.Equals(sourceColumn.ColumnName.Name, outputColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    assignmentExpression = sourceExpr + ".AddToDerivedTable(this)";
                }
                else
                {
                    assignmentExpression = sourceExpr + ".WithColumnName(" + ToCSharpStringLiteral(outputColumnName) + ").AddToDerivedTable(this)";
                }

                return true;
            }

            private static bool IsSimpleIdentifier(string value)
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

            private string MakeUniqueNestedTypeName(string candidate)
            {
                var current = candidate;
                var index = 1;
                while (!this._usedNestedTypeNames.Add(current))
                {
                    current = candidate + index.ToString(CultureInfo.InvariantCulture);
                    index++;
                }

                return current;
            }

            private string GetAliasName(IExprAlias alias)
            {
                if (alias is ExprAlias exprAlias)
                {
                    return exprAlias.Name;
                }

                if (alias is ExprAliasGuid guidAlias)
                {
                    return "A" + Math.Abs(guidAlias.Id.GetHashCode()).ToString(CultureInfo.InvariantCulture);
                }

                return "A0";
            }

            private static string NormalizeParameterName(string name)
            {
                var trimmed = name.Trim();
                if (trimmed.StartsWith("@", StringComparison.Ordinal))
                {
                    trimmed = trimmed.Substring(1);
                }
                return ToCamelCaseIdentifier(trimmed, "p");
            }
        }
    }
}
