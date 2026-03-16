using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.DbMetadata;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlTranspiler
{
    public sealed partial class SqExpressSqlTranspiler
    {
        public SqExpressSqlInlineTranspileResult TranspileInline(
            string sql,
            IReadOnlyList<SqExpressSqlInlineTableBinding> tableBindings,
            SqExpressSqlTranspilerOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new SqExpressSqlTranspilerException("SQL text cannot be empty.");
            }

            var effectiveOptions = options ?? new SqExpressSqlTranspilerOptions();
            var sourceSql = sql.Trim();

            if (ContainsKeyword(sourceSql, "HAVING"))
            {
                throw new SqExpressSqlTranspilerException("HAVING is not supported.");
            }

            if (IsSelectInto(sourceSql))
            {
                throw new SqExpressSqlTranspilerException("SELECT INTO is not supported yet.");
            }

            if (ContainsRangeWindowFrame(sourceSql))
            {
                throw new SqExpressSqlTranspilerException("RANGE window frame is not supported.");
            }

            if (ContainsDatabaseQualifiedScalarFunction(sourceSql))
            {
                throw new SqExpressSqlTranspilerException("Database-qualified scalar function calls are not supported yet.");
            }

            if (ContainsEmptyGroupBy(sourceSql))
            {
                throw new SqExpressSqlTranspilerException("GROUP BY clause must contain at least one expression.");
            }

            sourceSql = NormalizeBetween(sourceSql);
            sourceSql = NormalizeMergeAliasInSource(sourceSql);

            var parserOptions = new SqTSqlParserOptions
            {
                DefaultSchema = effectiveOptions.EffectiveDefaultSchemaName
            };

            if (!SqTSqlParser.TryParse(sourceSql, parserOptions, out IExpr? expr, out IReadOnlyList<SqTable>? tables, out string? parseError))
            {
                if (LooksLikeUnsupportedStatement(sourceSql))
                {
                    throw new SqExpressSqlTranspilerException("Only SELECT, INSERT, UPDATE, DELETE and MERGE statements are supported");
                }

                throw new SqExpressSqlTranspilerException("Could not parse SQL. " + (parseError ?? "Unknown parser error."));
            }

            IReadOnlyList<RawTableRef>? rawRefs = null;
            if (effectiveOptions.EffectiveDefaultSchemaName == null)
            {
                rawRefs = ReadRawTableRefs(sourceSql);
                EnsureNoAmbiguousUnqualifiedTables(rawRefs);
                expr = RemoveDefaultSchemaForUnqualifiedTables(expr!, rawRefs);
            }
            else
            {
                expr = ApplyDefaultSchema(expr!, effectiveOptions.EffectiveDefaultSchemaName);
            }

            expr = RebindParsedTables(expr!, tables!);

            var statementKind = DetectStatementKind(expr!);
            if (statementKind == "UNKNOWN")
            {
                throw new SqExpressSqlTranspilerException("Only SELECT, INSERT, UPDATE, DELETE and MERGE statements are supported");
            }

            var previewExpr = EnsureCurrentRowFrameWhenPresentInSql(expr!, sourceSql);
            var parameterDefaults = InferParameterDefaults(previewExpr, sourceSql);
            var listParameters = GetListParameterNames(previewExpr);
            var analysis = AnalyzeExpression(previewExpr);

            var buildUsages = MatchInlineTableBindings(analysis.TableUsages, tableBindings);
            var classNamesByTableKey = buildUsages
                .GroupBy(i => i.TableKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i.Key, i => i.First().ClassName, StringComparer.OrdinalIgnoreCase);

            var emitter = new QueryPreviewEmitter(
                previewExpr,
                statementKind,
                effectiveOptions,
                classNamesByTableKey,
                new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
                parameterDefaults,
                listParameters,
                reuseProvidedTopLevelSources: true);
            var model = emitter.BuildModel(buildUsages);

            var parameters = model.ParameterDeclarations
                .Select(declaration =>
                {
                    var variableName = declaration.Substring(0, declaration.IndexOf('=')).Trim();
                    var firstSpace = variableName.LastIndexOf(' ');
                    variableName = firstSpace >= 0 ? variableName.Substring(firstSpace + 1) : variableName;
                    var parameterName = variableName;
                    if (parameterName.StartsWith("@", StringComparison.Ordinal))
                    {
                        parameterName = parameterName.Substring(1);
                    }

                    var matchedParameter = parameterDefaults.Keys.FirstOrDefault(
                        i => string.Equals(NormalizeParameterNameForInline(i), variableName, StringComparison.Ordinal));
                    return new SqExpressSqlInlineParameter(
                        matchedParameter ?? variableName,
                        variableName,
                        declaration,
                        listParameters.Contains(matchedParameter ?? variableName));
                })
                .ToList();

            var localDeclarations = new List<string>(model.OutSources.Count + model.LocalSources.Count + 1);
            foreach (var usage in model.OutSources)
            {
                localDeclarations.Add("var " + usage.VariableName + " = " + usage.InitializationExpression + ";");
            }
            foreach (var local in model.LocalSources)
            {
                localDeclarations.Add("var " + local.VariableName + " = " + local.InitializationExpression + ";");
            }
            localDeclarations.Add("var " + effectiveOptions.QueryVariableName + " = " + model.QueryExpressionCode + ";");

            var nestedTypeDeclarations = model.NestedTypes
                .Select(i => i.ToFullString())
                .ToList();

            return new SqExpressSqlInlineTranspileResult(
                statementKind,
                effectiveOptions.QueryVariableName,
                parameters,
                localDeclarations,
                nestedTypeDeclarations);
        }

        private static IReadOnlyList<TableUsage> MatchInlineTableBindings(
            IReadOnlyList<TableUsage> expectedUsages,
            IReadOnlyList<SqExpressSqlInlineTableBinding> providedBindings)
        {
            var bindingsByAlias = providedBindings.ToDictionary(i => i.Alias, StringComparer.OrdinalIgnoreCase);
            var result = new List<TableUsage>(expectedUsages.Count);

            foreach (var usage in expectedUsages)
            {
                if (!bindingsByAlias.TryGetValue(usage.Alias, out var provided))
                {
                    throw new SqExpressSqlTranspilerException("Could not resolve existing table binding for alias '" + usage.Alias + "'.");
                }

                result.Add(new TableUsage(provided.TableKey, provided.Alias, provided.TypeName, provided.VariableName));
            }

            return result;
        }

        private static string NormalizeParameterNameForInline(string name)
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
