using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.DbMetadata;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using RoslynStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax;

namespace SqExpress.SqlTranspiler
{
    public sealed partial class SqExpressSqlTranspiler : ISqExpressSqlTranspiler
    {
        private static readonly Regex RxCastParamType = new Regex(@"CAST\s*\(\s*(?<p>@[A-Za-z_][A-Za-z0-9_]*)\s+AS\s+(?<t>[A-Za-z0-9_]+)", RegexOptions.IgnoreCase);
        private static readonly Regex RxColumnCompareParam = new Regex(@"(?<c>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))?)\s*(=|<>|<=|>=|<|>)\s*(?<p>@[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
        private static readonly Regex RxParamCompareColumn = new Regex(@"(?<p>@[A-Za-z_][A-Za-z0-9_]*)\s*(=|<>|<=|>=|<|>)\s*(?<c>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))?)", RegexOptions.IgnoreCase);
        private static readonly Regex RxParamCompareNumber = new Regex(@"(?<p>@[A-Za-z_][A-Za-z0-9_]*)\s*(=|<>|<=|>=|<|>)\s*(?<n>\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        private static readonly Regex RxInParam = new Regex(@"(?<c>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))?)\s+IN\s*\(\s*(?<p>@[A-Za-z_][A-Za-z0-9_]*)\s*\)", RegexOptions.IgnoreCase);
        private static readonly Regex RxBetweenSimple = new Regex(@"(?<l>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))\s+BETWEEN\s+(?<a>[^\s,\)]+)\s+AND\s+(?<b>[^\s,\)]+)", RegexOptions.IgnoreCase);
        private static readonly Regex RxDbQualifiedFn = new Regex(@"(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.IgnoreCase);

        private sealed class DeclarationsBuildResult
        {
            public DeclarationsBuildResult(
                string code,
                IReadOnlyDictionary<string, string> classNamesByTableKey,
                IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> columnTypesByClassName)
            {
                this.Code = code;
                this.ClassNamesByTableKey = classNamesByTableKey;
                this.ColumnTypesByClassName = columnTypesByClassName;
            }

            public string Code { get; }

            public IReadOnlyDictionary<string, string> ClassNamesByTableKey { get; }

            public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ColumnTypesByClassName { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string columnName, string propertyName, string typeName, string initExpression)
            {
                this.ColumnName = columnName;
                this.PropertyName = propertyName;
                this.TypeName = typeName;
                this.InitExpression = initExpression;
            }

            public string ColumnName { get; }

            public string PropertyName { get; }

            public string TypeName { get; }

            public string InitExpression { get; }
        }

        public SqExpressTranspileResult Transpile(string sql, SqExpressSqlTranspilerOptions? options = null)
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

            var statementKind = DetectStatementKind(expr!);
            if (statementKind == "UNKNOWN")
            {
                throw new SqExpressSqlTranspilerException("Only SELECT, INSERT, UPDATE, DELETE and MERGE statements are supported");
            }

            var previewExpr = expr!;
            var parameterDefaults = InferParameterDefaults(previewExpr, sourceSql);
            var listParameters = GetListParameterNames(previewExpr);
            if (parameterDefaults.Count > 0)
            {
                expr = previewExpr.WithParams(parameterDefaults);
            }
            else
            {
                expr = previewExpr;
            }

            previewExpr = EnsureCurrentRowFrameWhenPresentInSql(previewExpr, sourceSql);
            expr = EnsureCurrentRowFrameWhenPresentInSql(expr!, sourceSql);
            var canonicalSql = expr!.ToSql(TSqlExporter.Default);
            if (effectiveOptions.EffectiveDefaultSchemaName == null && rawRefs != null)
            {
                canonicalSql = RemoveDefaultSchemaFromSqlText(canonicalSql, rawRefs);
            }
            if (string.Equals(statementKind, "MERGE", StringComparison.Ordinal))
            {
                canonicalSql = NormalizeMergeTargetAlias(canonicalSql);
            }

            if (!SqTSqlParser.TryParse(canonicalSql, parserOptions, out IExpr? _, out tables, out string? canonicalError))
            {
                throw new SqExpressSqlTranspilerException("Could not parse SQL. " + (canonicalError ?? "Unknown parser error."));
            }

            var analysis = AnalyzeExpression(previewExpr);
            var declarations = BuildDeclarationsCode(
                effectiveOptions,
                tables!,
                analysis.TableUsages,
                analysis.DiscoveredColumnsByTableKey,
                analysis.NullableColumnsByTableKey,
                analysis.InferredColumnKindsByTableKey);
            var query = BuildQueryCode(
                canonicalSql,
                statementKind,
                previewExpr,
                effectiveOptions,
                analysis.TableUsages,
                declarations.ClassNamesByTableKey,
                declarations.ColumnTypesByClassName,
                parameterDefaults,
                listParameters);

            return new SqExpressTranspileResult(statementKind, query, declarations.Code);
        }

        public SqExpressTranspileResult TranspileSelect(string sql, SqExpressSqlTranspilerOptions? options = null)
        {
            var token = FirstToken(sql);
            if (!token.Equals("SELECT", StringComparison.OrdinalIgnoreCase)
                && !token.Equals("WITH", StringComparison.OrdinalIgnoreCase))
            {
                throw new SqExpressSqlTranspilerException("Expected SELECT statement");
            }

            var result = this.Transpile(sql, options);
            if (!string.Equals(result.StatementKind, "SELECT", StringComparison.Ordinal))
            {
                throw new SqExpressSqlTranspilerException("Expected SELECT statement");
            }

            return result;
        }

        private static string BuildQueryCode(
            string canonicalSql,
            string statementKind,
            IExpr previewExpr,
            SqExpressSqlTranspilerOptions options,
            IReadOnlyList<TableUsage> tableUsages,
            IReadOnlyDictionary<string, string> classNamesByTableKey,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> columnTypesByClassName,
            IReadOnlyDictionary<string, ExprValue> parameterDefaults,
            IReadOnlyCollection<string> listParameters)
        {
            var returnType = statementKind == "SELECT" ? "IExprQuery" : "IExprExec";
            var buildUsages = SelectBuildTableUsages(tableUsages, classNamesByTableKey);
            var emitter = new QueryPreviewEmitter(
                previewExpr,
                statementKind,
                options,
                classNamesByTableKey,
                columnTypesByClassName,
                parameterDefaults,
                listParameters);
            var model = emitter.BuildModel(buildUsages);

            var classMembers = new List<MemberDeclarationSyntax>();
            classMembers.AddRange(model.NestedTypes);
            classMembers.Add(BuildBuildMethod(returnType, options, model));

            if (statementKind == "SELECT")
            {
                classMembers.Add(BuildQueryMethod(options, model.OutSources, model.ReadStatements));
            }

            var classDeclaration = ClassDeclaration(options.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .WithMembers(List(classMembers));

            var namespaceDeclaration = NamespaceDeclaration(ParseName(options.NamespaceName))
                .AddUsings(UsingDirective(ParseName(options.EffectiveDeclarationsNamespaceName)))
                .AddMembers(classDeclaration);

            if (options.UseStaticSqQueryBuilderUsing)
            {
                namespaceDeclaration = namespaceDeclaration.AddUsings(
                    UsingDirective(ParseName("SqExpress.SqQueryBuilder")).WithStaticKeyword(Token(SyntaxKind.StaticKeyword)));
            }

            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("System.Threading.Tasks")),
                    UsingDirective(ParseName("SqExpress")),
                    UsingDirective(ParseName("SqExpress.DataAccess")),
                    UsingDirective(ParseName("SqExpress.Syntax")),
                    UsingDirective(ParseName("SqExpress.Syntax.Expressions")),
                    UsingDirective(ParseName("SqExpress.Syntax.Functions.Known")),
                    UsingDirective(ParseName("SqExpress.Syntax.Names")),
                    UsingDirective(ParseName("SqExpress.Syntax.Select")),
                    UsingDirective(ParseName("SqExpress.Syntax.Type")))
                .AddMembers(namespaceDeclaration);

            var csharpCode = compilationUnit.NormalizeWhitespace().ToFullString();
            return ApplyFluentMethodLineBreaks(csharpCode);
        }

        private static DeclarationsBuildResult BuildDeclarationsCode(
            SqExpressSqlTranspilerOptions options,
            IReadOnlyList<SqTable> tables,
            IReadOnlyList<TableUsage> tableUsages,
            IReadOnlyDictionary<string, HashSet<string>> discoveredColumnsByTableKey,
            IReadOnlyDictionary<string, HashSet<string>> nullableColumnsByTableKey,
            IReadOnlyDictionary<string, Dictionary<string, ParamKind>> inferredColumnKindsByTableKey)
        {
            var byKey = new Dictionary<string, SqTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables)
            {
                var key = GetTableKey(table.FullName.AsExprTableFullName());
                if (!byKey.ContainsKey(key))
                {
                    byKey[key] = table;
                }
            }

