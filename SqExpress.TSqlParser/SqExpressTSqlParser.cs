using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqExpress.DbMetadata;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.TSqlParser
{
    public sealed class SqExpressTSqlParser
    {
        private readonly Dictionary<TableIdentity, TableColumnMap> _inferredTableColumns =
            new Dictionary<TableIdentity, TableColumnMap>(TableIdentityComparer.Instance);

        private BuildPhase _buildPhase = BuildPhase.Emit;

        public bool TryParseScript(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out string? error)
            => this.TryParseScript(sql, out result, out _, out error);

        public bool TryParseScript(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(true)] out IReadOnlyList<SqTable>? tables,
            [NotNullWhen(false)] out string? error)
        {
            if (this.TrParseScript(sql, out result, out tables, out var errors))
            {
                error = null;
                return true;
            }

            tables = null;
            error = errors == null ? "Could not parse SQL." : string.Join(Environment.NewLine, errors);
            return false;
        }

        public bool TrParseScript(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
            => this.TrParseScript(sql, out result, out _, out errors);

        public bool TrParseScript(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(true)] out IReadOnlyList<SqTable>? tables,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (!this.TryParseSingleStatement(sql, out var statement, out errors))
            {
                result = null;
                tables = null;
                return false;
            }

            this.ResetInferredColumns();
            this._buildPhase = BuildPhase.Collect;
            this.TryBuildStructuredStatement(statement!, out _, out _);

            this._buildPhase = BuildPhase.Emit;
            if (this.TryBuildStructuredStatement(statement!, out var structuredResult, out errors))
            {
                result = structuredResult;
                tables = this.BuildSqTablesArtifact();
                return true;
            }

            result = null;
            tables = null;
            errors ??= new[] { $"SQL statement is not supported yet: {statement!.GetType().Name}" };
            return false;
        }

        private bool TryBuildStructuredStatement(
            TSqlStatement statement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            return statement switch
            {
                SelectStatement => this.TryBuildStructuredSelect(statement, out result, out errors),
                InsertStatement => this.TryBuildStructuredInsert(statement, out result, out errors),
                MergeStatement => this.TryBuildStructuredMerge(statement, out result, out errors),
                DeleteStatement => this.TryBuildStructuredDelete(statement, out result, out errors),
                UpdateStatement => this.TryBuildStructuredUpdate(statement, out result, out errors),
                _ => BuildUnsupportedStatement(out result, out errors)
            };
        }

        private static bool BuildUnsupportedStatement(
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            result = null;
            errors = new[] { "SQL statement is not supported yet." };
            return false;
        }

        public bool TryParseSingleStatement(
            string sql,
            [NotNullWhen(true)] out TSqlStatement? statement,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            try
            {
                statement = this.ParseSingleStatement(sql);
                errors = null;
                return true;
            }
            catch (SqExpressTSqlParserException ex)
            {
                statement = null;
                errors = new[] { ex.Message };
                return false;
            }
        }

        public bool TryFormatScript(
            string sql,
            [NotNullWhen(true)] out string? formattedSql,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            try
            {
                var script = this.ParseScriptDom(sql);
                var generator = new Sql160ScriptGenerator(
                    new SqlScriptGeneratorOptions
                    {
                        KeywordCasing = KeywordCasing.Uppercase
                    });
                generator.GenerateScript(script, out var generated);
                formattedSql = generated.Trim();
                errors = null;
                return true;
            }
            catch (SqExpressTSqlParserException ex)
            {
                formattedSql = null;
                errors = new[] { ex.Message };
                return false;
            }
        }

        private TSqlScript ParseScriptDom(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new SqExpressTSqlParserException("SQL text cannot be empty.");
            }

            var parser = new TSql160Parser(initialQuotedIdentifiers: true);
            using var reader = new StringReader(sql);
            var fragment = parser.Parse(reader, out var parserErrors);
            if (parserErrors.Count > 0)
            {
                var details = string.Join(
                    Environment.NewLine,
                    parserErrors.Select(e => $"({e.Line},{e.Column}) {e.Message}"));
                throw new SqExpressTSqlParserException($"Could not parse SQL:{Environment.NewLine}{details}");
            }

            if (fragment is not TSqlScript script)
            {
                throw new SqExpressTSqlParserException($"Unexpected parser root node: {fragment.GetType().Name}.");
            }

            return script;
        }

        private TSqlStatement ParseSingleStatement(string sql)
            => this.GetSingleStatement(this.ParseScriptDom(sql));

        private TSqlStatement GetSingleStatement(TSqlScript script)
        {
            if (script.Batches.Count != 1)
            {
                throw new SqExpressTSqlParserException("Only one SQL batch is supported.");
            }

            var batch = script.Batches[0];
            if (batch.Statements.Count != 1)
            {
                throw new SqExpressTSqlParserException("Only one SQL statement is supported.");
            }

            return batch.Statements[0];
        }

        private bool TryBuildStructuredSelect(
            TSqlStatement statement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (statement is not SelectStatement selectStatement)
            {
                result = null;
                errors = new[] { "Only SELECT statements are supported." };
                return false;
            }

            if (this.TryBuildSimpleStructuredSelect(selectStatement, out result, out errors))
            {
                return true;
            }

            return this.TryBuildGeneralStructuredSelect(selectStatement, out result, out errors);
        }

        private bool TryBuildSimpleStructuredSelect(
            SelectStatement selectStatement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (selectStatement.QueryExpression is not QuerySpecification querySpecification)
            {
                result = null;
                errors = new[] { "Only simple SELECT query specification is supported." };
                return false;
            }

            if (querySpecification.TopRowFilter != null
                || querySpecification.HavingClause != null
                || querySpecification.UniqueRowFilter != UniqueRowFilter.NotSpecified
                || querySpecification.WindowClause != null
                || querySpecification.OrderByClause != null
                || querySpecification.OffsetClause != null
                || querySpecification.ForClause != null)
            {
                result = null;
                errors = new[] { "Only SELECT without TOP/DISTINCT/HAVING/WINDOW/ORDER/OFFSET/FOR is supported." };
                return false;
            }

            if (querySpecification.FromClause == null || querySpecification.FromClause.TableReferences.Count != 1)
            {
                result = null;
                errors = new[] { "SELECT must contain exactly one FROM table reference." };
                return false;
            }

            if (querySpecification.FromClause.TableReferences[0] is not NamedTableReference namedTableReference)
            {
                result = null;
                errors = new[] { "Only named table references are supported in FROM clause." };
                return false;
            }

            if (querySpecification.WhereClause != null)
            {
                result = null;
                errors = new[] { "WHERE clause is not supported yet." };
                return false;
            }

            if (querySpecification.GroupByClause != null)
            {
                result = null;
                errors = new[] { "GROUP BY clause is not supported yet." };
                return false;
            }

            var tableSchemaObject = namedTableReference.SchemaObject;
            if (tableSchemaObject == null || tableSchemaObject.BaseIdentifier == null)
            {
                result = null;
                errors = new[] { "FROM table name is missing." };
                return false;
            }

            var tableName = tableSchemaObject.BaseIdentifier.Value;
            var schemaName = tableSchemaObject.SchemaIdentifier?.Value;
            if (string.IsNullOrWhiteSpace(schemaName)
                && tableSchemaObject.BaseIdentifier.QuoteType == QuoteType.NotQuoted)
            {
                schemaName = "dbo";
            }

            if (string.IsNullOrWhiteSpace(schemaName))
            {
                result = null;
                errors = new[] { "Simple SELECT path requires schema-qualified or unquoted table name." };
                return false;
            }

            var tableAliasName = namedTableReference.Alias?.Value;

            var projectedColumns = new List<ProjectedColumn>();
            foreach (var selectElement in querySpecification.SelectElements)
            {
                if (selectElement is not SelectScalarExpression scalarExpression)
                {
                    result = null;
                    errors = new[] { "Only scalar column projections are supported in SELECT list." };
                    return false;
                }

                if (scalarExpression.Expression is not ColumnReferenceExpression columnReference
                    || columnReference.ColumnType == ColumnType.Wildcard
                    || columnReference.MultiPartIdentifier?.Identifiers == null
                    || columnReference.MultiPartIdentifier.Identifiers.Count < 1
                    || columnReference.MultiPartIdentifier.Identifiers.Count > 2)
                {
                    result = null;
                    errors = new[] { "Only column references in SELECT list are supported." };
                    return false;
                }

                string? sourceAlias = null;
                string sourceColumnName;
                if (columnReference.MultiPartIdentifier.Identifiers.Count == 2)
                {
                    sourceAlias = columnReference.MultiPartIdentifier.Identifiers[0].Value;
                    sourceColumnName = columnReference.MultiPartIdentifier.Identifiers[1].Value;
                }
                else
                {
                    sourceColumnName = columnReference.MultiPartIdentifier.Identifiers[0].Value;
                }

                if (!string.IsNullOrWhiteSpace(sourceAlias) && !string.IsNullOrWhiteSpace(tableAliasName)
                    && !string.Equals(sourceAlias, tableAliasName, StringComparison.OrdinalIgnoreCase))
                {
                    result = null;
                    errors = new[] { $"Unknown table alias '{sourceAlias}' in SELECT projection." };
                    return false;
                }

                var outputAlias = scalarExpression.ColumnName?.Value;
                projectedColumns.Add(new ProjectedColumn(sourceColumnName, outputAlias));
            }

            var uniqueColumnNames = projectedColumns
                .Select(c => c.SourceColumnName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tableIdentity = new TableIdentity(tableSchemaObject.DatabaseIdentifier?.Value, schemaName, tableName);
            foreach (var columnName in uniqueColumnNames)
            {
                this.RegisterColumnHint(tableIdentity, columnName, InferredColumnKind.Int32);
            }

            if (this._buildPhase == BuildPhase.Collect)
            {
                result = SqQueryBuilder.SelectOne().Done();
                errors = null;
                return true;
            }

            var fullName = new ExprTableFullName(
                schemaName == null ? null : new ExprDbSchema(null, new ExprSchemaName(schemaName)),
                new ExprTableName(tableName));
            var alias = string.IsNullOrWhiteSpace(tableAliasName)
                ? null
                : new ExprTableAlias(new ExprAlias(tableAliasName!));
            var table = new ExprTable(fullName, alias);
            IExprColumnSource source = alias != null ? alias : fullName;

            var selectList = new List<IExprSelecting>(projectedColumns.Count);
            foreach (var projectedColumn in projectedColumns)
            {
                var tableColumn = new ExprColumn(source, new ExprColumnName(projectedColumn.SourceColumnName));
                if (!string.IsNullOrWhiteSpace(projectedColumn.OutputAlias)
                    && !string.Equals(projectedColumn.OutputAlias, projectedColumn.SourceColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    selectList.Add(tableColumn.As(projectedColumn.OutputAlias!));
                }
                else
                {
                    selectList.Add(tableColumn);
                }
            }

            result = SqQueryBuilder.Select(selectList).From(table).Done();
            errors = null;
            return true;
        }

        private bool TryBuildGeneralStructuredSelect(
            SelectStatement selectStatement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (selectStatement.Into != null)
            {
                result = null;
                errors = new[] { "SELECT INTO is not supported yet." };
                return false;
            }

            var cteRegistry = new Dictionary<string, CteRegistryEntry>(StringComparer.OrdinalIgnoreCase);
            if (!this.TryRegisterCtes(selectStatement.WithCtesAndXmlNamespaces, cteRegistry, out errors))
            {
                result = null;
                return false;
            }

            var rootContext = new SelectParseContext(parent: null, cteRegistry);
            if (!this.TryBuildTopLevelSelectExpression(
                    selectStatement.QueryExpression,
                    rootContext,
                    out var query,
                    out errors))
            {
                result = null;
                return false;
            }

            result = query;
            errors = null;
            return true;
        }

        private bool TryBuildTopLevelSelectExpression(
            QueryExpression queryExpression,
            SelectParseContext context,
            [NotNullWhen(true)] out IExpr? query,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (queryExpression is QueryParenthesisExpression queryParenthesis)
            {
                return this.TryBuildTopLevelSelectExpression(queryParenthesis.QueryExpression, context, out query, out errors);
            }

            if (queryExpression is QuerySpecification specification)
            {
                if (!this.TryBuildSelectQuerySpecification(specification, context, out var baseQuery, out errors))
                {
                    query = null;
                    return false;
                }

                return this.TryApplyTopLevelOrderByOffsetFetch(
                    baseQuery!,
                    specification.OrderByClause,
                    specification.OffsetClause,
                    context,
                    out query,
                    out errors);
            }

            if (queryExpression is BinaryQueryExpression binaryQuery)
            {
                if (!this.TryBuildSelectQueryExpression(
                        binaryQuery.FirstQueryExpression,
                        context,
                        out var left,
                        out errors,
                        allowOrderByAndOffset: false))
                {
                    query = null;
                    return false;
                }

                if (!this.TryBuildSelectQueryExpression(
                        binaryQuery.SecondQueryExpression,
                        context,
                        out var right,
                        out errors,
                        allowOrderByAndOffset: false))
                {
                    query = null;
                    return false;
                }

                ExprQueryExpressionType queryType;
                switch (binaryQuery.BinaryQueryExpressionType)
                {
                    case BinaryQueryExpressionType.Union:
                        queryType = binaryQuery.All ? ExprQueryExpressionType.UnionAll : ExprQueryExpressionType.Union;
                        break;
                    case BinaryQueryExpressionType.Except:
                        if (binaryQuery.All)
                        {
                            query = null;
                            errors = new[] { "EXCEPT ALL is not supported yet." };
                            return false;
                        }

                        queryType = ExprQueryExpressionType.Except;
                        break;
                    case BinaryQueryExpressionType.Intersect:
                        if (binaryQuery.All)
                        {
                            query = null;
                            errors = new[] { "INTERSECT ALL is not supported yet." };
                            return false;
                        }

                        queryType = ExprQueryExpressionType.Intersect;
                        break;
                    default:
                        query = null;
                        errors = new[] { $"Unsupported set operation: {binaryQuery.BinaryQueryExpressionType}." };
                        return false;
                }

                var baseQuery = new ExprQueryExpression(left!, right!, queryType);
                return this.TryApplyTopLevelOrderByOffsetFetch(
                    baseQuery,
                    binaryQuery.OrderByClause,
                    binaryQuery.OffsetClause,
                    context,
                    out query,
                    out errors);
            }

            query = null;
            errors = new[] { $"Unsupported query expression: {queryExpression.GetType().Name}." };
            return false;
        }

        private bool TryRegisterCtes(
            WithCtesAndXmlNamespaces? withClause,
            Dictionary<string, CteRegistryEntry> cteRegistry,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            errors = null;
            if (withClause == null)
            {
                return true;
            }

            if (withClause.ChangeTrackingContext != null || withClause.XmlNamespaces != null)
            {
                errors = new[] { "WITH XMLNAMESPACES/CHANGE_TRACKING_CONTEXT is not supported yet." };
                return false;
            }

            foreach (var cte in withClause.CommonTableExpressions)
            {
                var cteName = cte.ExpressionName?.Value;
                if (string.IsNullOrWhiteSpace(cteName))
                {
                    errors = new[] { "CTE name is missing." };
                    return false;
                }

                if (cteRegistry.ContainsKey(cteName!))
                {
                    errors = new[] { $"Duplicate CTE name '{cteName}'." };
                    return false;
                }

                cteRegistry[cteName!] = new CteRegistryEntry(cteName!);
            }

            var cteBuildContext = new SelectParseContext(parent: null, cteRegistry);
            foreach (var cte in withClause.CommonTableExpressions)
            {
                var cteName = cte.ExpressionName!.Value;
                if (cte.Columns != null && cte.Columns.Count > 0)
                {
                    errors = new[] { "CTE column list is not supported yet." };
                    return false;
                }

                var cteContext = cteBuildContext.CreateChild();
                if (!this.TryBuildSelectQueryExpression(cte.QueryExpression, cteContext, out var cteQuery, out errors))
                {
                    return false;
                }

                cteRegistry[cteName].Query = cteQuery;
            }

            return true;
        }

        private bool TryBuildSelectQueryExpression(
            QueryExpression queryExpression,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprSubQuery? query,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors,
            bool allowOrderByAndOffset = true)
        {
            if (queryExpression is QueryParenthesisExpression queryParenthesis)
            {
                return this.TryBuildSelectQueryExpression(
                    queryParenthesis.QueryExpression,
                    context,
                    out query,
                    out errors,
                    allowOrderByAndOffset);
            }

            if (queryExpression is QuerySpecification specification)
            {
                if (!allowOrderByAndOffset
                    && (specification.OrderByClause != null || specification.OffsetClause != null))
                {
                    query = null;
                    errors = new[] { "ORDER BY/OFFSET is not supported inside set-operation operands." };
                    return false;
                }

                if (!this.TryBuildSelectQuerySpecification(specification, context, out var baseQuery, out errors))
                {
                    query = null;
                    return false;
                }

                return this.TryApplyOrderByOffsetFetch(
                    baseQuery!,
                    specification.OrderByClause,
                    specification.OffsetClause,
                    context,
                    out query,
                    out errors);
            }

            if (queryExpression is BinaryQueryExpression binaryQuery)
            {
                if (!this.TryBuildSelectQueryExpression(
                        binaryQuery.FirstQueryExpression,
                        context,
                        out var left,
                        out errors,
                        allowOrderByAndOffset: false))
                {
                    query = null;
                    return false;
                }

                if (!this.TryBuildSelectQueryExpression(
                        binaryQuery.SecondQueryExpression,
                        context,
                        out var right,
                        out errors,
                        allowOrderByAndOffset: false))
                {
                    query = null;
                    return false;
                }

                ExprQueryExpressionType queryType;
                switch (binaryQuery.BinaryQueryExpressionType)
                {
                    case BinaryQueryExpressionType.Union:
                        queryType = binaryQuery.All ? ExprQueryExpressionType.UnionAll : ExprQueryExpressionType.Union;
                        break;
                    case BinaryQueryExpressionType.Except:
                        if (binaryQuery.All)
                        {
                            query = null;
                            errors = new[] { "EXCEPT ALL is not supported yet." };
                            return false;
                        }

                        queryType = ExprQueryExpressionType.Except;
                        break;
                    case BinaryQueryExpressionType.Intersect:
                        if (binaryQuery.All)
                        {
                            query = null;
                            errors = new[] { "INTERSECT ALL is not supported yet." };
                            return false;
                        }

                        queryType = ExprQueryExpressionType.Intersect;
                        break;
                    default:
                        query = null;
                        errors = new[] { $"Unsupported set operation: {binaryQuery.BinaryQueryExpressionType}." };
                        return false;
                }

                var binaryExpr = new ExprQueryExpression(left!, right!, queryType);
                if (!allowOrderByAndOffset
                    && (binaryQuery.OrderByClause != null || binaryQuery.OffsetClause != null))
                {
                    query = null;
                    errors = new[] { "ORDER BY/OFFSET is not supported inside nested set-operation operands." };
                    return false;
                }

                return this.TryApplyOrderByOffsetFetch(
                    binaryExpr,
                    binaryQuery.OrderByClause,
                    binaryQuery.OffsetClause,
                    context,
                    out query,
                    out errors);
            }

            query = null;
            errors = new[] { $"Unsupported query expression: {queryExpression.GetType().Name}." };
            return false;
        }

        private bool TryBuildSelectQuerySpecification(
            QuerySpecification specification,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprSubQuery? query,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (specification.WindowClause != null)
            {
                query = null;
                errors = new[] { "WINDOW clause is not supported yet." };
                return false;
            }

            if (specification.HavingClause != null)
            {
                query = null;
                errors = new[] { "HAVING clause is not supported yet." };
                return false;
            }

            if (specification.TopRowFilter?.Percent == true || specification.TopRowFilter?.WithTies == true)
            {
                query = null;
                errors = new[] { "TOP PERCENT/WITH TIES is not supported yet." };
                return false;
            }

            ExprValue? top = null;
            if (specification.TopRowFilter != null)
            {
                if (!this.TryBuildSelectValueExpression(
                        specification.TopRowFilter.Expression,
                        context,
                        out top,
                        out errors))
                {
                    query = null;
                    return false;
                }
            }

            IExprTableSource? from = null;
            if (specification.FromClause != null)
            {
                if (specification.FromClause.TableReferences.Count < 1)
                {
                    query = null;
                    errors = new[] { "FROM cannot be empty." };
                    return false;
                }

                if (!this.TryBuildSelectTableSource(
                        specification.FromClause.TableReferences[0],
                        context,
                        out from,
                        out errors))
                {
                    query = null;
                    return false;
                }

                for (var i = 1; i < specification.FromClause.TableReferences.Count; i++)
                {
                    if (!this.TryBuildSelectTableSource(
                            specification.FromClause.TableReferences[i],
                            context,
                            out var nextTable,
                            out errors))
                    {
                        query = null;
                        return false;
                    }

                    from = new ExprCrossedTable(from!, nextTable!);
                }
            }

            ExprBoolean? where = null;
            if (specification.WhereClause != null)
            {
                if (!this.TryBuildSelectBooleanExpression(
                        specification.WhereClause.SearchCondition,
                        context,
                        out where,
                        out errors))
                {
                    query = null;
                    return false;
                }
            }

            IReadOnlyList<ExprColumn>? groupBy = null;
            if (specification.GroupByClause != null)
            {
                var columns = new List<ExprColumn>(specification.GroupByClause.GroupingSpecifications.Count);
                foreach (var grouping in specification.GroupByClause.GroupingSpecifications)
                {
                    if (grouping is not ExpressionGroupingSpecification expressionGrouping)
                    {
                        query = null;
                        errors = new[] { $"Unsupported GROUP BY item: {grouping.GetType().Name}." };
                        return false;
                    }

                    if (!this.TryBuildSelectValueExpression(
                            expressionGrouping.Expression,
                            context,
                            out var groupingExpr,
                            out errors))
                    {
                        query = null;
                        return false;
                    }

                    if (groupingExpr is not ExprColumn groupingColumn)
                    {
                        query = null;
                        errors = new[] { "GROUP BY supports only column references for now." };
                        return false;
                    }

                    columns.Add(groupingColumn);
                }

                groupBy = columns;
            }

            var selectList = new List<IExprSelecting>(specification.SelectElements.Count);
            foreach (var selectElement in specification.SelectElements)
            {
                if (!this.TryBuildSelectElement(selectElement, context, out var selecting, out errors))
                {
                    query = null;
                    return false;
                }

                selectList.Add(selecting!);
            }

            if (selectList.Count < 1)
            {
                query = null;
                errors = new[] { "SELECT list cannot be empty." };
                return false;
            }

            query = new ExprQuerySpecification(
                selectList,
                top,
                specification.UniqueRowFilter == UniqueRowFilter.Distinct,
                from,
                where,
                groupBy);
            errors = null;
            return true;
        }

        private bool TryApplyOrderByOffsetFetch(
            IExprSubQuery baseQuery,
            OrderByClause? orderByClause,
            OffsetClause? offsetClause,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprSubQuery? query,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            query = baseQuery;
            errors = null;

            List<ExprOrderByItem>? orderItems = null;
            if (orderByClause != null)
            {
                if (orderByClause.OrderByElements.Count < 1)
                {
                    errors = new[] { "ORDER BY cannot be empty." };
                    query = null;
                    return false;
                }

                orderItems = new List<ExprOrderByItem>(orderByClause.OrderByElements.Count);
                foreach (var item in orderByClause.OrderByElements)
                {
                    if (!this.TryBuildOrderByItem(item, context, out var orderItem, out errors))
                    {
                        query = null;
                        return false;
                    }

                    orderItems.Add(orderItem!);
                }
            }

            if (offsetClause != null)
            {
                if (orderItems == null)
                {
                    query = null;
                    errors = new[] { "OFFSET/FETCH requires ORDER BY." };
                    return false;
                }

                if (!TryExtractInt32Value(offsetClause.OffsetExpression, out var offset))
                {
                    query = null;
                    errors = new[] { "OFFSET must be an Int32 literal." };
                    return false;
                }

                int? fetch = null;
                if (offsetClause.FetchExpression != null)
                {
                    if (!TryExtractInt32Value(offsetClause.FetchExpression, out var fetchValue))
                    {
                        query = null;
                        errors = new[] { "FETCH NEXT must be an Int32 literal." };
                        return false;
                    }

                    fetch = fetchValue;
                }

                query = new ExprSelectOffsetFetch(
                    baseQuery,
                    new ExprOrderByOffsetFetch(
                        orderItems,
                        new ExprOffsetFetch(
                            new ExprInt32Literal(offset),
                            fetch.HasValue ? new ExprInt32Literal(fetch.Value) : null)));
                errors = null;
                return true;
            }

            if (orderItems != null)
            {
                query = null;
                errors = new[] { "ORDER BY without OFFSET/FETCH is not supported in sub-query mode yet." };
                return false;
            }

            errors = null;
            return true;
        }

        private bool TryApplyTopLevelOrderByOffsetFetch(
            IExprSubQuery baseQuery,
            OrderByClause? orderByClause,
            OffsetClause? offsetClause,
            SelectParseContext context,
            [NotNullWhen(true)] out IExpr? query,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            query = baseQuery;
            errors = null;

            List<ExprOrderByItem>? orderItems = null;
            if (orderByClause != null)
            {
                if (orderByClause.OrderByElements.Count < 1)
                {
                    errors = new[] { "ORDER BY cannot be empty." };
                    query = null;
                    return false;
                }

                orderItems = new List<ExprOrderByItem>(orderByClause.OrderByElements.Count);
                foreach (var item in orderByClause.OrderByElements)
                {
                    if (!this.TryBuildOrderByItem(item, context, out var orderItem, out errors))
                    {
                        query = null;
                        return false;
                    }

                    orderItems.Add(orderItem!);
                }
            }

            if (offsetClause != null)
            {
                if (orderItems == null)
                {
                    query = null;
                    errors = new[] { "OFFSET/FETCH requires ORDER BY." };
                    return false;
                }

                if (!TryExtractInt32Value(offsetClause.OffsetExpression, out var offset))
                {
                    query = null;
                    errors = new[] { "OFFSET must be an Int32 literal." };
                    return false;
                }

                int? fetch = null;
                if (offsetClause.FetchExpression != null)
                {
                    if (!TryExtractInt32Value(offsetClause.FetchExpression, out var fetchValue))
                    {
                        query = null;
                        errors = new[] { "FETCH NEXT must be an Int32 literal." };
                        return false;
                    }

                    fetch = fetchValue;
                }

                query = new ExprSelectOffsetFetch(
                    baseQuery,
                    new ExprOrderByOffsetFetch(
                        orderItems,
                        new ExprOffsetFetch(
                            new ExprInt32Literal(offset),
                            fetch.HasValue ? new ExprInt32Literal(fetch.Value) : null)));
                errors = null;
                return true;
            }

            if (orderItems != null)
            {
                query = new ExprSelect(baseQuery, new ExprOrderBy(orderItems));
            }

            errors = null;
            return true;
        }

        private bool TryBuildOrderByItem(
            ExpressionWithSortOrder orderByItem,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprOrderByItem? item,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (!this.TryBuildSelectValueExpression(orderByItem.Expression, context, out var value, out errors))
            {
                item = null;
                return false;
            }

            item = new ExprOrderByItem(
                value!,
                orderByItem.SortOrder == SortOrder.Descending);
            errors = null;
            return true;
        }

        private bool TryBuildSelectElement(
            SelectElement selectElement,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprSelecting? selecting,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (selectElement is SelectScalarExpression scalar)
            {
                if (!this.TryBuildSelectingExpression(scalar.Expression, context, out selecting, out errors))
                {
                    return false;
                }

                var outputAlias = scalar.ColumnName?.Value;
                if (!string.IsNullOrWhiteSpace(outputAlias))
                {
                    selecting = selecting!.As(outputAlias!);
                }

                errors = null;
                return true;
            }

            if (selectElement is SelectStarExpression star)
            {
                if (star.Qualifier == null || star.Qualifier.Identifiers.Count == 0)
                {
                    selecting = new ExprAllColumns(null);
                    errors = null;
                    return true;
                }

                if (star.Qualifier.Identifiers.Count != 1)
                {
                    selecting = null;
                    errors = new[] { "Only one-part qualifier is supported for SELECT *." };
                    return false;
                }

                var qualifier = star.Qualifier.Identifiers[0].Value;
                if (!context.TryResolveSource(qualifier, out var source))
                {
                    selecting = null;
                    errors = new[] { $"Unknown source '{qualifier}' in SELECT *." };
                    return false;
                }

                selecting = new ExprAllColumns(source);
                errors = null;
                return true;
            }

            selecting = null;
            errors = new[] { $"Unsupported SELECT element: {selectElement.GetType().Name}." };
            return false;
        }

        private bool TryBuildSelectingExpression(
            ScalarExpression expression,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprSelecting? selecting,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (expression is FunctionCall functionCall)
            {
                var functionName = functionCall.FunctionName?.Value;
                var normalizedName = functionName?.ToUpperInvariant();
                var isAggregate = !string.IsNullOrWhiteSpace(normalizedName)
                    && IsAggregateFunctionName(normalizedName!);

                if (functionCall.OverClause != null || isAggregate || functionCall.UniqueRowFilter == UniqueRowFilter.Distinct)
                {
                    if (!this.TryBuildFunctionSelecting(functionCall, context, out selecting, out errors))
                    {
                        return false;
                    }

                    return true;
                }
            }

            if (!this.TryBuildSelectValueExpression(expression, context, out var value, out errors))
            {
                selecting = null;
                return false;
            }

            selecting = value;
            return true;
        }

        private bool TryBuildFunctionSelecting(
            FunctionCall functionCall,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprSelecting? selecting,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            var functionName = functionCall.FunctionName?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                selecting = null;
                errors = new[] { "Function name is missing." };
                return false;
            }

            var normalizedName = functionName!.ToUpperInvariant();
            var isAggregate = IsAggregateFunctionName(normalizedName)
                              || functionCall.UniqueRowFilter == UniqueRowFilter.Distinct;

            if (isAggregate)
            {
                if (!this.TryBuildAggregateSelecting(functionCall, context, out var aggregate, out errors))
                {
                    selecting = null;
                    return false;
                }

                if (functionCall.OverClause == null)
                {
                    selecting = aggregate;
                    errors = null;
                    return true;
                }

                if (!this.TryBuildOverClause(functionCall.OverClause, context, out var over, out errors))
                {
                    selecting = null;
                    return false;
                }

                selecting = new ExprAggregateOverFunction(aggregate!, over!);
                errors = null;
                return true;
            }

            if (functionCall.OverClause != null)
            {
                if (!this.TryBuildOverClause(functionCall.OverClause, context, out var over, out errors))
                {
                    selecting = null;
                    return false;
                }

                var arguments = new List<ExprValue>(functionCall.Parameters.Count);
                foreach (var parameter in functionCall.Parameters)
                {
                    if (IsStarExpression(parameter))
                    {
                        selecting = null;
                        errors = new[] { $"Function '{functionName}' does not support '*' argument here." };
                        return false;
                    }

                    if (!this.TryBuildSelectValueExpression(parameter, context, out var argument, out errors))
                    {
                        selecting = null;
                        return false;
                    }

                    arguments.Add(argument!);
                }

                selecting = new ExprAnalyticFunction(
                    new ExprFunctionName(builtIn: true, functionName),
                    arguments.Count == 0 ? null : arguments,
                    over!);
                errors = null;
                return true;
            }

            if (!this.TryBuildSelectFunctionValue(functionCall, context, out var scalarFunction, out errors))
            {
                selecting = null;
                return false;
            }

            selecting = scalarFunction;
            return true;
        }

        private bool TryBuildAggregateSelecting(
            FunctionCall functionCall,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprAggregateFunction? aggregate,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            var functionName = functionCall.FunctionName?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                aggregate = null;
                errors = new[] { "Aggregate function name is missing." };
                return false;
            }

            var normalizedName = functionName!.ToUpperInvariant();
            var distinct = functionCall.UniqueRowFilter == UniqueRowFilter.Distinct;

            if (normalizedName == "COUNT"
                && functionCall.Parameters.Count == 1
                && IsStarExpression(functionCall.Parameters[0]))
            {
                if (distinct)
                {
                    aggregate = null;
                    errors = new[] { "COUNT(DISTINCT *) is not supported." };
                    return false;
                }

                aggregate = SqQueryBuilder.CountOne();
                errors = null;
                return true;
            }

            if (functionCall.Parameters.Count != 1)
            {
                aggregate = null;
                errors = new[] { $"Aggregate function '{functionName}' supports exactly one argument." };
                return false;
            }

            if (IsStarExpression(functionCall.Parameters[0]))
            {
                aggregate = null;
                errors = new[] { $"Aggregate function '{functionName}' does not support '*' argument." };
                return false;
            }

            if (!this.TryBuildSelectValueExpression(functionCall.Parameters[0], context, out var argument, out errors))
            {
                aggregate = null;
                return false;
            }

            aggregate = normalizedName switch
            {
                "COUNT" => distinct ? SqQueryBuilder.CountDistinct(argument!) : SqQueryBuilder.Count(argument!),
                "MIN" => distinct ? SqQueryBuilder.MinDistinct(argument!) : SqQueryBuilder.Min(argument!),
                "MAX" => distinct ? SqQueryBuilder.MaxDistinct(argument!) : SqQueryBuilder.Max(argument!),
                "SUM" => distinct ? SqQueryBuilder.SumDistinct(argument!) : SqQueryBuilder.Sum(argument!),
                "AVG" => distinct ? SqQueryBuilder.AvgDistinct(argument!) : SqQueryBuilder.Avg(argument!),
                _ => SqQueryBuilder.AggregateFunction(functionName, distinct, argument!)
            };
            errors = null;
            return true;
        }

        private bool TryBuildOverClause(
            OverClause overClause,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprOver? over,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (overClause.WindowName != null)
            {
                over = null;
                errors = new[] { "Named windows in OVER clause are not supported yet." };
                return false;
            }

            var partitions = new List<ExprValue>(overClause.Partitions.Count);
            foreach (var partition in overClause.Partitions)
            {
                if (!this.TryBuildSelectValueExpression(partition, context, out var partitionExpr, out errors))
                {
                    over = null;
                    return false;
                }

                partitions.Add(partitionExpr!);
            }

            ExprOrderBy? orderBy = null;
            if (overClause.OrderByClause != null)
            {
                if (overClause.OrderByClause.OrderByElements.Count < 1)
                {
                    over = null;
                    errors = new[] { "OVER ORDER BY cannot be empty." };
                    return false;
                }

                var orderItems = new List<ExprOrderByItem>(overClause.OrderByClause.OrderByElements.Count);
                foreach (var orderByItem in overClause.OrderByClause.OrderByElements)
                {
                    if (!this.TryBuildOrderByItem(orderByItem, context, out var item, out errors))
                    {
                        over = null;
                        return false;
                    }

                    orderItems.Add(item!);
                }

                orderBy = new ExprOrderBy(orderItems);
            }

            ExprFrameClause? frameClause = null;
            if (overClause.WindowFrameClause != null)
            {
                if (!this.TryBuildFrameClause(overClause.WindowFrameClause, context, out frameClause, out errors))
                {
                    over = null;
                    return false;
                }
            }

            over = new ExprOver(partitions.Count == 0 ? null : partitions, orderBy, frameClause);
            errors = null;
            return true;
        }

        private bool TryBuildFrameClause(
            WindowFrameClause frame,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprFrameClause? frameClause,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (frame.WindowFrameType == WindowFrameType.Range)
            {
                frameClause = null;
                errors = new[] { "RANGE frame clause is not supported yet." };
                return false;
            }

            if (!this.TryBuildFrameBorder(frame.Top, context, out var start, out errors))
            {
                frameClause = null;
                return false;
            }

            ExprFrameBorder? end = null;
            if (frame.Bottom != null)
            {
                if (!this.TryBuildFrameBorder(frame.Bottom, context, out end, out errors))
                {
                    frameClause = null;
                    return false;
                }
            }

            frameClause = new ExprFrameClause(start!, end);
            errors = null;
            return true;
        }

        private bool TryBuildFrameBorder(
            WindowDelimiter delimiter,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprFrameBorder? border,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            switch (delimiter.WindowDelimiterType)
            {
                case WindowDelimiterType.CurrentRow:
                    border = ExprCurrentRowFrameBorder.Instance;
                    errors = null;
                    return true;
                case WindowDelimiterType.UnboundedPreceding:
                    border = new ExprUnboundedFrameBorder(FrameBorderDirection.Preceding);
                    errors = null;
                    return true;
                case WindowDelimiterType.UnboundedFollowing:
                    border = new ExprUnboundedFrameBorder(FrameBorderDirection.Following);
                    errors = null;
                    return true;
                case WindowDelimiterType.ValuePreceding:
                case WindowDelimiterType.ValueFollowing:
                    if (delimiter.OffsetValue == null)
                    {
                        border = null;
                        errors = new[] { "Frame border offset value is missing." };
                        return false;
                    }

                    if (!this.TryBuildSelectValueExpression(delimiter.OffsetValue, context, out var value, out errors))
                    {
                        border = null;
                        return false;
                    }

                    border = new ExprValueFrameBorder(
                        value!,
                        delimiter.WindowDelimiterType == WindowDelimiterType.ValuePreceding
                            ? FrameBorderDirection.Preceding
                            : FrameBorderDirection.Following);
                    errors = null;
                    return true;
            }

            border = null;
            errors = new[] { $"Unsupported window frame border: {delimiter.WindowDelimiterType}." };
            return false;
        }

        private bool TryBuildSelectTableSource(
            TableReference tableReference,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (tableReference is NamedTableReference namedTableReference)
            {
                return this.TryBuildSelectNamedTableSource(namedTableReference, context, out source, out errors);
            }

            if (tableReference is QueryDerivedTable queryDerivedTable)
            {
                return this.TryBuildSelectDerivedTableSource(queryDerivedTable, context, out source, out errors);
            }

            if (tableReference is InlineDerivedTable inlineDerivedTable)
            {
                return this.TryBuildSelectInlineDerivedTableSource(inlineDerivedTable, context, out source, out errors);
            }

            if (tableReference is SchemaObjectFunctionTableReference schemaFunctionTableReference)
            {
                return this.TryBuildSelectSchemaFunctionTableSource(schemaFunctionTableReference, context, out source, out errors);
            }

            if (tableReference is BuiltInFunctionTableReference builtInFunctionTableReference)
            {
                return this.TryBuildSelectBuiltInFunctionTableSource(builtInFunctionTableReference, context, out source, out errors);
            }

            if (tableReference is GlobalFunctionTableReference globalFunctionTableReference)
            {
                return this.TryBuildSelectGlobalFunctionTableSource(globalFunctionTableReference, context, out source, out errors);
            }

            if (tableReference is JoinParenthesisTableReference joinParenthesisTableReference)
            {
                return this.TryBuildSelectTableSource(joinParenthesisTableReference.Join, context, out source, out errors);
            }

            if (tableReference is QualifiedJoin qualifiedJoin)
            {
                if (!this.TryBuildSelectTableSource(qualifiedJoin.FirstTableReference, context, out var left, out errors))
                {
                    source = null;
                    return false;
                }

                if (!this.TryBuildSelectTableSource(qualifiedJoin.SecondTableReference, context, out var right, out errors))
                {
                    source = null;
                    return false;
                }

                if (!this.TryBuildSelectBooleanExpression(qualifiedJoin.SearchCondition, context, out var searchCondition, out errors))
                {
                    source = null;
                    return false;
                }

                ExprJoinedTable.ExprJoinType joinType;
                switch (qualifiedJoin.QualifiedJoinType)
                {
                    case QualifiedJoinType.Inner:
                        joinType = ExprJoinedTable.ExprJoinType.Inner;
                        break;
                    case QualifiedJoinType.LeftOuter:
                        joinType = ExprJoinedTable.ExprJoinType.Left;
                        break;
                    case QualifiedJoinType.RightOuter:
                        joinType = ExprJoinedTable.ExprJoinType.Right;
                        break;
                    case QualifiedJoinType.FullOuter:
                        joinType = ExprJoinedTable.ExprJoinType.Full;
                        break;
                    default:
                        source = null;
                        errors = new[] { $"Unsupported join type: {qualifiedJoin.QualifiedJoinType}." };
                        return false;
                }

                source = new ExprJoinedTable(left!, joinType, right!, searchCondition!);
                errors = null;
                return true;
            }

            if (tableReference is UnqualifiedJoin unqualifiedJoin)
            {
                if (!this.TryBuildSelectTableSource(unqualifiedJoin.FirstTableReference, context, out var left, out errors))
                {
                    source = null;
                    return false;
                }

                if (!this.TryBuildSelectTableSource(unqualifiedJoin.SecondTableReference, context, out var right, out errors))
                {
                    source = null;
                    return false;
                }

                source = unqualifiedJoin.UnqualifiedJoinType switch
                {
                    UnqualifiedJoinType.CrossJoin => new ExprCrossedTable(left!, right!),
                    UnqualifiedJoinType.CrossApply => new ExprLateralCrossedTable(left!, right!, outer: false),
                    UnqualifiedJoinType.OuterApply => new ExprLateralCrossedTable(left!, right!, outer: true),
                    _ => null
                };

                if (source == null)
                {
                    errors = new[] { $"Unsupported unqualified join type: {unqualifiedJoin.UnqualifiedJoinType}." };
                    return false;
                }

                errors = null;
                return true;
            }

            source = null;
            errors = new[] { $"Unsupported table reference: {tableReference.GetType().Name}." };
            return false;
        }

        private bool TryBuildSelectNamedTableSource(
            NamedTableReference tableReference,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            var schemaObject = tableReference.SchemaObject;
            if (schemaObject?.BaseIdentifier == null)
            {
                source = null;
                errors = new[] { "Named table reference is missing object name." };
                return false;
            }

            var objectName = schemaObject.BaseIdentifier.Value;
            var aliasName = tableReference.Alias?.Value;
            var alias = aliasName == null ? null : new ExprTableAlias(new ExprAlias(aliasName));

            if (schemaObject.SchemaIdentifier == null
                && schemaObject.DatabaseIdentifier == null
                && context.TryGetCte(objectName, out var cteEntry))
            {
                source = new DeferredCte(cteEntry!, alias);
                var cteColumnSource = alias ?? new ExprTableAlias(new ExprAlias(cteEntry!.Name));
                context.RegisterSource(cteColumnSource, aliasName, cteEntry!.Name, tableIdentity: null);
                errors = null;
                return true;
            }

            if (!this.TryBuildExprTable(tableReference, out var table, out errors))
            {
                source = null;
                return false;
            }

            source = table;
            var columnSource = table!.Alias != null
                ? (IExprColumnSource)table.Alias
                : table.FullName;
            var tableName = table.FullName.AsExprTableFullName().TableName.Name;
            var tableIdentity = this.GetTableIdentity(tableReference);
            context.RegisterSource(columnSource, aliasName, tableName, tableIdentity);

            errors = null;
            return true;
        }

        private bool TryBuildSelectDerivedTableSource(
            QueryDerivedTable queryDerivedTable,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            var aliasName = queryDerivedTable.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = null;
                errors = new[] { "Derived table alias is required." };
                return false;
            }

            var childContext = context.CreateChild();
            if (!this.TryBuildSelectQueryExpression(queryDerivedTable.QueryExpression, childContext, out var query, out errors))
            {
                source = null;
                return false;
            }

            IReadOnlyList<ExprColumnName>? columns = null;
            if (queryDerivedTable.Columns != null && queryDerivedTable.Columns.Count > 0)
            {
                columns = queryDerivedTable.Columns.Select(c => new ExprColumnName(c.Value)).ToList();
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprDerivedTableQuery(query!, alias, columns);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null);
            errors = null;
            return true;
        }

        private bool TryBuildSelectInlineDerivedTableSource(
            InlineDerivedTable inlineDerivedTable,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (inlineDerivedTable.RowValues.Count < 1)
            {
                source = null;
                errors = new[] { "VALUES table constructor cannot be empty." };
                return false;
            }

            var aliasName = inlineDerivedTable.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = null;
                errors = new[] { "VALUES derived table alias is required." };
                return false;
            }

            var rows = new List<ExprValueRow>(inlineDerivedTable.RowValues.Count);
            int? expectedRowWidth = null;
            foreach (var rowValue in inlineDerivedTable.RowValues)
            {
                if (expectedRowWidth == null)
                {
                    expectedRowWidth = rowValue.ColumnValues.Count;
                    if (expectedRowWidth < 1)
                    {
                        source = null;
                        errors = new[] { "VALUES row cannot be empty." };
                        return false;
                    }
                }
                else if (expectedRowWidth.Value != rowValue.ColumnValues.Count)
                {
                    source = null;
                    errors = new[] { "All VALUES rows must have the same number of items." };
                    return false;
                }

                var items = new List<ExprValue>(rowValue.ColumnValues.Count);
                foreach (var item in rowValue.ColumnValues)
                {
                    if (!this.TryBuildSelectValueExpression(item, context, out var value, out errors))
                    {
                        source = null;
                        return false;
                    }

                    items.Add(value!);
                }

                rows.Add(new ExprValueRow(items));
            }

            var columns = new List<ExprColumnName>();
            if (inlineDerivedTable.Columns.Count > 0)
            {
                foreach (var column in inlineDerivedTable.Columns)
                {
                    if (string.IsNullOrWhiteSpace(column.Value))
                    {
                        source = null;
                        errors = new[] { "VALUES derived table column name cannot be empty." };
                        return false;
                    }

                    columns.Add(new ExprColumnName(column.Value));
                }
            }
            else
            {
                for (var i = 0; i < expectedRowWidth!.Value; i++)
                {
                    columns.Add(new ExprColumnName("C" + (i + 1).ToString()));
                }
            }

            if (columns.Count != expectedRowWidth)
            {
                source = null;
                errors = new[] { "VALUES derived table column list size mismatch." };
                return false;
            }

            var derivedTable = new ExprDerivedTableValues(
                new ExprTableValueConstructor(rows),
                new ExprTableAlias(new ExprAlias(aliasName!)),
                columns);
            source = derivedTable;

            context.RegisterSource(derivedTable.Alias, aliasName!, aliasName!, tableIdentity: null);
            errors = null;
            return true;
        }

        private bool TryBuildSelectSchemaFunctionTableSource(
            SchemaObjectFunctionTableReference functionReference,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (functionReference.ForPath)
            {
                source = null;
                errors = new[] { "FOR PATH table functions are not supported." };
                return false;
            }

            var schemaObject = functionReference.SchemaObject;
            if (schemaObject == null || schemaObject.BaseIdentifier == null)
            {
                source = null;
                errors = new[] { "Table function name is missing." };
                return false;
            }

            if (schemaObject.ServerIdentifier != null)
            {
                source = null;
                errors = new[] { "Server-qualified table functions are not supported." };
                return false;
            }

            var functionName = schemaObject.BaseIdentifier.Value;
            var arguments = new List<ExprValue>(functionReference.Parameters.Count);
            foreach (var parameter in functionReference.Parameters)
            {
                if (!this.TryBuildSelectValueExpression(parameter, context, out var argument, out errors))
                {
                    source = null;
                    return false;
                }

                arguments.Add(argument!);
            }

            ExprTableFunction function;
            if (schemaObject.DatabaseIdentifier != null)
            {
                var schemaName = schemaObject.SchemaIdentifier?.Value;
                if (string.IsNullOrWhiteSpace(schemaName))
                {
                    source = null;
                    errors = new[] { "Database-qualified table function without schema is not supported." };
                    return false;
                }

                function = SqQueryBuilder.TableFunctionDbCustom(
                    schemaObject.DatabaseIdentifier.Value,
                    schemaName!,
                    functionName,
                    arguments.Count == 0 ? null : arguments);
            }
            else if (schemaObject.SchemaIdentifier != null)
            {
                function = SqQueryBuilder.TableFunctionCustom(
                    schemaObject.SchemaIdentifier.Value,
                    functionName,
                    arguments.Count == 0 ? null : arguments);
            }
            else
            {
                function = SqQueryBuilder.TableFunctionSys(
                    functionName,
                    arguments.Count == 0 ? null : arguments);
            }

            var aliasName = functionReference.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = function;
                errors = null;
                return true;
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprAliasedTableFunction(function, alias);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null);
            errors = null;
            return true;
        }

        private bool TryBuildSelectBuiltInFunctionTableSource(
            BuiltInFunctionTableReference functionReference,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (functionReference.ForPath)
            {
                source = null;
                errors = new[] { "FOR PATH table functions are not supported." };
                return false;
            }

            var functionName = functionReference.Name?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                source = null;
                errors = new[] { "Table function name cannot be empty." };
                return false;
            }

            var arguments = new List<ExprValue>(functionReference.Parameters.Count);
            foreach (var parameter in functionReference.Parameters)
            {
                if (!this.TryBuildSelectValueExpression(parameter, context, out var argument, out errors))
                {
                    source = null;
                    return false;
                }

                arguments.Add(argument!);
            }

            var function = SqQueryBuilder.TableFunctionSys(
                functionName!,
                arguments.Count == 0 ? null : arguments);

            var aliasName = functionReference.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = function;
                errors = null;
                return true;
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprAliasedTableFunction(function, alias);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null);
            errors = null;
            return true;
        }

        private bool TryBuildSelectGlobalFunctionTableSource(
            GlobalFunctionTableReference functionReference,
            SelectParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (functionReference.ForPath)
            {
                source = null;
                errors = new[] { "FOR PATH table functions are not supported." };
                return false;
            }

            var functionName = functionReference.Name?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                source = null;
                errors = new[] { "Table function name cannot be empty." };
                return false;
            }

            var arguments = new List<ExprValue>(functionReference.Parameters.Count);
            foreach (var parameter in functionReference.Parameters)
            {
                if (!this.TryBuildSelectValueExpression(parameter, context, out var argument, out errors))
                {
                    source = null;
                    return false;
                }

                arguments.Add(argument!);
            }

            var function = SqQueryBuilder.TableFunctionSys(
                functionName!,
                arguments.Count == 0 ? null : arguments);

            var aliasName = functionReference.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = function;
                errors = null;
                return true;
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprAliasedTableFunction(function, alias);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null);
            errors = null;
            return true;
        }

        private bool TryBuildSelectBooleanExpression(
            BooleanExpression booleanExpression,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprBoolean? boolean,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            switch (booleanExpression)
            {
                case BooleanParenthesisExpression parenthesisExpression:
                    return this.TryBuildSelectBooleanExpression(
                        parenthesisExpression.Expression,
                        context,
                        out boolean,
                        out errors);
                case BooleanNotExpression notExpression:
                    if (!this.TryBuildSelectBooleanExpression(notExpression.Expression, context, out var notInner, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    boolean = new ExprBooleanNot(notInner!);
                    errors = null;
                    return true;
                case BooleanBinaryExpression binaryExpression:
                    if (!this.TryBuildSelectBooleanExpression(binaryExpression.FirstExpression, context, out var left, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (!this.TryBuildSelectBooleanExpression(binaryExpression.SecondExpression, context, out var right, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    boolean = binaryExpression.BinaryExpressionType switch
                    {
                        BooleanBinaryExpressionType.And => new ExprBooleanAnd(left!, right!),
                        BooleanBinaryExpressionType.Or => new ExprBooleanOr(left!, right!),
                        _ => null
                    };

                    if (boolean == null)
                    {
                        errors = new[] { $"Unsupported boolean binary operation: {binaryExpression.BinaryExpressionType}." };
                        return false;
                    }

                    errors = null;
                    return true;
                case BooleanComparisonExpression comparisonExpression:
                    if (!this.TryBuildSelectValueExpression(comparisonExpression.FirstExpression, context, out var leftValue, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (!this.TryBuildSelectValueExpression(comparisonExpression.SecondExpression, context, out var rightValue, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (this.TryInferColumnKind(comparisonExpression.FirstExpression, context, out var leftKind))
                    {
                        this.MarkColumnReferencesAsKind(comparisonExpression.SecondExpression, context, leftKind);
                    }

                    if (this.TryInferColumnKind(comparisonExpression.SecondExpression, context, out var rightKind))
                    {
                        this.MarkColumnReferencesAsKind(comparisonExpression.FirstExpression, context, rightKind);
                    }

                    if (comparisonExpression.FirstExpression is NullLiteral)
                    {
                        this.MarkColumnReferencesAsNullable(comparisonExpression.SecondExpression, context);
                    }

                    if (comparisonExpression.SecondExpression is NullLiteral)
                    {
                        this.MarkColumnReferencesAsNullable(comparisonExpression.FirstExpression, context);
                    }

                    boolean = comparisonExpression.ComparisonType switch
                    {
                        BooleanComparisonType.Equals => new ExprBooleanEq(leftValue!, rightValue!),
                        BooleanComparisonType.NotEqualToBrackets => new ExprBooleanNotEq(leftValue!, rightValue!),
                        BooleanComparisonType.NotEqualToExclamation => new ExprBooleanNotEq(leftValue!, rightValue!),
                        BooleanComparisonType.GreaterThan => new ExprBooleanGt(leftValue!, rightValue!),
                        BooleanComparisonType.GreaterThanOrEqualTo => new ExprBooleanGtEq(leftValue!, rightValue!),
                        BooleanComparisonType.LessThan => new ExprBooleanLt(leftValue!, rightValue!),
                        BooleanComparisonType.LessThanOrEqualTo => new ExprBooleanLtEq(leftValue!, rightValue!),
                        _ => null
                    };

                    if (boolean == null)
                    {
                        errors = new[] { $"Unsupported comparison operator: {comparisonExpression.ComparisonType}." };
                        return false;
                    }

                    errors = null;
                    return true;
                case BooleanIsNullExpression isNullExpression:
                    if (!this.TryBuildSelectValueExpression(isNullExpression.Expression, context, out var testValue, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    this.MarkColumnReferencesAsNullable(isNullExpression.Expression, context);

                    boolean = new ExprIsNull(testValue!, isNullExpression.IsNot);
                    errors = null;
                    return true;
                case ExistsPredicate existsPredicate:
                    if (!this.TryBuildSelectQueryExpression(
                            existsPredicate.Subquery.QueryExpression,
                            context.CreateChild(),
                            out var existsQuery,
                            out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    boolean = new ExprExists(existsQuery!);
                    errors = null;
                    return true;
                case InPredicate inPredicate:
                    if (!this.TryBuildSelectValueExpression(inPredicate.Expression, context, out var inExpression, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    ExprBoolean inPredicateExpression;
                    if (inPredicate.Subquery != null)
                    {
                        if (!this.TryBuildSelectQueryExpression(
                                inPredicate.Subquery.QueryExpression,
                                context.CreateChild(),
                                out var inSubQuery,
                                out errors))
                        {
                            boolean = null;
                            return false;
                        }

                        inPredicateExpression = new ExprInSubQuery(inExpression!, inSubQuery!);
                    }
                    else
                    {
                        if (inPredicate.Values.Count < 1)
                        {
                            boolean = null;
                            errors = new[] { "IN predicate cannot be empty." };
                            return false;
                        }

                        var values = new List<ExprValue>(inPredicate.Values.Count);
                        foreach (var value in inPredicate.Values)
                        {
                            if (!this.TryBuildSelectValueExpression(value, context, out var inValue, out errors))
                            {
                                boolean = null;
                                return false;
                            }

                            if (value is NullLiteral)
                            {
                                this.MarkColumnReferencesAsNullable(inPredicate.Expression, context);
                            }

                            values.Add(inValue!);
                        }

                        if (this.TryInferColumnKindFromValues(inPredicate.Values, context, out var inKind))
                        {
                            this.MarkColumnReferencesAsKind(inPredicate.Expression, context, inKind);
                        }

                        inPredicateExpression = new ExprInValues(inExpression!, values);
                    }

                    boolean = inPredicate.NotDefined
                        ? new ExprBooleanNot(inPredicateExpression)
                        : inPredicateExpression;
                    errors = null;
                    return true;
                case LikePredicate likePredicate:
                    if (!this.TryBuildSelectValueExpression(likePredicate.FirstExpression, context, out var likeValue, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (likePredicate.SecondExpression is not StringLiteral likePattern)
                    {
                        boolean = null;
                        errors = new[] { "LIKE pattern must be a string literal." };
                        return false;
                    }

                    this.MarkColumnReferencesAsKind(likePredicate.FirstExpression, context, InferredColumnKind.NVarChar);

                    var like = SqQueryBuilder.Like(likeValue!, likePattern.Value);
                    boolean = likePredicate.NotDefined ? new ExprBooleanNot(like) : like;
                    errors = null;
                    return true;
            }

            boolean = null;
            errors = new[] { $"Unsupported boolean expression: {booleanExpression.GetType().Name}." };
            return false;
        }

        private bool TryBuildSelectValueExpression(
            ScalarExpression expression,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprValue? value,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            switch (expression)
            {
                case ParenthesisExpression parenthesisExpression:
                    return this.TryBuildSelectValueExpression(parenthesisExpression.Expression, context, out value, out errors);
                case ColumnReferenceExpression columnReference:
                    return this.TryBuildSelectColumn(columnReference, context, out value, out errors);
                case IntegerLiteral integerLiteral when int.TryParse(integerLiteral.Value, out var intValue):
                    value = SqQueryBuilder.Literal(intValue);
                    errors = null;
                    return true;
                case NumericLiteral numericLiteral when decimal.TryParse(
                    numericLiteral.Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var decimalValue):
                    value = SqQueryBuilder.Literal(decimalValue);
                    errors = null;
                    return true;
                case MoneyLiteral moneyLiteral when decimal.TryParse(
                    moneyLiteral.Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var moneyValue):
                    value = SqQueryBuilder.Literal(moneyValue);
                    errors = null;
                    return true;
                case StringLiteral stringLiteral:
                    value = SqQueryBuilder.Literal(stringLiteral.Value);
                    errors = null;
                    return true;
                case NullLiteral:
                    value = SqQueryBuilder.Null;
                    errors = null;
                    return true;
                case BinaryExpression binaryExpression:
                    if (!this.TryBuildSelectValueExpression(binaryExpression.FirstExpression, context, out var left, out errors))
                    {
                        value = null;
                        return false;
                    }

                    if (!this.TryBuildSelectValueExpression(binaryExpression.SecondExpression, context, out var right, out errors))
                    {
                        value = null;
                        return false;
                    }

                    if (binaryExpression.BinaryExpressionType == BinaryExpressionType.Add
                        && (binaryExpression.FirstExpression is StringLiteral || binaryExpression.SecondExpression is StringLiteral))
                    {
                        this.MarkColumnReferencesAsKind(binaryExpression.FirstExpression, context, InferredColumnKind.NVarChar);
                        this.MarkColumnReferencesAsKind(binaryExpression.SecondExpression, context, InferredColumnKind.NVarChar);
                    }
                    else if (IsArithmeticBinary(binaryExpression.BinaryExpressionType)
                             && (binaryExpression.FirstExpression is NumericLiteral
                                 || binaryExpression.SecondExpression is NumericLiteral
                                 || binaryExpression.FirstExpression is MoneyLiteral
                                 || binaryExpression.SecondExpression is MoneyLiteral))
                    {
                        this.MarkColumnReferencesAsKind(binaryExpression.FirstExpression, context, InferredColumnKind.Decimal);
                        this.MarkColumnReferencesAsKind(binaryExpression.SecondExpression, context, InferredColumnKind.Decimal);
                    }

                    value = binaryExpression.BinaryExpressionType switch
                    {
                        BinaryExpressionType.Add => left! + right!,
                        BinaryExpressionType.Subtract => left! - right!,
                        BinaryExpressionType.Multiply => left! * right!,
                        BinaryExpressionType.Divide => left! / right!,
                        BinaryExpressionType.Modulo => left! % right!,
                        BinaryExpressionType.BitwiseAnd => left! & right!,
                        BinaryExpressionType.BitwiseOr => left! | right!,
                        BinaryExpressionType.BitwiseXor => left! ^ right!,
                        _ => null
                    };

                    if (ReferenceEquals(value, null))
                    {
                        errors = new[] { $"Unsupported binary expression: {binaryExpression.BinaryExpressionType}." };
                        return false;
                    }

                    errors = null;
                    return true;
                case UnaryExpression unaryExpression:
                    if (!this.TryBuildSelectValueExpression(unaryExpression.Expression, context, out var unaryValue, out errors))
                    {
                        value = null;
                        return false;
                    }

                    value = unaryExpression.UnaryExpressionType switch
                    {
                        UnaryExpressionType.Negative => SqQueryBuilder.Literal(0) - unaryValue!,
                        UnaryExpressionType.Positive => unaryValue,
                        _ => null
                    };

                    if (ReferenceEquals(value, null))
                    {
                        errors = new[] { $"Unsupported unary expression: {unaryExpression.UnaryExpressionType}." };
                        return false;
                    }

                    errors = null;
                    return true;
                case SearchedCaseExpression searchedCaseExpression:
                    if (!this.TryBuildSearchedCaseExpression(searchedCaseExpression, context, out value, out errors))
                    {
                        value = null;
                        return false;
                    }

                    return true;
                case SimpleCaseExpression simpleCaseExpression:
                    if (!this.TryBuildSimpleCaseExpression(simpleCaseExpression, context, out value, out errors))
                    {
                        value = null;
                        return false;
                    }

                    return true;
                case CoalesceExpression coalesceExpression:
                    if (!this.TryBuildCoalesceExpression(coalesceExpression, context, out value, out errors))
                    {
                        value = null;
                        return false;
                    }

                    return true;
                case FunctionCall functionCall:
                    return this.TryBuildSelectFunctionValue(functionCall, context, out value, out errors);
                case ScalarSubquery scalarSubquery:
                    if (!this.TryBuildSelectQueryExpression(
                            scalarSubquery.QueryExpression,
                            context.CreateChild(),
                            out var subQuery,
                            out errors))
                    {
                        value = null;
                        return false;
                    }

                    value = new ExprValueQuery(subQuery!);
                    errors = null;
                    return true;
            }

            value = null;
            errors = new[] { $"Unsupported scalar expression: {expression.GetType().Name}." };
            return false;
        }

        private bool TryBuildSelectFunctionValue(
            FunctionCall functionCall,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprValue? value,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (functionCall.OverClause != null)
            {
                value = null;
                errors = new[] { "Window function cannot be used as scalar value in this context." };
                return false;
            }

            var functionName = functionCall.FunctionName?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                value = null;
                errors = new[] { "Function name is missing." };
                return false;
            }

            var normalizedFunctionName = functionName!.ToUpperInvariant();
            if (normalizedFunctionName == "GETDATE")
            {
                if (functionCall.Parameters.Count != 0)
                {
                    value = null;
                    errors = new[] { "GETDATE does not accept arguments." };
                    return false;
                }

                value = SqQueryBuilder.GetDate();
                errors = null;
                return true;
            }

            if (normalizedFunctionName == "GETUTCDATE")
            {
                if (functionCall.Parameters.Count != 0)
                {
                    value = null;
                    errors = new[] { "GETUTCDATE does not accept arguments." };
                    return false;
                }

                value = SqQueryBuilder.GetUtcDate();
                errors = null;
                return true;
            }

            if (normalizedFunctionName == "DATEADD")
            {
                if (functionCall.Parameters.Count != 3)
                {
                    value = null;
                    errors = new[] { "DATEADD supports exactly three arguments." };
                    return false;
                }

                if (!TryParseDateAddDatePart(functionCall.Parameters[0], out var dateAddDatePart))
                {
                    value = null;
                    errors = new[] { "Unsupported DATEADD date part." };
                    return false;
                }

                if (!TryExtractInt32Value(functionCall.Parameters[1], out var number))
                {
                    value = null;
                    errors = new[] { "DATEADD number argument must be an Int32 literal." };
                    return false;
                }

                if (!this.TryBuildSelectValueExpression(functionCall.Parameters[2], context, out var dateValue, out errors))
                {
                    value = null;
                    return false;
                }

                value = SqQueryBuilder.DateAdd(dateAddDatePart, number, dateValue!);
                errors = null;
                return true;
            }

            if (normalizedFunctionName == "DATEDIFF")
            {
                if (functionCall.Parameters.Count != 3)
                {
                    value = null;
                    errors = new[] { "DATEDIFF supports exactly three arguments." };
                    return false;
                }

                if (!TryParseDateDiffDatePart(functionCall.Parameters[0], out var dateDiffDatePart))
                {
                    value = null;
                    errors = new[] { "Unsupported DATEDIFF date part." };
                    return false;
                }

                if (!this.TryBuildSelectValueExpression(functionCall.Parameters[1], context, out var startDate, out errors))
                {
                    value = null;
                    return false;
                }

                if (!this.TryBuildSelectValueExpression(functionCall.Parameters[2], context, out var endDate, out errors))
                {
                    value = null;
                    return false;
                }

                value = SqQueryBuilder.DateDiff(dateDiffDatePart, startDate!, endDate!);
                errors = null;
                return true;
            }

            var arguments = new List<ExprValue>(functionCall.Parameters.Count);
            foreach (var parameter in functionCall.Parameters)
            {
                if (IsStarExpression(parameter))
                {
                    value = null;
                    errors = new[] { $"Function '{functionName}' does not support '*' argument in scalar context." };
                    return false;
                }

                if (!this.TryBuildSelectValueExpression(parameter, context, out var argument, out errors))
                {
                    value = null;
                    return false;
                }

                arguments.Add(argument!);
            }

            if (functionCall.CallTarget is MultiPartIdentifierCallTarget callTarget)
            {
                var identifiers = callTarget.MultiPartIdentifier?.Identifiers?.Select(i => i.Value).ToList();
                if (identifiers == null || identifiers.Count < 1)
                {
                    value = null;
                    errors = new[] { $"Invalid function call target for '{functionName}'." };
                    return false;
                }

                value = identifiers.Count switch
                {
                    1 => SqQueryBuilder.ScalarFunctionCustom(
                        identifiers[0],
                        functionName!,
                        arguments.Count == 0 ? null : arguments),
                    2 => SqQueryBuilder.ScalarFunctionDbCustom(
                        identifiers[0],
                        identifiers[1],
                        functionName!,
                        arguments.Count == 0 ? null : arguments),
                    _ => null
                };

                if (ReferenceEquals(value, null))
                {
                    errors = new[] { $"Function target for '{functionName}' must have one or two identifiers." };
                    return false;
                }

                errors = null;
                return true;
            }

            if (functionCall.CallTarget != null)
            {
                value = null;
                errors = new[] { $"Unsupported function target type: {functionCall.CallTarget.GetType().Name}." };
                return false;
            }

            value = SqQueryBuilder.ScalarFunctionSys(
                functionName!,
                arguments.Count == 0 ? null : arguments);
            errors = null;
            return true;
        }

        private bool TryBuildSelectColumn(
            ColumnReferenceExpression columnReference,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprValue? value,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (columnReference.ColumnType == ColumnType.Wildcard)
            {
                value = null;
                errors = new[] { "Wildcard is not supported in scalar expression context." };
                return false;
            }

            var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
            if (identifiers == null || identifiers.Count < 1 || identifiers.Count > 2)
            {
                value = null;
                errors = new[] { "Only one- or two-part column references are supported." };
                return false;
            }

            IExprColumnSource? source;
            string columnName;
            if (identifiers.Count == 1)
            {
                columnName = identifiers[0].Value;
                if (!context.TryResolveSingleSource(out source!))
                {
                    if (context.HasAnyVisibleSources())
                    {
                        value = null;
                        errors = new[] { $"Cannot resolve source for column '{columnName}'." };
                        return false;
                    }

                    source = null;
                }
            }
            else
            {
                var sourceName = identifiers[0].Value;
                columnName = identifiers[1].Value;
                if (!context.TryResolveSource(sourceName, out source!))
                {
                    value = null;
                    errors = new[] { $"Unknown source '{sourceName}' for column '{columnName}'." };
                    return false;
                }
            }

            if (source != null && context.TryGetTableIdentity(source, out var tableIdentity))
            {
                this.RegisterColumnHint(tableIdentity!, columnName, InferredColumnKind.Int32);
            }

            value = new ExprColumn(source, new ExprColumnName(columnName));
            errors = null;
            return true;
        }

        private bool TryBuildSearchedCaseExpression(
            SearchedCaseExpression expression,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprValue? value,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (expression.WhenClauses.Count < 1)
            {
                value = null;
                errors = new[] { "CASE expression must contain at least one WHEN clause." };
                return false;
            }

            var items = new List<ExprCaseWhenThen>(expression.WhenClauses.Count);
            foreach (var clause in expression.WhenClauses)
            {
                if (clause is not SearchedWhenClause searchedWhenClause)
                {
                    value = null;
                    errors = new[] { $"Unsupported CASE WHEN clause: {clause.GetType().Name}." };
                    return false;
                }

                if (!this.TryBuildSelectBooleanExpression(searchedWhenClause.WhenExpression, context, out var condition, out errors))
                {
                    value = null;
                    return false;
                }

                if (!this.TryBuildSelectValueExpression(searchedWhenClause.ThenExpression, context, out var thenValue, out errors))
                {
                    value = null;
                    return false;
                }

                items.Add(new ExprCaseWhenThen(condition!, thenValue!));
            }

            ExprValue defaultValue;
            if (expression.ElseExpression != null)
            {
                if (!this.TryBuildSelectValueExpression(expression.ElseExpression, context, out var elseValue, out errors))
                {
                    value = null;
                    return false;
                }

                defaultValue = elseValue!;
            }
            else
            {
                defaultValue = SqQueryBuilder.Null;
            }

            value = new ExprCase(items, defaultValue);
            errors = null;
            return true;
        }

        private bool TryBuildSimpleCaseExpression(
            SimpleCaseExpression expression,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprValue? value,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (expression.WhenClauses.Count < 1)
            {
                value = null;
                errors = new[] { "CASE expression must contain at least one WHEN clause." };
                return false;
            }

            if (!this.TryBuildSelectValueExpression(expression.InputExpression, context, out var inputValue, out errors))
            {
                value = null;
                return false;
            }

            var items = new List<ExprCaseWhenThen>(expression.WhenClauses.Count);
            foreach (var clause in expression.WhenClauses)
            {
                if (clause is not SimpleWhenClause simpleWhenClause)
                {
                    value = null;
                    errors = new[] { $"Unsupported CASE WHEN clause: {clause.GetType().Name}." };
                    return false;
                }

                if (!this.TryBuildSelectValueExpression(simpleWhenClause.WhenExpression, context, out var whenValue, out errors))
                {
                    value = null;
                    return false;
                }

                if (!this.TryBuildSelectValueExpression(simpleWhenClause.ThenExpression, context, out var thenValue, out errors))
                {
                    value = null;
                    return false;
                }

                items.Add(new ExprCaseWhenThen(new ExprBooleanEq(inputValue!, whenValue!), thenValue!));
            }

            ExprValue defaultValue;
            if (expression.ElseExpression != null)
            {
                if (!this.TryBuildSelectValueExpression(expression.ElseExpression, context, out var elseValue, out errors))
                {
                    value = null;
                    return false;
                }

                defaultValue = elseValue!;
            }
            else
            {
                defaultValue = SqQueryBuilder.Null;
            }

            value = new ExprCase(items, defaultValue);
            errors = null;
            return true;
        }

        private bool TryBuildCoalesceExpression(
            CoalesceExpression expression,
            SelectParseContext context,
            [NotNullWhen(true)] out ExprValue? value,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (expression.Expressions.Count < 2)
            {
                value = null;
                errors = new[] { "COALESCE requires at least two arguments." };
                return false;
            }

            var values = new List<ExprValue>(expression.Expressions.Count);
            foreach (var item in expression.Expressions)
            {
                if (!this.TryBuildSelectValueExpression(item, context, out var arg, out errors))
                {
                    value = null;
                    return false;
                }

                values.Add(arg!);
            }

            value = values.Count switch
            {
                2 => SqQueryBuilder.Coalesce(values[0], values[1]),
                3 => SqQueryBuilder.Coalesce(values[0], values[1], values[2]),
                _ => SqQueryBuilder.Coalesce(values[0], values[1], values.Skip(2).ToArray())
            };
            errors = null;
            return true;
        }

        private static bool IsAggregateFunctionName(string functionNameUpper)
            => functionNameUpper == "COUNT"
               || functionNameUpper == "SUM"
               || functionNameUpper == "AVG"
               || functionNameUpper == "MIN"
               || functionNameUpper == "MAX";

        private static bool IsStarExpression(ScalarExpression expression)
        {
            return expression is ColumnReferenceExpression columnReference
                   && columnReference.ColumnType == ColumnType.Wildcard;
        }

        private static bool TryParseDateAddDatePart(ScalarExpression expression, out DateAddDatePart datePart)
        {
            datePart = default;
            if (!TryGetDatePartToken(expression, out var token))
            {
                return false;
            }

            switch (token)
            {
                case "YEAR":
                case "YY":
                case "YYYY":
                    datePart = DateAddDatePart.Year;
                    return true;
                case "MONTH":
                case "MM":
                case "M":
                    datePart = DateAddDatePart.Month;
                    return true;
                case "DAY":
                case "DD":
                case "D":
                    datePart = DateAddDatePart.Day;
                    return true;
                case "WEEK":
                case "WK":
                case "WW":
                    datePart = DateAddDatePart.Week;
                    return true;
                case "HOUR":
                case "HH":
                    datePart = DateAddDatePart.Hour;
                    return true;
                case "MINUTE":
                case "MI":
                case "N":
                    datePart = DateAddDatePart.Minute;
                    return true;
                case "SECOND":
                case "SS":
                case "S":
                    datePart = DateAddDatePart.Second;
                    return true;
                case "MILLISECOND":
                case "MS":
                    datePart = DateAddDatePart.Millisecond;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseDateDiffDatePart(ScalarExpression expression, out DateDiffDatePart datePart)
        {
            datePart = default;
            if (!TryGetDatePartToken(expression, out var token))
            {
                return false;
            }

            switch (token)
            {
                case "YEAR":
                case "YY":
                case "YYYY":
                    datePart = DateDiffDatePart.Year;
                    return true;
                case "MONTH":
                case "MM":
                case "M":
                    datePart = DateDiffDatePart.Month;
                    return true;
                case "DAY":
                case "DD":
                case "D":
                    datePart = DateDiffDatePart.Day;
                    return true;
                case "HOUR":
                case "HH":
                    datePart = DateDiffDatePart.Hour;
                    return true;
                case "MINUTE":
                case "MI":
                case "N":
                    datePart = DateDiffDatePart.Minute;
                    return true;
                case "SECOND":
                case "SS":
                case "S":
                    datePart = DateDiffDatePart.Second;
                    return true;
                case "MILLISECOND":
                case "MS":
                    datePart = DateDiffDatePart.Millisecond;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetDatePartToken(ScalarExpression expression, out string token)
        {
            token = string.Empty;

            if (expression is ColumnReferenceExpression columnReference
                && columnReference.ColumnType != ColumnType.Wildcard
                && columnReference.MultiPartIdentifier?.Identifiers != null
                && columnReference.MultiPartIdentifier.Identifiers.Count == 1)
            {
                var singleIdentifier = columnReference.MultiPartIdentifier.Identifiers[0].Value;
                if (!string.IsNullOrWhiteSpace(singleIdentifier))
                {
                    token = singleIdentifier.ToUpperInvariant();
                    return true;
                }
            }

            if (expression is IdentifierLiteral identifierLiteral && !string.IsNullOrWhiteSpace(identifierLiteral.Value))
            {
                token = identifierLiteral.Value.ToUpperInvariant();
                return true;
            }

            if (expression is StringLiteral stringLiteral && !string.IsNullOrWhiteSpace(stringLiteral.Value))
            {
                token = stringLiteral.Value.ToUpperInvariant();
                return true;
            }

            return false;
        }

        private static bool TryExtractInt32Value(ScalarExpression expression, out int value)
        {
            switch (expression)
            {
                case IntegerLiteral integerLiteral:
                    return int.TryParse(integerLiteral.Value, out value);
                case NumericLiteral numericLiteral when int.TryParse(
                    numericLiteral.Value,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value):
                    return true;
                case ParenthesisExpression parenthesisExpression:
                    return TryExtractInt32Value(parenthesisExpression.Expression, out value);
            }

            value = default;
            return false;
        }

        private bool TryBuildStructuredUpdate(
            TSqlStatement statement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (statement is not UpdateStatement updateStatement)
            {
                result = null;
                errors = new[] { "Only UPDATE statements are supported." };
                return false;
            }

            if (updateStatement.OptimizerHints.Count > 0)
            {
                result = null;
                errors = new[] { "UPDATE optimizer hints are not supported yet." };
                return false;
            }

            var specification = updateStatement.UpdateSpecification;
            if (specification == null)
            {
                result = null;
                errors = new[] { "UPDATE specification is missing." };
                return false;
            }

            if (specification.TopRowFilter != null)
            {
                result = null;
                errors = new[] { "UPDATE TOP is not supported yet." };
                return false;
            }

            if (specification.OutputClause != null)
            {
                result = null;
                errors = new[] { "UPDATE OUTPUT is not supported by SqExpress." };
                return false;
            }

            if (specification.OutputIntoClause != null)
            {
                result = null;
                errors = new[] { "UPDATE OUTPUT INTO is not supported." };
                return false;
            }

            if (specification.SetClauses.Count < 1)
            {
                result = null;
                errors = new[] { "UPDATE SET cannot be empty." };
                return false;
            }

            var context = new UpdateParseContext();
            IExprTableSource? source = null;
            if (specification.FromClause != null)
            {
                if (specification.FromClause.TableReferences.Count != 1)
                {
                    result = null;
                    errors = new[] { "Only one root FROM table-reference is supported." };
                    return false;
                }

                if (!this.TryBuildTableSource(specification.FromClause.TableReferences[0], context, out source, out errors))
                {
                    result = null;
                    return false;
                }
            }

            if (!this.TryResolveUpdateTarget(specification.Target, context, out var target, out errors))
            {
                result = null;
                return false;
            }

            context.Target = target;

            var setClauses = new List<ExprColumnSetClause>(specification.SetClauses.Count);
            foreach (var setClause in specification.SetClauses)
            {
                if (setClause is not AssignmentSetClause assignmentSetClause)
                {
                    result = null;
                    errors = new[] { $"Unsupported UPDATE SET clause: {setClause.GetType().Name}." };
                    return false;
                }

                if (!this.TryBuildColumn(assignmentSetClause.Column, context, out var leftColumn, out errors))
                {
                    result = null;
                    return false;
                }

                if (!this.TryBuildAssigningExpression(assignmentSetClause.NewValue, context, out var assigning, out errors))
                {
                    result = null;
                    return false;
                }

                setClauses.Add(new ExprColumnSetClause(leftColumn!, assigning!));
            }

            ExprBoolean? filter = null;
            if (specification.WhereClause != null)
            {
                if (!this.TryBuildBooleanExpression(specification.WhereClause.SearchCondition, context, out filter, out errors))
                {
                    result = null;
                    return false;
                }
            }

            result = new ExprUpdate(target!, setClauses, source, filter);
            errors = null;
            return true;
        }

        private bool TryBuildStructuredDelete(
            TSqlStatement statement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (statement is not DeleteStatement deleteStatement)
            {
                result = null;
                errors = new[] { "Only DELETE statements are supported." };
                return false;
            }

            if (deleteStatement.OptimizerHints.Count > 0)
            {
                result = null;
                errors = new[] { "DELETE optimizer hints are not supported yet." };
                return false;
            }

            var specification = deleteStatement.DeleteSpecification;
            if (specification == null)
            {
                result = null;
                errors = new[] { "DELETE specification is missing." };
                return false;
            }

            if (specification.TopRowFilter != null)
            {
                result = null;
                errors = new[] { "DELETE TOP is not supported yet." };
                return false;
            }

            if (specification.OutputIntoClause != null)
            {
                result = null;
                errors = new[] { "DELETE OUTPUT INTO is not supported." };
                return false;
            }

            var context = new UpdateParseContext();
            IExprTableSource? source = null;
            if (specification.FromClause != null)
            {
                if (specification.FromClause.TableReferences.Count != 1)
                {
                    result = null;
                    errors = new[] { "Only one root FROM table-reference is supported." };
                    return false;
                }

                if (!this.TryBuildTableSource(specification.FromClause.TableReferences[0], context, out source, out errors))
                {
                    result = null;
                    return false;
                }
            }

            if (!this.TryResolveUpdateTarget(specification.Target, context, out var target, out errors))
            {
                result = null;
                return false;
            }

            context.Target = target;

            ExprBoolean? filter = null;
            if (specification.WhereClause != null)
            {
                if (!this.TryBuildBooleanExpression(specification.WhereClause.SearchCondition, context, out filter, out errors))
                {
                    result = null;
                    return false;
                }
            }

            var deleteExpr = new ExprDelete(target!, source, filter);
            if (specification.OutputClause != null)
            {
                if (!this.TryBuildDeleteOutputColumns(specification.OutputClause, context, out var outputColumns, out errors))
                {
                    result = null;
                    return false;
                }

                result = new ExprDeleteOutput(deleteExpr, outputColumns!);
                return true;
            }

            result = deleteExpr;
            errors = null;
            return true;
        }

        private bool TryBuildStructuredInsert(
            TSqlStatement statement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (statement is not InsertStatement insertStatement)
            {
                result = null;
                errors = new[] { "Only INSERT statements are supported." };
                return false;
            }

            if (insertStatement.OptimizerHints.Count > 0)
            {
                result = null;
                errors = new[] { "INSERT optimizer hints are not supported yet." };
                return false;
            }

            var specification = insertStatement.InsertSpecification;
            if (specification == null)
            {
                result = null;
                errors = new[] { "INSERT specification is missing." };
                return false;
            }

            if (specification.TopRowFilter != null)
            {
                result = null;
                errors = new[] { "INSERT TOP is not supported yet." };
                return false;
            }

            if (specification.OutputIntoClause != null)
            {
                result = null;
                errors = new[] { "INSERT OUTPUT INTO is not supported." };
                return false;
            }

            if (specification.InsertOption == InsertOption.Over)
            {
                result = null;
                errors = new[] { "INSERT OVER is not supported." };
                return false;
            }

            if (specification.Target is not NamedTableReference namedTarget)
            {
                result = null;
                errors = new[] { "Only named INSERT target is supported." };
                return false;
            }

            if (!this.TryBuildExprTable(namedTarget, out var targetTable, out errors))
            {
                result = null;
                return false;
            }

            List<ExprColumnName>? targetColumns = null;
            TryGetTableIdentity(targetTable!, out var insertTargetIdentity);
            if (specification.Columns.Count > 0)
            {
                targetColumns = new List<ExprColumnName>(specification.Columns.Count);
                foreach (var columnReference in specification.Columns)
                {
                    var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
                    if (identifiers == null || identifiers.Count < 1 || identifiers.Count > 2)
                    {
                        result = null;
                        errors = new[] { "INSERT target column must be one- or two-part identifier." };
                        return false;
                    }

                    var targetColumnName = identifiers[identifiers.Count - 1].Value;
                    targetColumns.Add(new ExprColumnName(targetColumnName));
                    if (insertTargetIdentity != null)
                    {
                        this.RegisterColumnHint(insertTargetIdentity, targetColumnName, InferredColumnKind.Int32);
                    }
                }
            }

            var insertSource = specification.InsertSource;
            if (insertSource == null)
            {
                result = null;
                errors = new[] { "INSERT source is missing." };
                return false;
            }

            if (insertSource is ValuesInsertSource valuesSource)
            {
                if (valuesSource.IsDefaultValues)
                {
                    result = null;
                    errors = new[] { "INSERT DEFAULT VALUES is not supported yet." };
                    return false;
                }

                if (valuesSource.RowValues.Count < 1)
                {
                    result = null;
                    errors = new[] { "INSERT VALUES cannot be empty." };
                    return false;
                }

                var rows = new List<ExprInsertValueRow>(valuesSource.RowValues.Count);
                int? expectedRowWidth = null;
                foreach (var row in valuesSource.RowValues)
                {
                    if (targetColumns != null && row.ColumnValues.Count != targetColumns.Count)
                    {
                        result = null;
                        errors = new[] { "INSERT column count does not match VALUES item count." };
                        return false;
                    }

                    if (targetColumns == null)
                    {
                        expectedRowWidth ??= row.ColumnValues.Count;
                        if (expectedRowWidth.Value < 1)
                        {
                            result = null;
                            errors = new[] { "INSERT VALUES row cannot be empty." };
                            return false;
                        }

                        if (row.ColumnValues.Count != expectedRowWidth.Value)
                        {
                            result = null;
                            errors = new[] { "All INSERT VALUES rows must have the same number of items." };
                            return false;
                        }
                    }

                    var rowItems = new List<IExprAssigning>(row.ColumnValues.Count);
                    var rowContext = new SelectParseContext(parent: null, new Dictionary<string, CteRegistryEntry>());
                    for (var i = 0; i < row.ColumnValues.Count; i++)
                    {
                        var columnValue = row.ColumnValues[i];
                        if (columnValue is DefaultLiteral)
                        {
                            result = null;
                            errors = new[] { "INSERT VALUES with DEFAULT item is not supported yet." };
                            return false;
                        }

                        if (!this.TryBuildSelectValueExpression(columnValue, rowContext, out var value, out errors))
                        {
                            result = null;
                            return false;
                        }

                        if (insertTargetIdentity != null && targetColumns != null)
                        {
                            var targetColumnName = targetColumns[i].Name;
                            if (columnValue is NullLiteral)
                            {
                                this.RegisterColumnHint(insertTargetIdentity, targetColumnName, InferredColumnKind.Int32, nullable: true);
                            }
                            else if (this.TryInferColumnKind(columnValue, rowContext, out var inferredKind))
                            {
                                this.RegisterColumnHint(insertTargetIdentity, targetColumnName, inferredKind);
                            }
                        }

                        rowItems.Add(value!);
                    }

                    rows.Add(new ExprInsertValueRow(rowItems));
                }

                var insertExpr = new ExprInsert(
                    targetTable!.FullName,
                    targetColumns,
                    new ExprInsertValues(rows));

                if (specification.OutputClause != null)
                {
                    if (!this.TryBuildInsertOutputColumns(specification.OutputClause, out var outputColumns, out errors))
                    {
                        result = null;
                        return false;
                    }

                    result = new ExprInsertOutput(insertExpr, outputColumns!);
                    return true;
                }

                result = insertExpr;
                errors = null;
                return true;
            }

            if (insertSource is SelectInsertSource selectSource)
            {
                if (selectSource.Select == null)
                {
                    result = null;
                    errors = new[] { "INSERT SELECT source is missing." };
                    return false;
                }

                var cteRegistry = new Dictionary<string, CteRegistryEntry>(StringComparer.OrdinalIgnoreCase);
                if (!this.TryRegisterCtes(insertStatement.WithCtesAndXmlNamespaces, cteRegistry, out errors))
                {
                    result = null;
                    return false;
                }

                var selectContext = new SelectParseContext(parent: null, cteRegistry);
                if (!this.TryBuildSelectQueryExpression(selectSource.Select, selectContext, out var query, out errors))
                {
                    result = null;
                    return false;
                }

                var insertExpr = new ExprInsert(
                    targetTable!.FullName,
                    targetColumns,
                    new ExprInsertQuery(query!));

                if (specification.OutputClause != null)
                {
                    if (!this.TryBuildInsertOutputColumns(specification.OutputClause, out var outputColumns, out errors))
                    {
                        result = null;
                        return false;
                    }

                    result = new ExprInsertOutput(insertExpr, outputColumns!);
                    return true;
                }

                result = insertExpr;
                errors = null;
                return true;
            }

            result = null;
            errors = new[] { $"Unsupported INSERT source: {insertSource.GetType().Name}." };
            return false;
        }

        private bool TryBuildStructuredMerge(
            TSqlStatement statement,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (statement is not MergeStatement mergeStatement)
            {
                result = null;
                errors = new[] { "Only MERGE statements are supported." };
                return false;
            }

            if (mergeStatement.OptimizerHints.Count > 0)
            {
                result = null;
                errors = new[] { "MERGE optimizer hints are not supported yet." };
                return false;
            }

            var specification = mergeStatement.MergeSpecification;
            if (specification == null)
            {
                result = null;
                errors = new[] { "MERGE specification is missing." };
                return false;
            }

            if (specification.TopRowFilter != null)
            {
                result = null;
                errors = new[] { "MERGE TOP is not supported yet." };
                return false;
            }

            if (specification.OutputIntoClause != null)
            {
                result = null;
                errors = new[] { "MERGE OUTPUT INTO is not supported." };
                return false;
            }

            if (specification.SearchCondition == null)
            {
                result = null;
                errors = new[] { "MERGE ON condition is required." };
                return false;
            }

            if (specification.Target is not NamedTableReference namedTarget)
            {
                result = null;
                errors = new[] { "Only named MERGE target is supported." };
                return false;
            }

            var context = new UpdateParseContext();
            if (!this.TryBuildTableSource(specification.TableReference, context, out var source, out errors))
            {
                result = null;
                return false;
            }

            if (!this.TryBuildExprTable(namedTarget, out var targetTable, out errors))
            {
                result = null;
                return false;
            }

            var tableAliasName = specification.TableAlias?.Value ?? namedTarget.Alias?.Value;
            if (!string.IsNullOrWhiteSpace(tableAliasName))
            {
                targetTable = new ExprTable(
                    targetTable!.FullName,
                    new ExprTableAlias(new ExprAlias(tableAliasName!)));
            }

            context.RegisterTable(targetTable!, this.GetTableIdentity(namedTarget));
            context.Target = targetTable!;

            if (!this.TryBuildBooleanExpression(specification.SearchCondition, context, out var onCondition, out errors))
            {
                result = null;
                return false;
            }

            if (!this.TryBuildMergeActionClauses(
                    specification.ActionClauses,
                    context,
                    out var whenMatched,
                    out var whenNotMatchedByTarget,
                    out var whenNotMatchedBySource,
                    out errors))
            {
                result = null;
                return false;
            }

            var mergeExpr = new ExprMerge(
                targetTable!,
                source!,
                onCondition!,
                whenMatched,
                whenNotMatchedByTarget,
                whenNotMatchedBySource);

            if (specification.OutputClause != null)
            {
                if (!this.TryBuildMergeOutput(specification.OutputClause, context, out var output, out errors))
                {
                    result = null;
                    return false;
                }

                result = ExprMergeOutput.FromMerge(mergeExpr, output!);
                return true;
            }

            result = mergeExpr;
            errors = null;
            return true;
        }

        private bool TryBuildMergeActionClauses(
            IList<MergeActionClause> actionClauses,
            UpdateParseContext context,
            out IExprMergeMatched? whenMatched,
            out IExprMergeNotMatched? whenNotMatchedByTarget,
            out IExprMergeMatched? whenNotMatchedBySource,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            whenMatched = null;
            whenNotMatchedByTarget = null;
            whenNotMatchedBySource = null;

            foreach (var actionClause in actionClauses)
            {
                switch (actionClause.Condition)
                {
                    case MergeCondition.Matched:
                        if (whenMatched != null)
                        {
                            errors = new[] { "Multiple WHEN MATCHED clauses are not supported yet." };
                            return false;
                        }

                        if (!this.TryBuildMergeMatchedAction(actionClause, context, out whenMatched, out errors))
                        {
                            return false;
                        }

                        break;
                    case MergeCondition.NotMatched:
                    case MergeCondition.NotMatchedByTarget:
                        if (whenNotMatchedByTarget != null)
                        {
                            errors = new[] { "Multiple WHEN NOT MATCHED BY TARGET clauses are not supported yet." };
                            return false;
                        }

                        if (!this.TryBuildMergeNotMatchedByTargetAction(actionClause, context, out whenNotMatchedByTarget, out errors))
                        {
                            return false;
                        }

                        break;
                    case MergeCondition.NotMatchedBySource:
                        if (whenNotMatchedBySource != null)
                        {
                            errors = new[] { "Multiple WHEN NOT MATCHED BY SOURCE clauses are not supported yet." };
                            return false;
                        }

                        if (!this.TryBuildMergeMatchedAction(actionClause, context, out whenNotMatchedBySource, out errors))
                        {
                            return false;
                        }

                        break;
                    default:
                        errors = new[] { $"Unsupported MERGE action condition: {actionClause.Condition}." };
                        return false;
                }
            }

            errors = null;
            return true;
        }

        private bool TryBuildMergeMatchedAction(
            MergeActionClause actionClause,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprMergeMatched? action,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            ExprBoolean? and = null;
            if (actionClause.SearchCondition != null)
            {
                if (!this.TryBuildBooleanExpression(actionClause.SearchCondition, context, out and, out errors))
                {
                    action = null;
                    return false;
                }
            }

            if (actionClause.Action is DeleteMergeAction)
            {
                action = new ExprMergeMatchedDelete(and);
                errors = null;
                return true;
            }

            if (actionClause.Action is UpdateMergeAction updateAction)
            {
                var setClauses = new List<ExprColumnSetClause>(updateAction.SetClauses.Count);
                foreach (var setClause in updateAction.SetClauses)
                {
                    if (setClause is not AssignmentSetClause assignment)
                    {
                        action = null;
                        errors = new[] { $"Unsupported MERGE UPDATE SET clause: {setClause.GetType().Name}." };
                        return false;
                    }

                    if (!this.TryBuildColumn(assignment.Column, context, out var column, out errors))
                    {
                        action = null;
                        return false;
                    }

                    if (!this.TryBuildAssigningExpression(assignment.NewValue, context, out var value, out errors))
                    {
                        action = null;
                        return false;
                    }

                    setClauses.Add(new ExprColumnSetClause(column!, value!));
                }

                action = new ExprMergeMatchedUpdate(and, setClauses);
                errors = null;
                return true;
            }

            action = null;
            errors = new[] { $"Unsupported MERGE matched action: {actionClause.Action.GetType().Name}." };
            return false;
        }

        private bool TryBuildMergeNotMatchedByTargetAction(
            MergeActionClause actionClause,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprMergeNotMatched? action,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (actionClause.Action is not InsertMergeAction insertAction)
            {
                action = null;
                errors = new[] { $"Unsupported MERGE not-matched action: {actionClause.Action.GetType().Name}." };
                return false;
            }

            ExprBoolean? and = null;
            if (actionClause.SearchCondition != null)
            {
                if (!this.TryBuildBooleanExpression(actionClause.SearchCondition, context, out and, out errors))
                {
                    action = null;
                    return false;
                }
            }

            if (insertAction.Columns.Count == 0
                && insertAction.Source != null
                && insertAction.Source.RowValues.Count == 0)
            {
                action = new ExprExprMergeNotMatchedInsertDefault(and);
                errors = null;
                return true;
            }

            if (insertAction.Columns.Count < 1)
            {
                action = null;
                errors = new[] { "MERGE INSERT column list cannot be empty unless DEFAULT VALUES is used." };
                return false;
            }

            var source = insertAction.Source;
            if (source == null)
            {
                action = null;
                errors = new[] { "MERGE INSERT source is missing." };
                return false;
            }

            if (source.RowValues.Count != 1)
            {
                action = null;
                errors = new[] { "MERGE INSERT currently supports exactly one VALUES row." };
                return false;
            }

            var valuesRow = source.RowValues[0];
            if (valuesRow.ColumnValues.Count != insertAction.Columns.Count)
            {
                action = null;
                errors = new[] { "MERGE INSERT column count does not match VALUES item count." };
                return false;
            }

            var columns = new List<ExprColumnName>(insertAction.Columns.Count);
            for (var i = 0; i < insertAction.Columns.Count; i++)
            {
                var identifiers = insertAction.Columns[i].MultiPartIdentifier?.Identifiers;
                if (identifiers == null || identifiers.Count < 1)
                {
                    action = null;
                    errors = new[] { "MERGE INSERT column reference is invalid." };
                    return false;
                }

                columns.Add(new ExprColumnName(identifiers[identifiers.Count - 1].Value));
            }

            var values = new List<IExprAssigning>(valuesRow.ColumnValues.Count);
            foreach (var valueExpression in valuesRow.ColumnValues)
            {
                if (!this.TryBuildAssigningExpression(valueExpression, context, out var assigning, out errors))
                {
                    action = null;
                    return false;
                }

                values.Add(assigning!);
            }

            action = new ExprExprMergeNotMatchedInsert(and, columns, values);
            errors = null;
            return true;
        }

        private bool TryBuildInsertOutputColumns(
            OutputClause outputClause,
            [NotNullWhen(true)] out IReadOnlyList<ExprAliasedColumnName>? outputColumns,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (outputClause.SelectColumns.Count < 1)
            {
                outputColumns = null;
                errors = new[] { "INSERT OUTPUT column list cannot be empty." };
                return false;
            }

            var result = new List<ExprAliasedColumnName>(outputClause.SelectColumns.Count);
            foreach (var selectElement in outputClause.SelectColumns)
            {
                if (selectElement is not SelectScalarExpression scalar)
                {
                    outputColumns = null;
                    errors = new[] { $"Unsupported INSERT OUTPUT element: {selectElement.GetType().Name}." };
                    return false;
                }

                if (!TryBuildOutputColumnReference(scalar.Expression, out var sourceName, out var columnName))
                {
                    outputColumns = null;
                    errors = new[] { "INSERT OUTPUT supports only one- or two-part column references." };
                    return false;
                }

                if (sourceName != null && !string.Equals(sourceName, "INSERTED", StringComparison.OrdinalIgnoreCase))
                {
                    outputColumns = null;
                    errors = new[] { "INSERT OUTPUT supports only INSERTED pseudo-table columns." };
                    return false;
                }

                var alias = scalar.ColumnName == null ? null : new ExprColumnAlias(scalar.ColumnName.Value);
                result.Add(new ExprAliasedColumnName(new ExprColumnName(columnName!), alias));
            }

            outputColumns = result;
            errors = null;
            return true;
        }

        private bool TryBuildDeleteOutputColumns(
            OutputClause outputClause,
            UpdateParseContext context,
            [NotNullWhen(true)] out IReadOnlyList<ExprAliasedColumn>? outputColumns,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (outputClause.SelectColumns.Count < 1)
            {
                outputColumns = null;
                errors = new[] { "DELETE OUTPUT column list cannot be empty." };
                return false;
            }

            var result = new List<ExprAliasedColumn>(outputClause.SelectColumns.Count);
            foreach (var selectElement in outputClause.SelectColumns)
            {
                if (selectElement is not SelectScalarExpression scalar)
                {
                    outputColumns = null;
                    errors = new[] { $"Unsupported DELETE OUTPUT element: {selectElement.GetType().Name}." };
                    return false;
                }

                if (!TryBuildOutputColumnReference(scalar.Expression, out var sourceName, out var columnName))
                {
                    outputColumns = null;
                    errors = new[] { "DELETE OUTPUT supports only one- or two-part column references." };
                    return false;
                }

                if (sourceName != null && !string.Equals(sourceName, "DELETED", StringComparison.OrdinalIgnoreCase))
                {
                    outputColumns = null;
                    errors = new[] { "DELETE OUTPUT supports only DELETED pseudo-table columns." };
                    return false;
                }

                var alias = scalar.ColumnName == null ? null : new ExprColumnAlias(scalar.ColumnName.Value);
                result.Add(new ExprAliasedColumn(new ExprColumn(null, new ExprColumnName(columnName!)), alias));

                if (context.Target != null && TryGetTableIdentity(context.Target, out var tableIdentity))
                {
                    this.RegisterColumnHint(tableIdentity!, columnName!, InferredColumnKind.Int32);
                }
            }

            outputColumns = result;
            errors = null;
            return true;
        }

        private bool TryBuildMergeOutput(
            OutputClause outputClause,
            UpdateParseContext context,
            [NotNullWhen(true)] out ExprOutput? output,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (outputClause.SelectColumns.Count < 1)
            {
                output = null;
                errors = new[] { "MERGE OUTPUT column list cannot be empty." };
                return false;
            }

            var columns = new List<IExprOutputColumn>(outputClause.SelectColumns.Count);
            foreach (var selectElement in outputClause.SelectColumns)
            {
                if (selectElement is not SelectScalarExpression scalar)
                {
                    output = null;
                    errors = new[] { $"Unsupported MERGE OUTPUT element: {selectElement.GetType().Name}." };
                    return false;
                }

                var alias = scalar.ColumnName == null ? null : new ExprColumnAlias(scalar.ColumnName.Value);

                if (TryIsActionExpression(scalar.Expression))
                {
                    columns.Add(new ExprOutputAction(alias));
                    continue;
                }

                if (!TryBuildOutputColumnReference(scalar.Expression, out var sourceName, out var columnName))
                {
                    output = null;
                    errors = new[] { $"MERGE OUTPUT supports only column references and $ACTION. Unsupported expression: {scalar.Expression.GetType().Name}." };
                    return false;
                }

                if (sourceName == null)
                {
                    output = null;
                    errors = new[] { "MERGE OUTPUT column reference must be qualified." };
                    return false;
                }

                if (string.Equals(sourceName, "INSERTED", StringComparison.OrdinalIgnoreCase))
                {
                    columns.Add(new ExprOutputColumnInserted(new ExprAliasedColumnName(new ExprColumnName(columnName!), alias)));
                    continue;
                }

                if (string.Equals(sourceName, "DELETED", StringComparison.OrdinalIgnoreCase))
                {
                    columns.Add(new ExprOutputColumnDeleted(new ExprAliasedColumnName(new ExprColumnName(columnName!), alias)));
                    continue;
                }

                if (!context.TryResolveSource(sourceName, out var source))
                {
                    output = null;
                    errors = new[] { $"Unknown source '{sourceName}' in MERGE OUTPUT." };
                    return false;
                }

                columns.Add(new ExprOutputColumn(new ExprAliasedColumn(new ExprColumn(source, new ExprColumnName(columnName!)), alias)));
            }

            output = new ExprOutput(columns);
            errors = null;
            return true;
        }

        private static bool TryBuildOutputColumnReference(
            ScalarExpression expression,
            [NotNullWhen(true)] out string? sourceName,
            [NotNullWhen(true)] out string? columnName)
        {
            sourceName = null;
            columnName = null;

            if (expression is not ColumnReferenceExpression columnReference
                || columnReference.ColumnType == ColumnType.Wildcard)
            {
                return false;
            }

            var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
            if (identifiers == null || identifiers.Count < 1 || identifiers.Count > 2)
            {
                return false;
            }

            if (identifiers.Count == 2)
            {
                sourceName = identifiers[0].Value;
                columnName = identifiers[1].Value;
                return !string.IsNullOrWhiteSpace(sourceName) && !string.IsNullOrWhiteSpace(columnName);
            }

            columnName = identifiers[0].Value;
            return !string.IsNullOrWhiteSpace(columnName);
        }

        private static bool TryIsActionExpression(ScalarExpression expression)
        {
            if (expression is ColumnReferenceExpression columnReference)
            {
                if (columnReference.ColumnType == ColumnType.PseudoColumnAction)
                {
                    return true;
                }

                if (columnReference.ColumnType == ColumnType.Wildcard)
                {
                    return false;
                }

                var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
                if (identifiers != null && identifiers.Count == 1)
                {
                    return string.Equals(identifiers[0].Value, "$ACTION", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (expression is VariableReference variableReference)
            {
                return string.Equals(variableReference.Name, "$ACTION", StringComparison.OrdinalIgnoreCase);
            }

            if (expression is GlobalVariableExpression globalVariableExpression)
            {
                return string.Equals(globalVariableExpression.Name, "$ACTION", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private bool TryBuildTableSource(
            TableReference reference,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (reference is NamedTableReference namedTableReference)
            {
                return this.TryBuildUpdateNamedTableSource(namedTableReference, context, out source, out errors);
            }

            if (reference is QueryDerivedTable queryDerivedTable)
            {
                return this.TryBuildUpdateDerivedTableSource(queryDerivedTable, context, out source, out errors);
            }

            if (reference is InlineDerivedTable inlineDerivedTable)
            {
                return this.TryBuildUpdateInlineDerivedTableSource(inlineDerivedTable, context, out source, out errors);
            }

            if (reference is SchemaObjectFunctionTableReference schemaFunctionTableReference)
            {
                return this.TryBuildUpdateSchemaFunctionTableSource(schemaFunctionTableReference, context, out source, out errors);
            }

            if (reference is BuiltInFunctionTableReference builtInFunctionTableReference)
            {
                return this.TryBuildUpdateBuiltInFunctionTableSource(builtInFunctionTableReference, context, out source, out errors);
            }

            if (reference is GlobalFunctionTableReference globalFunctionTableReference)
            {
                return this.TryBuildUpdateGlobalFunctionTableSource(globalFunctionTableReference, context, out source, out errors);
            }

            if (reference is JoinParenthesisTableReference joinParenthesisTableReference)
            {
                return this.TryBuildTableSource(joinParenthesisTableReference.Join, context, out source, out errors);
            }

            if (reference is QualifiedJoin qualifiedJoin)
            {
                if (!this.TryBuildTableSource(qualifiedJoin.FirstTableReference, context, out var left, out errors))
                {
                    source = null;
                    return false;
                }

                if (!this.TryBuildTableSource(qualifiedJoin.SecondTableReference, context, out var right, out errors))
                {
                    source = null;
                    return false;
                }

                if (!this.TryBuildBooleanExpression(qualifiedJoin.SearchCondition, context, out var condition, out errors))
                {
                    source = null;
                    return false;
                }

                var joinType = qualifiedJoin.QualifiedJoinType switch
                {
                    QualifiedJoinType.Inner => ExprJoinedTable.ExprJoinType.Inner,
                    QualifiedJoinType.LeftOuter => ExprJoinedTable.ExprJoinType.Left,
                    QualifiedJoinType.RightOuter => ExprJoinedTable.ExprJoinType.Right,
                    QualifiedJoinType.FullOuter => ExprJoinedTable.ExprJoinType.Full,
                    _ => (ExprJoinedTable.ExprJoinType?)null
                };

                if (joinType == null)
                {
                    source = null;
                    errors = new[] { $"Unsupported join type: {qualifiedJoin.QualifiedJoinType}." };
                    return false;
                }

                source = new ExprJoinedTable(left!, joinType.Value, right!, condition!);
                return true;
            }

            if (reference is UnqualifiedJoin unqualifiedJoin)
            {
                if (!this.TryBuildTableSource(unqualifiedJoin.FirstTableReference, context, out var left, out errors))
                {
                    source = null;
                    return false;
                }

                if (!this.TryBuildTableSource(unqualifiedJoin.SecondTableReference, context, out var right, out errors))
                {
                    source = null;
                    return false;
                }

                source = unqualifiedJoin.UnqualifiedJoinType switch
                {
                    UnqualifiedJoinType.CrossJoin => new ExprCrossedTable(left!, right!),
                    UnqualifiedJoinType.CrossApply => new ExprLateralCrossedTable(left!, right!, outer: false),
                    UnqualifiedJoinType.OuterApply => new ExprLateralCrossedTable(left!, right!, outer: true),
                    _ => null
                };

                if (source == null)
                {
                    errors = new[] { $"Unsupported unqualified join type: {unqualifiedJoin.UnqualifiedJoinType}." };
                    return false;
                }

                return true;
            }

            source = null;
            errors = new[] { $"Unsupported UPDATE FROM table source: {reference.GetType().Name}." };
            return false;
        }

        private bool TryBuildUpdateNamedTableSource(
            NamedTableReference tableReference,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (!this.TryBuildExprTable(tableReference, out var table, out errors))
            {
                source = null;
                return false;
            }

            var tableName = table!.FullName.AsExprTableFullName().TableName.Name;
            var aliasName = table.Alias?.Alias is ExprAlias alias ? alias.Name : null;
            var columnSource = table.Alias != null
                ? (IExprColumnSource)table.Alias
                : table.FullName;
            var tableIdentity = this.GetTableIdentity(tableReference);

            context.RegisterSource(columnSource, aliasName, tableName, tableIdentity, table);
            source = table;
            errors = null;
            return true;
        }

        private bool TryBuildUpdateDerivedTableSource(
            QueryDerivedTable queryDerivedTable,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            var aliasName = queryDerivedTable.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = null;
                errors = new[] { "Derived table alias is required." };
                return false;
            }

            var selectContext = context.CreateSelectContext().CreateChild();
            if (!this.TryBuildSelectQueryExpression(queryDerivedTable.QueryExpression, selectContext, out var query, out errors))
            {
                source = null;
                return false;
            }

            IReadOnlyList<ExprColumnName>? columns = null;
            if (queryDerivedTable.Columns != null && queryDerivedTable.Columns.Count > 0)
            {
                columns = queryDerivedTable.Columns.Select(i => new ExprColumnName(i.Value)).ToList();
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprDerivedTableQuery(query!, alias, columns);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null, table: null);
            errors = null;
            return true;
        }

        private bool TryBuildUpdateInlineDerivedTableSource(
            InlineDerivedTable inlineDerivedTable,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (inlineDerivedTable.RowValues.Count < 1)
            {
                source = null;
                errors = new[] { "VALUES table constructor cannot be empty." };
                return false;
            }

            var aliasName = inlineDerivedTable.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = null;
                errors = new[] { "VALUES derived table alias is required." };
                return false;
            }

            var selectContext = context.CreateSelectContext();
            var rows = new List<ExprValueRow>(inlineDerivedTable.RowValues.Count);
            int? expectedRowWidth = null;
            foreach (var row in inlineDerivedTable.RowValues)
            {
                expectedRowWidth ??= row.ColumnValues.Count;
                if (expectedRowWidth.Value < 1)
                {
                    source = null;
                    errors = new[] { "VALUES row cannot be empty." };
                    return false;
                }

                if (row.ColumnValues.Count != expectedRowWidth.Value)
                {
                    source = null;
                    errors = new[] { "All VALUES rows must have the same number of items." };
                    return false;
                }

                var items = new List<ExprValue>(row.ColumnValues.Count);
                foreach (var item in row.ColumnValues)
                {
                    if (!this.TryBuildSelectValueExpression(item, selectContext, out var value, out errors))
                    {
                        source = null;
                        return false;
                    }

                    items.Add(value!);
                }

                rows.Add(new ExprValueRow(items));
            }

            var columns = new List<ExprColumnName>();
            if (inlineDerivedTable.Columns.Count > 0)
            {
                foreach (var column in inlineDerivedTable.Columns)
                {
                    if (string.IsNullOrWhiteSpace(column.Value))
                    {
                        source = null;
                        errors = new[] { "VALUES derived table column name cannot be empty." };
                        return false;
                    }

                    columns.Add(new ExprColumnName(column.Value));
                }
            }
            else
            {
                for (var i = 0; i < expectedRowWidth!.Value; i++)
                {
                    columns.Add(new ExprColumnName("C" + (i + 1).ToString()));
                }
            }

            if (columns.Count != expectedRowWidth)
            {
                source = null;
                errors = new[] { "VALUES derived table column list size mismatch." };
                return false;
            }

            var derivedTable = new ExprDerivedTableValues(
                new ExprTableValueConstructor(rows),
                new ExprTableAlias(new ExprAlias(aliasName!)),
                columns);
            source = derivedTable;

            context.RegisterSource(derivedTable.Alias, aliasName!, aliasName!, tableIdentity: null, table: null);
            errors = null;
            return true;
        }

        private bool TryBuildUpdateSchemaFunctionTableSource(
            SchemaObjectFunctionTableReference functionReference,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (functionReference.ForPath)
            {
                source = null;
                errors = new[] { "FOR PATH table functions are not supported." };
                return false;
            }

            var schemaObject = functionReference.SchemaObject;
            if (schemaObject == null || schemaObject.BaseIdentifier == null)
            {
                source = null;
                errors = new[] { "Table function name is missing." };
                return false;
            }

            if (schemaObject.ServerIdentifier != null)
            {
                source = null;
                errors = new[] { "Server-qualified table functions are not supported." };
                return false;
            }

            var selectContext = context.CreateSelectContext();
            var arguments = new List<ExprValue>(functionReference.Parameters.Count);
            foreach (var parameter in functionReference.Parameters)
            {
                if (!this.TryBuildSelectValueExpression(parameter, selectContext, out var argument, out errors))
                {
                    source = null;
                    return false;
                }

                arguments.Add(argument!);
            }

            var functionName = schemaObject.BaseIdentifier.Value;
            ExprTableFunction function;
            if (schemaObject.DatabaseIdentifier != null)
            {
                var schemaName = schemaObject.SchemaIdentifier?.Value;
                if (string.IsNullOrWhiteSpace(schemaName))
                {
                    source = null;
                    errors = new[] { "Database-qualified table function without schema is not supported." };
                    return false;
                }

                function = SqQueryBuilder.TableFunctionDbCustom(
                    schemaObject.DatabaseIdentifier.Value,
                    schemaName!,
                    functionName,
                    arguments.Count == 0 ? null : arguments);
            }
            else if (schemaObject.SchemaIdentifier != null)
            {
                function = SqQueryBuilder.TableFunctionCustom(
                    schemaObject.SchemaIdentifier.Value,
                    functionName,
                    arguments.Count == 0 ? null : arguments);
            }
            else
            {
                function = SqQueryBuilder.TableFunctionSys(
                    functionName,
                    arguments.Count == 0 ? null : arguments);
            }

            var aliasName = functionReference.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = function;
                errors = null;
                return true;
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprAliasedTableFunction(function, alias);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null, table: null);
            errors = null;
            return true;
        }

        private bool TryBuildUpdateBuiltInFunctionTableSource(
            BuiltInFunctionTableReference functionReference,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (functionReference.ForPath)
            {
                source = null;
                errors = new[] { "FOR PATH table functions are not supported." };
                return false;
            }

            var functionName = functionReference.Name?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                source = null;
                errors = new[] { "Table function name cannot be empty." };
                return false;
            }

            var selectContext = context.CreateSelectContext();
            var arguments = new List<ExprValue>(functionReference.Parameters.Count);
            foreach (var parameter in functionReference.Parameters)
            {
                if (!this.TryBuildSelectValueExpression(parameter, selectContext, out var argument, out errors))
                {
                    source = null;
                    return false;
                }

                arguments.Add(argument!);
            }

            var function = SqQueryBuilder.TableFunctionSys(
                functionName!,
                arguments.Count == 0 ? null : arguments);

            var aliasName = functionReference.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = function;
                errors = null;
                return true;
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprAliasedTableFunction(function, alias);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null, table: null);
            errors = null;
            return true;
        }

        private bool TryBuildUpdateGlobalFunctionTableSource(
            GlobalFunctionTableReference functionReference,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprTableSource? source,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (functionReference.ForPath)
            {
                source = null;
                errors = new[] { "FOR PATH table functions are not supported." };
                return false;
            }

            var functionName = functionReference.Name?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                source = null;
                errors = new[] { "Table function name cannot be empty." };
                return false;
            }

            var selectContext = context.CreateSelectContext();
            var arguments = new List<ExprValue>(functionReference.Parameters.Count);
            foreach (var parameter in functionReference.Parameters)
            {
                if (!this.TryBuildSelectValueExpression(parameter, selectContext, out var argument, out errors))
                {
                    source = null;
                    return false;
                }

                arguments.Add(argument!);
            }

            var function = SqQueryBuilder.TableFunctionSys(
                functionName!,
                arguments.Count == 0 ? null : arguments);

            var aliasName = functionReference.Alias?.Value;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                source = function;
                errors = null;
                return true;
            }

            var alias = new ExprTableAlias(new ExprAlias(aliasName!));
            source = new ExprAliasedTableFunction(function, alias);
            context.RegisterSource(alias, aliasName!, aliasName!, tableIdentity: null, table: null);
            errors = null;
            return true;
        }

        private bool TryResolveUpdateTarget(
            TableReference? targetReference,
            UpdateParseContext context,
            [NotNullWhen(true)] out ExprTable? target,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (targetReference is not NamedTableReference namedTarget)
            {
                target = null;
                errors = new[] { "Only named UPDATE target is supported." };
                return false;
            }

            var schemaObject = namedTarget.SchemaObject;
            var baseIdentifier = schemaObject?.BaseIdentifier?.Value;
            if (string.IsNullOrWhiteSpace(baseIdentifier))
            {
                target = null;
                errors = new[] { "UPDATE target name is missing." };
                return false;
            }

            if (context.TryGetByAlias(baseIdentifier!, out var byAlias))
            {
                target = byAlias;
                errors = null;
                return true;
            }

            if (context.TryGetSingleByTableName(baseIdentifier!, out var byTableName))
            {
                target = byTableName;
                errors = null;
                return true;
            }

            if (!this.TryBuildExprTable(namedTarget, out var explicitTarget, out errors))
            {
                target = null;
                return false;
            }

            context.RegisterTable(explicitTarget!, this.GetTableIdentity(namedTarget));
            target = explicitTarget;
            errors = null;
            return true;
        }

        private bool TryBuildExprTable(
            NamedTableReference namedTableReference,
            [NotNullWhen(true)] out ExprTable? table,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            var schemaObject = namedTableReference.SchemaObject;
            if (schemaObject == null || schemaObject.BaseIdentifier == null)
            {
                table = null;
                errors = new[] { "Table reference is missing name." };
                return false;
            }

            var schemaName = schemaObject.SchemaIdentifier?.Value;
            var tableName = schemaObject.BaseIdentifier.Value;
            var aliasName = namedTableReference.Alias?.Value;

            if (string.IsNullOrWhiteSpace(schemaName)
                && schemaObject.DatabaseIdentifier == null
                && schemaObject.BaseIdentifier.QuoteType == QuoteType.NotQuoted)
            {
                schemaName = "dbo";
            }

            ExprDbSchema? schema = schemaName == null
                ? null
                : new ExprDbSchema(null, new ExprSchemaName(schemaName));

            var fullName = new ExprTableFullName(schema, new ExprTableName(tableName));
            ExprTableAlias? alias = aliasName == null ? null : new ExprTableAlias(new ExprAlias(aliasName));
            table = new ExprTable(fullName, alias);
            errors = null;
            return true;
        }

        private bool TryBuildColumn(
            ColumnReferenceExpression columnReference,
            UpdateParseContext context,
            [NotNullWhen(true)] out ExprColumn? column,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            column = null;
            var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
            if (identifiers == null || identifiers.Count < 1 || identifiers.Count > 2)
            {
                errors = new[] { "Only one- or two-part column references are supported." };
                return false;
            }

            IExprColumnSource? source = null;
            TableIdentity? tableIdentity = null;
            string columnName;
            if (identifiers.Count == 2)
            {
                var sourceToken = identifiers[0].Value;
                columnName = identifiers[1].Value;
                if (!context.TryResolveSource(sourceToken, out source))
                {
                    errors = new[] { $"Unknown source '{sourceToken}' for column '{columnName}'." };
                    return false;
                }

                context.TryGetTableIdentity(source!, out tableIdentity);
            }
            else
            {
                columnName = identifiers[0].Value;
                if (context.Target != null)
                {
                    source = context.Target.Alias != null
                        ? (IExprColumnSource)context.Target.Alias
                        : context.Target.FullName;
                    TryGetTableIdentity(context.Target, out tableIdentity);
                }
                else if (!context.TryResolveSingleSource(out source))
                {
                    errors = new[] { $"Cannot resolve source for column '{columnName}'." };
                    return false;
                }

                if (source != null)
                {
                    context.TryGetTableIdentity(source, out tableIdentity);
                }
            }

            if (tableIdentity != null)
            {
                this.RegisterColumnHint(tableIdentity, columnName, InferredColumnKind.Int32);
            }

            column = new ExprColumn(source, new ExprColumnName(columnName));
            errors = null;
            return true;
        }

        private bool TryBuildAssigningExpression(
            ScalarExpression expression,
            UpdateParseContext context,
            [NotNullWhen(true)] out IExprAssigning? assigning,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (!this.TryBuildValueExpression(expression, context, out var value, out errors))
            {
                assigning = null;
                return false;
            }

            assigning = value!;
            return true;
        }

        private bool TryBuildValueExpression(
            ScalarExpression expression,
            UpdateParseContext context,
            [NotNullWhen(true)] out ExprValue? value,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            switch (expression)
            {
                case ColumnReferenceExpression columnReference:
                    if (!this.TryBuildColumn(columnReference, context, out var column, out errors))
                    {
                        value = null;
                        return false;
                    }

                    value = column;
                    return true;
                case IntegerLiteral integerLiteral when int.TryParse(integerLiteral.Value, out var intValue):
                    value = SqQueryBuilder.Literal(intValue);
                    errors = null;
                    return true;
                case NumericLiteral numericLiteral when decimal.TryParse(
                    numericLiteral.Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var decimalValue):
                    value = SqQueryBuilder.Literal(decimalValue);
                    errors = null;
                    return true;
                case StringLiteral stringLiteral:
                    value = SqQueryBuilder.Literal(stringLiteral.Value);
                    errors = null;
                    return true;
                case NullLiteral:
                    value = SqQueryBuilder.Null;
                    errors = null;
                    return true;
            }

            value = null;
            errors = new[] { $"Unsupported scalar expression: {expression.GetType().Name}." };
            return false;
        }

        private bool TryBuildBooleanExpression(
            BooleanExpression booleanExpression,
            UpdateParseContext context,
            [NotNullWhen(true)] out ExprBoolean? boolean,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            switch (booleanExpression)
            {
                case BooleanParenthesisExpression parenthesisExpression:
                    return this.TryBuildBooleanExpression(parenthesisExpression.Expression, context, out boolean, out errors);
                case BooleanNotExpression notExpression:
                    if (!this.TryBuildBooleanExpression(notExpression.Expression, context, out var inner, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    boolean = new ExprBooleanNot(inner!);
                    return true;
                case BooleanBinaryExpression binaryExpression:
                    if (!this.TryBuildBooleanExpression(binaryExpression.FirstExpression, context, out var left, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (!this.TryBuildBooleanExpression(binaryExpression.SecondExpression, context, out var right, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    boolean = binaryExpression.BinaryExpressionType switch
                    {
                        BooleanBinaryExpressionType.And => new ExprBooleanAnd(left!, right!),
                        BooleanBinaryExpressionType.Or => new ExprBooleanOr(left!, right!),
                        _ => null
                    };

                    if (boolean == null)
                    {
                        errors = new[] { $"Unsupported boolean binary operation: {binaryExpression.BinaryExpressionType}." };
                        return false;
                    }

                    errors = null;
                    return true;
                case BooleanComparisonExpression comparisonExpression:
                    if (!this.TryBuildValueExpression(comparisonExpression.FirstExpression, context, out var leftValue, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (!this.TryBuildValueExpression(comparisonExpression.SecondExpression, context, out var rightValue, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (comparisonExpression.FirstExpression is ColumnReferenceExpression leftColumn
                        && this.TryInferColumnKindForUpdate(comparisonExpression.SecondExpression, context, out var leftKind))
                    {
                        this.MarkUpdateColumnKind(leftColumn, context, leftKind);
                    }

                    if (comparisonExpression.SecondExpression is ColumnReferenceExpression rightColumn
                        && this.TryInferColumnKindForUpdate(comparisonExpression.FirstExpression, context, out var rightKind))
                    {
                        this.MarkUpdateColumnKind(rightColumn, context, rightKind);
                    }

                    if (comparisonExpression.FirstExpression is NullLiteral
                        && comparisonExpression.SecondExpression is ColumnReferenceExpression nullableRight)
                    {
                        this.MarkUpdateColumnNullable(nullableRight, context);
                    }

                    if (comparisonExpression.SecondExpression is NullLiteral
                        && comparisonExpression.FirstExpression is ColumnReferenceExpression nullableLeft)
                    {
                        this.MarkUpdateColumnNullable(nullableLeft, context);
                    }

                    boolean = comparisonExpression.ComparisonType switch
                    {
                        BooleanComparisonType.Equals => new ExprBooleanEq(leftValue!, rightValue!),
                        BooleanComparisonType.NotEqualToBrackets => new ExprBooleanNotEq(leftValue!, rightValue!),
                        BooleanComparisonType.NotEqualToExclamation => new ExprBooleanNotEq(leftValue!, rightValue!),
                        BooleanComparisonType.GreaterThan => new ExprBooleanGt(leftValue!, rightValue!),
                        BooleanComparisonType.GreaterThanOrEqualTo => new ExprBooleanGtEq(leftValue!, rightValue!),
                        BooleanComparisonType.LessThan => new ExprBooleanLt(leftValue!, rightValue!),
                        BooleanComparisonType.LessThanOrEqualTo => new ExprBooleanLtEq(leftValue!, rightValue!),
                        _ => null
                    };

                    if (boolean == null)
                    {
                        errors = new[] { $"Unsupported comparison operator: {comparisonExpression.ComparisonType}." };
                        return false;
                    }

                    errors = null;
                    return true;
                case LikePredicate likePredicate:
                    if (!this.TryBuildValueExpression(likePredicate.FirstExpression, context, out var likeTest, out errors))
                    {
                        boolean = null;
                        return false;
                    }

                    if (likePredicate.SecondExpression is not StringLiteral likePattern)
                    {
                        boolean = null;
                        errors = new[] { "LIKE is supported only with string literal pattern." };
                        return false;
                    }

                    if (likePredicate.FirstExpression is ColumnReferenceExpression likeColumn)
                    {
                        this.MarkUpdateColumnKind(likeColumn, context, InferredColumnKind.NVarChar);
                    }

                    var like = SqQueryBuilder.Like(likeTest!, likePattern.Value);
                    boolean = likePredicate.NotDefined ? new ExprBooleanNot(like) : like;
                    errors = null;
                    return true;
            }

            boolean = null;
            errors = new[] { $"Unsupported boolean expression: {booleanExpression.GetType().Name}." };
            return false;
        }

        private void ResetInferredColumns()
        {
            this._inferredTableColumns.Clear();
        }

        private IReadOnlyList<SqTable> BuildSqTablesArtifact()
        {
            var result = new List<SqTable>(this._inferredTableColumns.Count);
            foreach (var item in this._inferredTableColumns
                         .OrderBy(i => i.Key.DatabaseName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(i => i.Key.SchemaName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(i => i.Key.TableName, StringComparer.OrdinalIgnoreCase))
            {
                var columns = item.Value.Columns.Values
                    .OrderBy(i => i.Order)
                    .ToList();

                SqTable table;
                if (!string.IsNullOrWhiteSpace(item.Key.DatabaseName) && !string.IsNullOrWhiteSpace(item.Key.SchemaName))
                {
                    table = SqTable.Create(
                        item.Key.DatabaseName!,
                        item.Key.SchemaName!,
                        item.Key.TableName,
                        appender =>
                        {
                            var createdColumns = new List<TableColumn>(columns.Count);
                            foreach (var column in columns)
                            {
                                createdColumns.Add(CreateTableColumn(appender, column));
                            }

                            return appender.AppendColumns(createdColumns);
                        });
                }
                else
                {
                    table = SqTable.Create(
                        item.Key.SchemaName,
                        item.Key.TableName,
                        appender =>
                        {
                            var createdColumns = new List<TableColumn>(columns.Count);
                            foreach (var column in columns)
                            {
                                createdColumns.Add(CreateTableColumn(appender, column));
                            }

                            return appender.AppendColumns(createdColumns);
                        });
                }

                result.Add(table);
            }

            return result;
        }

        private TableIdentity GetTableIdentity(NamedTableReference tableReference)
        {
            var schemaObject = tableReference.SchemaObject!;
            var schemaName = schemaObject.SchemaIdentifier?.Value;
            if (string.IsNullOrWhiteSpace(schemaName)
                && schemaObject.DatabaseIdentifier == null
                && schemaObject.BaseIdentifier.QuoteType == QuoteType.NotQuoted)
            {
                schemaName = "dbo";
            }

            return new TableIdentity(
                schemaObject.DatabaseIdentifier?.Value,
                schemaName,
                schemaObject.BaseIdentifier!.Value);
        }

        private static bool TryGetTableIdentity(ExprTable table, [NotNullWhen(true)] out TableIdentity? tableIdentity)
        {
            var fullName = table.FullName.AsExprTableFullName();
            var tableName = fullName.TableName.Name;
            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableIdentity = null;
                return false;
            }

            var schemaName = fullName.DbSchema?.Schema.Name;
            tableIdentity = new TableIdentity(databaseName: null, schemaName, tableName);
            return true;
        }

        private List<InferredTableColumn> GetOrderedColumns(TableIdentity tableIdentity)
        {
            if (!this._inferredTableColumns.TryGetValue(tableIdentity, out var map))
            {
                return new List<InferredTableColumn>();
            }

            return map.Columns.Values
                .OrderBy(i => i.Order)
                .ToList();
        }

        private void RegisterColumnHint(TableIdentity tableIdentity, string columnName, InferredColumnKind hint, bool nullable = false)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return;
            }

            if (!this._inferredTableColumns.TryGetValue(tableIdentity, out var tableColumns))
            {
                tableColumns = new TableColumnMap();
                this._inferredTableColumns[tableIdentity] = tableColumns;
            }

            var normalizedHint = ApplyNamingHeuristics(columnName, hint, out var hintedStringLength);
            if (tableColumns.Columns.TryGetValue(columnName, out var existing))
            {
                existing.Kind = MergeColumnKinds(existing.Kind, normalizedHint);
                existing.IsNullable = existing.IsNullable || nullable;
                if (existing.Kind == InferredColumnKind.NVarChar)
                {
                    existing.StringLength = MergeStringLength(existing.StringLength, hintedStringLength);
                }
                else
                {
                    existing.StringLength = null;
                }

                return;
            }

            tableColumns.Columns[columnName] = new InferredTableColumn(
                columnName,
                normalizedHint,
                nullable,
                normalizedHint == InferredColumnKind.NVarChar ? hintedStringLength : null,
                tableColumns.NextOrder++);
        }

        private static TableColumn CreateTableColumn(ITableColumnAppender appender, InferredTableColumn column)
        {
            return column.Kind switch
            {
                InferredColumnKind.NVarChar => column.IsNullable
                    ? appender.CreateNullableStringColumn(column.Name, column.StringLength, isUnicode: true)
                    : appender.CreateStringColumn(column.Name, column.StringLength, isUnicode: true),
                InferredColumnKind.Boolean => column.IsNullable
                    ? appender.CreateNullableBooleanColumn(column.Name)
                    : appender.CreateBooleanColumn(column.Name),
                InferredColumnKind.Decimal => column.IsNullable
                    ? appender.CreateNullableDecimalColumn(column.Name)
                    : appender.CreateDecimalColumn(column.Name),
                InferredColumnKind.DateTime => column.IsNullable
                    ? appender.CreateNullableDateTimeColumn(column.Name)
                    : appender.CreateDateTimeColumn(column.Name),
                InferredColumnKind.DateTimeOffset => column.IsNullable
                    ? appender.CreateNullableDateTimeOffsetColumn(column.Name)
                    : appender.CreateDateTimeOffsetColumn(column.Name),
                InferredColumnKind.Guid => column.IsNullable
                    ? appender.CreateNullableGuidColumn(column.Name)
                    : appender.CreateGuidColumn(column.Name),
                InferredColumnKind.ByteArray => column.IsNullable
                    ? appender.CreateNullableByteArrayColumn(column.Name, size: null)
                    : appender.CreateByteArrayColumn(column.Name, size: null),
                _ => column.IsNullable
                    ? appender.CreateNullableInt32Column(column.Name)
                    : appender.CreateInt32Column(column.Name)
            };
        }

        private bool TryInferColumnKind(
            ScalarExpression expression,
            SelectParseContext context,
            out InferredColumnKind kind)
        {
            kind = InferredColumnKind.Int32;
            switch (expression)
            {
                case StringLiteral:
                    kind = InferredColumnKind.NVarChar;
                    return true;
                case BinaryLiteral:
                    kind = InferredColumnKind.ByteArray;
                    return true;
                case NumericLiteral:
                case MoneyLiteral:
                    kind = InferredColumnKind.Decimal;
                    return true;
                case NullLiteral:
                    kind = InferredColumnKind.Int32;
                    return true;
                case ColumnReferenceExpression columnReference:
                    return this.TryInferColumnKindFromColumnReference(columnReference, context, out kind);
                case ParenthesisExpression parenthesis:
                    return this.TryInferColumnKind(parenthesis.Expression, context, out kind);
            }

            return false;
        }

        private bool TryInferColumnKindForUpdate(
            ScalarExpression expression,
            UpdateParseContext context,
            out InferredColumnKind kind)
        {
            kind = InferredColumnKind.Int32;
            switch (expression)
            {
                case StringLiteral:
                    kind = InferredColumnKind.NVarChar;
                    return true;
                case BinaryLiteral:
                    kind = InferredColumnKind.ByteArray;
                    return true;
                case NumericLiteral:
                case MoneyLiteral:
                    kind = InferredColumnKind.Decimal;
                    return true;
                case IntegerLiteral:
                    kind = InferredColumnKind.Int32;
                    return true;
                case NullLiteral:
                    kind = InferredColumnKind.Int32;
                    return true;
                case ParenthesisExpression parenthesis:
                    return this.TryInferColumnKindForUpdate(parenthesis.Expression, context, out kind);
                case ColumnReferenceExpression columnReference:
                    if (this.TryResolveUpdateColumnReference(columnReference, context, out var tableIdentity, out var columnName)
                        && this._inferredTableColumns.TryGetValue(tableIdentity!, out var columnMap)
                        && columnMap.Columns.TryGetValue(columnName!, out var existing))
                    {
                        kind = existing.Kind;
                        return true;
                    }

                    return false;
            }

            return false;
        }

        private bool TryInferColumnKindFromValues(
            IList<ScalarExpression> values,
            SelectParseContext context,
            out InferredColumnKind kind)
        {
            kind = InferredColumnKind.Int32;
            var hasKind = false;
            foreach (var item in values)
            {
                if (item is NullLiteral)
                {
                    continue;
                }

                if (!this.TryInferColumnKind(item, context, out var itemKind))
                {
                    continue;
                }

                kind = hasKind ? MergeColumnKinds(kind, itemKind) : itemKind;
                hasKind = true;
            }

            return hasKind;
        }

        private bool TryInferColumnKindFromColumnReference(
            ColumnReferenceExpression columnReference,
            SelectParseContext context,
            out InferredColumnKind kind)
        {
            kind = InferredColumnKind.Int32;
            if (!this.TryResolveSelectColumnReference(columnReference, context, out var tableIdentity, out var columnName))
            {
                return false;
            }

            if (!this._inferredTableColumns.TryGetValue(tableIdentity!, out var columnMap)
                || !columnMap.Columns.TryGetValue(columnName!, out var column))
            {
                return false;
            }

            kind = column.Kind;
            return true;
        }

        private void MarkColumnReferencesAsKind(
            ScalarExpression expression,
            SelectParseContext context,
            InferredColumnKind kind)
        {
            foreach (var columnReference in EnumerateColumnReferences(expression))
            {
                if (this.TryResolveSelectColumnReference(columnReference, context, out var tableIdentity, out var columnName))
                {
                    this.RegisterColumnHint(tableIdentity!, columnName!, kind);
                }
            }
        }

        private void MarkColumnReferencesAsNullable(ScalarExpression expression, SelectParseContext context)
        {
            foreach (var columnReference in EnumerateColumnReferences(expression))
            {
                if (this.TryResolveSelectColumnReference(columnReference, context, out var tableIdentity, out var columnName))
                {
                    this.RegisterColumnHint(tableIdentity!, columnName!, InferredColumnKind.Int32, nullable: true);
                }
            }
        }

        private void MarkUpdateColumnKind(
            ColumnReferenceExpression columnReference,
            UpdateParseContext context,
            InferredColumnKind kind)
        {
            if (this.TryResolveUpdateColumnReference(columnReference, context, out var tableIdentity, out var columnName))
            {
                this.RegisterColumnHint(tableIdentity!, columnName!, kind);
            }
        }

        private void MarkUpdateColumnNullable(ColumnReferenceExpression columnReference, UpdateParseContext context)
        {
            if (this.TryResolveUpdateColumnReference(columnReference, context, out var tableIdentity, out var columnName))
            {
                this.RegisterColumnHint(tableIdentity!, columnName!, InferredColumnKind.Int32, nullable: true);
            }
        }

        private bool TryResolveSelectColumnReference(
            ColumnReferenceExpression columnReference,
            SelectParseContext context,
            [NotNullWhen(true)] out TableIdentity? tableIdentity,
            [NotNullWhen(true)] out string? columnName)
        {
            tableIdentity = null;
            columnName = null;
            if (columnReference.ColumnType == ColumnType.Wildcard)
            {
                return false;
            }

            var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
            if (identifiers == null || identifiers.Count < 1 || identifiers.Count > 2)
            {
                return false;
            }

            IExprColumnSource? source = null;
            if (identifiers.Count == 1)
            {
                if (!context.TryResolveSingleSource(out source))
                {
                    return false;
                }

                columnName = identifiers[0].Value;
            }
            else
            {
                if (!context.TryResolveSource(identifiers[0].Value, out source))
                {
                    return false;
                }

                columnName = identifiers[1].Value;
            }

            if (source == null || string.IsNullOrWhiteSpace(columnName))
            {
                return false;
            }

            return context.TryGetTableIdentity(source, out tableIdentity);
        }

        private bool TryResolveUpdateColumnReference(
            ColumnReferenceExpression columnReference,
            UpdateParseContext context,
            [NotNullWhen(true)] out TableIdentity? tableIdentity,
            [NotNullWhen(true)] out string? columnName)
        {
            tableIdentity = null;
            columnName = null;
            if (columnReference.ColumnType == ColumnType.Wildcard)
            {
                return false;
            }

            var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
            if (identifiers == null || identifiers.Count < 1 || identifiers.Count > 2)
            {
                return false;
            }

            if (identifiers.Count == 2)
            {
                if (!context.TryResolveSource(identifiers[0].Value, out var source))
                {
                    return false;
                }

                columnName = identifiers[1].Value;
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    return false;
                }

                return context.TryGetTableIdentity(source!, out tableIdentity);
            }

            IExprColumnSource? singleSource = null;
            if (context.Target != null)
            {
                singleSource = context.Target.Alias != null
                    ? (IExprColumnSource)context.Target.Alias
                    : context.Target.FullName;
            }
            else
            {
                context.TryResolveSingleSource(out singleSource);
            }

            columnName = identifiers[0].Value;
            if (singleSource == null || string.IsNullOrWhiteSpace(columnName))
            {
                return false;
            }

            return context.TryGetTableIdentity(singleSource, out tableIdentity);
        }

        private static IReadOnlyList<ColumnReferenceExpression> EnumerateColumnReferences(ScalarExpression expression)
        {
            var result = new List<ColumnReferenceExpression>();
            CollectColumnReferences(expression, result);
            return result;
        }

        private static void CollectColumnReferences(ScalarExpression expression, List<ColumnReferenceExpression> result)
        {
            switch (expression)
            {
                case ColumnReferenceExpression columnReference when columnReference.ColumnType != ColumnType.Wildcard:
                    result.Add(columnReference);
                    return;
                case ParenthesisExpression parenthesis:
                    CollectColumnReferences(parenthesis.Expression, result);
                    return;
                case UnaryExpression unary:
                    CollectColumnReferences(unary.Expression, result);
                    return;
                case BinaryExpression binary:
                    CollectColumnReferences(binary.FirstExpression, result);
                    CollectColumnReferences(binary.SecondExpression, result);
                    return;
                case CastCall castCall:
                    CollectColumnReferences(castCall.Parameter, result);
                    return;
                case CoalesceExpression coalesce:
                    foreach (var inner in coalesce.Expressions)
                    {
                        CollectColumnReferences(inner, result);
                    }

                    return;
                case FunctionCall functionCall:
                    foreach (var parameter in functionCall.Parameters)
                    {
                        CollectColumnReferences(parameter, result);
                    }

                    return;
                default:
                    return;
            }
        }

        private static bool IsArithmeticBinary(BinaryExpressionType type)
        {
            switch (type)
            {
                case BinaryExpressionType.Add:
                case BinaryExpressionType.Subtract:
                case BinaryExpressionType.Multiply:
                case BinaryExpressionType.Divide:
                case BinaryExpressionType.Modulo:
                    return true;
                default:
                    return false;
            }
        }

        private static InferredColumnKind ApplyNamingHeuristics(string sqlName, InferredColumnKind hint, out int? stringLength)
        {
            stringLength = null;
            if (hint != InferredColumnKind.Int32)
            {
                return hint;
            }

            var normalized = new string(
                sqlName
                    .Where(ch => ch != '_' && ch != '-' && !char.IsWhiteSpace(ch))
                    .ToArray())
                .ToUpperInvariant();

            if (normalized.StartsWith("IS", StringComparison.Ordinal)
                || normalized.StartsWith("HAS", StringComparison.Ordinal)
                || normalized.StartsWith("CAN", StringComparison.Ordinal)
                || normalized.EndsWith("FLAG", StringComparison.Ordinal)
                || normalized.StartsWith("ENABLE", StringComparison.Ordinal)
                || normalized.StartsWith("DISABLE", StringComparison.Ordinal))
            {
                return InferredColumnKind.Boolean;
            }

            if (normalized.EndsWith("NAME", StringComparison.Ordinal)
                || normalized.EndsWith("DESCRIPTION", StringComparison.Ordinal)
                || normalized.EndsWith("TITLE", StringComparison.Ordinal)
                || normalized.EndsWith("COMMENT", StringComparison.Ordinal)
                || normalized.EndsWith("NOTE", StringComparison.Ordinal)
                || normalized.EndsWith("TEXT", StringComparison.Ordinal))
            {
                stringLength = 255;
                return InferredColumnKind.NVarChar;
            }

            if (normalized.EndsWith("DATE", StringComparison.Ordinal)
                || normalized.EndsWith("TIME", StringComparison.Ordinal)
                || normalized.EndsWith("AT", StringComparison.Ordinal)
                || normalized.IndexOf("UTC", StringComparison.Ordinal) >= 0
                || normalized.IndexOf("TIMESTAMP", StringComparison.Ordinal) >= 0
                || normalized.EndsWith("ON", StringComparison.Ordinal))
            {
                return InferredColumnKind.DateTime;
            }

            if (normalized.EndsWith("GUID", StringComparison.Ordinal)
                || normalized.EndsWith("UUID", StringComparison.Ordinal)
                || normalized.EndsWith("UID", StringComparison.Ordinal))
            {
                return InferredColumnKind.Guid;
            }

            if (normalized.EndsWith("AMOUNT", StringComparison.Ordinal)
                || normalized.EndsWith("PRICE", StringComparison.Ordinal)
                || normalized.EndsWith("COST", StringComparison.Ordinal)
                || normalized.EndsWith("RATE", StringComparison.Ordinal)
                || normalized.EndsWith("PERCENT", StringComparison.Ordinal)
                || normalized.EndsWith("BALANCE", StringComparison.Ordinal))
            {
                return InferredColumnKind.Decimal;
            }

            return InferredColumnKind.Int32;
        }

        private static int? MergeStringLength(int? existing, int? hint)
        {
            if (existing.HasValue && hint.HasValue)
            {
                return Math.Max(existing.Value, hint.Value);
            }

            return existing ?? hint;
        }

        private static InferredColumnKind MergeColumnKinds(InferredColumnKind existing, InferredColumnKind hint)
        {
            if (existing == hint)
            {
                return existing;
            }

            if (existing == InferredColumnKind.Int32)
            {
                return hint;
            }

            if (hint == InferredColumnKind.Int32)
            {
                return existing;
            }

            return InferredColumnKind.NVarChar;
        }

        private sealed class CteRegistryEntry
        {
            public CteRegistryEntry(string name)
            {
                this.Name = name;
            }

            public string Name { get; }

            public IExprSubQuery? Query { get; set; }
        }

        private sealed class DeferredCte : ExprCte
        {
            private readonly CteRegistryEntry _entry;

            public DeferredCte(CteRegistryEntry entry, ExprTableAlias? alias) : base(entry.Name, alias)
            {
                this._entry = entry;
            }

            public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
                => visitor.VisitExprCteQuery(new ExprCteQuery(this._entry.Name, this.Alias, this.CreateQuery()), arg);

            public override IExprSubQuery CreateQuery()
            {
                if (this._entry.Query == null)
                {
                    throw new SqExpressTSqlParserException($"CTE '{this._entry.Name}' query has not been initialized.");
                }

                return this._entry.Query;
            }
        }

        private sealed class SelectParseContext
        {
            private readonly SelectParseContext? _parent;

            private readonly IReadOnlyDictionary<string, CteRegistryEntry> _ctes;

            private readonly Dictionary<string, IExprColumnSource> _byAlias =
                new Dictionary<string, IExprColumnSource>(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, List<IExprColumnSource>> _bySourceName =
                new Dictionary<string, List<IExprColumnSource>>(StringComparer.OrdinalIgnoreCase);

            private readonly List<IExprColumnSource> _sources = new List<IExprColumnSource>();

            private readonly Dictionary<IExprColumnSource, TableIdentity> _tableIdentitiesBySource =
                new Dictionary<IExprColumnSource, TableIdentity>();

            public SelectParseContext(SelectParseContext? parent, IReadOnlyDictionary<string, CteRegistryEntry> ctes)
            {
                this._parent = parent;
                this._ctes = ctes;
            }

            public SelectParseContext CreateChild()
                => new SelectParseContext(this, this._ctes);

            public void RegisterSource(IExprColumnSource source, string? aliasName, string? sourceName, TableIdentity? tableIdentity)
            {
                this._sources.Add(source);

                if (!string.IsNullOrWhiteSpace(aliasName))
                {
                    this._byAlias[aliasName!] = source;
                }

                if (!string.IsNullOrWhiteSpace(sourceName))
                {
                    if (!this._bySourceName.TryGetValue(sourceName!, out var list))
                    {
                        list = new List<IExprColumnSource>();
                        this._bySourceName[sourceName!] = list;
                    }

                    list.Add(source);
                }

                if (tableIdentity != null)
                {
                    this._tableIdentitiesBySource[source] = tableIdentity;
                }
            }

            public bool TryResolveSource(string token, [NotNullWhen(true)] out IExprColumnSource? source)
            {
                if (this._byAlias.TryGetValue(token, out source))
                {
                    return true;
                }

                if (this._bySourceName.TryGetValue(token, out var byName))
                {
                    if (byName.Count == 1)
                    {
                        source = byName[0];
                        return true;
                    }

                    source = null;
                    return false;
                }

                if (this._parent != null)
                {
                    return this._parent.TryResolveSource(token, out source);
                }

                source = null;
                return false;
            }

            public bool TryResolveSingleSource([NotNullWhen(true)] out IExprColumnSource? source)
            {
                if (this._sources.Count == 1)
                {
                    source = this._sources[0];
                    return true;
                }

                if (this._sources.Count == 0 && this._parent != null)
                {
                    return this._parent.TryResolveSingleSource(out source);
                }

                source = null;
                return false;
            }

            public bool HasAnyVisibleSources()
            {
                if (this._sources.Count > 0)
                {
                    return true;
                }

                return this._parent?.HasAnyVisibleSources() == true;
            }

            public bool TryGetCte(string name, [NotNullWhen(true)] out CteRegistryEntry? entry)
                => this._ctes.TryGetValue(name, out entry);

            public bool TryGetTableIdentity(IExprColumnSource source, [NotNullWhen(true)] out TableIdentity? tableIdentity)
            {
                if (this._tableIdentitiesBySource.TryGetValue(source, out tableIdentity))
                {
                    return true;
                }

                if (this._parent != null)
                {
                    return this._parent.TryGetTableIdentity(source, out tableIdentity);
                }

                tableIdentity = null;
                return false;
            }
        }

        private sealed class ProjectedColumn
        {
            public ProjectedColumn(string sourceColumnName, string? outputAlias)
            {
                this.SourceColumnName = sourceColumnName;
                this.OutputAlias = outputAlias;
            }

            public string SourceColumnName { get; }

            public string? OutputAlias { get; }
        }

        private enum BuildPhase
        {
            Collect = 1,
            Emit
        }

        private enum InferredColumnKind
        {
            Int32 = 1,
            NVarChar,
            Boolean,
            Decimal,
            DateTime,
            DateTimeOffset,
            Guid,
            ByteArray
        }

        private sealed class TableIdentity
        {
            public TableIdentity(string? databaseName, string? schemaName, string tableName)
            {
                this.DatabaseName = databaseName;
                this.SchemaName = schemaName;
                this.TableName = tableName;
            }

            public string? DatabaseName { get; }

            public string? SchemaName { get; }

            public string TableName { get; }
        }

        private sealed class TableIdentityComparer : IEqualityComparer<TableIdentity>
        {
            public static readonly TableIdentityComparer Instance = new TableIdentityComparer();

            public bool Equals(TableIdentity? x, TableIdentity? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return string.Equals(x.DatabaseName, y.DatabaseName, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(x.SchemaName, y.SchemaName, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(x.TableName, y.TableName, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(TableIdentity obj)
            {
                unchecked
                {
                    var hashCode = 17;
                    hashCode = (hashCode * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TableName);
                    hashCode = (hashCode * 31)
                               + (obj.SchemaName == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SchemaName));
                    hashCode = (hashCode * 31)
                               + (obj.DatabaseName == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.DatabaseName));
                    return hashCode;
                }
            }
        }

        private sealed class TableColumnMap
        {
            public readonly Dictionary<string, InferredTableColumn> Columns =
                new Dictionary<string, InferredTableColumn>(StringComparer.OrdinalIgnoreCase);

            public int NextOrder { get; set; }
        }

        private sealed class InferredTableColumn
        {
            public InferredTableColumn(string name, InferredColumnKind kind, bool isNullable, int? stringLength, int order)
            {
                this.Name = name;
                this.Kind = kind;
                this.IsNullable = isNullable;
                this.StringLength = stringLength;
                this.Order = order;
            }

            public string Name { get; }

            public InferredColumnKind Kind { get; set; }

            public bool IsNullable { get; set; }

            public int? StringLength { get; set; }

            public int Order { get; }
        }

        private sealed class UpdateParseContext
        {
            private readonly Dictionary<string, ExprTable> _tableByAlias =
                new Dictionary<string, ExprTable>(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, List<ExprTable>> _tablesByName =
                new Dictionary<string, List<ExprTable>>(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, IExprColumnSource> _sourcesByAlias =
                new Dictionary<string, IExprColumnSource>(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, List<IExprColumnSource>> _sourcesByName =
                new Dictionary<string, List<IExprColumnSource>>(StringComparer.OrdinalIgnoreCase);

            private readonly List<ExprTable> _tables = new List<ExprTable>();

            private readonly List<IExprColumnSource> _sources = new List<IExprColumnSource>();

            private readonly List<SourceEntry> _sourceEntries = new List<SourceEntry>();

            private readonly Dictionary<IExprColumnSource, TableIdentity> _tableIdentitiesBySource =
                new Dictionary<IExprColumnSource, TableIdentity>();

            public ExprTable? Target { get; set; }

            public void RegisterTable(ExprTable table, TableIdentity? tableIdentity = null)
            {
                var source = table.Alias != null
                    ? (IExprColumnSource)table.Alias
                    : table.FullName;
                var aliasName = table.Alias?.Alias is ExprAlias alias ? alias.Name : null;
                var tableName = table.FullName.AsExprTableFullName().TableName.Name;

                if (tableIdentity == null && SqExpressTSqlParser.TryGetTableIdentity(table, out var resolvedIdentity))
                {
                    tableIdentity = resolvedIdentity;
                }

                this.RegisterSource(source, aliasName, tableName, tableIdentity, table);
            }

            public void RegisterSource(
                IExprColumnSource source,
                string? aliasName,
                string? sourceName,
                TableIdentity? tableIdentity,
                ExprTable? table)
            {
                this._sources.Add(source);
                this._sourceEntries.Add(new SourceEntry(source, aliasName, sourceName, tableIdentity));

                if (!string.IsNullOrWhiteSpace(aliasName))
                {
                    this._sourcesByAlias[aliasName!] = source;
                }

                if (!string.IsNullOrWhiteSpace(sourceName))
                {
                    if (!this._sourcesByName.TryGetValue(sourceName!, out var sourceList))
                    {
                        sourceList = new List<IExprColumnSource>();
                        this._sourcesByName[sourceName!] = sourceList;
                    }

                    sourceList.Add(source);
                }

                if (tableIdentity != null)
                {
                    this._tableIdentitiesBySource[source] = tableIdentity;
                }

                if (table == null)
                {
                    return;
                }

                this._tables.Add(table);

                var tableName = table.FullName.AsExprTableFullName().TableName.Name;
                if (!this._tablesByName.TryGetValue(tableName, out var tableList))
                {
                    tableList = new List<ExprTable>();
                    this._tablesByName[tableName] = tableList;
                }

                tableList.Add(table);

                if (table.Alias?.Alias is ExprAlias alias)
                {
                    this._tableByAlias[alias.Name] = table;
                }
            }

            public bool TryGetByAlias(string alias, [NotNullWhen(true)] out ExprTable? table)
                => this._tableByAlias.TryGetValue(alias, out table);

            public bool TryGetSingleByTableName(string tableName, [NotNullWhen(true)] out ExprTable? table)
            {
                table = null;
                if (!this._tablesByName.TryGetValue(tableName, out var tables) || tables.Count != 1)
                {
                    return false;
                }

                table = tables[0];
                return true;
            }

            public ExprTable? TryGetSingleTable()
                => this._tables.Count == 1 ? this._tables[0] : null;

            public bool TryResolveSource(string token, [NotNullWhen(true)] out IExprColumnSource? source)
            {
                if (this._sourcesByAlias.TryGetValue(token, out source))
                {
                    return true;
                }

                if (!this._sourcesByName.TryGetValue(token, out var byName))
                {
                    source = null;
                    return false;
                }

                if (byName.Count == 1)
                {
                    source = byName[0];
                    return true;
                }

                source = null;
                return false;
            }

            public bool TryResolveSingleSource([NotNullWhen(true)] out IExprColumnSource? source)
            {
                if (this._sources.Count == 1)
                {
                    source = this._sources[0];
                    return true;
                }

                source = null;
                return false;
            }

            public bool TryGetTableIdentity(IExprColumnSource source, [NotNullWhen(true)] out TableIdentity? tableIdentity)
                => this._tableIdentitiesBySource.TryGetValue(source, out tableIdentity);

            public SelectParseContext CreateSelectContext()
            {
                var context = new SelectParseContext(parent: null, ctes: new Dictionary<string, CteRegistryEntry>());
                foreach (var source in this._sourceEntries)
                {
                    context.RegisterSource(
                        source.Source,
                        source.AliasName,
                        source.SourceName,
                        source.TableIdentity);
                }

                return context;
            }

            private readonly struct SourceEntry
            {
                public SourceEntry(IExprColumnSource source, string? aliasName, string? sourceName, TableIdentity? tableIdentity)
                {
                    this.Source = source;
                    this.AliasName = aliasName;
                    this.SourceName = sourceName;
                    this.TableIdentity = tableIdentity;
                }

                public IExprColumnSource Source { get; }

                public string? AliasName { get; }

                public string? SourceName { get; }

                public TableIdentity? TableIdentity { get; }
            }
        }
    }
}
