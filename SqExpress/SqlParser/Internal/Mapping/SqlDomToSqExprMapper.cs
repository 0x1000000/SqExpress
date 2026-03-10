using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using SqExpress.DbMetadata;
using SqExpress.SqlParser.Internal.Dom;
using SqExpress.SqlParser.Internal.Parsing;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlParser.Internal.Mapping
{
    internal static class SqlDomToSqExprMapper
    {
        public static bool TryMap(
            SqlDomStatement statement,
            [NotNullWhen(true)] out IExpr? result,
            out IReadOnlyList<SqTable>? tables,
            [NotNullWhen(false)] out string? error)
        {
            tables = null;
            try
            {
                var context = new MappingContext(statement.WithClause);
                result = statement.Kind switch
                {
                    SqlDomStatementKind.Select => MapSelect(statement, context),
                    SqlDomStatementKind.Insert => MapInsert(statement, context),
                    SqlDomStatementKind.Update => MapUpdate(statement, context),
                    SqlDomStatementKind.Delete => MapDelete(statement, context),
                    SqlDomStatementKind.Merge => MapMerge(statement, context),
                    _ => throw new MapException("Parsed SQL cannot be mapped to SqExpress AST.")
                };
                if (result == null)
                {
                    throw new MapException("Parsed SQL cannot be mapped to SqExpress AST.");
                }
                error = null;
                return true;
            }
            catch (MapException ex)
            {
                result = null;
                error = ex.Message;
                return false;
            }
        }

        private static IExprSubQuery ParseCteQuery(string sql, MappingContext context)
        {
            if (!SqlDomParser.TryParseSingleStatement(sql, out var statement, out _)
                || statement == null
                || statement.Kind != SqlDomStatementKind.Select)
            {
                throw new MapException("CTE query could not be parsed.");
            }

            var mapped = MapSelect(statement, context);
            if (mapped is IExprSubQuery subQuery)
            {
                return subQuery;
            }

            throw new MapException("CTE query cannot be represented as subquery.");
        }

        private sealed class MappingContext
        {
            private readonly Dictionary<string, SqlDomCte> _domCtes;
            private readonly Dictionary<string, IExprSubQuery> _resolved;
            private readonly Dictionary<string, DeferredSubQuery> _deferred;
            private readonly HashSet<string> _resolving;

            public MappingContext(SqlDomWithClause? withClause)
            {
                this._domCtes = new Dictionary<string, SqlDomCte>(StringComparer.OrdinalIgnoreCase);
                this._resolved = new Dictionary<string, IExprSubQuery>(StringComparer.OrdinalIgnoreCase);
                this._deferred = new Dictionary<string, DeferredSubQuery>(StringComparer.OrdinalIgnoreCase);
                this._resolving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (withClause == null)
                {
                    return;
                }

                for (var i = 0; i < withClause.Ctes.Count; i++)
                {
                    this._domCtes[withClause.Ctes[i].Name] = withClause.Ctes[i];
                }
            }

            public bool TryGetCteReference(string name, string? alias, [NotNullWhen(true)] out ExprCteQuery? cte)
            {
                cte = null;
                if (!this._domCtes.ContainsKey(name))
                {
                    return false;
                }

                if (!this.TryResolveCteQuery(name, out var query) || query == null)
                {
                    return false;
                }

                cte = new ExprCteQuery(
                    name,
                    alias == null ? null : new ExprTableAlias(new ExprAlias(alias)),
                    query);
                return true;
            }

            private bool TryResolveCteQuery(string name, [NotNullWhen(true)] out IExprSubQuery? query)
            {
                if (this._resolved.TryGetValue(name, out query))
                {
                    return true;
                }

                if (!this._domCtes.TryGetValue(name, out var cte))
                {
                    query = null;
                    return false;
                }

                if (this._resolving.Contains(name))
                {
                    query = this.GetDeferred(name);
                    return true;
                }

                this._resolving.Add(name);
                try
                {
                    query = ParseCteQuery(cte.QuerySql, this);
                    this._resolved[name] = query;
                    return true;
                }
                finally
                {
                    this._resolving.Remove(name);
                }
            }

            private DeferredSubQuery GetDeferred(string name)
            {
                if (this._deferred.TryGetValue(name, out var existing))
                {
                    return existing;
                }

                var deferred = new DeferredSubQuery(
                    name,
                    () =>
                    {
                        this._resolved.TryGetValue(name, out var resolved);
                        return resolved;
                    });
                this._deferred[name] = deferred;
                return deferred;
            }
        }

        private sealed class DeferredSubQuery : IExprSubQuery
        {
            private readonly string _cteName;
            private readonly Func<IExprSubQuery?> _resolver;

            public DeferredSubQuery(string cteName, Func<IExprSubQuery?> resolver)
            {
                this._cteName = cteName;
                this._resolver = resolver;
            }

            public IReadOnlyList<string?> GetOutputColumnNames()
                => this.Resolve().GetOutputColumnNames();

            public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
                => this.Resolve().Accept(visitor, arg);

            private IExprSubQuery Resolve()
                => this._resolver() ?? throw new MapException($"CTE '{this._cteName}' could not be resolved.");
        }

        private static IExpr MapSelect(SqlDomStatement statement, MappingContext context)
        {
            var top = statement.TopLevelSelect;
            if (top == null)
            {
                throw new MapException("Parsed SQL cannot be mapped to SqExpress AST.");
            }

            if (top.HasSetOperation)
            {
                return MapSelectWithSetOperation(statement.RawSql, context);
            }

            var selectList = top.Items.Select(i => ParseSelectItem(i, context)).ToList();
            IExprTableSource? from = top.From == null ? null : ParseTableSource(top.From, context);
            ExprBoolean? where = string.IsNullOrWhiteSpace(top.WhereSql) ? null : ParseBoolean(top.WhereSql!, context);
            IReadOnlyList<ExprColumn>? groupBy = null;
            if (!string.IsNullOrWhiteSpace(top.GroupBySql))
            {
                groupBy = SplitComma(top.GroupBySql!).Select(i => ParseValue(i, context) as ExprColumn ?? throw new MapException("GROUP BY supports only columns.")).ToList();
            }

            ExprValue? topExpr = null;
            if (!string.IsNullOrWhiteSpace(top.TopSql))
            {
                topExpr = ParseValue(top.TopSql!, context);
            }

            IExprSubQuery query = new ExprQuerySpecification(selectList, topExpr, top.IsDistinct, from, where, groupBy);

            if (!string.IsNullOrWhiteSpace(top.OrderBySql))
            {
                var order = ParseOrderBy(top.OrderBySql!, context);
                if (!string.IsNullOrWhiteSpace(top.OffsetFetchSql))
                {
                    var (offset, fetch) = ParseOffsetFetch(top.OffsetFetchSql!, context);
                    return new ExprSelectOffsetFetch(query, new ExprOrderByOffsetFetch(order.OrderList, new ExprOffsetFetch(offset, fetch)));
                }

                return new ExprSelect(query, order);
            }

            return query;
        }

        private static IExpr MapSelectWithSetOperation(string sql, MappingContext context)
        {
            var tokens = SqlLexer.Tokenize(sql)
                .Where(t => t.Type != SqlTokenType.EndOfFile && t.Type != SqlTokenType.Semicolon)
                .ToList();
            if (tokens.Count < 1)
            {
                throw new MapException("Set query expression is empty.");
            }

            var firstSelectIndex = FindFirstTopLevelKeyword(tokens, 0, "SELECT");
            if (firstSelectIndex < 0)
            {
                throw new MapException("Set query expression does not contain SELECT.");
            }

            var segmentRanges = new List<(int Start, int End)>();
            var operators = new List<ExprQueryExpressionType>();
            var segmentStart = firstSelectIndex;
            var depth = 0;

            for (var i = firstSelectIndex; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    continue;
                }

                if (tokens[i].Type == SqlTokenType.CloseParen)
                {
                    if (depth > 0)
                    {
                        depth--;
                    }

                    continue;
                }

                if (depth != 0)
                {
                    continue;
                }

                if (tokens[i].IsKeyword("UNION"))
                {
                    segmentRanges.Add((segmentStart, i));
                    if ((i + 1) < tokens.Count && tokens[i + 1].IsKeyword("ALL"))
                    {
                        operators.Add(ExprQueryExpressionType.UnionAll);
                        i++;
                    }
                    else
                    {
                        operators.Add(ExprQueryExpressionType.Union);
                    }

                    segmentStart = i + 1;
                    continue;
                }

                if (tokens[i].IsKeyword("EXCEPT"))
                {
                    segmentRanges.Add((segmentStart, i));
                    operators.Add(ExprQueryExpressionType.Except);
                    segmentStart = i + 1;
                    continue;
                }

                if (tokens[i].IsKeyword("INTERSECT"))
                {
                    segmentRanges.Add((segmentStart, i));
                    operators.Add(ExprQueryExpressionType.Intersect);
                    segmentStart = i + 1;
                }
            }

            segmentRanges.Add((segmentStart, tokens.Count));
            if (segmentRanges.Count < 2)
            {
                throw new MapException("Set query expressions are not supported yet.");
            }

            var last = segmentRanges[segmentRanges.Count - 1];
            var topLevelOrderIndex = FindFirstTopLevelKeyword(tokens, last.Start, "ORDER");
            var topLevelOffsetIndex = FindFirstTopLevelKeyword(tokens, last.Start, "OFFSET");
            var tailStart = MinPositive(topLevelOrderIndex, topLevelOffsetIndex);

            string? orderBySql = null;
            string? offsetFetchSql = null;
            if (tailStart >= 0 && tailStart < last.End)
            {
                var lastSegmentEnd = tailStart;
                segmentRanges[segmentRanges.Count - 1] = (last.Start, lastSegmentEnd);

                if (topLevelOrderIndex >= 0 && topLevelOrderIndex == tailStart)
                {
                    var orderByStart = topLevelOrderIndex + 1;
                    if (orderByStart < tokens.Count && tokens[orderByStart].IsKeyword("BY"))
                    {
                        orderByStart++;
                    }

                    if (topLevelOffsetIndex > topLevelOrderIndex)
                    {
                        orderBySql = SliceSqlByTokenRange(sql, tokens, orderByStart, topLevelOffsetIndex);
                        offsetFetchSql = SliceSqlByTokenRange(sql, tokens, topLevelOffsetIndex, tokens.Count);
                    }
                    else
                    {
                        orderBySql = SliceSqlByTokenRange(sql, tokens, orderByStart, tokens.Count);
                    }
                }
                else if (topLevelOffsetIndex >= 0 && topLevelOffsetIndex == tailStart)
                {
                    offsetFetchSql = SliceSqlByTokenRange(sql, tokens, topLevelOffsetIndex, tokens.Count);
                }
            }

            var segments = segmentRanges
                .Select(r => SliceSqlByTokenRange(sql, tokens, r.Start, r.End))
                .ToList();
            if (segments.Any(string.IsNullOrWhiteSpace))
            {
                throw new MapException("Set query expression contains an empty branch.");
            }

            var queries = segments.Select(i => ParseSetSegment(i, context)).ToList();
            IExprSubQuery setQuery = queries[0];
            for (var i = 0; i < operators.Count; i++)
            {
                setQuery = new ExprQueryExpression(setQuery, queries[i + 1], operators[i]);
            }

            if (!string.IsNullOrWhiteSpace(offsetFetchSql))
            {
                var (offset, fetch) = ParseOffsetFetch(offsetFetchSql!, context);
                var orderList = !string.IsNullOrWhiteSpace(orderBySql)
                    ? ParseOrderBy(orderBySql!, context).OrderList
                    : Array.Empty<ExprOrderByItem>();
                return new ExprSelectOffsetFetch(setQuery, new ExprOrderByOffsetFetch(orderList, new ExprOffsetFetch(offset, fetch)));
            }

            if (!string.IsNullOrWhiteSpace(orderBySql))
            {
                var order = ParseOrderBy(orderBySql!, context);
                return new ExprSelect(setQuery, order);
            }

            return setQuery;
        }

        private static IExpr MapSelectWithSetOperation(string sql)
            => MapSelectWithSetOperation(sql, new MappingContext(null));

        private static IExprSubQuery ParseSetSegment(string sql, MappingContext context)
        {
            if (!SqlDomParser.TryParseSingleStatement(sql, out var statement, out _)
                || statement == null
                || statement.Kind != SqlDomStatementKind.Select)
            {
                throw new MapException("Set query branch could not be parsed.");
            }

            var mapped = MapSelect(statement, context);
            if (mapped is IExprSubQuery subQuery)
            {
                return subQuery;
            }

            throw new MapException("Set query branch cannot be represented as subquery.");
        }

        private static IExpr MapUpdate(SqlDomStatement statement, MappingContext context)
        {
            var sql = statement.RawSql;
            var tokens = SqlLexer.Tokenize(sql)
                .Where(t => t.Type != SqlTokenType.EndOfFile && t.Type != SqlTokenType.Semicolon)
                .ToList();

            var updatePos = FindFirstTopLevelKeyword(tokens, 0, "UPDATE");
            var setPos = updatePos < 0 ? -1 : FindFirstTopLevelKeyword(tokens, updatePos + 1, "SET");
            if (setPos < 0 || setPos + 1 >= tokens.Count)
            {
                throw new MapException("UPDATE statement must contain SET clause.");
            }

            var fromPos = FindFirstTopLevelKeyword(tokens, setPos + 1, "FROM");
            var wherePos = FindFirstTopLevelKeyword(tokens, setPos + 1, "WHERE");
            var outputPos = FindFirstTopLevelKeyword(tokens, setPos + 1, "OUTPUT");

            var setEnd = tokens.Count;
            if (fromPos >= 0)
            {
                setEnd = Math.Min(setEnd, fromPos);
            }

            if (wherePos >= 0)
            {
                setEnd = Math.Min(setEnd, wherePos);
            }

            if (outputPos >= 0)
            {
                setEnd = Math.Min(setEnd, outputPos);
            }

            if (setEnd <= setPos + 1)
            {
                throw new MapException("Invalid UPDATE SET clause.");
            }

            var setSql = SliceSqlByTokenRange(sql, tokens, setPos + 1, setEnd);
            var setList = ParseSetClauses(setSql, context);

            if (outputPos >= 0)
            {
                throw new MapException("Feature 'OUTPUT' is not supported by SqExpress parser for UPDATE statements.");
            }

            var targetCursor = updatePos + 1;
            if (targetCursor < tokens.Count && tokens[targetCursor].IsKeyword("TOP"))
            {
                targetCursor++;
                if (targetCursor < tokens.Count && tokens[targetCursor].Type == SqlTokenType.OpenParen)
                {
                    var close = FindMatchingCloseParen(tokens, targetCursor);
                    if (close < 0)
                    {
                        throw new MapException("UPDATE TOP clause is invalid.");
                    }

                    targetCursor = close + 1;
                }
                else if (targetCursor < tokens.Count)
                {
                    targetCursor++;
                }

                if (targetCursor < tokens.Count && tokens[targetCursor].IsKeyword("PERCENT"))
                {
                    targetCursor++;
                }
            }

            var targetParts = ReadMultipartIdentifier(tokens, ref targetCursor);
            if (targetParts.Count < 1)
            {
                throw new MapException("UPDATE target table is not resolved.");
            }

            string? targetAlias = null;
            if (targetCursor < setPos && tokens[targetCursor].IsIdentifierLike)
            {
                targetAlias = tokens[targetCursor].IdentifierValue;
            }

            IExprTableSource? source = null;
            if (fromPos >= 0)
            {
                var fromEnd = wherePos >= 0 ? wherePos : tokens.Count;
                if (fromEnd <= fromPos + 1)
                {
                    throw new MapException("UPDATE FROM clause is invalid.");
                }

                var fromSql = SliceSqlByTokenRange(sql, tokens, fromPos + 1, fromEnd);
                source = ParseTableSourceSql(fromSql, context);
            }

            var target = ResolveUpdateTarget(targetParts, targetAlias, source, statement);
            if (target == null)
            {
                throw new MapException("UPDATE target table is not resolved.");
            }

            ExprBoolean? filter = null;
            if (wherePos >= 0)
            {
                filter = ParseBoolean(SliceSqlByTokenRange(sql, tokens, wherePos + 1, tokens.Count), context);
            }

            return new ExprUpdate(target, setList, source, filter);
        }

        private static IExpr MapUpdate(SqlDomStatement statement)
            => MapUpdate(statement, new MappingContext(null));

        private static IExpr MapInsert(SqlDomStatement statement, MappingContext context)
        {
            var sql = statement.RawSql;
            var tokens = SqlLexer.Tokenize(sql)
                .Where(t => t.Type != SqlTokenType.EndOfFile && t.Type != SqlTokenType.Semicolon)
                .ToList();
            if (tokens.Count < 3)
            {
                throw new MapException("INSERT statement is invalid.");
            }

            var intoIndex = FindFirstTopLevelKeyword(tokens, 0, "INTO");
            if (intoIndex < 0 || intoIndex + 1 >= tokens.Count)
            {
                throw new MapException("INSERT target table is not resolved.");
            }

            var cursor = intoIndex + 1;
            var nameParts = ReadMultipartIdentifier(tokens, ref cursor);
            if (nameParts.Count < 1)
            {
                throw new MapException("INSERT target table is not resolved.");
            }

            var target = BuildTableFullName(nameParts);

            IReadOnlyList<ExprColumnName>? targetColumns = null;
            if (cursor < tokens.Count && tokens[cursor].Type == SqlTokenType.OpenParen)
            {
                var close = FindMatchingCloseParen(tokens, cursor);
                if (close < 0)
                {
                    throw new MapException("INSERT target column list is invalid.");
                }

                var columns = SplitComma(tokens.Skip(cursor + 1).Take(close - cursor - 1).ToList())
                    .Select(i =>
                    {
                        var parts = i.Where(t => t.IsIdentifierLike).ToList();
                        if (parts.Count < 1)
                        {
                            throw new MapException("INSERT target column list is invalid.");
                        }

                        return new ExprColumnName(parts[parts.Count - 1].IdentifierValue);
                    })
                    .ToList();

                targetColumns = columns;
                cursor = close + 1;
            }

            var outputIndex = FindFirstTopLevelKeyword(tokens, cursor, "OUTPUT");
            var sourceSearchStart = outputIndex >= 0 ? outputIndex + 1 : cursor;

            var sourceStart = FindFirstTopLevelKeyword(tokens, sourceSearchStart, "VALUES");
            var sourceIsValues = true;
            if (sourceStart < 0)
            {
                sourceStart = FindFirstTopLevelKeyword(tokens, sourceSearchStart, "SELECT");
                sourceIsValues = false;
            }

            if (sourceStart < 0)
            {
                throw new MapException("INSERT source is not resolved.");
            }

            IReadOnlyList<ExprAliasedColumnName>? outputColumns = null;
            if (outputIndex >= 0)
            {
                if (sourceStart <= outputIndex + 1)
                {
                    throw new MapException("INSERT OUTPUT clause is invalid.");
                }

                outputColumns = ParseInsertOutputColumns(tokens, outputIndex + 1, sourceStart);
            }

            IExprInsertSource source;
            if (sourceIsValues)
            {
                var rows = ParseInsertValues(tokens, sourceStart, context);
                source = new ExprInsertValues(rows);
            }
            else
            {
                var querySql = SliceSqlByTokenRange(sql, tokens, sourceStart, tokens.Count);
                var query = ParseNestedSubQuery(querySql, context);
                source = new ExprInsertQuery(query);
            }

            var insert = new ExprInsert(target, targetColumns, source);
            if (outputColumns != null)
            {
                return new ExprInsertOutput(insert, outputColumns);
            }

            return insert;
        }

        private static IExpr MapInsert(SqlDomStatement statement)
            => MapInsert(statement, new MappingContext(null));

        private static IExpr MapDelete(SqlDomStatement statement, MappingContext context)
        {
            var sql = statement.RawSql;
            var tokens = SqlLexer.Tokenize(sql)
                .Where(t => t.Type != SqlTokenType.EndOfFile && t.Type != SqlTokenType.Semicolon)
                .ToList();
            if (tokens.Count < 2)
            {
                throw new MapException("DELETE statement is invalid.");
            }

            var fromIndex = FindFirstTopLevelKeyword(tokens, 0, "FROM");
            if (fromIndex < 0 || fromIndex + 1 >= tokens.Count)
            {
                throw new MapException("DELETE statement must contain FROM clause.");
            }

            string? targetAlias = null;
            if ((1 < tokens.Count)
                && !tokens[1].IsKeyword("FROM")
                && !tokens[1].IsKeyword("TOP")
                && tokens[1].IsIdentifierLike)
            {
                targetAlias = tokens[1].IdentifierValue;
            }

            var outputIndex = FindFirstTopLevelKeyword(tokens, 0, "OUTPUT");
            var whereIndex = FindFirstTopLevelKeyword(tokens, fromIndex + 1, "WHERE");
            var fromEnd = whereIndex >= 0 ? whereIndex : tokens.Count;
            var fromSliceEnd = outputIndex >= 0 && outputIndex > fromIndex && outputIndex < fromEnd ? outputIndex : fromEnd;
            var fromSql = SliceSqlByTokenRange(sql, tokens, fromIndex + 1, fromSliceEnd);
            var source = ParseTableSourceSql(fromSql, context);

            IReadOnlyList<ExprAliasedColumn>? outputColumns = null;
            if (outputIndex >= 0)
            {
                if (outputIndex < fromIndex)
                {
                    outputColumns = ParseDeleteOutputColumns(tokens, outputIndex + 1, fromIndex);
                }
                else if (outputIndex > fromIndex && outputIndex < fromEnd)
                {
                    outputColumns = ParseDeleteOutputColumns(tokens, outputIndex + 1, fromEnd);
                }
                else
                {
                    throw new MapException("DELETE OUTPUT clause is invalid.");
                }
            }

            ExprBoolean? filter = null;
            if (whereIndex >= 0)
            {
                var whereSql = SliceSqlByTokenRange(sql, tokens, whereIndex + 1, tokens.Count);
                filter = ParseBoolean(whereSql, context);
            }

            var target = ResolveDeleteTarget(statement, source, targetAlias);

            IExprTableSource? deleteSource = source;
            if (source is ExprTable tableSource
                && target.FullName.Equals(tableSource.FullName)
                && string.Equals(GetAliasName(target.Alias), GetAliasName(tableSource.Alias), StringComparison.OrdinalIgnoreCase))
            {
                deleteSource = null;
            }

            var delete = new ExprDelete(target, deleteSource, filter);
            if (outputColumns != null)
            {
                return new ExprDeleteOutput(delete, outputColumns);
            }

            return delete;
        }

        private static IExpr MapDelete(SqlDomStatement statement)
            => MapDelete(statement, new MappingContext(null));

        private static IExpr MapMerge(SqlDomStatement statement, MappingContext context)
        {
            var sql = statement.RawSql;
            var tokens = SqlLexer.Tokenize(sql)
                .Where(t => t.Type != SqlTokenType.EndOfFile && t.Type != SqlTokenType.Semicolon)
                .ToList();
            if (tokens.Count < 6)
            {
                throw new MapException("MERGE statement is invalid.");
            }

            if (FindFirstTopLevelKeyword(tokens, 0, "OUTPUT") >= 0)
            {
                throw new MapException("Feature 'OUTPUT' is not supported by SqExpress parser for MERGE statements.");
            }

            var mergeIndex = FindFirstTopLevelKeyword(tokens, 0, "MERGE");
            var usingIndex = FindFirstTopLevelKeyword(tokens, 0, "USING");
            var onIndex = FindFirstTopLevelKeyword(tokens, 0, "ON");
            if (mergeIndex < 0 || usingIndex < 0 || onIndex < 0 || !(mergeIndex < usingIndex && usingIndex < onIndex))
            {
                throw new MapException("MERGE statement must contain target, source and ON clause.");
            }

            var targetCursor = mergeIndex + 1;
            if (targetCursor < usingIndex && tokens[targetCursor].IsKeyword("INTO"))
            {
                targetCursor++;
            }

            var targetParts = ReadMultipartIdentifier(tokens, ref targetCursor);
            if (targetParts.Count < 1)
            {
                throw new MapException("MERGE target table is not resolved.");
            }

            var targetAlias = targetCursor < usingIndex && tokens[targetCursor].IsIdentifierLike
                ? tokens[targetCursor].IdentifierValue
                : targetParts[targetParts.Count - 1];
            var targetTable = new ExprTable(
                BuildTableFullName(targetParts),
                new ExprTableAlias(new ExprAlias(targetAlias)));

            var sourceSql = SliceSqlByTokenRange(sql, tokens, usingIndex + 1, onIndex);
            var source = ParseTableSourceSql(sourceSql, context);

            var firstWhen = FindFirstTopLevelKeyword(tokens, onIndex + 1, "WHEN");
            if (firstWhen < 0)
            {
                throw new MapException("MERGE statement must contain at least one WHEN clause.");
            }

            var onSql = SliceSqlByTokenRange(sql, tokens, onIndex + 1, firstWhen);
            var on = ParseBoolean(onSql, context);

            IExprMergeMatched? whenMatched = null;
            IExprMergeNotMatched? whenNotMatchedByTarget = null;
            IExprMergeMatched? whenNotMatchedBySource = null;

            var clauseStart = firstWhen;
            while (clauseStart >= 0 && clauseStart < tokens.Count)
            {
                var nextWhen = FindFirstTopLevelKeyword(tokens, clauseStart + 1, "WHEN");
                var clauseEnd = nextWhen >= 0 ? nextWhen : tokens.Count;
                ParseMergeClause(
                    sql,
                    tokens,
                    clauseStart,
                    clauseEnd,
                    context,
                    ref whenMatched,
                    ref whenNotMatchedByTarget,
                    ref whenNotMatchedBySource);

                clauseStart = nextWhen;
            }

            return new ExprMerge(targetTable, source, on, whenMatched, whenNotMatchedByTarget, whenNotMatchedBySource);
        }

        private static IExpr MapMerge(SqlDomStatement statement)
            => MapMerge(statement, new MappingContext(null));

        private static IReadOnlyList<ExprInsertValueRow> ParseInsertValues(IReadOnlyList<SqlToken> tokens, int valuesIndex, MappingContext context)
        {
            var rows = new List<ExprInsertValueRow>();
            var i = valuesIndex + 1;
            while (i < tokens.Count)
            {
                if (tokens[i].Type == SqlTokenType.Comma)
                {
                    i++;
                    continue;
                }

                if (tokens[i].Type != SqlTokenType.OpenParen)
                {
                    break;
                }

                var close = FindMatchingCloseParen(tokens, i);
                if (close < 0)
                {
                    throw new MapException("INSERT VALUES row is invalid.");
                }

                var rowItems = SplitComma(tokens.Skip(i + 1).Take(close - i - 1).ToList())
                    .Select(segment => ParseAssigning(string.Join(" ", segment.Select(t => t.Text)), context))
                    .ToList();
                rows.Add(new ExprInsertValueRow(rowItems));
                i = close + 1;
            }

            if (rows.Count < 1)
            {
                throw new MapException("INSERT VALUES source is empty.");
            }

            return rows;
        }

        private static IReadOnlyList<ExprAliasedColumnName> ParseInsertOutputColumns(IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            var segments = SplitComma(tokens.Skip(startInclusive).Take(endExclusive - startInclusive).ToList());
            var result = new List<ExprAliasedColumnName>(segments.Count);
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Count < 1)
                {
                    continue;
                }

                string? alias;
                IReadOnlyList<SqlToken> bodyTokens;
                if (!TryExtractAlias(segment, out alias, out bodyTokens))
                {
                    alias = null;
                    bodyTokens = segment;
                }

                var columnName = ParseOutputColumnName(bodyTokens, "INSERTED");
                result.Add(new ExprAliasedColumnName(columnName, alias == null ? null : new ExprColumnAlias(alias)));
            }

            if (result.Count < 1)
            {
                throw new MapException("INSERT OUTPUT clause is invalid.");
            }

            return result;
        }

        private static IReadOnlyList<ExprAliasedColumn> ParseDeleteOutputColumns(IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            var segments = SplitComma(tokens.Skip(startInclusive).Take(endExclusive - startInclusive).ToList());
            var result = new List<ExprAliasedColumn>(segments.Count);
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Count < 1)
                {
                    continue;
                }

                string? alias;
                IReadOnlyList<SqlToken> bodyTokens;
                if (!TryExtractAlias(segment, out alias, out bodyTokens))
                {
                    alias = null;
                    bodyTokens = segment;
                }

                var columnName = ParseOutputColumnName(bodyTokens, "DELETED");
                var column = new ExprColumn(null, columnName);
                result.Add(new ExprAliasedColumn(column, alias == null ? null : new ExprColumnAlias(alias)));
            }

            if (result.Count < 1)
            {
                throw new MapException("DELETE OUTPUT clause is invalid.");
            }

            return result;
        }

        private static ExprColumnName ParseOutputColumnName(IReadOnlyList<SqlToken> bodyTokens, string expectedPrefix)
        {
            if (bodyTokens.Count == 1 && bodyTokens[0].IsIdentifierLike)
            {
                return new ExprColumnName(bodyTokens[0].IdentifierValue);
            }

            if (bodyTokens.Count == 3
                && bodyTokens[0].IsIdentifierLike
                && bodyTokens[1].Type == SqlTokenType.Dot
                && bodyTokens[2].IsIdentifierLike)
            {
                if (!string.Equals(bodyTokens[0].IdentifierValue, expectedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    throw new MapException($"Only '{expectedPrefix}.[Column]' is supported in OUTPUT clause.");
                }

                return new ExprColumnName(bodyTokens[2].IdentifierValue);
            }

            throw new MapException("OUTPUT clause can contain only projected columns.");
        }

        private static IExprAssigning ParseAssigning(string sql, MappingContext context)
        {
            if (string.Equals(sql.Trim(), "DEFAULT", StringComparison.OrdinalIgnoreCase))
            {
                return ExprDefault.Instance;
            }

            return ParseValue(sql, context);
        }

        private static IExprAssigning ParseAssigning(string sql)
            => ParseAssigning(sql, new MappingContext(null));

        private static void ParseMergeClause(
            string rawSql,
            IReadOnlyList<SqlToken> tokens,
            int clauseStart,
            int clauseEnd,
            MappingContext context,
            ref IExprMergeMatched? whenMatched,
            ref IExprMergeNotMatched? whenNotMatchedByTarget,
            ref IExprMergeMatched? whenNotMatchedBySource)
        {
            if (clauseStart >= clauseEnd || !tokens[clauseStart].IsKeyword("WHEN"))
            {
                throw new MapException("MERGE WHEN clause is invalid.");
            }

            var idx = clauseStart + 1;
            if (idx >= clauseEnd)
            {
                throw new MapException("MERGE WHEN clause is invalid.");
            }

            var thenIndex = FindFirstTopLevelKeyword(tokens, idx, clauseEnd, "THEN");
            if (thenIndex < 0 || thenIndex + 1 >= clauseEnd)
            {
                throw new MapException("MERGE WHEN clause must contain THEN action.");
            }

            if (tokens[idx].IsKeyword("MATCHED"))
            {
                idx++;
                ExprBoolean? and = null;
                if (idx < thenIndex && tokens[idx].IsKeyword("AND"))
                {
                    and = ParseBoolean(SliceSqlByTokenRange(rawSql, tokens, idx + 1, thenIndex), context);
                }

                var actionIndex = thenIndex + 1;
                if (tokens[actionIndex].IsKeyword("DELETE"))
                {
                    whenMatched = new ExprMergeMatchedDelete(and);
                    return;
                }

                if (tokens[actionIndex].IsKeyword("UPDATE"))
                {
                    var setIndex = FindFirstTopLevelKeyword(tokens, actionIndex + 1, clauseEnd, "SET");
                    if (setIndex < 0)
                    {
                        throw new MapException("MERGE UPDATE action must contain SET clause.");
                    }

                    var setSql = SliceSqlByTokenRange(rawSql, tokens, setIndex + 1, clauseEnd);
                    whenMatched = new ExprMergeMatchedUpdate(and, ParseSetClauses(setSql, context));
                    return;
                }

                throw new MapException("MERGE WHEN MATCHED action is not supported.");
            }

            if (tokens[idx].IsKeyword("NOT"))
            {
                idx++;
                if (!(idx < thenIndex && tokens[idx].IsKeyword("MATCHED")))
                {
                    throw new MapException("MERGE WHEN NOT clause is invalid.");
                }

                idx++;
                var bySource = false;
                if (idx < thenIndex && tokens[idx].IsKeyword("BY"))
                {
                    idx++;
                    if (!(idx < thenIndex && (tokens[idx].IsKeyword("SOURCE") || tokens[idx].IsKeyword("TARGET"))))
                    {
                        throw new MapException("MERGE WHEN NOT MATCHED BY clause is invalid.");
                    }

                    bySource = tokens[idx].IsKeyword("SOURCE");
                    idx++;
                }

                ExprBoolean? and = null;
                if (idx < thenIndex && tokens[idx].IsKeyword("AND"))
                {
                    and = ParseBoolean(SliceSqlByTokenRange(rawSql, tokens, idx + 1, thenIndex), context);
                }

                var actionIndex = thenIndex + 1;
                if (bySource)
                {
                    if (tokens[actionIndex].IsKeyword("DELETE"))
                    {
                        whenNotMatchedBySource = new ExprMergeMatchedDelete(and);
                        return;
                    }

                    if (tokens[actionIndex].IsKeyword("UPDATE"))
                    {
                        var setIndex = FindFirstTopLevelKeyword(tokens, actionIndex + 1, clauseEnd, "SET");
                        if (setIndex < 0)
                        {
                            throw new MapException("MERGE UPDATE action must contain SET clause.");
                        }

                        var setSql = SliceSqlByTokenRange(rawSql, tokens, setIndex + 1, clauseEnd);
                        whenNotMatchedBySource = new ExprMergeMatchedUpdate(and, ParseSetClauses(setSql, context));
                        return;
                    }

                    throw new MapException("MERGE WHEN NOT MATCHED BY SOURCE action is not supported.");
                }

                if (!tokens[actionIndex].IsKeyword("INSERT"))
                {
                    throw new MapException("MERGE WHEN NOT MATCHED action must be INSERT.");
                }

                var actionCursor = actionIndex + 1;
                IReadOnlyList<ExprColumnName> columns;
                if (actionCursor < clauseEnd && tokens[actionCursor].Type == SqlTokenType.OpenParen)
                {
                    var close = FindMatchingCloseParen(tokens, actionCursor);
                    if (close < 0 || close >= clauseEnd)
                    {
                        throw new MapException("MERGE INSERT column list is invalid.");
                    }

                    columns = SplitComma(tokens.Skip(actionCursor + 1).Take(close - actionCursor - 1).ToList())
                        .Select(i =>
                        {
                            var columnToken = i.LastOrDefault(t => t.IsIdentifierLike);
                            if (!columnToken.IsIdentifierLike)
                            {
                                throw new MapException("MERGE INSERT column list is invalid.");
                            }

                            return new ExprColumnName(columnToken.IdentifierValue);
                        })
                        .ToList();
                    actionCursor = close + 1;
                }
                else
                {
                    columns = Array.Empty<ExprColumnName>();
                }

                if (actionCursor < clauseEnd
                    && tokens[actionCursor].IsKeyword("DEFAULT")
                    && (actionCursor + 1) < clauseEnd
                    && tokens[actionCursor + 1].IsKeyword("VALUES"))
                {
                    whenNotMatchedByTarget = new ExprExprMergeNotMatchedInsertDefault(and);
                    return;
                }

                if (!(actionCursor < clauseEnd && tokens[actionCursor].IsKeyword("VALUES")))
                {
                    throw new MapException("MERGE INSERT action must contain VALUES.");
                }

                actionCursor++;
                if (!(actionCursor < clauseEnd && tokens[actionCursor].Type == SqlTokenType.OpenParen))
                {
                    throw new MapException("MERGE INSERT VALUES row is invalid.");
                }

                var valuesClose = FindMatchingCloseParen(tokens, actionCursor);
                if (valuesClose < 0 || valuesClose > clauseEnd)
                {
                    throw new MapException("MERGE INSERT VALUES row is invalid.");
                }

                var values = SplitComma(tokens.Skip(actionCursor + 1).Take(valuesClose - actionCursor - 1).ToList())
                    .Select(i => ParseAssigning(string.Join(" ", i.Select(t => t.Text)), context))
                    .ToList();
                whenNotMatchedByTarget = new ExprExprMergeNotMatchedInsert(and, columns, values);
                return;
            }

            throw new MapException("MERGE WHEN clause is not supported.");
        }

        private static IReadOnlyList<ExprColumnSetClause> ParseSetClauses(string setSql, MappingContext context)
            => SplitComma(setSql)
                .Select(i =>
                {
                    var eq = i.IndexOf('=');
                    if (eq < 1)
                    {
                        throw new MapException("Invalid SET clause.");
                    }

                    var leftColumn = ParseValue(i.Substring(0, eq).Trim(), context) as ExprColumn;
                    if (leftColumn is null)
                    {
                        throw new MapException("SET left side must be a column.");
                    }

                    var right = ParseValue(i.Substring(eq + 1).Trim(), context);
                    return new ExprColumnSetClause(leftColumn, right);
                })
                .ToList();

        private static IReadOnlyList<ExprColumnSetClause> ParseSetClauses(string setSql)
            => ParseSetClauses(setSql, new MappingContext(null));

        private static ExprTable ResolveDeleteTarget(SqlDomStatement statement, IExprTableSource source, string? targetAlias)
        {
            if (!string.IsNullOrWhiteSpace(targetAlias))
            {
                var byAlias = statement.TableReferences
                    .FirstOrDefault(t => string.Equals(t.Alias, targetAlias, StringComparison.OrdinalIgnoreCase));
                if (byAlias != null)
                {
                    return BuildTable(byAlias.Schema, byAlias.Table, byAlias.Alias);
                }

                if (TryFindTableByAlias(source, targetAlias!, out var sourceTable))
                {
                    return sourceTable!;
                }
            }

            if (source is ExprTable singleSource)
            {
                return singleSource;
            }

            if (statement.TableReferences.Count > 0)
            {
                var table = statement.TableReferences[0];
                return BuildTable(table.Schema, table.Table, table.Alias);
            }

            throw new MapException("DELETE target table is not resolved.");
        }

        private static ExprTable? ResolveUpdateTarget(
            IReadOnlyList<string> targetNameParts,
            string? targetAlias,
            IExprTableSource? source,
            SqlDomStatement statement)
        {
            if (source != null)
            {
                var aliasCandidate = targetAlias ?? (targetNameParts.Count == 1 ? targetNameParts[0] : null);
                if (!string.IsNullOrWhiteSpace(aliasCandidate)
                    && TryFindTableByAlias(source, aliasCandidate!, out var byAlias))
                {
                    return byAlias!;
                }

                string targetTableName = targetNameParts[targetNameParts.Count - 1];
                string? targetSchemaName = targetNameParts.Count >= 2 ? targetNameParts[targetNameParts.Count - 2] : null;
                if (TryFindTableByName(source, targetSchemaName, targetTableName, out var byName))
                {
                    return byName!;
                }
            }

            if (targetNameParts.Count == 1)
            {
                var token = targetNameParts[0];
                var byAlias = statement.TableReferences.FirstOrDefault(i => string.Equals(i.Alias, token, StringComparison.OrdinalIgnoreCase));
                if (byAlias != null)
                {
                    return BuildTable(byAlias.Schema, byAlias.Table, byAlias.Alias);
                }

                var byTable = statement.TableReferences.FirstOrDefault(i => string.Equals(i.Table, token, StringComparison.OrdinalIgnoreCase));
                if (byTable != null)
                {
                    return BuildTable(byTable.Schema, byTable.Table, byTable.Alias);
                }
            }
            else
            {
                return new ExprTable(
                    BuildTableFullName(targetNameParts),
                    string.IsNullOrWhiteSpace(targetAlias) ? null : new ExprTableAlias(new ExprAlias(targetAlias!)));
            }

            if (statement.TableReferences.Count > 0)
            {
                var first = statement.TableReferences[0];
                return BuildTable(first.Schema, first.Table, first.Alias);
            }

            return null;
        }

        private static bool TryFindTableByAlias(IExprTableSource source, string alias, [NotNullWhen(true)] out ExprTable? table)
        {
            if (source is ExprTable t)
            {
                if (string.Equals(GetAliasName(t.Alias), alias, StringComparison.OrdinalIgnoreCase))
                {
                    table = t;
                    return true;
                }

                table = null;
                return false;
            }

            if (source is ExprJoinedTable join)
            {
                if (TryFindTableByAlias(join.Left, alias, out table))
                {
                    return true;
                }

                return TryFindTableByAlias(join.Right, alias, out table);
            }

            if (source is ExprCrossedTable cross)
            {
                if (TryFindTableByAlias(cross.Left, alias, out table))
                {
                    return true;
                }

                return TryFindTableByAlias(cross.Right, alias, out table);
            }

            if (source is ExprLateralCrossedTable lateral)
            {
                if (TryFindTableByAlias(lateral.Left, alias, out table))
                {
                    return true;
                }

                return TryFindTableByAlias(lateral.Right, alias, out table);
            }

            table = null;
            return false;
        }

        private static bool TryFindTableByName(IExprTableSource source, string? schemaName, string tableName, [NotNullWhen(true)] out ExprTable? table)
        {
            if (source is ExprTable t)
            {
                var full = t.FullName.AsExprTableFullName();
                if (string.Equals(full.TableName.Name, tableName, StringComparison.OrdinalIgnoreCase)
                    && (schemaName == null || string.Equals(full.DbSchema?.Schema.Name, schemaName, StringComparison.OrdinalIgnoreCase)))
                {
                    table = t;
                    return true;
                }

                table = null;
                return false;
            }

            if (source is ExprJoinedTable join)
            {
                if (TryFindTableByName(join.Left, schemaName, tableName, out table))
                {
                    return true;
                }

                return TryFindTableByName(join.Right, schemaName, tableName, out table);
            }

            if (source is ExprCrossedTable cross)
            {
                if (TryFindTableByName(cross.Left, schemaName, tableName, out table))
                {
                    return true;
                }

                return TryFindTableByName(cross.Right, schemaName, tableName, out table);
            }

            if (source is ExprLateralCrossedTable lateral)
            {
                if (TryFindTableByName(lateral.Left, schemaName, tableName, out table))
                {
                    return true;
                }

                return TryFindTableByName(lateral.Right, schemaName, tableName, out table);
            }

            table = null;
            return false;
        }

        private static string? GetAliasName(ExprTableAlias? alias)
            => alias?.Alias is ExprAlias exprAlias ? exprAlias.Name : null;

        private static IExprTableSource ParseTableSourceSql(string fromSql, MappingContext context)
        {
            if (!SqlDomParser.TryParseSingleStatement("SELECT 1 FROM " + fromSql, out var statement, out _)
                || statement == null
                || statement.TopLevelSelect?.From == null)
            {
                throw new MapException("Table source is not supported.");
            }

            return ParseTableSource(statement.TopLevelSelect.From, context);
        }

        private static IExprTableSource ParseTableSourceSql(string fromSql)
            => ParseTableSourceSql(fromSql, new MappingContext(null));

        private static ExprTable BuildTable(string? schema, string table, string? alias)
            => new ExprTable(
                new ExprTableFullName(
                    new ExprDbSchema(null, new ExprSchemaName(string.IsNullOrWhiteSpace(schema) ? "dbo" : schema!)),
                    new ExprTableName(table)),
                string.IsNullOrWhiteSpace(alias) ? null : new ExprTableAlias(new ExprAlias(alias!)));

        private static ExprTableFullName BuildTableFullName(IReadOnlyList<string> nameParts)
        {
            if (nameParts.Count < 1)
            {
                throw new MapException("Table name is missing.");
            }

            var table = nameParts[nameParts.Count - 1];
            var schema = nameParts.Count >= 2 ? nameParts[nameParts.Count - 2] : "dbo";
            return new ExprTableFullName(
                new ExprDbSchema(null, new ExprSchemaName(schema)),
                new ExprTableName(table));
        }

        private static List<string> ReadMultipartIdentifier(IReadOnlyList<SqlToken> tokens, ref int index)
        {
            var result = new List<string>();
            if (index >= tokens.Count || !tokens[index].IsIdentifierLike)
            {
                return result;
            }

            result.Add(tokens[index].IdentifierValue);
            index++;

            while ((index + 1) < tokens.Count
                   && tokens[index].Type == SqlTokenType.Dot
                   && tokens[index + 1].IsIdentifierLike)
            {
                index++;
                result.Add(tokens[index].IdentifierValue);
                index++;
            }

            return result;
        }

        private static int FindMatchingCloseParen(IReadOnlyList<SqlToken> tokens, int openIndex)
        {
            var depth = 0;
            for (var i = openIndex; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    continue;
                }

                if (tokens[i].Type == SqlTokenType.CloseParen)
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

        private static int FindFirstTopLevelKeyword(IReadOnlyList<SqlToken> tokens, int startIndex, string keyword)
            => FindFirstTopLevelKeyword(tokens, startIndex, tokens.Count, keyword);

        private static int FindFirstTopLevelKeyword(IReadOnlyList<SqlToken> tokens, int startIndex, int endExclusive, string keyword)
        {
            var depth = 0;
            for (var i = startIndex; i < endExclusive; i++)
            {
                if (tokens[i].Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    continue;
                }

                if (tokens[i].Type == SqlTokenType.CloseParen)
                {
                    if (depth > 0)
                    {
                        depth--;
                    }

                    continue;
                }

                if (depth == 0 && tokens[i].IsKeyword(keyword))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int MinPositive(int first, int second)
        {
            if (first < 0)
            {
                return second;
            }

            if (second < 0)
            {
                return first;
            }

            return Math.Min(first, second);
        }

        private static string SliceSqlByTokenRange(string sql, IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            if (startInclusive < 0 || endExclusive > tokens.Count || startInclusive >= endExclusive)
            {
                return string.Empty;
            }

            var start = tokens[startInclusive].Start;
            var end = tokens[endExclusive - 1].End;
            return sql.Substring(start, end - start).Trim();
        }

        private static IExprSelecting ParseSelectItem(SqlDomSelectItem item, MappingContext context)
        {
            var itemSql = item.Sql;
            var alias = item.Alias;

            var tokens = SqlLexer.Tokenize(itemSql).Where(t => t.Type != SqlTokenType.EndOfFile).ToList();
            if (TryExtractAlias(tokens, out var extractedAlias, out var bodyTokens))
            {
                // Remove explicit "... AS Alias" tail from expression text.
                // If alias is already known from DOM, keep it and only trim body.
                if (alias == null || string.Equals(alias, extractedAlias, StringComparison.OrdinalIgnoreCase))
                {
                    itemSql = string.Join(" ", bodyTokens.Select(t => t.Text));
                    alias ??= extractedAlias;
                }
            }

            IExprSelecting value;
            try
            {
                value = ParseSelectingExpression(itemSql, context);
            }
            catch (MapException ex)
            {
                throw new MapException("Select item is not supported: [" + itemSql + "]. " + ex.Message);
            }
            if (alias == null)
            {
                return value;
            }

            if (value is ExprColumn col)
            {
                if (string.Equals(alias, col.ColumnName.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return col;
                }

                return new ExprAliasedColumn(col, new ExprColumnAlias(alias));
            }

            return new ExprAliasedSelecting(value, new ExprColumnAlias(alias));
        }

        private static IExprSelecting ParseSelectItem(SqlDomSelectItem item)
            => ParseSelectItem(item, new MappingContext(null));

        private static IExprSelecting ParseSelectingExpression(string sql, MappingContext context)
        {
            var tokens = SqlLexer.Tokenize(sql).Where(t => t.Type != SqlTokenType.EndOfFile).ToList();
            if (tokens.Count < 1)
            {
                throw new MapException("Empty selecting expression.");
            }

            if (tokens.Count == 1 && tokens[0].Type == SqlTokenType.Operator && tokens[0].Text == "*")
            {
                return new ExprAllColumns(null);
            }

            if (tokens.Count == 3
                && tokens[0].IsIdentifierLike
                && tokens[1].Type == SqlTokenType.Dot
                && tokens[2].Type == SqlTokenType.Operator
                && tokens[2].Text == "*")
            {
                return new ExprAllColumns(new ExprTableAlias(new ExprAlias(tokens[0].IdentifierValue)));
            }

            if (TryParseTopLevelFunction(tokens, out var fnParts, out var argTokens, out var tailTokens))
            {
                var functionName = fnParts[fnParts.Count - 1];
                var upperName = functionName.ToUpperInvariant();

                if (upperName == "CAST")
                {
                    return ParseValue(sql, context);
                }

                if (tailTokens.Count > 0 && tailTokens[0].IsKeyword("OVER"))
                {
                    var args = ParseFunctionArgs(argTokens, context);
                    var overTokens = ExtractOverTokens(tailTokens);
                    var over = ParseOverClause(overTokens, context);
                    return new ExprAnalyticFunction(new ExprFunctionName(true, functionName), args, over);
                }

                if (tailTokens.Count == 0 && (upperName == "COUNT" || upperName == "SUM" || upperName == "AVG" || upperName == "MIN" || upperName == "MAX"))
                {
                    var args = ParseFunctionArgs(argTokens, context);
                    var distinct = false;
                    ExprValue argument;
                    if (args == null || args.Count < 1)
                    {
                        argument = new ExprInt32Literal(1);
                    }
                    else
                    {
                        argument = args[0];
                        if (argTokens.Count > 0 && argTokens[0].IsKeyword("DISTINCT"))
                        {
                            distinct = true;
                        }
                    }

                    return new ExprAggregateFunction(distinct, new ExprFunctionName(true, functionName), argument);
                }
            }

            return ParseValue(sql, context);
        }

        private static IExprSelecting ParseSelectingExpression(string sql)
            => ParseSelectingExpression(sql, new MappingContext(null));

        private static bool TryParseTopLevelFunction(
            IReadOnlyList<SqlToken> tokens,
            out IReadOnlyList<string> functionNameParts,
            out IReadOnlyList<SqlToken> argTokens,
            out IReadOnlyList<SqlToken> tailTokens)
        {
            functionNameParts = Array.Empty<string>();
            argTokens = Array.Empty<SqlToken>();
            tailTokens = Array.Empty<SqlToken>();

            var idx = 0;
            if (!tokens[idx].IsIdentifierLike)
            {
                return false;
            }

            var parts = new List<string> { tokens[idx].IdentifierValue };
            idx++;
            while (idx + 1 < tokens.Count && tokens[idx].Type == SqlTokenType.Dot && tokens[idx + 1].IsIdentifierLike)
            {
                idx++;
                parts.Add(tokens[idx].IdentifierValue);
                idx++;
            }

            if (idx >= tokens.Count || tokens[idx].Type != SqlTokenType.OpenParen)
            {
                return false;
            }

            var start = idx + 1;
            var depth = 1;
            idx++;
            while (idx < tokens.Count && depth > 0)
            {
                if (tokens[idx].Type == SqlTokenType.OpenParen) depth++;
                if (tokens[idx].Type == SqlTokenType.CloseParen) depth--;
                idx++;
            }

            if (depth != 0)
            {
                return false;
            }

            functionNameParts = parts;
            argTokens = tokens.Skip(start).Take(idx - start - 1).ToList();
            tailTokens = tokens.Skip(idx).ToList();
            return true;
        }

        private static IReadOnlyList<ExprValue>? ParseFunctionArgs(IReadOnlyList<SqlToken> argTokens, MappingContext context)
        {
            if (argTokens.Count < 1)
            {
                return null;
            }

            return SplitComma(argTokens).Select(segment =>
            {
                if (segment.Count == 1 && segment[0].Type == SqlTokenType.Operator && segment[0].Text == "*")
                {
                    return (ExprValue)new ExprInt32Literal(1);
                }

                if (segment.Count > 1 && segment[0].IsKeyword("DISTINCT"))
                {
                    return ParseValue(string.Join(" ", segment.Skip(1).Select(t => t.Text)), context);
                }

                return ParseValue(string.Join(" ", segment.Select(t => t.Text)), context);
            }).ToList();
        }

        private static IReadOnlyList<ExprValue>? ParseFunctionArgs(IReadOnlyList<SqlToken> argTokens)
            => ParseFunctionArgs(argTokens, new MappingContext(null));

        private static IReadOnlyList<SqlToken> ExtractOverTokens(IReadOnlyList<SqlToken> tailTokens)
        {
            if (tailTokens.Count < 3 || !tailTokens[0].IsKeyword("OVER") || tailTokens[1].Type != SqlTokenType.OpenParen)
            {
                throw new MapException("Invalid OVER clause.");
            }

            var depth = 1;
            var idx = 2;
            while (idx < tailTokens.Count && depth > 0)
            {
                if (tailTokens[idx].Type == SqlTokenType.OpenParen) depth++;
                if (tailTokens[idx].Type == SqlTokenType.CloseParen) depth--;
                idx++;
            }

            if (depth != 0)
            {
                throw new MapException("Invalid OVER clause.");
            }

            return tailTokens.Skip(2).Take(idx - 3).ToList();
        }

        private static ExprOver ParseOverClause(IReadOnlyList<SqlToken> overTokens, MappingContext context)
        {
            IReadOnlyList<ExprValue>? partitions = null;
            ExprOrderBy? orderBy = null;

            var orderByIndex = FindTopLevelKeyword(overTokens, "ORDER");
            var partitionByIndex = FindTopLevelKeyword(overTokens, "PARTITION");

            if (partitionByIndex >= 0)
            {
                var start = partitionByIndex + 1;
                if (start < overTokens.Count && overTokens[start].IsKeyword("BY"))
                {
                    start++;
                }

                var end = orderByIndex >= 0 ? orderByIndex : overTokens.Count;
                var partTokens = overTokens.Skip(start).Take(end - start).ToList();
                partitions = SplitComma(partTokens)
                    .Select(i => ParseValue(string.Join(" ", i.Select(t => t.Text)), context))
                    .ToList();
            }

            if (orderByIndex >= 0)
            {
                var start = orderByIndex + 1;
                if (start < overTokens.Count && overTokens[start].IsKeyword("BY"))
                {
                    start++;
                }

                var end = FindTopLevelKeyword(overTokens.Skip(start).ToList(), "ROWS");
                var orderTokens = end >= 0 ? overTokens.Skip(start).Take(end).ToList() : overTokens.Skip(start).ToList();
                var orderSql = string.Join(" ", orderTokens.Select(i => i.Text));
                orderBy = ParseOrderBy(orderSql, context);
            }

            return new ExprOver(partitions, orderBy, null);
        }

        private static ExprOver ParseOverClause(IReadOnlyList<SqlToken> overTokens)
            => ParseOverClause(overTokens, new MappingContext(null));

        private static int FindTopLevelKeyword(IReadOnlyList<SqlToken> tokens, string keyword)
        {
            var depth = 0;
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.OpenParen) depth++;
                if (tokens[i].Type == SqlTokenType.CloseParen) depth--;
                if (depth == 0 && tokens[i].IsKeyword(keyword))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryExtractAlias(IReadOnlyList<SqlToken> tokens, out string? alias, out IReadOnlyList<SqlToken> body)
        {
            alias = null;
            body = tokens;
            if (tokens.Count < 2)
            {
                return false;
            }

            var last = tokens[tokens.Count - 1];
            if (!last.IsIdentifierLike)
            {
                return false;
            }

            var prev = tokens[tokens.Count - 2];
            if (prev.Type == SqlTokenType.Dot)
            {
                return false;
            }

            if (prev.IsKeyword("AS"))
            {
                if (tokens.Count < 3)
                {
                    return false;
                }

                alias = last.IdentifierValue;
                body = tokens.Take(tokens.Count - 2).ToList();
                return true;
            }

            if (last.Type == SqlTokenType.Identifier && IsNonAliasTerminalKeyword(last.Text))
            {
                return false;
            }

            alias = last.IdentifierValue;
            body = tokens.Take(tokens.Count - 1).ToList();
            return true;
        }

        private static bool IsNonAliasTerminalKeyword(string text)
        {
            switch (text.ToUpperInvariant())
            {
                case "END":
                case "ELSE":
                case "THEN":
                case "WHEN":
                case "FROM":
                case "WHERE":
                case "GROUP":
                case "ORDER":
                case "HAVING":
                case "OFFSET":
                case "FETCH":
                case "UNION":
                case "INTERSECT":
                case "EXCEPT":
                case "JOIN":
                case "ON":
                case "IN":
                case "IS":
                case "LIKE":
                case "AND":
                case "OR":
                case "NOT":
                case "NULL":
                case "AS":
                case "OVER":
                case "PARTITION":
                case "BY":
                case "CASE":
                    return true;
                default:
                    return false;
            }
        }

        private static IExprTableSource ParseTableSource(SqlDomTableSource source, MappingContext context)
        {
            switch (source)
            {
                case SqlDomNamedTableSource named:
                    if (context.TryGetCteReference(named.Table, named.Alias, out var cte))
                    {
                        return cte;
                    }

                    return new ExprTable(
                        new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName(named.Schema ?? "dbo")), new ExprTableName(named.Table)),
                        named.Alias == null ? null : new ExprTableAlias(new ExprAlias(named.Alias)));
                case SqlDomJoinedTableSource join:
                    var left = ParseTableSource(join.Left, context);
                    var right = ParseTableSource(join.Right, context);
                    return join.JoinType switch
                    {
                        SqlDomJoinType.Cross => new ExprCrossedTable(left, right),
                        SqlDomJoinType.CrossApply => new ExprLateralCrossedTable(left, right, false),
                        SqlDomJoinType.OuterApply => new ExprLateralCrossedTable(left, right, true),
                        SqlDomJoinType.Inner => new ExprJoinedTable(left, ExprJoinedTable.ExprJoinType.Inner, right, ParseBoolean(join.OnSql ?? "1=1", context)),
                        SqlDomJoinType.Left => new ExprJoinedTable(left, ExprJoinedTable.ExprJoinType.Left, right, ParseBoolean(join.OnSql ?? "1=1", context)),
                        SqlDomJoinType.Right => new ExprJoinedTable(left, ExprJoinedTable.ExprJoinType.Right, right, ParseBoolean(join.OnSql ?? "1=1", context)),
                        SqlDomJoinType.Full => new ExprJoinedTable(left, ExprJoinedTable.ExprJoinType.Full, right, ParseBoolean(join.OnSql ?? "1=1", context)),
                        _ => throw new MapException("Join type is not supported.")
                    };
                case SqlDomDerivedTableSource derived:
                    if (derived.Alias == null)
                    {
                        throw new MapException("Derived table must have an alias.");
                    }
                    return new ExprDerivedTableQuery(
                        ParseNestedSubQuery(derived.Sql, context),
                        new ExprTableAlias(new ExprAlias(derived.Alias)),
                        null);
                case SqlDomValuesTableSource values:
                    if (values.Alias == null || values.ColumnAliases.Count < 1)
                    {
                        throw new MapException("VALUES source must contain alias and column aliases.");
                    }
                    return new ExprDerivedTableValues(
                        ParseValues(values.Sql, context),
                        new ExprTableAlias(new ExprAlias(values.Alias)),
                        values.ColumnAliases.Select(i => new ExprColumnName(i)).ToList());
                case SqlDomFunctionTableSource fn:
                    if (fn.Alias == null)
                    {
                        throw new MapException("Function table source must have an alias.");
                    }
                    return new ExprAliasedTableFunction(ParseTableFunction(fn.Name, fn.ArgumentsSql, context), new ExprTableAlias(new ExprAlias(fn.Alias)));
                default:
                    throw new MapException("Table source is not supported.");
            }
        }

        private static IExprTableSource ParseTableSource(SqlDomTableSource source)
            => ParseTableSource(source, new MappingContext(null));

        private static ExprTableFunction ParseTableFunction(string name, string argsSql, MappingContext context)
        {
            var nameParts = SqlLexer.Tokenize(name)
                .Where(t => t.Type != SqlTokenType.EndOfFile && t.Type != SqlTokenType.Dot)
                .Where(t => t.IsIdentifierLike)
                .Select(t => t.IdentifierValue)
                .ToList();

            IReadOnlyList<ExprValue>? args = null;
            var argText = argsSql.Trim();
            if (argText.Length > 0)
            {
                args = SplitComma(argText).Select(i => ParseValue(i, context)).ToList();
            }

            if (nameParts.Count == 1)
            {
                return new ExprTableFunction(null, new ExprFunctionName(true, nameParts[0]), args);
            }

            if (nameParts.Count == 2)
            {
                return new ExprTableFunction(
                    new ExprDbSchema(null, new ExprSchemaName(nameParts[0])),
                    new ExprFunctionName(false, nameParts[1]),
                    args);
            }

            return new ExprTableFunction(
                new ExprDbSchema(new ExprDatabaseName(nameParts[nameParts.Count - 3]), new ExprSchemaName(nameParts[nameParts.Count - 2])),
                new ExprFunctionName(false, nameParts[nameParts.Count - 1]),
                args);
        }

        private static ExprTableFunction ParseTableFunction(string name, string argsSql)
            => ParseTableFunction(name, argsSql, new MappingContext(null));

        private static IExprSubQuery ParseNestedSubQuery(string sql, MappingContext context)
        {
            if (!SqlDomParser.TryParseSingleStatement(sql, out var statement, out _))
            {
                throw new MapException("Derived table query could not be parsed.");
            }

            if (((SqlDomStatement)statement!).Kind != SqlDomStatementKind.Select)
            {
                throw new MapException("Derived table query must be SELECT.");
            }

            var mapped = MapSelect((SqlDomStatement)statement!, context);
            if (mapped is IExprSubQuery subQuery)
            {
                return subQuery;
            }

            if (mapped is ExprSelect select)
            {
                if (select.OrderBy.OrderList.Count < 1 && select.SelectQuery is IExprSubQuery innerSubQuery)
                {
                    return innerSubQuery;
                }

                if (select.SelectQuery is ExprQuerySpecification specification && specification.Top is ExprValue top)
                {
                    var queryWithoutTop = new ExprQuerySpecification(
                        specification.SelectList,
                        top: null,
                        specification.Distinct,
                        specification.From,
                        specification.Where,
                        specification.GroupBy);

                    return new ExprSelectOffsetFetch(
                        queryWithoutTop,
                        new ExprOrderByOffsetFetch(
                            select.OrderBy.OrderList,
                            new ExprOffsetFetch(new ExprInt32Literal(0), top)));
                }

                throw new MapException("Derived table query with ORDER BY is not supported in this form.");
            }

            throw new MapException("Derived table query cannot be represented as subquery.");
        }

        private static IExprSubQuery ParseNestedSubQuery(string sql)
            => ParseNestedSubQuery(sql, new MappingContext(null));

        private static ExprTableValueConstructor ParseValues(string sql, MappingContext context)
        {
            var tokens = SqlLexer.Tokenize(sql);
            var rows = new List<ExprValueRow>();
            var i = 0;
            while (i < tokens.Count && !tokens[i].IsKeyword("VALUES"))
            {
                i++;
            }

            i++;
            while (i < tokens.Count && tokens[i].Type != SqlTokenType.EndOfFile)
            {
                if (tokens[i].Type != SqlTokenType.OpenParen)
                {
                    i++;
                    continue;
                }

                var start = i + 1;
                var depth = 1;
                i++;
                while (i < tokens.Count && depth > 0)
                {
                    if (tokens[i].Type == SqlTokenType.OpenParen) depth++;
                    if (tokens[i].Type == SqlTokenType.CloseParen) depth--;
                    i++;
                }

                var rowTokens = tokens.Skip(start).Take(i - start - 1).ToList();
                var items = SplitComma(rowTokens)
                    .Select(segment => ParseValue(string.Join(" ", segment.Select(t => t.Text)), context))
                    .ToList();
                rows.Add(new ExprValueRow(items));
            }

            return new ExprTableValueConstructor(rows);
        }

        private static ExprTableValueConstructor ParseValues(string sql)
            => ParseValues(sql, new MappingContext(null));

        private static ExprOrderBy ParseOrderBy(string sql, MappingContext context)
        {
            var trimmed = sql.Trim();
            if (trimmed.StartsWith("ORDER BY", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(8).Trim();
            }

            var items = new List<ExprOrderByItem>();
            foreach (var part in SplitComma(trimmed))
            {
                var p = part.Trim();
                var desc = p.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase);
                var core = desc || p.EndsWith(" ASC", StringComparison.OrdinalIgnoreCase) ? p.Substring(0, p.LastIndexOf(' ')).Trim() : p;
                items.Add(new ExprOrderByItem(ParseValue(core, context), desc));
            }

            return new ExprOrderBy(items);
        }

        private static ExprOrderBy ParseOrderBy(string sql)
            => ParseOrderBy(sql, new MappingContext(null));

        private static (ExprValue offset, ExprValue? fetch) ParseOffsetFetch(string sql, MappingContext context)
        {
            var tokens = SqlLexer.Tokenize(sql).Where(i => i.Type != SqlTokenType.EndOfFile).ToList();
            var offsetIndex = tokens.FindIndex(i => i.IsKeyword("OFFSET"));
            if (offsetIndex < 0 || offsetIndex + 1 >= tokens.Count)
            {
                throw new MapException("OFFSET/FETCH clause is invalid.");
            }

            var offset = ParseValue(tokens[offsetIndex + 1].Text, context);
            var fetchIndex = tokens.FindIndex(i => i.IsKeyword("FETCH"));
            if (fetchIndex < 0 || fetchIndex + 2 >= tokens.Count)
            {
                return (offset, null);
            }

            var fetch = ParseValue(tokens[fetchIndex + 2].Text, context);
            return (offset, fetch);
        }

        private static (ExprValue offset, ExprValue? fetch) ParseOffsetFetch(string sql)
            => ParseOffsetFetch(sql, new MappingContext(null));

        private static ExprBoolean ParseBoolean(string sql, MappingContext context)
            => new ExprParser(sql, context).ParseBoolean();

        private static ExprBoolean ParseBoolean(string sql)
            => ParseBoolean(sql, new MappingContext(null));

        private static ExprValue ParseValue(string sql, MappingContext context)
            => new ExprParser(sql, context).ParseValue();

        private static ExprValue ParseValue(string sql)
            => ParseValue(sql, new MappingContext(null));

        private static IReadOnlyList<string> SplitComma(string sql)
            => SplitComma(SqlLexer.Tokenize(sql).Where(i => i.Type != SqlTokenType.EndOfFile).ToList())
                .Select(i => string.Join(" ", i.Select(t => t.Text)))
                .ToList();

        private static IReadOnlyList<IReadOnlyList<SqlToken>> SplitComma(IReadOnlyList<SqlToken> tokens)
        {
            var result = new List<IReadOnlyList<SqlToken>>();
            var acc = new List<SqlToken>();
            var depth = 0;
            foreach (var t in tokens)
            {
                if (t.Type == SqlTokenType.OpenParen) depth++;
                if (t.Type == SqlTokenType.CloseParen) depth--;
                if (depth == 0 && t.Type == SqlTokenType.Comma)
                {
                    result.Add(acc);
                    acc = new List<SqlToken>();
                    continue;
                }

                acc.Add(t);
            }

            if (acc.Count > 0)
            {
                result.Add(acc);
            }

            return result;
        }

        private sealed class ExprParser
        {
            private readonly List<SqlToken> _tokens;
            private readonly MappingContext _context;
            private readonly string _sourceSql;
            private int _index;

            public ExprParser(string sql, MappingContext context)
            {
                this._tokens = SqlLexer.Tokenize(sql).Where(i => i.Type != SqlTokenType.EndOfFile).ToList();
                this._context = context;
                this._sourceSql = sql;
            }

            public ExprValue ParseValue()
            {
                var res = this.ParseAddSub();
                if (!this.IsEnd)
                {
                    throw new MapException("Value expression is not supported.");
                }

                return res;
            }

            public ExprBoolean ParseBoolean()
            {
                var res = this.ParseOr();
                if (!this.IsEnd)
                {
                    throw new MapException("Boolean expression is not supported.");
                }

                return res;
            }

            private ExprBoolean ParseOr()
            {
                var left = this.ParseAnd();
                while (this.TryKeyword("OR"))
                {
                    left = new ExprBooleanOr(left, this.ParseAnd());
                }

                return left;
            }

            private ExprBoolean ParseAnd()
            {
                var left = this.ParseNot();
                while (this.TryKeyword("AND"))
                {
                    left = new ExprBooleanAnd(left, this.ParseNot());
                }

                return left;
            }

            private ExprBoolean ParseNot()
            {
                if (this.TryKeyword("NOT"))
                {
                    return new ExprBooleanNot(this.ParseNot());
                }

                return this.ParsePredicate();
            }

            private ExprBoolean ParsePredicate()
            {
                if (this.TryKeyword("EXISTS"))
                {
                    var nested = this.ReadParenthesizedTokens();
                    return new ExprExists(ParseNestedSubQuery(string.Join(" ", nested.Select(i => i.Text)), this._context));
                }

                if (this.TryType(SqlTokenType.OpenParen))
                {
                    var nested = this.ReadBalancedInner();
                    return new ExprParser(string.Join(" ", nested.Select(i => i.Text)), this._context).ParseBoolean();
                }

                var left = this.ParseAddSub();

                if (this.TryKeyword("IS"))
                {
                    var not = this.TryKeyword("NOT");
                    this.ExpectKeyword("NULL", "Expected NULL.");
                    return new ExprIsNull(left, not);
                }

                if (this.TryKeyword("LIKE"))
                {
                    return new ExprLike(left, this.ParseAddSub());
                }

                if (this.TryKeyword("IN"))
                {
                    var nested = this.ReadParenthesizedTokens();
                    var nestedSql = string.Join(" ", nested.Select(i => i.Text));
                    if (nested.Any(i => i.IsKeyword("SELECT")))
                    {
                        return new ExprInSubQuery(left, ParseNestedSubQuery(nestedSql, this._context));
                    }

                    return new ExprInValues(left, SplitComma(nested).Select(i => new ExprParser(string.Join(" ", i.Select(t => t.Text)), this._context).ParseValue()).ToList());
                }

                var op = this.TryReadComparison();
                if (op == null)
                {
                    throw new MapException("Predicate operator is expected.");
                }

                var right = this.ParseAddSub();
                return op switch
                {
                    "=" => new ExprBooleanEq(left, right),
                    "!=" => new ExprBooleanNotEq(left, right),
                    "<>" => new ExprBooleanNotEq(left, right),
                    ">" => new ExprBooleanGt(left, right),
                    ">=" => new ExprBooleanGtEq(left, right),
                    "<" => new ExprBooleanLt(left, right),
                    "<=" => new ExprBooleanLtEq(left, right),
                    _ => throw new MapException("Comparison operator is not supported.")
                };
            }

            private ExprValue ParseAddSub()
            {
                var left = this.ParseMulDiv();
                while (this.TryOp("+") || this.TryOp("-"))
                {
                    var op = this._tokens[this._index - 1].Text;
                    var right = this.ParseMulDiv();
                    left = op == "+" ? new ExprSum(left, right) : new ExprSub(left, right);
                }

                return left;
            }

            private ExprValue ParseMulDiv()
            {
                var left = this.ParsePrimary();
                while (this.TryOp("*") || this.TryOp("/") || this.TryOp("%"))
                {
                    var op = this._tokens[this._index - 1].Text;
                    var right = this.ParsePrimary();
                    left = op switch
                    {
                        "*" => new ExprMul(left, right),
                        "/" => new ExprDiv(left, right),
                        "%" => new ExprModulo(left, right),
                        _ => left
                    };
                }

                return left;
            }

            private ExprValue ParsePrimary()
            {
                if (this.IsEnd)
                {
                    throw new MapException("Unexpected end of value expression.");
                }

                if (this.TryType(SqlTokenType.OpenParen))
                {
                    var nested = this.ReadBalancedInner();
                    var nestedSql = string.Join(" ", nested.Select(i => i.Text));
                    if (nested.Any(i => i.IsKeyword("SELECT")))
                    {
                        return new ExprValueQuery(ParseNestedSubQuery(nestedSql, this._context));
                    }

                    return new ExprParser(nestedSql, this._context).ParseValue();
                }

                if (this.TryOp("-"))
                {
                    return new ExprSub(new ExprInt32Literal(0), this.ParsePrimary());
                }

                var current = this.Current;
                if (current.IsKeyword("CASE"))
                {
                    return this.ParseCase();
                }

                if (current.IsKeyword("CAST"))
                {
                    return this.ParseCast();
                }

                if (current.Type == SqlTokenType.StringLiteral)
                {
                    this._index++;
                    return new ExprStringLiteral(current.Text.Length >= 3 && (current.Text[0] == 'N' || current.Text[0] == 'n') && current.Text[1] == '\'' ? current.Text.Substring(2, current.Text.Length - 3).Replace("''", "'") : current.Text.Length >= 2 ? current.Text.Substring(1, current.Text.Length - 2).Replace("''", "'") : string.Empty);
                }

                if (current.Type == SqlTokenType.NumberLiteral)
                {
                    this._index++;
                    if (int.TryParse(current.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        return new ExprInt32Literal(i);
                    }

                    if (decimal.TryParse(current.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                    {
                        return new ExprDecimalLiteral(d);
                    }
                }

                if (current.IsKeyword("NULL"))
                {
                    this._index++;
                    return ExprNull.Instance;
                }

                if (current.IsIdentifierLike)
                {
                    var id = current.IdentifierValue;
                    if (id.StartsWith("@", StringComparison.Ordinal) && !id.StartsWith("@@", StringComparison.Ordinal))
                    {
                        this._index++;
                        return new ExprParameter(null, id.TrimStart('@'));
                    }

                    var parts = new List<string>();
                    parts.Add(this.NextIdentifier());
                    while (this.TryType(SqlTokenType.Dot))
                    {
                        if (this.Current.Type == SqlTokenType.Operator && this.Current.Text == "*")
                        {
                            throw new MapException("Wildcard selector is not allowed in value expression.");
                        }

                        parts.Add(this.NextIdentifier());
                    }

	                    if (this.TryType(SqlTokenType.OpenParen))
	                    {
	                        var argsTokens = this.ReadBalancedInner();
	                        var argSegments = argsTokens.Count == 0
	                            ? null
	                            : SplitComma(argsTokens);
	                        var args = argSegments == null
	                            ? null
	                            : argSegments.Select(i => new ExprParser(string.Join(" ", i.Select(t => t.Text)), this._context).ParseValue()).ToList();

	                        if (parts.Count == 1)
	                        {
	                            if (TryMapKnownScalarFunction(parts[0], argSegments, args, out var known))
	                            {
	                                return known;
	                            }

	                            if (TryMapPortableScalarFunction(parts[0], args, out var portable))
	                            {
	                                return portable;
	                            }

	                            return new ExprScalarFunction(null, new ExprFunctionName(true, parts[0]), args);
	                        }

                        if (parts.Count == 2)
                        {
                            return new ExprScalarFunction(new ExprDbSchema(null, new ExprSchemaName(parts[0])), new ExprFunctionName(false, parts[1]), args);
                        }

                        return new ExprScalarFunction(
                            new ExprDbSchema(new ExprDatabaseName(parts[parts.Count - 3]), new ExprSchemaName(parts[parts.Count - 2])),
                            new ExprFunctionName(false, parts[parts.Count - 1]),
                            args);
                    }

                    if (parts.Count == 1)
                    {
                        return new ExprColumn(null, new ExprColumnName(parts[0]));
                    }

	                    return new ExprColumn(new ExprTableAlias(new ExprAlias(parts[parts.Count - 2])), new ExprColumnName(parts[parts.Count - 1]));
	                }

	                throw new MapException("Value token is not supported: " + current.Text + " in [" + this._sourceSql + "]");
	            }

            private ExprCast ParseCast()
            {
                this.ExpectKeyword("CAST", "CAST expression should start with CAST keyword.");
                this.ExpectType(SqlTokenType.OpenParen, "CAST expression should contain '(' after CAST.");
                var inner = this.ReadBalancedInner();
                if (inner.Count < 3)
                {
                    throw new MapException("CAST expression is invalid.");
                }

                var asIndex = FindTopLevelAsIndex(inner);
                if (asIndex <= 0 || asIndex >= inner.Count - 1)
                {
                    throw new MapException("CAST expression should contain 'AS <type>'.");
                }

                var valueSql = string.Join(" ", inner.Take(asIndex).Select(i => i.Text));
                var valueExpr = new ExprParser(valueSql, this._context).ParseValue();
                var typeTokens = inner.Skip(asIndex + 1).ToList();
                var type = ParseCastType(typeTokens);

                return new ExprCast(valueExpr, type);
            }

            private static int FindTopLevelAsIndex(IReadOnlyList<SqlToken> tokens)
            {
                var depth = 0;
                for (var i = 0; i < tokens.Count; i++)
                {
                    if (tokens[i].Type == SqlTokenType.OpenParen)
                    {
                        depth++;
                        continue;
                    }

                    if (tokens[i].Type == SqlTokenType.CloseParen)
                    {
                        if (depth > 0)
                        {
                            depth--;
                        }

                        continue;
                    }

                    if (depth == 0 && tokens[i].IsKeyword("AS"))
                    {
                        return i;
                    }
                }

                return -1;
            }

            private static ExprType ParseCastType(IReadOnlyList<SqlToken> tokens)
            {
                if (tokens.Count < 1 || !tokens[0].IsIdentifierLike)
                {
                    throw new MapException("CAST target type is invalid.");
                }

                var index = 0;
                var typeName = tokens[index].IdentifierValue;
                index++;

                while (index + 1 < tokens.Count
                       && tokens[index].Type == SqlTokenType.Dot
                       && tokens[index + 1].IsIdentifierLike)
                {
                    typeName = tokens[index + 1].IdentifierValue;
                    index += 2;
                }

                IReadOnlyList<SqlToken>? argTokens = null;
                if (index < tokens.Count)
                {
                    if (tokens[index].Type != SqlTokenType.OpenParen)
                    {
                        throw new MapException("CAST target type is invalid.");
                    }

                    argTokens = ReadParenthesizedTokens(tokens, ref index);
                }

                if (index != tokens.Count)
                {
                    throw new MapException("CAST target type is invalid.");
                }

                var normalized = typeName.ToUpperInvariant();
                switch (normalized)
                {
                    case "BIT":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeBoolean.Instance;

                    case "TINYINT":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeByte.Instance;

                    case "SMALLINT":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeInt16.Instance;

                    case "INT":
                    case "INTEGER":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeInt32.Instance;

                    case "BIGINT":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeInt64.Instance;

                    case "DECIMAL":
                    case "NUMERIC":
                        return new ExprTypeDecimal(ParseDecimalPrecisionScale(argTokens, typeName));

                    case "FLOAT":
                    case "REAL":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeDouble.Instance;

                    case "DATE":
                        AssertNoArguments(argTokens, typeName);
                        return new ExprTypeDateTime(isDate: true);

                    case "DATETIME":
                    case "SMALLDATETIME":
                        AssertNoArguments(argTokens, typeName);
                        return new ExprTypeDateTime(isDate: false);

                    case "DATETIME2":
                        AssertOptionalSingleIntArgument(argTokens, typeName, minInclusive: 0, maxInclusive: 7);
                        return new ExprTypeDateTime(isDate: false);

                    case "DATETIMEOFFSET":
                        AssertOptionalSingleIntArgument(argTokens, typeName, minInclusive: 0, maxInclusive: 7);
                        return ExprTypeDateTimeOffset.Instance;

                    case "UNIQUEIDENTIFIER":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeGuid.Instance;

                    case "VARCHAR":
                        return new ExprTypeString(ParseLengthOrMax(argTokens, typeName, allowMax: true, defaultLength: 30), isUnicode: false, isText: false);

                    case "NVARCHAR":
                        return new ExprTypeString(ParseLengthOrMax(argTokens, typeName, allowMax: true, defaultLength: 30), isUnicode: true, isText: false);

                    case "CHAR":
                        return new ExprTypeFixSizeString(ParseRequiredLength(argTokens, typeName, defaultLength: 30), isUnicode: false);

                    case "NCHAR":
                        return new ExprTypeFixSizeString(ParseRequiredLength(argTokens, typeName, defaultLength: 30), isUnicode: true);

                    case "TEXT":
                        AssertNoArguments(argTokens, typeName);
                        return new ExprTypeString(size: null, isUnicode: false, isText: true);

                    case "NTEXT":
                        AssertNoArguments(argTokens, typeName);
                        return new ExprTypeString(size: null, isUnicode: true, isText: true);

                    case "BINARY":
                        return new ExprTypeFixSizeByteArray(ParseRequiredLength(argTokens, typeName, defaultLength: 30));

                    case "VARBINARY":
                        return new ExprTypeByteArray(ParseLengthOrMax(argTokens, typeName, allowMax: true, defaultLength: 30));

                    case "XML":
                        AssertNoArguments(argTokens, typeName);
                        return ExprTypeXml.Instance;

                    default:
                        throw new MapException("CAST type '" + typeName + "' is not supported by SqExpress parser.");
                }
            }

            private static IReadOnlyList<SqlToken> ReadParenthesizedTokens(IReadOnlyList<SqlToken> tokens, ref int index)
            {
                index++;
                var depth = 1;
                var result = new List<SqlToken>();

                while (index < tokens.Count)
                {
                    var token = tokens[index];
                    index++;

                    if (token.Type == SqlTokenType.OpenParen)
                    {
                        depth++;
                        result.Add(token);
                        continue;
                    }

                    if (token.Type == SqlTokenType.CloseParen)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return result;
                        }

                        result.Add(token);
                        continue;
                    }

                    result.Add(token);
                }

                throw new MapException("CAST target type arguments are invalid.");
            }

            private static DecimalPrecisionScale? ParseDecimalPrecisionScale(IReadOnlyList<SqlToken>? argTokens, string typeName)
            {
                if (argTokens == null || argTokens.Count < 1)
                {
                    return null;
                }

                var args = SplitComma(argTokens);
                if (args.Count != 1 && args.Count != 2)
                {
                    throw new MapException("Type '" + typeName + "' expects one or two numeric arguments.");
                }

                var precision = ParseSingleIntToken(args[0], typeName, minInclusive: 1);
                int? scale = null;
                if (args.Count == 2)
                {
                    scale = ParseSingleIntToken(args[1], typeName, minInclusive: 0);
                    if (scale.Value > precision)
                    {
                        throw new MapException("Type '" + typeName + "' scale cannot be greater than precision.");
                    }
                }

                return new DecimalPrecisionScale(precision, scale);
            }

            private static int ParseRequiredLength(IReadOnlyList<SqlToken>? argTokens, string typeName, int defaultLength)
            {
                return ParseLengthOrMax(argTokens, typeName, allowMax: false, defaultLength)
                       ?? throw new MapException("Type '" + typeName + "' cannot use MAX length.");
            }

            private static int? ParseLengthOrMax(IReadOnlyList<SqlToken>? argTokens, string typeName, bool allowMax, int defaultLength)
            {
                if (argTokens == null || argTokens.Count < 1)
                {
                    return defaultLength;
                }

                var args = SplitComma(argTokens);
                if (args.Count != 1)
                {
                    throw new MapException("Type '" + typeName + "' expects a single length argument.");
                }

                var valueToken = args[0];
                if (valueToken.Count != 1)
                {
                    throw new MapException("Type '" + typeName + "' length argument is invalid.");
                }

                var single = valueToken[0];
                if (single.IsKeyword("MAX"))
                {
                    if (!allowMax)
                    {
                        throw new MapException("Type '" + typeName + "' cannot use MAX length.");
                    }

                    return null;
                }

                if (single.Type != SqlTokenType.NumberLiteral
                    || !int.TryParse(single.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)
                    || size < 1)
                {
                    throw new MapException("Type '" + typeName + "' length argument is invalid.");
                }

                return size;
            }

            private static void AssertNoArguments(IReadOnlyList<SqlToken>? argTokens, string typeName)
            {
                if (argTokens != null && argTokens.Count > 0)
                {
                    throw new MapException("Type '" + typeName + "' does not accept arguments.");
                }
            }

            private static void AssertOptionalSingleIntArgument(
                IReadOnlyList<SqlToken>? argTokens,
                string typeName,
                int minInclusive,
                int maxInclusive)
            {
                if (argTokens == null || argTokens.Count < 1)
                {
                    return;
                }

                var args = SplitComma(argTokens);
                if (args.Count != 1)
                {
                    throw new MapException("Type '" + typeName + "' expects a single numeric argument.");
                }

                _ = ParseSingleIntToken(args[0], typeName, minInclusive, maxInclusive);
            }

            private static int ParseSingleIntToken(
                IReadOnlyList<SqlToken> arg,
                string typeName,
                int minInclusive,
                int? maxInclusive = null)
            {
                if (arg.Count != 1
                    || arg[0].Type != SqlTokenType.NumberLiteral
                    || !int.TryParse(arg[0].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    throw new MapException("Type '" + typeName + "' numeric argument is invalid.");
                }

                if (value < minInclusive || (maxInclusive.HasValue && value > maxInclusive.Value))
                {
                    throw new MapException("Type '" + typeName + "' numeric argument is out of range.");
                }

                return value;
            }

            private static bool TryMapKnownScalarFunction(
                string name,
                IReadOnlyList<IReadOnlyList<SqlToken>>? argSegments,
                IReadOnlyList<ExprValue>? args,
                [NotNullWhen(true)] out ExprValue? result)
            {
                result = null;
                var normalized = name.ToUpperInvariant();

                switch (normalized)
                {
                    case "GETDATE":
                    case "SYSDATETIME":
                    case "CURRENT_TIMESTAMP":
                        if (argSegments == null || argSegments.Count == 0)
                        {
                            result = ExprGetDate.Instance;
                            return true;
                        }

                        return false;

                    case "GETUTCDATE":
                    case "SYSUTCDATETIME":
                    case "GETUTCNOW":
                        if (argSegments == null || argSegments.Count == 0)
                        {
                            result = ExprGetUtcDate.Instance;
                            return true;
                        }

                        return false;

                    case "DATEADD":
                        return TryMapDateAdd(argSegments, args, out result);

                    case "DATEDIFF":
                        return TryMapDateDiff(argSegments, args, out result);

                    case "ISNULL":
                        return TryMapIsNull(args, out result);

                    case "COALESCE":
                        return TryMapCoalesce(args, out result);

                    default:
                        return false;
                }
            }

            private static bool TryMapDateAdd(
                IReadOnlyList<IReadOnlyList<SqlToken>>? argSegments,
                IReadOnlyList<ExprValue>? args,
                [NotNullWhen(true)] out ExprValue? result)
            {
                result = null;
                if (argSegments == null || args == null || argSegments.Count != 3 || args.Count != 3)
                {
                    return false;
                }

                if (!TryParseDateAddPart(argSegments[0], out var part))
                {
                    return false;
                }

                if (!TryParseIntConstant(argSegments[1], out var number))
                {
                    return false;
                }

                result = new ExprDateAdd(part, number, args[2]);
                return true;
            }

            private static bool TryMapDateDiff(
                IReadOnlyList<IReadOnlyList<SqlToken>>? argSegments,
                IReadOnlyList<ExprValue>? args,
                [NotNullWhen(true)] out ExprValue? result)
            {
                result = null;
                if (argSegments == null || args == null || argSegments.Count != 3 || args.Count != 3)
                {
                    return false;
                }

                if (!TryParseDateDiffPart(argSegments[0], out var part))
                {
                    return false;
                }

                result = new ExprDateDiff(part, args[1], args[2]);
                return true;
            }

            private static bool TryMapIsNull(
                IReadOnlyList<ExprValue>? args,
                [NotNullWhen(true)] out ExprValue? result)
            {
                result = null;
                if (args == null || args.Count != 2)
                {
                    return false;
                }

                result = new ExprFuncIsNull(args[0], args[1]);
                return true;
            }

            private static bool TryMapCoalesce(
                IReadOnlyList<ExprValue>? args,
                [NotNullWhen(true)] out ExprValue? result)
            {
                result = null;
                if (args == null || args.Count < 2)
                {
                    return false;
                }

                result = new ExprFuncCoalesce(args[0], args.Skip(1).ToList());
                return true;
            }

            private static bool TryParseDateAddPart(IReadOnlyList<SqlToken> tokens, out DateAddDatePart part)
            {
                if (!TryGetDatePartTokenValue(tokens, out var value))
                {
                    part = default;
                    return false;
                }

                switch (value.ToUpperInvariant())
                {
                    case "YEAR":
                    case "YY":
                    case "YYYY":
                        part = DateAddDatePart.Year;
                        return true;

                    case "MONTH":
                    case "MM":
                    case "M":
                        part = DateAddDatePart.Month;
                        return true;

                    case "DAY":
                    case "DD":
                    case "D":
                        part = DateAddDatePart.Day;
                        return true;

                    case "WEEK":
                    case "WK":
                    case "WW":
                        part = DateAddDatePart.Week;
                        return true;

                    case "HOUR":
                    case "HH":
                        part = DateAddDatePart.Hour;
                        return true;

                    case "MINUTE":
                    case "MI":
                    case "N":
                        part = DateAddDatePart.Minute;
                        return true;

                    case "SECOND":
                    case "SS":
                    case "S":
                        part = DateAddDatePart.Second;
                        return true;

                    case "MILLISECOND":
                    case "MS":
                        part = DateAddDatePart.Millisecond;
                        return true;

                    default:
                        part = default;
                        return false;
                }
            }

            private static bool TryParseDateDiffPart(IReadOnlyList<SqlToken> tokens, out DateDiffDatePart part)
            {
                if (!TryGetDatePartTokenValue(tokens, out var value))
                {
                    part = default;
                    return false;
                }

                switch (value.ToUpperInvariant())
                {
                    case "YEAR":
                    case "YY":
                    case "YYYY":
                        part = DateDiffDatePart.Year;
                        return true;

                    case "MONTH":
                    case "MM":
                    case "M":
                        part = DateDiffDatePart.Month;
                        return true;

                    case "DAY":
                    case "DD":
                    case "D":
                        part = DateDiffDatePart.Day;
                        return true;

                    case "HOUR":
                    case "HH":
                        part = DateDiffDatePart.Hour;
                        return true;

                    case "MINUTE":
                    case "MI":
                    case "N":
                        part = DateDiffDatePart.Minute;
                        return true;

                    case "SECOND":
                    case "SS":
                    case "S":
                        part = DateDiffDatePart.Second;
                        return true;

                    case "MILLISECOND":
                    case "MS":
                        part = DateDiffDatePart.Millisecond;
                        return true;

                    default:
                        part = default;
                        return false;
                }
            }

            private static bool TryGetDatePartTokenValue(IReadOnlyList<SqlToken> tokens, [NotNullWhen(true)] out string? value)
            {
                value = null;
                if (tokens.Count != 1)
                {
                    return false;
                }

                var token = tokens[0];
                if (token.IsIdentifierLike)
                {
                    value = token.IdentifierValue;
                    return true;
                }

                if (token.Type == SqlTokenType.StringLiteral)
                {
                    value = token.Text.Length >= 3 && (token.Text[0] == 'N' || token.Text[0] == 'n') && token.Text[1] == '\'' ? token.Text.Substring(2, token.Text.Length - 3).Replace("''", "'") : token.Text.Length >= 2 ? token.Text.Substring(1, token.Text.Length - 2).Replace("''", "'") : string.Empty;
                    return true;
                }

                return false;
            }

            private static bool TryParseIntConstant(IReadOnlyList<SqlToken> tokens, out int value)
            {
                value = 0;
                if (tokens.Count == 1
                    && tokens[0].Type == SqlTokenType.NumberLiteral
                    && int.TryParse(tokens[0].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }

                if (tokens.Count == 2
                    && tokens[0].Type == SqlTokenType.Operator
                    && (tokens[0].Text == "-" || tokens[0].Text == "+")
                    && tokens[1].Type == SqlTokenType.NumberLiteral
                    && int.TryParse(tokens[1].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    value = tokens[0].Text == "-" ? -n : n;
                    return true;
                }

                return false;
            }

	            private static bool TryMapPortableScalarFunction(string name, IReadOnlyList<ExprValue>? args, [NotNullWhen(true)] out ExprPortableScalarFunction? result)
	            {
	                result = null;
	                var normalized = name.ToUpperInvariant();

	                switch (normalized)
	                {
	                    case "LEN":
	                    case "CHAR_LENGTH":
	                        return TryCreateSingleArg(PortableScalarFunction.Len, args, out result);

	                    case "DATALENGTH":
	                    case "OCTET_LENGTH":
	                        return TryCreateSingleArg(PortableScalarFunction.DataLen, args, out result);

	                    case "YEAR":
	                        return TryCreateSingleArg(PortableScalarFunction.Year, args, out result);

	                    case "MONTH":
	                        return TryCreateSingleArg(PortableScalarFunction.Month, args, out result);

	                    case "DAY":
	                        return TryCreateSingleArg(PortableScalarFunction.Day, args, out result);

	                    case "HOUR":
	                        return TryCreateSingleArg(PortableScalarFunction.Hour, args, out result);

	                    case "MINUTE":
	                        return TryCreateSingleArg(PortableScalarFunction.Minute, args, out result);

	                    case "SECOND":
	                        return TryCreateSingleArg(PortableScalarFunction.Second, args, out result);

	                    case "LEFT":
	                        return TryCreateTwoArgs(PortableScalarFunction.Left, args, out result);

	                    case "RIGHT":
	                        return TryCreateTwoArgs(PortableScalarFunction.Right, args, out result);

	                    case "REPLICATE":
	                    case "REPEAT":
	                        return TryCreateTwoArgs(PortableScalarFunction.Repeat, args, out result);

	                    case "CHARINDEX":
	                    case "LOCATE":
	                        return TryCreateTwoArgs(PortableScalarFunction.IndexOf, args, out result);

	                    case "STRPOS":
	                        if (args?.Count == 2)
	                        {
	                            result = new ExprPortableScalarFunction(PortableScalarFunction.IndexOf, new[] { args[1], args[0] });
	                            return true;
	                        }
	                        return false;

	                    default:
	                        return false;
	                }

	                static bool TryCreateSingleArg(PortableScalarFunction function, IReadOnlyList<ExprValue>? args1, [NotNullWhen(true)] out ExprPortableScalarFunction? res1)
	                {
	                    if (args1?.Count == 1)
	                    {
	                        res1 = new ExprPortableScalarFunction(function, args1);
	                        return true;
	                    }

	                    res1 = null;
	                    return false;
	                }

	                static bool TryCreateTwoArgs(PortableScalarFunction function, IReadOnlyList<ExprValue>? args2, [NotNullWhen(true)] out ExprPortableScalarFunction? res2)
	                {
	                    if (args2?.Count == 2)
	                    {
	                        res2 = new ExprPortableScalarFunction(function, args2);
	                        return true;
	                    }

	                    res2 = null;
	                    return false;
	                }
	            }

            private string NextIdentifier()
            {
                if (!this.Current.IsIdentifierLike)
                {
                    throw new MapException("Identifier expected.");
                }

                var res = this.Current.IdentifierValue;
                this._index++;
                return res;
            }

            private ExprValue ParseCase()
            {
                this.ExpectKeyword("CASE", "CASE expression should start with CASE keyword.");

                ExprValue? simpleCaseValue = null;
                if (!this.IsEnd && !this.Current.IsKeyword("WHEN"))
                {
                    var simpleTokens = this.ReadUntilCaseKeyword("WHEN");
                    if (simpleTokens.Count < 1)
                    {
                        throw new MapException("CASE expression is invalid.");
                    }

                    simpleCaseValue = new ExprParser(string.Join(" ", simpleTokens.Select(i => i.Text)), this._context).ParseValue();
                }

                var branches = new List<ExprCaseWhenThen>();
                while (this.TryKeyword("WHEN"))
                {
                    var conditionTokens = this.ReadUntilCaseKeyword("THEN");
                    this.ExpectKeyword("THEN", "CASE WHEN branch must contain THEN.");

                    var valueTokens = this.ReadUntilCaseKeyword("WHEN", "ELSE", "END");
                    if (valueTokens.Count < 1)
                    {
                        throw new MapException("CASE WHEN branch must contain result expression.");
                    }

                    ExprBoolean condition;
                    if (simpleCaseValue is null)
                    {
                        condition = new ExprParser(string.Join(" ", conditionTokens.Select(i => i.Text)), this._context).ParseBoolean();
                    }
                    else
                    {
                        var compared = new ExprParser(string.Join(" ", conditionTokens.Select(i => i.Text)), this._context).ParseValue();
                        condition = new ExprBooleanEq(simpleCaseValue, compared);
                    }

                    var value = new ExprParser(string.Join(" ", valueTokens.Select(i => i.Text)), this._context).ParseValue();
                    branches.Add(new ExprCaseWhenThen(condition, value));
                }

                if (branches.Count < 1)
                {
                    throw new MapException("CASE expression must contain at least one WHEN branch.");
                }

                ExprValue elseValue = ExprNull.Instance;
                if (this.TryKeyword("ELSE"))
                {
                    var elseTokens = this.ReadUntilCaseKeyword("END");
                    if (elseTokens.Count < 1)
                    {
                        throw new MapException("CASE ELSE branch must contain value expression.");
                    }

                    elseValue = new ExprParser(string.Join(" ", elseTokens.Select(i => i.Text)), this._context).ParseValue();
                }

                this.ExpectKeyword("END", "CASE expression must be terminated by END.");
                return new ExprCase(branches, elseValue);
            }

            private IReadOnlyList<SqlToken> ReadUntilCaseKeyword(params string[] keywords)
            {
                var result = new List<SqlToken>();
                var parenDepth = 0;
                var nestedCaseDepth = 0;
                while (!this.IsEnd)
                {
                    var token = this.Current;
                    if (token.Type == SqlTokenType.OpenParen)
                    {
                        parenDepth++;
                        result.Add(token);
                        this._index++;
                        continue;
                    }

                    if (token.Type == SqlTokenType.CloseParen)
                    {
                        if (parenDepth > 0)
                        {
                            parenDepth--;
                        }

                        result.Add(token);
                        this._index++;
                        continue;
                    }

                    if (parenDepth == 0)
                    {
                        if (token.IsKeyword("CASE"))
                        {
                            nestedCaseDepth++;
                            result.Add(token);
                            this._index++;
                            continue;
                        }

                        if (token.IsKeyword("END"))
                        {
                            if (nestedCaseDepth == 0 && keywords.Any(k => string.Equals(k, "END", StringComparison.OrdinalIgnoreCase)))
                            {
                                break;
                            }

                            if (nestedCaseDepth > 0)
                            {
                                nestedCaseDepth--;
                                result.Add(token);
                                this._index++;
                                continue;
                            }
                        }

                        if (nestedCaseDepth == 0 && keywords.Any(token.IsKeyword))
                        {
                            break;
                        }
                    }

                    result.Add(token);
                    this._index++;
                }

                return result;
            }

            private IReadOnlyList<SqlToken> ReadParenthesizedTokens()
            {
                this.ExpectType(SqlTokenType.OpenParen, "Expected '('.");
                return this.ReadBalancedInner();
            }

            private IReadOnlyList<SqlToken> ReadBalancedInner()
            {
                var depth = 1;
                var list = new List<SqlToken>();
                while (!this.IsEnd)
                {
                    var t = this.Current;
                    this._index++;
                    if (t.Type == SqlTokenType.OpenParen)
                    {
                        depth++;
                        list.Add(t);
                        continue;
                    }

                    if (t.Type == SqlTokenType.CloseParen)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return list;
                        }

                        list.Add(t);
                        continue;
                    }

                    list.Add(t);
                }

                throw new MapException("Unbalanced parentheses.");
            }

            private string? TryReadComparison()
            {
                if (this.TryOp("=")) return "=";
                if (this.TryOp("!") && this.TryOp("=")) return "!=";
                if (this.TryOp("<"))
                {
                    if (this.TryOp(">")) return "<>";
                    if (this.TryOp("=")) return "<=";
                    return "<";
                }

                if (this.TryOp(">"))
                {
                    if (this.TryOp("=")) return ">=";
                    return ">";
                }

                return null;
            }

            private bool TryKeyword(string keyword)
            {
                if (!this.IsEnd && this.Current.IsKeyword(keyword))
                {
                    this._index++;
                    return true;
                }

                return false;
            }

            private void ExpectKeyword(string keyword, string error)
            {
                if (!this.TryKeyword(keyword))
                {
                    throw new MapException(error);
                }
            }

            private bool TryType(SqlTokenType type)
            {
                if (!this.IsEnd && this.Current.Type == type)
                {
                    this._index++;
                    return true;
                }

                return false;
            }

            private void ExpectType(SqlTokenType type, string error)
            {
                if (!this.TryType(type))
                {
                    throw new MapException(error);
                }
            }

            private bool TryOp(string op)
            {
                if (!this.IsEnd && this.Current.Type == SqlTokenType.Operator && this.Current.Text == op)
                {
                    this._index++;
                    return true;
                }

                return false;
            }

            private SqlToken Current => this._tokens[this._index];
            private bool IsEnd => this._index >= this._tokens.Count;
        }

        private sealed class MapException : Exception
        {
            public MapException(string message) : base(message)
            {
            }
        }
    }
}