            foreach (var usage in tableUsages)
            {
                if (byKey.ContainsKey(usage.TableKey))
                {
                    continue;
                }

                if (TryParseTableKey(usage.TableKey, out var usageSchema, out var usageTableName))
                {
                    var sameName = byKey.Values
                        .Where(i => string.Equals(i.FullName.AsExprTableFullName().TableName.Name, usageTableName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (sameName.Count == 1)
                    {
                        var table = sameName[0];
                        var currentSchema = table.FullName.AsExprTableFullName().DbSchema?.Schema.Name;
                        if (!string.Equals(currentSchema, usageSchema, StringComparison.OrdinalIgnoreCase))
                        {
                            byKey[usage.TableKey] = table.With(fullName: table.FullName.WithSchemaName(usageSchema));
                        }
                        else
                        {
                            byKey[usage.TableKey] = table;
                        }

                        continue;
                    }

                    byKey[usage.TableKey] = SqTable.Create(
                        usageSchema,
                        usageTableName,
                        _ => Array.Empty<TableColumn>());
                }
            }

            if (options.EffectiveDefaultSchemaName == null)
            {
                var removable = new List<string>();
                foreach (var key in byKey.Keys)
                {
                    if (!TryParseTableKey(key, out var schema, out var tableName) || string.IsNullOrWhiteSpace(schema))
                    {
                        continue;
                    }

                    var noSchemaKey = "." + tableName;
                    if (byKey.ContainsKey(noSchemaKey))
                    {
                        removable.Add(key);
                    }
                }

                foreach (var key in removable)
                {
                    byKey.Remove(key);
                }
            }

            var classNamesByTableKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var columnTypesByClassName = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var classNameCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var declarations = new List<MemberDeclarationSyntax>();

            foreach (var pair in byKey.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase))
            {
                var full = pair.Value.FullName.AsExprTableFullName();
                var schema = full.DbSchema?.Schema.Name;
                var tableName = full.TableName.Name;
                if (TryParseTableKey(pair.Key, out var keySchema, out _))
                {
                    schema = keySchema;
                }

                var baseName = options.TableDescriptorClassPrefix + ToPascalCaseIdentifier(tableName, "Table") + options.TableDescriptorClassSuffix;
                var className = MakeUniqueClassName(baseName, classNameCount);
                classNamesByTableKey[pair.Key] = className;

                var columnsByName = new Dictionary<string, ColumnSpec>(StringComparer.OrdinalIgnoreCase);
                var hasDiscoveredColumns = discoveredColumnsByTableKey.TryGetValue(pair.Key, out var discoveredColumns)
                                           && discoveredColumns != null
                                           && discoveredColumns.Count > 0;

                foreach (var column in pair.Value.Columns)
                {
                    if (hasDiscoveredColumns && !discoveredColumns!.Contains(column.ColumnName.Name))
                    {
                        continue;
                    }

                    var spec = CreateColumnSpec(column);
                    columnsByName[column.ColumnName.Name] = spec;
                }

                if (hasDiscoveredColumns)
                {
                    foreach (var columnName in discoveredColumns!.OrderBy(i => i, StringComparer.OrdinalIgnoreCase))
                    {
                        if (columnsByName.TryGetValue(columnName, out var existing))
                        {
                            columnsByName[columnName] = MergeColumnSpec(existing, InferColumnSpec(columnName));
                        }
                        else
                        {
                            columnsByName[columnName] = InferColumnSpec(columnName);
                        }
                    }
                }

                if (inferredColumnKindsByTableKey.TryGetValue(pair.Key, out var inferredKinds))
                {
                    foreach (var inferred in inferredKinds)
                    {
                        var inferredSpec = InferColumnSpec(inferred.Key, inferred.Value);
                        columnsByName[inferred.Key] = inferredSpec;
                    }
                }

                var usedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var properties = new List<ColumnSpec>(columnsByName.Count);
                foreach (var entry in columnsByName.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var propName = MakeUniquePropertyName(entry.Value.PropertyName, usedPropertyNames);
                    var adjusted = entry.Value;
                    if (nullableColumnsByTableKey.TryGetValue(pair.Key, out var nullableColumns)
                        && nullableColumns.Contains(entry.Key))
                    {
                        adjusted = MakeNullableColumnSpec(adjusted);
                    }

                    properties.Add(new ColumnSpec(adjusted.ColumnName, propName, adjusted.TypeName, adjusted.InitExpression));
                }

                var classColumnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var column in properties)
                {
                    classColumnTypes[column.ColumnName] = column.TypeName;
                }
                columnTypesByClassName[className] = classColumnTypes;

                declarations.Add(BuildTableDescriptorClass(className, schema, tableName, properties));
            }

            var namespaceDeclaration = NamespaceDeclaration(ParseName(options.EffectiveDeclarationsNamespaceName))
                .WithMembers(List(declarations));

            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("SqExpress")),
                    UsingDirective(ParseName("SqExpress.Syntax.Type")))
                .AddMembers(namespaceDeclaration);

            return new DeclarationsBuildResult(
                compilationUnit.NormalizeWhitespace().ToFullString(),
                classNamesByTableKey,
                columnTypesByClassName);
        }

        private static MethodDeclarationSyntax BuildBuildMethod(
            string returnType,
            SqExpressSqlTranspilerOptions options,
            QueryPreviewBuildModel model)
        {
            var parameters = new List<ParameterSyntax>(model.OutSources.Count);
            var statements = new List<RoslynStatementSyntax>(
                model.OutSources.Count + model.LocalSources.Count + model.ParameterDeclarations.Count + 3);

            foreach (var usage in model.OutSources)
            {
                parameters.Add(
                    Parameter(Identifier(usage.VariableName))
                        .WithType(ParseTypeName(usage.ClassName))
                        .AddModifiers(Token(SyntaxKind.OutKeyword)));

                statements.Add(ParseStatement(
                    usage.VariableName + " = new " + usage.ClassName + "(" + ToCSharpStringLiteral(usage.Alias) + ");"));
            }

            foreach (var usage in model.LocalSources)
            {
                statements.Add(ParseStatement(
                    "var " + usage.VariableName + " = " + usage.InitializationExpression + ";"));
            }

            foreach (var declaration in model.ParameterDeclarations)
            {
                statements.Add(ParseStatement(declaration));
            }

            statements.Add(ParseStatement("var " + options.QueryVariableName + " = " + model.QueryExpressionCode + ";"));
            statements.Add(ParseStatement("return " + options.QueryVariableName + ";"));

            return MethodDeclaration(ParseTypeName(returnType), Identifier(options.MethodName))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .WithParameterList(ParameterList(SeparatedList(parameters)))
                .WithBody(Block(statements));
        }

        private static string ApplyFluentMethodLineBreaks(string code)
        {
            var lines = code.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder(code.Length + 256);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var rewritten = TryRewriteFluentLine(line, minLength: 120);
                sb.Append(rewritten ?? line);
                if (i < lines.Length - 1)
                {
                    sb.Append("\r\n");
                }
            }

            return sb.ToString();
        }

        private static string? TryRewriteFluentLine(string line, int minLength)
        {
            if (line.Length < minLength)
            {
                return null;
            }

            var trimmed = line.TrimStart();
            if (trimmed.Length == 0)
            {
                return null;
            }

            var lineStartIndent = line.Length - trimmed.Length;
            int expressionStart;
            string prefix;

            var assignIndex = line.IndexOf('=', lineStartIndent);
            if (assignIndex > 0)
            {
                expressionStart = assignIndex + 1;
                while (expressionStart < line.Length && line[expressionStart] == ' ')
                {
                    expressionStart++;
                }

                prefix = line.Substring(0, expressionStart);
            }
            else if (trimmed.StartsWith("return ", StringComparison.Ordinal))
            {
                expressionStart = lineStartIndent + "return ".Length;
                prefix = line.Substring(0, expressionStart);
            }
            else if (trimmed.Contains(").", StringComparison.Ordinal))
            {
                expressionStart = lineStartIndent;
                prefix = line.Substring(0, expressionStart);
            }
            else
            {
                return null;
            }

            if (expressionStart >= line.Length)
            {
                return null;
            }

            var expression = line.Substring(expressionStart);
            var splitPositions = FindTopLevelFluentSplitPositions(expression);
            var segments = new List<string>(splitPositions.Count + 1);
            if (splitPositions.Count == 0)
            {
                segments.Add(expression);
            }
            else
            {
                var cursor = 0;
                foreach (var pos in splitPositions)
                {
                    segments.Add(expression.Substring(cursor, pos + 1 - cursor));
                    cursor = pos + 1;
                }

                segments.Add(expression.Substring(cursor));
            }

            var continuationIndent = new string(' ', lineStartIndent + 4);
            var anySegmentChanged = splitPositions.Count > 0;
            for (var i = 0; i < segments.Count; i++)
            {
                var rewrittenSegment = RewriteLongInvocationArguments(segments[i], prefix.Length, minLength);
                if (rewrittenSegment != null)
                {
                    segments[i] = rewrittenSegment;
                    anySegmentChanged = true;
                }
            }

            if (!anySegmentChanged)
            {
                return null;
            }

            var result = new StringBuilder(line.Length + splitPositions.Count * (continuationIndent.Length + 2));
            result.Append(prefix);
            result.Append(segments[0]);
            for (var i = 1; i < segments.Count; i++)
            {
                result.Append("\r\n");
                result.Append(continuationIndent);
                result.Append(segments[i]);
            }

            return result.ToString();
        }

        private static List<int> FindTopLevelFluentSplitPositions(string expression)
        {
            var positions = new List<int>();
            var depth = 0;
            var inString = false;
            for (var i = 0; i < expression.Length - 1; i++)
            {
                var ch = expression[i];
                if (inString)
                {
                    if (ch == '"' && expression[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (ch == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inString = true;
                    continue;
                }

                if (ch == '(')
                {
                    depth++;
                    continue;
                }

                if (ch == ')')
                {
                    depth = Math.Max(0, depth - 1);
                    if (depth == 0 && expression[i + 1] == '.')
                    {
                        positions.Add(i);
                    }
                }
            }

            return positions;
        }

        private static string? RewriteLongInvocationArguments(string segment, int lineIndentLength, int minLength)
        {
            var openIndex = segment.IndexOf('(');
            if (openIndex < 0)
            {
                return null;
            }

            var closeIndex = FindMatchingParen(segment, openIndex);
            if (closeIndex < 0 || closeIndex <= openIndex + 1)
            {
                return null;
            }

            var args = SplitTopLevelArguments(segment, openIndex + 1, closeIndex);
            var methodName = GetInvocationMethodName(segment, openIndex);
            if (!ShouldWrapInvocationArguments(segment, methodName, args, minLength))
            {
                return null;
            }

            var methodPrefix = segment.Substring(0, openIndex + 1);
            var afterClose = segment.Substring(closeIndex + 1);
            var indent = new string(' ', lineIndentLength);
            var argIndent = new string(' ', lineIndentLength + 4);

            var sb = new StringBuilder(segment.Length + (args.Count * (lineIndentLength + 8)));
            sb.Append(methodPrefix);
            sb.Append(args[0].Trim());
            for (var i = 1; i < args.Count; i++)
            {
                sb.Append(",\r\n");
                sb.Append(argIndent);
                sb.Append(args[i].Trim());
            }

            sb.Append("\r\n");
            sb.Append(indent);
            sb.Append(')');
            sb.Append(afterClose);
            return sb.ToString();
        }

        private static bool ShouldWrapInvocationArguments(string segment, string? methodName, IReadOnlyList<string> args, int minLength)
        {
            if (args.Count < 2)
            {
                return false;
            }

            if (string.Equals(methodName, "Select", StringComparison.Ordinal))
            {
                return args.Count >= 3 || segment.Length >= minLength;
            }

            if (args.Count >= 3 && segment.Length >= 80)
            {
                return true;
            }

            if (segment.Length >= minLength)
            {
                return true;
            }

            for (var i = 0; i < args.Count; i++)
            {
                if (args[i].Trim().Length >= 48)
                {
                    return true;
                }
            }

            return false;
        }

        private static string? GetInvocationMethodName(string segment, int openIndex)
        {
            var end = openIndex - 1;
            while (end >= 0 && char.IsWhiteSpace(segment[end]))
            {
                end--;
            }

            if (end < 0)
            {
                return null;
            }

            var start = end;
            while (start >= 0 && (char.IsLetterOrDigit(segment[start]) || segment[start] == '_'))
            {
                start--;
            }

            start++;
            if (start > end)
            {
                return null;
            }

            return segment.Substring(start, end - start + 1);
        }

        private static int FindMatchingParen(string text, int openIndex)
        {
            var depth = 0;
            var inString = false;
            for (var i = openIndex; i < text.Length; i++)
            {
                var ch = text[i];
                if (inString)
                {
                    if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (ch == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inString = true;
                    continue;
                }

                if (ch == '(')
                {
                    depth++;
                    continue;
                }

                if (ch == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static List<string> SplitTopLevelArguments(string text, int start, int end)
        {
            var args = new List<string>();
            var depth = 0;
            var inString = false;
            var segmentStart = start;
            for (var i = start; i < end; i++)
            {
                var ch = text[i];
                if (inString)
                {
                    if (ch == '"' && i + 1 < end && text[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (ch == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inString = true;
                    continue;
                }

                if (ch == '(')
                {
                    depth++;
                    continue;
                }

                if (ch == ')')
                {
                    depth = Math.Max(0, depth - 1);
                    continue;
                }

                if (ch == ',' && depth == 0)
                {
                    args.Add(text.Substring(segmentStart, i - segmentStart));
                    segmentStart = i + 1;
                }
            }

            args.Add(text.Substring(segmentStart, end - segmentStart));
            return args;
        }

        private static MethodDeclarationSyntax BuildQueryMethod(
            SqExpressSqlTranspilerOptions options,
            IReadOnlyList<QueryPreviewBuildSource> buildUsages,
            IReadOnlyList<RoslynStatementSyntax> readStatements)
        {
            var buildArgs = buildUsages.Count > 0
                ? string.Join(", ", buildUsages.Select(i => "out var " + i.VariableName))
                : string.Empty;

            var buildCall = options.MethodName + "(" + buildArgs + ")";
            var forEachStatement = ForEachStatement(
                IdentifierName("var"),
                Identifier("r"),
                ParseExpression(buildCall + ".Query(database)"),
                Block(readStatements))
                .WithAwaitKeyword(Token(SyntaxKind.AwaitKeyword));

            return MethodDeclaration(ParseTypeName("Task"), Identifier("Query"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.AsyncKeyword))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("database")).WithType(ParseTypeName("ISqDatabase"))
                })))
                .WithBody(Block(forEachStatement));
        }

        private static MemberDeclarationSyntax BuildTableDescriptorClass(
            string className,
            string? schema,
            string tableName,
            IReadOnlyList<ColumnSpec> columns)
        {
            var members = new List<MemberDeclarationSyntax>(columns.Count + 1);
            foreach (var column in columns)
            {
                members.Add(
                    PropertyDeclaration(ParseTypeName(column.TypeName), Identifier(column.PropertyName))
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .WithAccessorList(
                            AccessorList(List(new[]
                            {
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            }))));
            }

            var ctorStatements = new List<RoslynStatementSyntax>(columns.Count);
            foreach (var column in columns)
            {
                ctorStatements.Add(ParseStatement("this." + column.PropertyName + " = " + column.InitExpression + ";"));
            }

            var constructor = ConstructorDeclaration(className)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("alias"))
                        .WithType(ParseTypeName("Alias"))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword))))
                })))
                .WithInitializer(
                    ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
                        .WithArgumentList(ArgumentList(SeparatedList(new[]
                        {
                            Argument(schema == null
                                ? (ExpressionSyntax)LiteralExpression(SyntaxKind.NullLiteralExpression)
                                : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(schema))),
                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(tableName))),
                            Argument(IdentifierName("alias"))
                        }))))
                .WithBody(Block(ctorStatements));

            members.Add(constructor);

            return ClassDeclaration(className)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
                .WithBaseList(BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(ParseTypeName("TableBase")))))
                .WithMembers(List(members));
        }

        private static IReadOnlyList<TableUsage> SelectBuildTableUsages(
            IReadOnlyList<TableUsage> tableUsages,
            IReadOnlyDictionary<string, string> classNamesByTableKey)
        {
            var result = new List<TableUsage>(tableUsages.Count);
            var usedVarNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var usage in tableUsages)
            {
                if (!classNamesByTableKey.TryGetValue(usage.TableKey, out var className))
                {
                    continue;
                }

                var variableName = MakeUniqueVariableName(
                    ToCamelCaseIdentifier(usage.Alias, "t"),
                    usedVarNames);

                result.Add(new TableUsage(usage.TableKey, usage.Alias, className, variableName));
            }

            return result;
        }

        private static string MakeUniqueClassName(string baseName, Dictionary<string, int> classNameCount)
        {
            if (classNameCount.TryGetValue(baseName, out var index))
            {
                index++;
                classNameCount[baseName] = index;
                return baseName + index.ToString();
            }

            classNameCount[baseName] = 0;
            return baseName;
        }

        private static string MakeUniquePropertyName(string candidate, HashSet<string> usedPropertyNames)
        {
            var current = candidate;
            var suffix = 1;
            while (!usedPropertyNames.Add(current))
            {
                current = candidate + suffix.ToString();
                suffix++;
            }

            return current;
        }

        private static string MakeUniqueVariableName(string candidate, HashSet<string> usedVariableNames)
        {
            var current = candidate;
            var suffix = 1;
            while (!usedVariableNames.Add(current))
            {
                current = candidate + suffix.ToString();
                suffix++;
            }

            return current;
        }

        private static string ToCamelCaseIdentifier(string value, string fallback)
        {
            var pascal = ToPascalCaseIdentifier(value, fallback);
            if (string.IsNullOrEmpty(pascal))
            {
                return fallback;
            }

            if (pascal.Length == 1)
            {
                return char.ToLowerInvariant(pascal[0]).ToString();
            }

            return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }

        private static string? TryGetAliasName(IExprAlias alias)
        {
            if (alias is ExprAlias exprAlias)
            {
                return exprAlias.Name;
            }

            return null;
        }

        private static string GetTableKey(ExprTableFullName fullName)
        {
            return (fullName.DbSchema?.Schema.Name ?? string.Empty) + "." + fullName.TableName.Name;
        }

        private static bool TryParseTableKey(string tableKey, out string? schema, out string tableName)
        {
            schema = null;
            tableName = string.Empty;
            if (string.IsNullOrWhiteSpace(tableKey))
            {
                return false;
            }

            var dot = tableKey.IndexOf('.');
            if (dot < 0)
            {
                tableName = tableKey;
                return tableName.Length > 0;
            }

            schema = dot == 0 ? null : tableKey.Substring(0, dot);
            tableName = dot + 1 < tableKey.Length ? tableKey.Substring(dot + 1) : string.Empty;
            return tableName.Length > 0;
        }

        private static ColumnSpec CreateColumnSpec(TableColumn column)
        {
            var propertyName = ToPascalCaseIdentifier(column.ColumnName.Name, "Column");
            var literalName = ToCSharpStringLiteral(column.ColumnName.Name);
            var sqlType = column.SqlType;

            if (sqlType is ExprTypeBoolean)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableBooleanTableColumn", "CreateNullableBooleanColumn(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "BooleanTableColumn", "CreateBooleanColumn(" + literalName + ")");
            }

            if (sqlType is ExprTypeByte)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableByteTableColumn", "CreateNullableByteColumn(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "ByteTableColumn", "CreateByteColumn(" + literalName + ")");
            }

            if (sqlType is ExprTypeInt16)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableInt16TableColumn", "CreateNullableInt16Column(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "Int16TableColumn", "CreateInt16Column(" + literalName + ")");
            }

            if (sqlType is ExprTypeInt32)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableInt32TableColumn", "CreateNullableInt32Column(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "Int32TableColumn", "CreateInt32Column(" + literalName + ")");
            }

            if (sqlType is ExprTypeInt64)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableInt64TableColumn", "CreateNullableInt64Column(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "Int64TableColumn", "CreateInt64Column(" + literalName + ")");
            }

            if (sqlType is ExprTypeDecimal)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableDecimalTableColumn", "CreateNullableDecimalColumn(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "DecimalTableColumn", "CreateDecimalColumn(" + literalName + ")");
            }

            if (sqlType is ExprTypeDouble)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableDoubleTableColumn", "CreateNullableDoubleColumn(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "DoubleTableColumn", "CreateDoubleColumn(" + literalName + ")");
            }

            if (sqlType is ExprTypeDateTime exprTypeDateTime)
            {
                var dateArg = exprTypeDateTime.IsDate ? ", true" : string.Empty;
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableDateTimeTableColumn", "CreateNullableDateTimeColumn(" + literalName + dateArg + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "DateTimeTableColumn", "CreateDateTimeColumn(" + literalName + dateArg + ")");
            }

            if (sqlType is ExprTypeDateTimeOffset)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableDateTimeOffsetTableColumn", "CreateNullableDateTimeOffsetColumn(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "DateTimeOffsetTableColumn", "CreateDateTimeOffsetColumn(" + literalName + ")");
            }

            if (sqlType is ExprTypeGuid)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableGuidTableColumn", "CreateNullableGuidColumn(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "GuidTableColumn", "CreateGuidColumn(" + literalName + ")");
            }

            if (sqlType is ExprTypeFixSizeByteArray fixedSizeByteArray)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableByteArrayTableColumn", "CreateNullableFixedSizeByteArrayColumn(" + literalName + ", " + fixedSizeByteArray.Size + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "ByteArrayTableColumn", "CreateFixedSizeByteArrayColumn(" + literalName + ", " + fixedSizeByteArray.Size + ")");
            }

            if (sqlType is ExprTypeByteArray byteArray)
            {
                var sizeArg = byteArray.Size.HasValue ? byteArray.Size.Value.ToString() : "null";
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableByteArrayTableColumn", "CreateNullableByteArrayColumn(" + literalName + ", " + sizeArg + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "ByteArrayTableColumn", "CreateByteArrayColumn(" + literalName + ", " + sizeArg + ")");
            }

            if (sqlType is ExprTypeXml)
            {
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableStringTableColumn", "CreateNullableXmlColumn(" + literalName + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "StringTableColumn", "CreateXmlColumn(" + literalName + ")");
            }

            if (sqlType is ExprTypeFixSizeString fixedSizeString)
            {
                var unicodeArg = fixedSizeString.IsUnicode ? ", true" : string.Empty;
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableStringTableColumn", "CreateNullableFixedSizeStringColumn(" + literalName + ", " + fixedSizeString.Size + unicodeArg + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "StringTableColumn", "CreateFixedSizeStringColumn(" + literalName + ", " + fixedSizeString.Size + unicodeArg + ")");
            }

            if (sqlType is ExprTypeString exprTypeString)
            {
                var sizeArg = exprTypeString.Size.HasValue ? exprTypeString.Size.Value.ToString() : "null";
                var unicodeArg = exprTypeString.IsUnicode ? ", true" : ", false";
                var textArg = exprTypeString.IsText ? ", true" : string.Empty;
                return column.IsNullable
                    ? new ColumnSpec(column.ColumnName.Name, propertyName, "NullableStringTableColumn", "CreateNullableStringColumn(" + literalName + ", " + sizeArg + unicodeArg + textArg + ")")
                    : new ColumnSpec(column.ColumnName.Name, propertyName, "StringTableColumn", "CreateStringColumn(" + literalName + ", " + sizeArg + unicodeArg + textArg + ")");
            }

            return InferColumnSpec(column.ColumnName.Name);
        }

        private static ColumnSpec InferColumnSpec(string columnName)
        {
            var propertyName = ToPascalCaseIdentifier(columnName, "Column");
            var literalName = ToCSharpStringLiteral(columnName);
            var kind = InferKindFromColumnToken(columnName);
            return InferColumnSpec(columnName, kind, propertyName, literalName);
        }

        private static ColumnSpec InferColumnSpec(string columnName, ParamKind kind)
        {
            var propertyName = ToPascalCaseIdentifier(columnName, "Column");
            var literalName = ToCSharpStringLiteral(columnName);
            return InferColumnSpec(columnName, kind, propertyName, literalName);
        }

        private static ColumnSpec InferColumnSpec(string columnName, ParamKind kind, string propertyName, string literalName)
        {
            switch (kind)
            {
                case ParamKind.Boolean:
                    return new ColumnSpec(columnName, propertyName, "BooleanTableColumn", "CreateBooleanColumn(" + literalName + ")");
                case ParamKind.Int32:
                    return new ColumnSpec(columnName, propertyName, "Int32TableColumn", "CreateInt32Column(" + literalName + ")");
                case ParamKind.Decimal:
                    return new ColumnSpec(columnName, propertyName, "DecimalTableColumn", "CreateDecimalColumn(" + literalName + ")");
                case ParamKind.Guid:
                    return new ColumnSpec(columnName, propertyName, "GuidTableColumn", "CreateGuidColumn(" + literalName + ")");
                case ParamKind.DateTime:
                    return new ColumnSpec(columnName, propertyName, "DateTimeTableColumn", "CreateDateTimeColumn(" + literalName + ")");
                case ParamKind.DateTimeOffset:
                    return new ColumnSpec(columnName, propertyName, "DateTimeOffsetTableColumn", "CreateDateTimeOffsetColumn(" + literalName + ")");
                case ParamKind.ByteArray:
                    return new ColumnSpec(columnName, propertyName, "ByteArrayTableColumn", "CreateByteArrayColumn(" + literalName + ", null)");
                default:
                    return new ColumnSpec(columnName, propertyName, "StringTableColumn", "CreateStringColumn(" + literalName + ", 255, true)");
            }
        }

        private static ColumnSpec MergeColumnSpec(ColumnSpec left, ColumnSpec right)
        {
            if (string.Equals(left.TypeName, right.TypeName, StringComparison.Ordinal)
                && string.Equals(left.InitExpression, right.InitExpression, StringComparison.Ordinal))
            {
                return left;
            }

            if (IsStringColumnType(left.TypeName) && !IsStringColumnType(right.TypeName))
            {
                return new ColumnSpec(left.ColumnName, left.PropertyName, right.TypeName, right.InitExpression);
            }

            if (IsStringColumnType(right.TypeName) && !IsStringColumnType(left.TypeName))
            {
                return left;
            }

            return new ColumnSpec(
                left.ColumnName,
                left.PropertyName,
                "StringTableColumn",
                "CreateStringColumn(" + ToCSharpStringLiteral(left.ColumnName) + ", 255, true)");
        }

        private static bool IsStringColumnType(string typeName)
            => string.Equals(typeName, "StringTableColumn", StringComparison.Ordinal)
               || string.Equals(typeName, "NullableStringTableColumn", StringComparison.Ordinal);

        private static ColumnSpec MakeNullableColumnSpec(ColumnSpec source)
        {
            if (source.TypeName == "StringTableColumn" && source.InitExpression.StartsWith("CreateStringColumn(", StringComparison.Ordinal))
            {
                return new ColumnSpec(
                    source.ColumnName,
                    source.PropertyName,
                    "NullableStringTableColumn",
                    source.InitExpression.Replace("CreateStringColumn(", "CreateNullableStringColumn("));
            }

            if (source.TypeName == "BooleanTableColumn" && source.InitExpression.StartsWith("CreateBooleanColumn(", StringComparison.Ordinal))
            {
                return new ColumnSpec(
                    source.ColumnName,
                    source.PropertyName,
                    "NullableBooleanTableColumn",
                    source.InitExpression.Replace("CreateBooleanColumn(", "CreateNullableBooleanColumn("));
            }

            if (source.TypeName == "Int32TableColumn" && source.InitExpression.StartsWith("CreateInt32Column(", StringComparison.Ordinal))
            {
                return new ColumnSpec(
                    source.ColumnName,
                    source.PropertyName,
                    "NullableInt32TableColumn",
                    source.InitExpression.Replace("CreateInt32Column(", "CreateNullableInt32Column("));
            }

            return source;
        }

        private static ExprAnalysis AnalyzeExpression(IExpr expr)
        {
            var tableCollector = new TableUsageCollectorVisitor();
            expr.Accept(tableCollector);

            var fallbackTableKey = tableCollector.TableKeys.Count == 1 ? tableCollector.TableKeys[0] : null;
            var columnCollector = new TableColumnCollectorVisitor(tableCollector.AliasToTableKeys, fallbackTableKey);
            expr.Accept(columnCollector);

            var aliasesByTableKey = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in tableCollector.AliasToTableKeys)
            {
                foreach (var tableKey in pair.Value)
                {
                    if (!aliasesByTableKey.TryGetValue(tableKey, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        aliasesByTableKey[tableKey] = set;
                    }

                    set.Add(pair.Key);
                }
            }

            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ASC", "DESC" };
            foreach (var pair in columnCollector.DiscoveredColumnsByTableKey)
            {
                aliasesByTableKey.TryGetValue(pair.Key, out var aliases);
                var dot = pair.Key.LastIndexOf('.');
                var tableName = dot >= 0 && dot + 1 < pair.Key.Length ? pair.Key.Substring(dot + 1) : pair.Key;
                pair.Value.RemoveWhere(c =>
                    keywords.Contains(c)
                    || string.Equals(c, tableName, StringComparison.OrdinalIgnoreCase)
                    || (aliases != null && aliases.Contains(c)));
            }

            return new ExprAnalysis(
                tableCollector.TableUsages,
                columnCollector.DiscoveredColumnsByTableKey,
                columnCollector.NullableColumnsByTableKey,
                columnCollector.InferredColumnKindsByTableKey);
        }

        private static IExpr RemoveDefaultSchemaForUnqualifiedTables(IExpr expr, IReadOnlyList<RawTableRef> rawRefs)
        {
            var unqualifiedOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in rawRefs.GroupBy(i => i.TableName, StringComparer.OrdinalIgnoreCase))
            {
                var hasUnqualified = g.Any(i => i.SchemaName == null);
                var hasQualified = g.Any(i => i.SchemaName != null);
                if (hasUnqualified && !hasQualified)
                {
                    unqualifiedOnly.Add(g.Key);
                }
            }

            if (unqualifiedOnly.Count < 1)
            {
                return expr;
            }

            var m = expr.SyntaxTree().ModifyDescendants(i =>
            {
                if (i is IExprTableFullName t)
                {
                    var f = t.AsExprTableFullName();
                    if (f.DbSchema != null && unqualifiedOnly.Contains(f.TableName.Name))
                    {
                        return (IExpr)t.WithSchemaName(null);
                    }
                }

                return i;
            });

            return m ?? expr;
        }

        private static void EnsureNoAmbiguousUnqualifiedTables(IReadOnlyList<RawTableRef> rawRefs)
        {
            var state = new Dictionary<string, Tuple<bool, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rawRefs)
            {
                if (!state.TryGetValue(r.TableName, out var current))
                {
                    current = Tuple.Create(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }

                if (string.IsNullOrWhiteSpace(r.SchemaName))
                {
                    current = Tuple.Create(true, current.Item2);
                }
                else
                {
                    current.Item2.Add(r.SchemaName!);
                }

                state[r.TableName] = current;
            }

            foreach (var p in state)
            {
                if (p.Value.Item1 && p.Value.Item2.Count > 1)
                {
                    throw new SqExpressSqlTranspilerException("Ambiguous unqualified table reference '" + p.Key + "'");
                }
            }
        }

        private static IExpr ApplyDefaultSchema(IExpr expr, string defaultSchema)
        {
            var m = expr.SyntaxTree().ModifyDescendants(i =>
            {
                if (i is IExprTableFullName t)
                {
                    var f = t.AsExprTableFullName();
                    if (f.DbSchema == null && !f.TableName.Name.StartsWith("#", StringComparison.Ordinal))
                    {
                        return (IExpr)t.WithSchemaName(defaultSchema);
                    }
                }

                return i;
            });
            return m ?? expr;
        }

        private static IExpr EnsureCurrentRowFrameWhenPresentInSql(IExpr expr, string sql)
        {
            var hasCurrentRowFrame = sql.IndexOf("ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW", StringComparison.OrdinalIgnoreCase) >= 0;
            var hasUnboundedFollowingFrame = sql.IndexOf("ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!hasCurrentRowFrame && !hasUnboundedFollowingFrame)
            {
                return expr;
            }

            var m = expr.SyntaxTree().ModifyDescendants(i =>
            {
                if (i is ExprAggregateOverFunction agg && agg.Over.OrderBy != null && agg.Over.FrameClause == null)
                {
                    var over = new ExprOver(agg.Over.Partitions, agg.Over.OrderBy, new ExprFrameClause(new ExprUnboundedFrameBorder(FrameBorderDirection.Preceding), ExprCurrentRowFrameBorder.Instance));
                    return (IExpr)new ExprAggregateOverFunction(agg.Function, over);
                }

                if (i is ExprAnalyticFunction analytic && analytic.Over.OrderBy != null && analytic.Over.FrameClause == null)
                {
                    var name = analytic.Name.Name.ToUpperInvariant();
                    if (hasCurrentRowFrame && (name == "SUM" || name == "MIN" || name == "MAX" || name == "AVG" || name == "COUNT"))
                    {
                        var over = new ExprOver(analytic.Over.Partitions, analytic.Over.OrderBy, new ExprFrameClause(new ExprUnboundedFrameBorder(FrameBorderDirection.Preceding), ExprCurrentRowFrameBorder.Instance));
                        return (IExpr)new ExprAnalyticFunction(analytic.Name, analytic.Arguments, over);
                    }
                    if (hasUnboundedFollowingFrame && name == "LAST_VALUE")
                    {
                        var over = new ExprOver(analytic.Over.Partitions, analytic.Over.OrderBy, new ExprFrameClause(new ExprUnboundedFrameBorder(FrameBorderDirection.Preceding), new ExprUnboundedFrameBorder(FrameBorderDirection.Following)));
                        return (IExpr)new ExprAnalyticFunction(analytic.Name, analytic.Arguments, over);
                    }
                }

                return i;
            });

            return m ?? expr;
        }

        private static Dictionary<string, ExprValue> InferParameterDefaults(IExpr expr, string sql)
        {
            var kinds = new Dictionary<string, ParamKind>(StringComparer.Ordinal);
            foreach (var p in expr.SyntaxTree().DescendantsAndSelf().OfType<ExprParameter>())
            {
                if (!string.IsNullOrWhiteSpace(p.TagName))
                {
                    kinds[p.TagName!] = InferKindFromColumnToken(p.TagName!);
                }
            }
            if (kinds.Count < 1)
            {
                return new Dictionary<string, ExprValue>(StringComparer.Ordinal);
            }

            foreach (Match m in RxCastParamType.Matches(sql)) { Hint(kinds, m.Groups["p"].Value, MapCastType(m.Groups["t"].Value)); }
            foreach (Match m in RxColumnCompareParam.Matches(sql)) { Hint(kinds, m.Groups["p"].Value, InferKindFromColumnToken(m.Groups["c"].Value)); }
            foreach (Match m in RxParamCompareColumn.Matches(sql)) { Hint(kinds, m.Groups["p"].Value, InferKindFromColumnToken(m.Groups["c"].Value)); }
            foreach (Match m in RxInParam.Matches(sql)) { Hint(kinds, m.Groups["p"].Value, InferKindFromColumnToken(m.Groups["c"].Value)); }
            foreach (Match m in RxParamCompareNumber.Matches(sql)) { Hint(kinds, m.Groups["p"].Value, m.Groups["n"].Value.Contains(".") ? ParamKind.Decimal : ParamKind.Int32); }

            var result = new Dictionary<string, ExprValue>(kinds.Count, StringComparer.Ordinal);
            foreach (var p in kinds) { result[p.Key] = CreateDefaultExprValue(p.Value); }
            return result;
        }

        private static IReadOnlyCollection<string> GetListParameterNames(IExpr expr)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var inValues in expr.SyntaxTree().DescendantsAndSelf().OfType<ExprInValues>())
            {
                if (inValues.Items.Count == 1 && inValues.Items[0] is ExprParameter parameter && !string.IsNullOrWhiteSpace(parameter.TagName))
                {
                    result.Add(parameter.TagName!);
                }
            }

            return result;
        }

        private static void Hint(Dictionary<string, ParamKind> kinds, string rawName, ParamKind hint)
        {
            var n = rawName.Trim().TrimStart('@');
            if (!kinds.ContainsKey(n)) { return; }
            kinds[n] = MergeParamKind(kinds[n], hint);
        }

        private static ParamKind MergeParamKind(ParamKind existing, ParamKind hint)
        {
            if (existing == hint) { return existing; }
            if (existing == ParamKind.String) { return hint; }
            if (hint == ParamKind.String) { return existing; }
            if ((existing == ParamKind.Int32 && hint == ParamKind.Decimal) || (existing == ParamKind.Decimal && hint == ParamKind.Int32)) { return ParamKind.Decimal; }
            return existing;
        }

        private static ParamKind InferKindFromColumnToken(string token)
        {
            var col = token.Trim();
            var dot = col.LastIndexOf('.');
            if (dot >= 0 && dot + 1 < col.Length) { col = col.Substring(dot + 1); }
            col = col.Trim().Trim('[', ']');
            var upper = col.ToUpperInvariant();
            if (upper.Contains("GUID") || upper.Contains("UUID") || upper.EndsWith("EXTERNALID")) { return ParamKind.Guid; }
            if (upper.StartsWith("IS") || upper.StartsWith("HAS") || upper.EndsWith("FLAG")) { return ParamKind.Boolean; }
            if (upper.Contains("PAYLOAD") || upper.Contains("BINARY") || upper.Contains("BYTE")) { return ParamKind.ByteArray; }
            if (upper.Contains("AMOUNT") || upper.Contains("PRICE") || upper.Contains("RATE")) { return ParamKind.Decimal; }
            if (upper.Contains("DATE") || upper.Contains("TIME") || upper.Contains("UTC") || upper.EndsWith("AT")) { return ParamKind.DateTime; }
            if (upper.EndsWith("ID") || upper.Contains("COUNT") || upper.Contains("NUM")) { return ParamKind.Int32; }
            return ParamKind.String;
        }

        private static ParamKind MapCastType(string t)
        {
            switch (t.Trim().ToUpperInvariant())
            {
                case "BIT": return ParamKind.Boolean;
                case "INT":
                case "BIGINT":
                case "SMALLINT":
                case "TINYINT": return ParamKind.Int32;
                case "DECIMAL":
                case "NUMERIC":
                case "MONEY":
                case "SMALLMONEY": return ParamKind.Decimal;
                case "DATETIME":
                case "DATETIME2":
                case "SMALLDATETIME":
                case "DATE":
                case "TIME": return ParamKind.DateTime;
                case "DATETIMEOFFSET": return ParamKind.DateTimeOffset;
                case "UNIQUEIDENTIFIER": return ParamKind.Guid;
                case "VARBINARY":
                case "BINARY":
                case "IMAGE": return ParamKind.ByteArray;
                default: return ParamKind.String;
            }
        }

        private static ExprValue CreateDefaultExprValue(ParamKind kind)
        {
            switch (kind)
            {
                case ParamKind.Int32: return SqQueryBuilder.Literal(0);
                case ParamKind.Decimal: return SqQueryBuilder.Literal(0m);
                case ParamKind.Boolean: return SqQueryBuilder.Literal(false);
                case ParamKind.Guid: return SqQueryBuilder.Literal(default(Guid));
                case ParamKind.DateTime: return SqQueryBuilder.Literal(default(DateTime));
                case ParamKind.DateTimeOffset: return SqQueryBuilder.Literal(default(DateTimeOffset));
                case ParamKind.ByteArray: return SqQueryBuilder.Literal(Array.Empty<byte>());
                default: return SqQueryBuilder.Literal(string.Empty);
            }
        }

        private static bool ContainsKeyword(string sql, string keyword)
        {
            var tokens = TokenizeSqlForValidation(sql);
            return tokens.Any(t => t.IsWord(keyword));
        }

        private static bool IsSelectInto(string sql)
        {
            var tokens = TokenizeSqlForValidation(sql);
            var depth = 0;
            var seenSelect = false;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.IsSeparator("("))
                {
                    depth++;
                    continue;
                }

                if (token.IsSeparator(")"))
                {
                    depth = Math.Max(0, depth - 1);
                    continue;
                }

                if (depth != 0 || token.Kind != SqlTokenKind.Word)
                {
                    continue;
                }

                if (!seenSelect)
                {
                    if (token.IsWord("SELECT"))
                    {
                        seenSelect = true;
                    }

                    continue;
                }

                if (token.IsWord("INTO"))
                {
                    return true;
                }

                if (token.IsWord("FROM"))
                {
                    return false;
                }
            }

            return false;
        }

        private static bool ContainsRangeWindowFrame(string sql)
        {
            var tokens = TokenizeSqlForValidation(sql);
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!tokens[i].IsWord("OVER"))
                {
                    continue;
                }

                var open = i + 1;
                if (open >= tokens.Count || !tokens[open].IsSeparator("("))
                {
                    continue;
                }

                var depth = 1;
                for (var j = open + 1; j < tokens.Count; j++)
                {
                    if (tokens[j].IsSeparator("("))
                    {
                        depth++;
                        continue;
                    }

                    if (tokens[j].IsSeparator(")"))
                    {
                        depth--;
                        if (depth == 0)
                        {
                            break;
                        }

                        continue;
                    }

                    if (depth == 1 && tokens[j].IsWord("RANGE"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsDatabaseQualifiedScalarFunction(string sql)
        {
            var tokens = TokenizeSqlForValidation(sql);
            for (var i = 0; i + 5 < tokens.Count; i++)
            {
                if (!IsIdentifierToken(tokens[i]) || !tokens[i + 1].IsSeparator("."))
                {
                    continue;
                }

                if (!IsIdentifierToken(tokens[i + 2]) || !tokens[i + 3].IsSeparator("."))
                {
                    continue;
                }

                if (!IsIdentifierToken(tokens[i + 4]))
                {
                    continue;
                }

                if (tokens[i + 5].IsSeparator("("))
                {
                    return true;
                }
            }

            return false;
        }
        private static bool ContainsEmptyGroupBy(string sql)
        {
            var tokens = TokenizeSqlForValidation(sql);
            for (var i = 0; i + 1 < tokens.Count; i++)
            {
                if (!tokens[i].IsWord("GROUP") || !tokens[i + 1].IsWord("BY"))
                {
                    continue;
                }

                var nextIndex = i + 2;
                while (nextIndex < tokens.Count && tokens[nextIndex].Kind == SqlTokenKind.Separator)
                {
                    nextIndex++;
                }

                if (nextIndex >= tokens.Count || IsClauseStarter(tokens, nextIndex))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsClauseStarter(IReadOnlyList<SqlToken> tokens, int index)
        {
            if (tokens[index].IsWord("ORDER") && index + 1 < tokens.Count && tokens[index + 1].IsWord("BY")) { return true; }
            if (tokens[index].IsWord("GROUP") && index + 1 < tokens.Count && tokens[index + 1].IsWord("BY")) { return true; }
            if (tokens[index].IsWord("HAVING")) { return true; }
            if (tokens[index].IsWord("WHERE")) { return true; }
            if (tokens[index].IsWord("FROM")) { return true; }
            if (tokens[index].IsWord("JOIN")) { return true; }
            if (tokens[index].IsWord("INNER")) { return true; }
            if (tokens[index].IsWord("LEFT")) { return true; }
            if (tokens[index].IsWord("RIGHT")) { return true; }
            if (tokens[index].IsWord("FULL")) { return true; }
            if (tokens[index].IsWord("CROSS")) { return true; }
            if (tokens[index].IsWord("OUTER")) { return true; }
            if (tokens[index].IsWord("UNION")) { return true; }
            if (tokens[index].IsWord("INTERSECT")) { return true; }
            if (tokens[index].IsWord("EXCEPT")) { return true; }
            if (tokens[index].IsWord("OFFSET")) { return true; }
            if (tokens[index].IsWord("FETCH")) { return true; }
            if (tokens[index].IsWord("FOR")) { return true; }
            if (tokens[index].IsWord("OPTION")) { return true; }
            return false;
        }

        private static bool IsIdentifierToken(SqlToken token)
        {
            return token.Kind == SqlTokenKind.Word || token.Kind == SqlTokenKind.Identifier;
        }

        private static List<SqlToken> TokenizeSqlForValidation(string sql)
        {
            var result = new List<SqlToken>(Math.Max(8, sql.Length / 3));
            for (var i = 0; i < sql.Length;)
            {
                var ch = sql[i];
                var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

                if (char.IsWhiteSpace(ch))
                {
                    i++;
                    continue;
                }

                if (ch == '-' && next == '-')
                {
                    i += 2;
                    while (i < sql.Length && sql[i] != '\n')
                    {
                        i++;
                    }

                    continue;
                }

                if (ch == '/' && next == '*')
                {
                    i += 2;
                    while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/'))
                    {
                        i++;
                    }

                    if (i + 1 < sql.Length)
                    {
                        i += 2;
                    }

                    continue;
                }

                if (ch == '\'')
                {
                    var start = i++;
                    while (i < sql.Length)
                    {
                        if (sql[i] == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'')
                        {
                            i += 2;
                            continue;
                        }

                        if (sql[i] == '\'')
                        {
                            i++;
                            break;
                        }

                        i++;
                    }

                    result.Add(new SqlToken(SqlTokenKind.Literal, sql.Substring(start, i - start)));
                    continue;
                }

                if (ch == '[')
                {
                    var start = i++;
                    while (i < sql.Length && sql[i] != ']')
                    {
                        i++;
                    }

                    if (i < sql.Length)
                    {
                        i++;
                    }

                    result.Add(new SqlToken(SqlTokenKind.Identifier, sql.Substring(start, i - start)));
                    continue;
                }

                if (ch == '"')
                {
                    var start = i++;
                    while (i < sql.Length)
                    {
                        if (sql[i] == '"' && i + 1 < sql.Length && sql[i + 1] == '"')
                        {
                            i += 2;
                            continue;
                        }

                        if (sql[i] == '"')
                        {
                            i++;
                            break;
                        }

                        i++;
                    }

                    result.Add(new SqlToken(SqlTokenKind.Identifier, sql.Substring(start, i - start)));
                    continue;
                }

                if (IsSqlWordStart(ch))
                {
                    var start = i++;
                    while (i < sql.Length && IsSqlWordPart(sql[i]))
                    {
                        i++;
                    }

                    result.Add(new SqlToken(SqlTokenKind.Word, sql.Substring(start, i - start)));
                    continue;
                }

                if (ch == ',' || ch == ';' || ch == ')' || ch == '(' || ch == '.')
                {
                    result.Add(new SqlToken(SqlTokenKind.Separator, ch.ToString()));
                    i++;
                    continue;
                }

                result.Add(new SqlToken(SqlTokenKind.Other, ch.ToString()));
                i++;
            }

            return result;
        }

        private static bool IsSqlWordStart(char ch)
        {
            return char.IsLetter(ch) || ch == '_' || ch == '@' || ch == '#';
        }

        private static bool IsSqlWordPart(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || ch == '@' || ch == '#';
        }

        private static string NormalizeBetween(string sql) => RxBetweenSimple.Replace(sql, m => "(" + m.Groups["l"].Value + " >= " + m.Groups["a"].Value + " AND " + m.Groups["l"].Value + " <= " + m.Groups["b"].Value + ")");

        private static string AddSyntheticSelectAliases(string sql)
        {
            var aliasIndex = 0;
            sql = Regex.Replace(sql, @"\bFROM\s+(?<tbl>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))?)\s*(?=(WHERE|GROUP\s+BY|ORDER\s+BY|UNION|INTERSECT|EXCEPT|OFFSET|$))", m => "FROM " + m.Groups["tbl"].Value + " [A" + (aliasIndex++).ToString() + "] ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bJOIN\s+(?<tbl>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))?)\s*(?=(ON|WHERE|GROUP\s+BY|ORDER\s+BY|UNION|INTERSECT|EXCEPT|$))", m => "JOIN " + m.Groups["tbl"].Value + " [A" + (aliasIndex++).ToString() + "] ", RegexOptions.IgnoreCase);
            return sql;
        }
        private static string NormalizeMergeAliasInSource(string sql)
        {
            var m = Regex.Match(sql, @"^\s*MERGE\s+(?<target>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))?)\s+AS\s+(?<alias>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
            if (!m.Success) { return sql; }
            var alias = m.Groups["alias"].Value;
            sql = Regex.Replace(sql, @"^\s*MERGE\s+" + Regex.Escape(m.Groups["target"].Value) + @"\s+AS\s+" + Regex.Escape(alias), "MERGE " + m.Groups["target"].Value + " A0", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\b" + Regex.Escape(alias) + @"\.", "A0.", RegexOptions.IgnoreCase);
            return sql;
        }

        private static IReadOnlyList<RawTableRef> ReadRawTableRefs(string sql)
        {
            var cleaned = Regex.Replace(sql, @"'([^']|'')*'", "''");
            var matches = Regex.Matches(cleaned, @"\b(?:FROM|JOIN|INTO|USING|MERGE)\s+(?<name>(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*))?)", RegexOptions.IgnoreCase);
            var result = new List<RawTableRef>(matches.Count);
            foreach (Match m in matches)
            {
                var p = m.Index + m.Length;
                while (p < cleaned.Length && char.IsWhiteSpace(cleaned[p])) { p++; }
                if (p < cleaned.Length && cleaned[p] == '(') { continue; }

                var name = m.Groups["name"].Value;
                var parts = name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim().Trim('[', ']')).ToArray();
                if (parts.Length == 1) { result.Add(new RawTableRef(null, parts[0])); }
                else if (parts.Length >= 2) { result.Add(new RawTableRef(parts[parts.Length - 2], parts[parts.Length - 1])); }
            }
            return result;
        }

        private static string RemoveDefaultSchemaFromSqlText(string sql, IReadOnlyList<RawTableRef> rawRefs)
        {
            var unqualifiedOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in rawRefs.GroupBy(i => i.TableName, StringComparer.OrdinalIgnoreCase))
            {
                var hasUnqualified = g.Any(i => i.SchemaName == null);
                var hasQualified = g.Any(i => i.SchemaName != null);
                if (hasUnqualified && !hasQualified) { unqualifiedOnly.Add(g.Key); }
            }
            foreach (var table in unqualifiedOnly)
            {
                sql = Regex.Replace(sql, @"\[[^\]]+\]\.\[" + Regex.Escape(table) + @"\]", "[" + table + "]", RegexOptions.IgnoreCase);
            }
            return sql;
        }

        private static string NormalizeMergeTargetAlias(string sql)
        {
            sql = Regex.Replace(sql, @"\bMERGE\s+(?<t>\[[^\]]+\]\.\[[^\]]+\])\s+\[AS\]", "MERGE ${t} [A0]", RegexOptions.IgnoreCase);
            return sql.Replace("[t].", "[A0].");
        }

        private static string DetectStatementKind(IExpr expr)
        {
            if (expr is ExprInsert || expr is ExprIdentityInsert || expr is ExprInsertOutput) { return "INSERT"; }
            if (expr is ExprUpdate) { return "UPDATE"; }
            if (expr is ExprDelete || expr is ExprDeleteOutput) { return "DELETE"; }
            if (expr is ExprMerge || expr is ExprMergeOutput) { return "MERGE"; }
            if (expr is IExprQuery) { return "SELECT"; }
            return "UNKNOWN";
        }

        private static bool LooksLikeUnsupportedStatement(string sql)
        {
            var token = FirstToken(sql);
            return token.Length > 0
                   && !token.Equals("SELECT", StringComparison.OrdinalIgnoreCase)
                   && !token.Equals("INSERT", StringComparison.OrdinalIgnoreCase)
                   && !token.Equals("UPDATE", StringComparison.OrdinalIgnoreCase)
                   && !token.Equals("DELETE", StringComparison.OrdinalIgnoreCase)
                   && !token.Equals("MERGE", StringComparison.OrdinalIgnoreCase)
                   && !token.Equals("WITH", StringComparison.OrdinalIgnoreCase);
        }

        private static string FirstToken(string sql)
        {
            var i = 0;
            while (i < sql.Length && char.IsWhiteSpace(sql[i])) { i++; }
            var s = i;
            while (i < sql.Length && char.IsLetter(sql[i])) { i++; }
            return i > s ? sql.Substring(s, i - s) : string.Empty;
        }

        private static string ToPascalCaseIdentifier(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) { return fallback; }
            var sb = new StringBuilder(value.Length);
            var upper = true;
            foreach (var c in value)
            {
                if (!char.IsLetterOrDigit(c)) { upper = true; continue; }
                var n = upper ? char.ToUpperInvariant(c) : c;
                if (sb.Length == 0 && !char.IsLetter(n) && n != '_') { sb.Append('_'); }
                sb.Append(n);
                upper = false;
            }
            return sb.Length == 0 ? fallback : sb.ToString();
        }

        private static string ToCSharpStringLiteral(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        private static string ToVerbatimStringLiteral(string value) => "@\"" + value.Replace("\"", "\"\"") + "\"";

        private sealed class ExprAnalysis
        {
            public ExprAnalysis(
                IReadOnlyList<TableUsage> tableUsages,
                IReadOnlyDictionary<string, HashSet<string>> discoveredColumnsByTableKey,
                IReadOnlyDictionary<string, HashSet<string>> nullableColumnsByTableKey,
                IReadOnlyDictionary<string, Dictionary<string, ParamKind>> inferredColumnKindsByTableKey)
            {
                this.TableUsages = tableUsages;
                this.DiscoveredColumnsByTableKey = discoveredColumnsByTableKey;
                this.NullableColumnsByTableKey = nullableColumnsByTableKey;
                this.InferredColumnKindsByTableKey = inferredColumnKindsByTableKey;
            }

            public IReadOnlyList<TableUsage> TableUsages { get; }

            public IReadOnlyDictionary<string, HashSet<string>> DiscoveredColumnsByTableKey { get; }

            public IReadOnlyDictionary<string, HashSet<string>> NullableColumnsByTableKey { get; }

            public IReadOnlyDictionary<string, Dictionary<string, ParamKind>> InferredColumnKindsByTableKey { get; }
        }

        private sealed class TableUsage
        {
            public TableUsage(string tableKey, string alias, string className = "", string variableName = "")
            {
                this.TableKey = tableKey;
                this.Alias = alias;
                this.ClassName = className;
                this.VariableName = variableName;
            }

            public string TableKey { get; }

            public string Alias { get; }

            public string ClassName { get; }

            public string VariableName { get; }
        }

        private sealed class TableUsageCollectorVisitor : ExprVisitorBase
        {
            private readonly Dictionary<string, HashSet<string>> _aliasToTableKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            private readonly List<TableUsage> _tableUsages = new List<TableUsage>();
            private readonly HashSet<string> _seenAliasAndKey = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _tableKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public IReadOnlyDictionary<string, HashSet<string>> AliasToTableKeys => this._aliasToTableKeys;

            public IReadOnlyList<TableUsage> TableUsages => this._tableUsages;

            public IReadOnlyList<string> TableKeys => this._tableKeys.ToList();

            public override void VisitExprTable(ExprTable expr)
            {
                base.VisitExprTable(expr);
                var tableKey = GetTableKey(expr.FullName.AsExprTableFullName());
                this._tableKeys.Add(tableKey);

                var alias = expr.Alias == null
                    ? ToCamelCaseIdentifier(expr.FullName.AsExprTableFullName().TableName.Name, "t")
                    : TryGetAliasName(expr.Alias.Alias);

                if (!string.IsNullOrWhiteSpace(alias))
                {
                    if (!this._aliasToTableKeys.TryGetValue(alias!, out var keys))
                    {
                        keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        this._aliasToTableKeys[alias!] = keys;
                    }

                    keys.Add(tableKey);
                    var usageKey = alias + "|" + tableKey;
                    if (this._seenAliasAndKey.Add(usageKey))
                    {
                        this._tableUsages.Add(new TableUsage(tableKey, alias!));
                    }
                }
            }

            public override void VisitExprInsert(ExprInsert expr)
            {
                base.VisitExprInsert(expr);
                this.RegisterTable(expr.Target);
            }

            public override void VisitExprIdentityInsert(ExprIdentityInsert expr)
            {
                base.VisitExprIdentityInsert(expr);
                this.RegisterTable(expr.Insert.Target);
            }

            public override void VisitExprDelete(ExprDelete expr)
            {
                base.VisitExprDelete(expr);
                this.RegisterTable(expr.Target.FullName);
            }

            public override void VisitExprUpdate(ExprUpdate expr)
            {
                base.VisitExprUpdate(expr);
                this.RegisterTable(expr.Target.FullName);
            }

            private void RegisterTable(IExprTableFullName fullName)
            {
                var tableKey = GetTableKey(fullName.AsExprTableFullName());
                this._tableKeys.Add(tableKey);
                var alias = ToCamelCaseIdentifier(fullName.AsExprTableFullName().TableName.Name, "t");
                if (!this._aliasToTableKeys.TryGetValue(alias, out var keys))
                {
                    keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    this._aliasToTableKeys[alias] = keys;
                }

                keys.Add(tableKey);
                var usageKey = alias + "|" + tableKey;
                if (this._seenAliasAndKey.Add(usageKey))
                {
                    this._tableUsages.Add(new TableUsage(tableKey, alias));
                }
            }
        }

        private sealed class TableColumnCollectorVisitor : ExprVisitorBase
        {
            private readonly IReadOnlyDictionary<string, HashSet<string>> _aliasToTableKeys;
            private readonly string? _fallbackTableKey;

            public TableColumnCollectorVisitor(IReadOnlyDictionary<string, HashSet<string>> aliasToTableKeys, string? fallbackTableKey)
            {
                this._aliasToTableKeys = aliasToTableKeys;
                this._fallbackTableKey = fallbackTableKey;
            }

            public Dictionary<string, HashSet<string>> DiscoveredColumnsByTableKey { get; } =
                new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, HashSet<string>> NullableColumnsByTableKey { get; } =
                new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, Dictionary<string, ParamKind>> InferredColumnKindsByTableKey { get; } =
                new Dictionary<string, Dictionary<string, ParamKind>>(StringComparer.OrdinalIgnoreCase);

            public override void VisitExprColumn(ExprColumn expr)
            {
                base.VisitExprColumn(expr);
                this.AddColumn(this.DiscoveredColumnsByTableKey, expr);
            }

            public override void VisitExprIsNull(ExprIsNull expr)
            {
                base.VisitExprIsNull(expr);
                if (expr.Test is ExprColumn column)
                {
                    this.AddColumn(this.NullableColumnsByTableKey, column);
                }
            }

            public override void VisitExprBooleanEq(ExprBooleanEq expr)
            {
                base.VisitExprBooleanEq(expr);
                this.HintFromComparison(expr.Left, expr.Right);
                this.HintFromComparison(expr.Right, expr.Left);
            }

            public override void VisitExprBooleanNotEq(ExprBooleanNotEq expr)
            {
                base.VisitExprBooleanNotEq(expr);
                this.HintFromComparison(expr.Left, expr.Right);
                this.HintFromComparison(expr.Right, expr.Left);
            }

            public override void VisitExprBooleanGt(ExprBooleanGt expr)
            {
                base.VisitExprBooleanGt(expr);
                this.HintFromComparison(expr.Left, expr.Right);
                this.HintFromComparison(expr.Right, expr.Left);
            }

            public override void VisitExprBooleanGtEq(ExprBooleanGtEq expr)
            {
                base.VisitExprBooleanGtEq(expr);
                this.HintFromComparison(expr.Left, expr.Right);
                this.HintFromComparison(expr.Right, expr.Left);
            }

            public override void VisitExprBooleanLt(ExprBooleanLt expr)
            {
                base.VisitExprBooleanLt(expr);
                this.HintFromComparison(expr.Left, expr.Right);
                this.HintFromComparison(expr.Right, expr.Left);
            }

            public override void VisitExprBooleanLtEq(ExprBooleanLtEq expr)
            {
                base.VisitExprBooleanLtEq(expr);
                this.HintFromComparison(expr.Left, expr.Right);
                this.HintFromComparison(expr.Right, expr.Left);
            }

            public override void VisitExprInValues(ExprInValues expr)
            {
                base.VisitExprInValues(expr);
                if (expr.TestExpression is ExprColumn column && expr.Items.Count > 0)
                {
                    var kind = InferKindFromValue(expr.Items[0]);
                    if (kind.HasValue)
                    {
                        this.AddKindHint(column, kind.Value);
                    }
                }
            }

            public override void VisitExprScalarFunction(ExprScalarFunction expr)
            {
                base.VisitExprScalarFunction(expr);
                if (expr.Arguments == null || expr.Arguments.Count < 1)
                {
                    return;
                }

                var fn = expr.Name.Name.ToUpperInvariant();
                if (fn == "ABS" || fn == "ROUND" || fn == "CEILING" || fn == "FLOOR")
                {
                    if (expr.Arguments[0] is ExprColumn col)
                    {
                        this.AddKindHint(col, ParamKind.Decimal);
                    }
                }
                else if ((fn == "LEN" || fn == "CHAR_LENGTH") && expr.Arguments[0] is ExprColumn col)
                {
                    this.AddKindHint(col, ParamKind.String);
                }
            }

            public override void VisitExprPortableScalarFunction(ExprPortableScalarFunction expr)
            {
                base.VisitExprPortableScalarFunction(expr);
                if (expr.Arguments == null || expr.Arguments.Count < 1 || expr.Arguments[0] is not ExprColumn col)
                {
                    return;
                }

                if (expr.PortableFunction == PortableScalarFunction.Len
                    || expr.PortableFunction == PortableScalarFunction.IndexOf
                    || expr.PortableFunction == PortableScalarFunction.Left
                    || expr.PortableFunction == PortableScalarFunction.Right
                    || expr.PortableFunction == PortableScalarFunction.Repeat)
                {
                    this.AddKindHint(col, ParamKind.String);
                }
            }

            public override void VisitExprDateAdd(ExprDateAdd expr)
            {
                base.VisitExprDateAdd(expr);
                if (expr.Date is ExprColumn col)
                {
                    this.AddKindHint(col, ParamKind.DateTime);
                }
            }

            public override void VisitExprDateDiff(ExprDateDiff expr)
            {
                base.VisitExprDateDiff(expr);
                if (expr.StartDate is ExprColumn start)
                {
                    this.AddKindHint(start, ParamKind.DateTime);
                }

                if (expr.EndDate is ExprColumn end)
                {
                    this.AddKindHint(end, ParamKind.DateTime);
                }
            }

            public override void VisitExprInsert(ExprInsert expr)
            {
                base.VisitExprInsert(expr);
                if (expr.TargetColumns == null || expr.TargetColumns.Count < 1)
                {
                    return;
                }

                var tableKey = GetTableKey(expr.Target.AsExprTableFullName());
                foreach (var column in expr.TargetColumns)
                {
                    this.AddColumnByTableKey(this.DiscoveredColumnsByTableKey, tableKey, column.Name);
                }
            }

            public override void VisitExprIdentityInsert(ExprIdentityInsert expr)
            {
                base.VisitExprIdentityInsert(expr);
                if (expr.Insert.TargetColumns == null || expr.Insert.TargetColumns.Count < 1)
                {
                    return;
                }

                var tableKey = GetTableKey(expr.Insert.Target.AsExprTableFullName());
                foreach (var column in expr.Insert.TargetColumns)
                {
                    this.AddColumnByTableKey(this.DiscoveredColumnsByTableKey, tableKey, column.Name);
                }
            }

            private void AddColumn(Dictionary<string, HashSet<string>> map, ExprColumn expr)
            {
                var tableKeys = this.ResolveTableKeys(expr);
                if (tableKeys == null)
                {
                    return;
                }

                foreach (var tableKey in tableKeys)
                {
                    if (!map.TryGetValue(tableKey, out var columns))
                    {
                        columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        map[tableKey] = columns;
                    }

                    columns.Add(expr.ColumnName.Name);
                }
            }

            private void AddColumnByTableKey(Dictionary<string, HashSet<string>> map, string tableKey, string columnName)
            {
                if (!map.TryGetValue(tableKey, out var columns))
                {
                    columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    map[tableKey] = columns;
                }

                columns.Add(columnName);
            }

            private IReadOnlyCollection<string>? ResolveTableKeys(ExprColumn expr)
            {
                if (expr.Source is ExprTableAlias sourceAlias)
                {
                    var aliasName = TryGetAliasName(sourceAlias.Alias);
                    if (!string.IsNullOrWhiteSpace(aliasName)
                        && this._aliasToTableKeys.TryGetValue(aliasName!, out var keysByAlias))
                    {
                        return keysByAlias;
                    }
                }

                if (!string.IsNullOrWhiteSpace(this._fallbackTableKey))
                {
                    return new[] { this._fallbackTableKey! };
                }

                return null;
            }

            private void AddKindHint(ExprColumn column, ParamKind kind)
            {
                var tableKeys = this.ResolveTableKeys(column);
                if (tableKeys == null)
                {
                    return;
                }

                foreach (var tableKey in tableKeys)
                {
                    if (!this.InferredColumnKindsByTableKey.TryGetValue(tableKey, out var map))
                    {
                        map = new Dictionary<string, ParamKind>(StringComparer.OrdinalIgnoreCase);
                        this.InferredColumnKindsByTableKey[tableKey] = map;
                    }

                    if (map.TryGetValue(column.ColumnName.Name, out var existing))
                    {
                        map[column.ColumnName.Name] = MergeColumnKind(existing, kind);
                    }
                    else
                    {
                        map[column.ColumnName.Name] = kind;
                    }
                }
            }

            private void HintFromComparison(ExprValue left, ExprValue right)
            {
                if (left is not ExprColumn leftColumn)
                {
                    return;
                }

                if (right is ExprCast cast)
                {
                    var castKind = InferKindFromSqlType(cast.SqlType);
                    if (castKind.HasValue)
                    {
                        this.AddKindHint(leftColumn, castKind.Value);
                    }

                    return;
                }

                var valueKind = InferKindFromValue(right);
                if (valueKind.HasValue)
                {
                    var kind = valueKind.Value;
                    if (kind == ParamKind.Int32 && InferKindFromColumnToken(leftColumn.ColumnName.Name) == ParamKind.Boolean)
                    {
                        kind = ParamKind.Boolean;
                    }

                    this.AddKindHint(leftColumn, kind);
                }
            }

            private static ParamKind MergeColumnKind(ParamKind existing, ParamKind hint)
            {
                if (existing == hint)
                {
                    return existing;
                }

                if (existing == ParamKind.String || hint == ParamKind.String)
                {
                    return ParamKind.String;
                }

                if ((existing == ParamKind.Int32 && hint == ParamKind.Decimal)
                    || (existing == ParamKind.Decimal && hint == ParamKind.Int32))
                {
                    return ParamKind.Decimal;
                }

                if ((existing == ParamKind.DateTime && hint == ParamKind.DateTimeOffset)
                    || (existing == ParamKind.DateTimeOffset && hint == ParamKind.DateTime))
                {
                    return ParamKind.DateTimeOffset;
                }

                return hint;
            }

            private static ParamKind? InferKindFromValue(ExprValue value)
            {
                switch (value)
                {
                    case ExprBoolLiteral:
                        return ParamKind.Boolean;
                    case ExprInt16Literal:
                    case ExprInt32Literal:
                    case ExprInt64Literal:
                    case ExprByteLiteral:
                        return ParamKind.Int32;
                    case ExprDecimalLiteral:
                    case ExprDoubleLiteral:
                        return ParamKind.Decimal;
                    case ExprGuidLiteral:
                        return ParamKind.Guid;
                    case ExprDateTimeLiteral:
                        return ParamKind.DateTime;
                    case ExprDateTimeOffsetLiteral:
                        return ParamKind.DateTimeOffset;
                    case ExprByteArrayLiteral:
                        return ParamKind.ByteArray;
                    case ExprStringLiteral:
                        return ParamKind.String;
                    default:
                        return null;
                }
            }

            private static ParamKind? InferKindFromSqlType(ExprType sqlType)
            {
                switch (sqlType)
                {
                    case ExprTypeBoolean:
                        return ParamKind.Boolean;
                    case ExprTypeByte:
                    case ExprTypeInt16:
                    case ExprTypeInt32:
                    case ExprTypeInt64:
                        return ParamKind.Int32;
                    case ExprTypeDecimal:
                    case ExprTypeDouble:
                        return ParamKind.Decimal;
                    case ExprTypeGuid:
                        return ParamKind.Guid;
                    case ExprTypeDateTime:
                        return ParamKind.DateTime;
                    case ExprTypeDateTimeOffset:
                        return ParamKind.DateTimeOffset;
                    case ExprTypeByteArray:
                    case ExprTypeFixSizeByteArray:
                        return ParamKind.ByteArray;
                    case ExprTypeString:
                    case ExprTypeFixSizeString:
                    case ExprTypeXml:
                        return ParamKind.String;
                    default:
                        return null;
                }
            }
        }

        private sealed class RawTableRef
        {
            public RawTableRef(string? schemaName, string tableName)
            {
                this.SchemaName = schemaName;
                this.TableName = tableName;
            }

            public string? SchemaName { get; }
            public string TableName { get; }
        }

        private enum SqlTokenKind
        {
            Word,
            Identifier,
            Literal,
            Separator,
            Other
        }

        private readonly struct SqlToken
        {
            public SqlToken(SqlTokenKind kind, string value)
            {
                this.Kind = kind;
                this.Value = value;
            }

            public SqlTokenKind Kind { get; }

            public string Value { get; }

            public bool IsWord(string value)
            {
                return this.Kind == SqlTokenKind.Word
                       && string.Equals(this.Value, value, StringComparison.OrdinalIgnoreCase);
            }

            public bool IsSeparator(string value)
            {
                return this.Kind == SqlTokenKind.Separator
                       && string.Equals(this.Value, value, StringComparison.Ordinal);
            }
        }

        private enum ParamKind
        {
            String,
            Int32,
            Decimal,
            Boolean,
            Guid,
            DateTime,
            DateTimeOffset,
            ByteArray
        }
    }
}
