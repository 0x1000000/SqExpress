using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SqExpress.SqlParser.Internal.Dom;

namespace SqExpress.SqlParser.Internal.Parsing
{
    internal sealed class SqlDomParser
    {
        public static bool TryParseSingleStatement(
            string sql,
            [NotNullWhen(true)] out SqlDomStatement? statement,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                statement = null;
                errors = new[] { "SQL text cannot be empty." };
                return false;
            }

            var rawSql = sql.Trim();
            var tokens = SqlLexer.Tokenize(rawSql);

            if (HasMultipleStatements(tokens))
            {
                statement = null;
                errors = new[] { "Only one SQL statement is supported." };
                return false;
            }

            if (TryDetectUnsupportedFeature(tokens, out var unsupportedFeatureError))
            {
                statement = null;
                errors = new[] { unsupportedFeatureError };
                return false;
            }

            var cursor = new TokenCursor(tokens, rawSql);
            var withClause = ParseWithClause(cursor);
            var kind = DetermineStatementKind(cursor.Current);

            if (TryDetectBasicSyntaxError(tokens, cursor.Index, kind, out var syntaxError))
            {
                statement = null;
                errors = new[] { syntaxError };
                return false;
            }

            var topLevelSelect = ParseTopLevelSelectIfAny(rawSql, tokens, cursor.Index, kind);
            var tableReferences = ExtractTableReferences(tokens);
            var columnReferences = ExtractColumnReferences(tokens);
            var normalizedSql = SqlTextNormalizer.Normalize(rawSql);

            statement = new SqlDomStatement(
                kind,
                rawSql,
                normalizedSql,
                withClause,
                topLevelSelect,
                tableReferences,
                columnReferences);
            errors = null;
            return true;
        }

        private static bool HasMultipleStatements(IReadOnlyList<SqlToken> tokens)
        {
            var hasAnyToken = false;
            var statementCount = 0;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Type == SqlTokenType.EndOfFile)
                {
                    break;
                }

                if (token.Type == SqlTokenType.Semicolon)
                {
                    if (hasAnyToken)
                    {
                        statementCount++;
                        hasAnyToken = false;
                    }

                    continue;
                }

                hasAnyToken = true;
            }

            if (hasAnyToken)
            {
                statementCount++;
            }

            return statementCount > 1;
        }

        private static bool TryDetectUnsupportedFeature(IReadOnlyList<SqlToken> tokens, [NotNullWhen(true)] out string? error)
        {
            if (ContainsKeyword(tokens, "PIVOT"))
            {
                error = "Feature 'PIVOT' is not supported by SqExpress parser.";
                return true;
            }

            if (ContainsKeyword(tokens, "UNPIVOT"))
            {
                error = "Feature 'UNPIVOT' is not supported by SqExpress parser.";
                return true;
            }

            if (ContainsForJsonOrXml(tokens))
            {
                error = "Feature 'FOR JSON/XML' is not supported by SqExpress parser.";
                return true;
            }

            if (ContainsTopLevelOptionHint(tokens))
            {
                error = "Feature 'OPTION(...)' is not supported by SqExpress parser.";
                return true;
            }

            if (ContainsOutputInto(tokens))
            {
                error = "Feature 'OUTPUT ... INTO' is not supported by SqExpress parser.";
                return true;
            }

            error = null;
            return false;
        }

        private static bool TryDetectBasicSyntaxError(
            IReadOnlyList<SqlToken> tokens,
            int statementStartIndex,
            SqlDomStatementKind kind,
            [NotNullWhen(true)] out string? error)
        {
            if (kind == SqlDomStatementKind.Unknown)
            {
                error = "Unsupported or invalid statement start.";
                return true;
            }

            if (HasUnbalancedParentheses(tokens))
            {
                error = "Syntax error: unbalanced parentheses.";
                return true;
            }

            if (HasDanglingTailToken(tokens))
            {
                error = "Syntax error: unexpected end of statement.";
                return true;
            }

            if (kind == SqlDomStatementKind.Select && IsSelectProjectionMissing(tokens, statementStartIndex))
            {
                error = "Syntax error: SELECT list is missing.";
                return true;
            }

            if (kind == SqlDomStatementKind.Update && !ContainsTopLevelKeyword(tokens, statementStartIndex + 1, "SET"))
            {
                error = "Syntax error: UPDATE statement must contain SET clause.";
                return true;
            }

            if (kind == SqlDomStatementKind.Merge && !ContainsTopLevelKeyword(tokens, statementStartIndex + 1, "ON"))
            {
                error = "Syntax error: MERGE statement must contain ON clause.";
                return true;
            }

            error = null;
            return false;
        }

        private static bool HasUnbalancedParentheses(IReadOnlyList<SqlToken> tokens)
        {
            var depth = 0;
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.EndOfFile)
                {
                    break;
                }

                if (tokens[i].Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    continue;
                }

                if (tokens[i].Type == SqlTokenType.CloseParen)
                {
                    depth--;
                    if (depth < 0)
                    {
                        return true;
                    }
                }
            }

            return depth != 0;
        }

        private static bool HasDanglingTailToken(IReadOnlyList<SqlToken> tokens)
        {
            var last = tokens.Count - 1;
            while (last >= 0
                   && (tokens[last].Type == SqlTokenType.EndOfFile || tokens[last].Type == SqlTokenType.Semicolon))
            {
                last--;
            }

            if (last < 0)
            {
                return true;
            }

            return tokens[last].Type == SqlTokenType.Operator
                   || tokens[last].Type == SqlTokenType.Comma
                   || tokens[last].Type == SqlTokenType.Dot
                   || tokens[last].Type == SqlTokenType.OpenParen;
        }

        private static bool IsSelectProjectionMissing(IReadOnlyList<SqlToken> tokens, int startIndex)
        {
            var index = startIndex + 1;
            if (index >= tokens.Count)
            {
                return true;
            }

            if (tokens[index].IsKeyword("DISTINCT"))
            {
                index++;
            }

            if (index < tokens.Count && tokens[index].IsKeyword("TOP"))
            {
                index++;
                if (index < tokens.Count && tokens[index].Type == SqlTokenType.OpenParen)
                {
                    var close = FindMatchingCloseParen(tokens, index);
                    if (close < 0)
                    {
                        return true;
                    }

                    index = close + 1;
                }
                else if (index < tokens.Count)
                {
                    index++;
                }
            }

            if (index >= tokens.Count)
            {
                return true;
            }

            return tokens[index].IsKeyword("FROM")
                   || tokens[index].IsKeyword("WHERE")
                   || tokens[index].IsKeyword("GROUP")
                   || tokens[index].IsKeyword("HAVING")
                   || tokens[index].IsKeyword("ORDER")
                   || tokens[index].IsKeyword("OFFSET")
                   || tokens[index].IsKeyword("UNION")
                   || tokens[index].IsKeyword("INTERSECT")
                   || tokens[index].IsKeyword("EXCEPT")
                   || tokens[index].Type == SqlTokenType.Comma
                   || tokens[index].Type == SqlTokenType.EndOfFile
                   || tokens[index].Type == SqlTokenType.Semicolon;
        }

        private static bool ContainsTopLevelKeyword(IReadOnlyList<SqlToken> tokens, int startIndex, string keyword)
        {
            var depth = 0;
            for (var i = startIndex; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.EndOfFile || tokens[i].Type == SqlTokenType.Semicolon)
                {
                    break;
                }

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
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsKeyword(IReadOnlyList<SqlToken> tokens, string keyword)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].IsKeyword(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsForJsonOrXml(IReadOnlyList<SqlToken> tokens)
        {
            var depth = 0;
            for (var i = 0; i < tokens.Count - 1; i++)
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

                if (depth == 0
                    && tokens[i].IsKeyword("FOR")
                    && (tokens[i + 1].IsKeyword("JSON") || tokens[i + 1].IsKeyword("XML")))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsTopLevelOptionHint(IReadOnlyList<SqlToken> tokens)
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

                if (depth == 0 && tokens[i].IsKeyword("OPTION"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsOutputInto(IReadOnlyList<SqlToken> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!tokens[i].IsKeyword("OUTPUT"))
                {
                    continue;
                }

                var depth = 0;
                for (var j = i + 1; j < tokens.Count; j++)
                {
                    if (tokens[j].Type == SqlTokenType.EndOfFile || tokens[j].Type == SqlTokenType.Semicolon)
                    {
                        break;
                    }

                    if (tokens[j].Type == SqlTokenType.OpenParen)
                    {
                        depth++;
                        continue;
                    }

                    if (tokens[j].Type == SqlTokenType.CloseParen)
                    {
                        if (depth > 0)
                        {
                            depth--;
                        }

                        continue;
                    }

                    if (depth == 0 && tokens[j].IsKeyword("INTO"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static SqlDomStatementKind DetermineStatementKind(SqlToken token)
        {
            if (token.IsKeyword("SELECT"))
            {
                return SqlDomStatementKind.Select;
            }

            if (token.IsKeyword("INSERT"))
            {
                return SqlDomStatementKind.Insert;
            }

            if (token.IsKeyword("UPDATE"))
            {
                return SqlDomStatementKind.Update;
            }

            if (token.IsKeyword("DELETE"))
            {
                return SqlDomStatementKind.Delete;
            }

            if (token.IsKeyword("MERGE"))
            {
                return SqlDomStatementKind.Merge;
            }

            return SqlDomStatementKind.Unknown;
        }

        private static SqlDomWithClause? ParseWithClause(TokenCursor cursor)
        {
            if (!cursor.Current.IsKeyword("WITH"))
            {
                return null;
            }

            cursor.MoveNext();
            var ctes = new List<SqlDomCte>();

            while (cursor.Current.IsIdentifierLike)
            {
                var cteName = cursor.Current.IdentifierValue;
                cursor.MoveNext();

                if (cursor.Current.Type == SqlTokenType.OpenParen)
                {
                    SkipBalancedParenthesis(cursor);
                }

                if (!cursor.Current.IsKeyword("AS"))
                {
                    break;
                }

                cursor.MoveNext();

                if (cursor.Current.Type != SqlTokenType.OpenParen)
                {
                    break;
                }

                var open = cursor.Current;
                var closeIndex = FindMatchingCloseParen(cursor.Tokens, cursor.Index);
                if (closeIndex < 0)
                {
                    break;
                }

                var close = cursor.Tokens[closeIndex];
                var querySql = cursor.Sql.Substring(open.End, close.Start - open.End).Trim();
                ctes.Add(new SqlDomCte(cteName, querySql));

                cursor.Index = closeIndex + 1;
                if (cursor.Current.Type == SqlTokenType.Comma)
                {
                    cursor.MoveNext();
                    continue;
                }

                break;
            }

            return ctes.Count > 0
                ? new SqlDomWithClause(ctes)
                : null;
        }

        private static SqlDomSelectClause? ParseTopLevelSelectIfAny(
            string sql,
            IReadOnlyList<SqlToken> tokens,
            int startIndex,
            SqlDomStatementKind kind)
        {
            if (kind != SqlDomStatementKind.Select)
            {
                return null;
            }

            var index = startIndex;
            if (!tokens[index].IsKeyword("SELECT"))
            {
                return null;
            }

            index++;
            var isDistinct = false;
            string? topSql = null;

            if (tokens[index].IsKeyword("DISTINCT"))
            {
                isDistinct = true;
                index++;
            }

            if (tokens[index].IsKeyword("TOP"))
            {
                var topStart = index + 1;
                index++;
                if (tokens[index].Type == SqlTokenType.OpenParen)
                {
                    var closeIndex = FindMatchingCloseParen(tokens, index);
                    if (closeIndex > index)
                    {
                        index = closeIndex + 1;
                    }
                }
                else if (tokens[index].Type != SqlTokenType.EndOfFile)
                {
                    index++;
                }

                topSql = SliceSql(sql, tokens, topStart, index);
            }

            var selectStart = index;
            var selectEnd = FindFirstTopLevel(tokens, index, new[] { "FROM", "WHERE", "GROUP", "HAVING", "ORDER", "OFFSET", "UNION", "INTERSECT", "EXCEPT" });
            if (selectEnd < 0)
            {
                selectEnd = FindStatementEnd(tokens, index);
            }

            var items = ParseSelectItems(sql, tokens, selectStart, selectEnd);

            SqlDomTableSource? from = null;
            string? whereSql = null;
            string? groupBySql = null;
            string? havingSql = null;
            string? orderBySql = null;
            string? offsetFetchSql = null;
            var hasSetOperation = false;

            var current = selectEnd;
            if (current >= 0 && tokens[current].IsKeyword("FROM"))
            {
                var fromStart = current + 1;
                var fromEnd = FindFirstTopLevel(tokens, fromStart, new[] { "WHERE", "GROUP", "HAVING", "ORDER", "OFFSET", "UNION", "INTERSECT", "EXCEPT" });
                if (fromEnd < 0)
                {
                    fromEnd = FindStatementEnd(tokens, fromStart);
                }

                from = ParseTableSource(sql, tokens, fromStart, fromEnd);
                current = fromEnd;
            }

            while (current >= 0 && current < tokens.Count)
            {
                if (tokens[current].Type == SqlTokenType.EndOfFile || tokens[current].Type == SqlTokenType.Semicolon)
                {
                    break;
                }

                if (tokens[current].IsKeyword("WHERE"))
                {
                    var end = FindFirstTopLevel(tokens, current + 1, new[] { "GROUP", "HAVING", "ORDER", "OFFSET", "UNION", "INTERSECT", "EXCEPT" });
                    if (end < 0)
                    {
                        end = FindStatementEnd(tokens, current + 1);
                    }

                    whereSql = SliceSql(sql, tokens, current + 1, end);
                    current = end;
                    continue;
                }

                if (tokens[current].IsKeyword("GROUP") && IsKeyword(tokens, current + 1, "BY"))
                {
                    var end = FindFirstTopLevel(tokens, current + 2, new[] { "HAVING", "ORDER", "OFFSET", "UNION", "INTERSECT", "EXCEPT" });
                    if (end < 0)
                    {
                        end = FindStatementEnd(tokens, current + 2);
                    }

                    groupBySql = SliceSql(sql, tokens, current + 2, end);
                    current = end;
                    continue;
                }

                if (tokens[current].IsKeyword("HAVING"))
                {
                    var end = FindFirstTopLevel(tokens, current + 1, new[] { "ORDER", "OFFSET", "UNION", "INTERSECT", "EXCEPT" });
                    if (end < 0)
                    {
                        end = FindStatementEnd(tokens, current + 1);
                    }

                    havingSql = SliceSql(sql, tokens, current + 1, end);
                    current = end;
                    continue;
                }

                if (tokens[current].IsKeyword("ORDER") && IsKeyword(tokens, current + 1, "BY"))
                {
                    var end = FindFirstTopLevel(tokens, current + 2, new[] { "OFFSET", "UNION", "INTERSECT", "EXCEPT" });
                    if (end < 0)
                    {
                        end = FindStatementEnd(tokens, current + 2);
                    }

                    orderBySql = SliceSql(sql, tokens, current + 2, end);
                    current = end;
                    continue;
                }

                if (tokens[current].IsKeyword("OFFSET"))
                {
                    var end = FindFirstTopLevel(tokens, current + 1, new[] { "UNION", "INTERSECT", "EXCEPT" });
                    if (end < 0)
                    {
                        end = FindStatementEnd(tokens, current + 1);
                    }

                    offsetFetchSql = SliceSql(sql, tokens, current, end);
                    current = end;
                    continue;
                }

                if (tokens[current].IsKeyword("UNION")
                    || tokens[current].IsKeyword("INTERSECT")
                    || tokens[current].IsKeyword("EXCEPT"))
                {
                    hasSetOperation = true;
                    break;
                }

                current++;
            }

            return new SqlDomSelectClause(
                items,
                from,
                whereSql,
                groupBySql,
                havingSql,
                orderBySql,
                offsetFetchSql,
                isDistinct,
                topSql,
                hasSetOperation);
        }

        private static IReadOnlyList<SqlDomSelectItem> ParseSelectItems(string sql, IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            var result = new List<SqlDomSelectItem>();
            var segmentStart = startInclusive;
            var depth = 0;

            for (var i = startInclusive; i < endExclusive; i++)
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

                if (tokens[i].Type == SqlTokenType.Comma && depth == 0)
                {
                    AddSelectItem(sql, tokens, segmentStart, i, result);
                    segmentStart = i + 1;
                }
            }

            if (segmentStart < endExclusive)
            {
                AddSelectItem(sql, tokens, segmentStart, endExclusive, result);
            }

            return result;
        }

        private static void AddSelectItem(string sql, IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive, ICollection<SqlDomSelectItem> result)
        {
            var itemSql = SliceSql(sql, tokens, startInclusive, endExclusive);
            if (itemSql.Length == 0)
            {
                return;
            }

            var alias = TryParseProjectionAlias(tokens, startInclusive, endExclusive);
            result.Add(new SqlDomSelectItem(itemSql, alias));
        }

        private static string? TryParseProjectionAlias(IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            if (endExclusive - startInclusive < 2)
            {
                return null;
            }

            var last = tokens[endExclusive - 1];
            if (!last.IsIdentifierLike)
            {
                return null;
            }

            var prev = tokens[endExclusive - 2];
            if (prev.IsKeyword("AS"))
            {
                return last.IdentifierValue;
            }

            if (prev.Type == SqlTokenType.CloseParen
                || prev.IsIdentifierLike
                || prev.Type == SqlTokenType.NumberLiteral
                || prev.Type == SqlTokenType.StringLiteral)
            {
                return null;
            }

            return last.IdentifierValue;
        }

        private static SqlDomTableSource? ParseTableSource(string sql, IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            var index = startInclusive;
            var left = ParseTableFactor(sql, tokens, ref index, endExclusive);
            if (left == null)
            {
                return null;
            }

            while (index < endExclusive)
            {
                if (!TryParseJoinType(tokens, ref index, endExclusive, out var joinType))
                {
                    break;
                }

                var right = ParseTableFactor(sql, tokens, ref index, endExclusive);
                if (right == null)
                {
                    break;
                }

                string? onSql = null;
                if (joinType == SqlDomJoinType.Inner
                    || joinType == SqlDomJoinType.Left
                    || joinType == SqlDomJoinType.Right
                    || joinType == SqlDomJoinType.Full)
                {
                    if (index < endExclusive && tokens[index].IsKeyword("ON"))
                    {
                        var onStart = index + 1;
                        index = FindNextJoinBoundary(tokens, onStart, endExclusive);
                        onSql = SliceSql(sql, tokens, onStart, index);
                    }
                }

                left = new SqlDomJoinedTableSource(left, right, joinType, onSql);
            }

            return left;
        }

        private static SqlDomTableSource? ParseTableFactor(string sql, IReadOnlyList<SqlToken> tokens, ref int index, int endExclusive)
        {
            if (index >= endExclusive)
            {
                return null;
            }

            if (tokens[index].Type == SqlTokenType.OpenParen)
            {
                var openIndex = index;
                var closeIndex = FindMatchingCloseParen(tokens, openIndex);
                if (closeIndex < 0 || closeIndex >= endExclusive)
                {
                    return null;
                }

                var innerSql = sql.Substring(tokens[openIndex].End, tokens[closeIndex].Start - tokens[openIndex].End).Trim();
                index = closeIndex + 1;

                var alias = ParseOptionalAlias(tokens, ref index, endExclusive);
                var columnAliases = ParseOptionalColumnAliasList(tokens, ref index, endExclusive);

                if (innerSql.StartsWith("VALUES", StringComparison.OrdinalIgnoreCase))
                {
                    return new SqlDomValuesTableSource(innerSql, alias, columnAliases);
                }

                return new SqlDomDerivedTableSource(innerSql, alias);
            }

            var nameParts = ParseMultipartIdentifier(tokens, ref index, endExclusive);
            if (nameParts.Count < 1)
            {
                return null;
            }

            if (index < endExclusive && tokens[index].Type == SqlTokenType.OpenParen)
            {
                var openIndex = index;
                var closeIndex = FindMatchingCloseParen(tokens, openIndex);
                if (closeIndex < 0)
                {
                    return null;
                }

                var argsSql = sql.Substring(tokens[openIndex].End, tokens[closeIndex].Start - tokens[openIndex].End).Trim();
                index = closeIndex + 1;
                var alias = ParseOptionalAlias(tokens, ref index, endExclusive);
                return new SqlDomFunctionTableSource(string.Join(".", nameParts), argsSql, alias);
            }

            var table = nameParts[nameParts.Count - 1];
            string? schema = nameParts.Count >= 2 ? nameParts[nameParts.Count - 2] : null;
            var finalAlias = ParseOptionalAlias(tokens, ref index, endExclusive);
            return new SqlDomNamedTableSource(schema, table, finalAlias);
        }

        private static bool TryParseJoinType(IReadOnlyList<SqlToken> tokens, ref int index, int endExclusive, out SqlDomJoinType joinType)
        {
            joinType = SqlDomJoinType.Inner;
            if (index >= endExclusive)
            {
                return false;
            }

            if (IsKeyword(tokens, index, "JOIN"))
            {
                index++;
                joinType = SqlDomJoinType.Inner;
                return true;
            }

            if (IsKeyword(tokens, index, "INNER") && IsKeyword(tokens, index + 1, "JOIN"))
            {
                index += 2;
                joinType = SqlDomJoinType.Inner;
                return true;
            }

            if (IsKeyword(tokens, index, "LEFT"))
            {
                index++;
                if (IsKeyword(tokens, index, "OUTER"))
                {
                    index++;
                }

                if (IsKeyword(tokens, index, "JOIN"))
                {
                    index++;
                    joinType = SqlDomJoinType.Left;
                    return true;
                }
            }

            if (IsKeyword(tokens, index, "RIGHT"))
            {
                index++;
                if (IsKeyword(tokens, index, "OUTER"))
                {
                    index++;
                }

                if (IsKeyword(tokens, index, "JOIN"))
                {
                    index++;
                    joinType = SqlDomJoinType.Right;
                    return true;
                }
            }

            if (IsKeyword(tokens, index, "FULL"))
            {
                index++;
                if (IsKeyword(tokens, index, "OUTER"))
                {
                    index++;
                }

                if (IsKeyword(tokens, index, "JOIN"))
                {
                    index++;
                    joinType = SqlDomJoinType.Full;
                    return true;
                }
            }

            if (IsKeyword(tokens, index, "CROSS"))
            {
                if (IsKeyword(tokens, index + 1, "JOIN"))
                {
                    index += 2;
                    joinType = SqlDomJoinType.Cross;
                    return true;
                }

                if (IsKeyword(tokens, index + 1, "APPLY"))
                {
                    index += 2;
                    joinType = SqlDomJoinType.CrossApply;
                    return true;
                }
            }

            if (IsKeyword(tokens, index, "OUTER") && IsKeyword(tokens, index + 1, "APPLY"))
            {
                index += 2;
                joinType = SqlDomJoinType.OuterApply;
                return true;
            }

            return false;
        }

        private static List<SqlDomTableReference> ExtractTableReferences(IReadOnlyList<SqlToken> tokens)
        {
            var result = new List<SqlDomTableReference>();
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!IsReferenceLeadKeyword(tokens[i]))
                {
                    continue;
                }

                var index = i + 1;
                if (TryReadNamedTableReference(tokens, ref index, out var tableReference))
                {
                    result.Add(tableReference);
                    i = index - 1;
                }
            }

            return result;
        }

        private static bool IsReferenceLeadKeyword(SqlToken token)
        {
            return token.IsKeyword("FROM")
                   || token.IsKeyword("JOIN")
                   || token.IsKeyword("INTO")
                   || token.IsKeyword("USING")
                   || token.IsKeyword("MERGE");
        }

        private static bool TryReadNamedTableReference(IReadOnlyList<SqlToken> tokens, ref int index, [NotNullWhen(true)] out SqlDomTableReference? tableReference)
        {
            tableReference = null;
            if (index >= tokens.Count || tokens[index].Type == SqlTokenType.EndOfFile)
            {
                return false;
            }

            if (tokens[index].Type == SqlTokenType.OpenParen)
            {
                var closeIndex = FindMatchingCloseParen(tokens, index);
                if (closeIndex < 0)
                {
                    return false;
                }

                index = closeIndex + 1;
                ParseOptionalAlias(tokens, ref index, tokens.Count);
                return false;
            }

            var nameParts = ParseMultipartIdentifier(tokens, ref index, tokens.Count);
            if (nameParts.Count < 1)
            {
                return false;
            }

            if (index < tokens.Count && tokens[index].Type == SqlTokenType.OpenParen)
            {
                var closeIndex = FindMatchingCloseParen(tokens, index);
                if (closeIndex < 0)
                {
                    return false;
                }

                index = closeIndex + 1;
                ParseOptionalAlias(tokens, ref index, tokens.Count);
                return false;
            }

            var table = nameParts[nameParts.Count - 1];
            var schema = nameParts.Count >= 2 ? nameParts[nameParts.Count - 2] : null;
            var alias = ParseOptionalAlias(tokens, ref index, tokens.Count);
            tableReference = new SqlDomTableReference(schema, table, alias);
            return true;
        }

        private static List<SqlDomColumnReference> ExtractColumnReferences(IReadOnlyList<SqlToken> tokens)
        {
            var result = new List<SqlDomColumnReference>();
            for (var i = 0; i < tokens.Count - 2; i++)
            {
                if (!tokens[i].IsIdentifierLike || tokens[i + 1].Type != SqlTokenType.Dot || !tokens[i + 2].IsIdentifierLike)
                {
                    continue;
                }

                result.Add(new SqlDomColumnReference(tokens[i].IdentifierValue, tokens[i + 2].IdentifierValue));
            }

            return result;
        }

        private static List<string> ParseMultipartIdentifier(IReadOnlyList<SqlToken> tokens, ref int index, int endExclusive)
        {
            var result = new List<string>();
            if (index >= endExclusive || !tokens[index].IsIdentifierLike)
            {
                return result;
            }

            result.Add(tokens[index].IdentifierValue);
            index++;

            while ((index + 1) < endExclusive && tokens[index].Type == SqlTokenType.Dot && tokens[index + 1].IsIdentifierLike)
            {
                index++;
                result.Add(tokens[index].IdentifierValue);
                index++;
            }

            return result;
        }

        private static string? ParseOptionalAlias(IReadOnlyList<SqlToken> tokens, ref int index, int endExclusive)
        {
            if (index >= endExclusive)
            {
                return null;
            }

            if (IsKeyword(tokens, index, "AS"))
            {
                index++;
            }

            if (index < endExclusive && tokens[index].IsIdentifierLike && !IsReservedWord(tokens[index]))
            {
                var alias = tokens[index].IdentifierValue;
                index++;
                return alias;
            }

            return null;
        }

        private static IReadOnlyList<string> ParseOptionalColumnAliasList(IReadOnlyList<SqlToken> tokens, ref int index, int endExclusive)
        {
            if (index >= endExclusive || tokens[index].Type != SqlTokenType.OpenParen)
            {
                return Array.Empty<string>();
            }

            var closeIndex = FindMatchingCloseParen(tokens, index);
            if (closeIndex < 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            for (var i = index + 1; i < closeIndex; i++)
            {
                if (tokens[i].IsIdentifierLike)
                {
                    result.Add(tokens[i].IdentifierValue);
                }
            }

            index = closeIndex + 1;
            return result;
        }

        private static int FindMatchingCloseParen(IReadOnlyList<SqlToken> tokens, int openParenIndex)
        {
            var depth = 0;
            for (var i = openParenIndex; i < tokens.Count; i++)
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

        private static int FindStatementEnd(IReadOnlyList<SqlToken> tokens, int startIndex)
        {
            for (var i = startIndex; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.EndOfFile || tokens[i].Type == SqlTokenType.Semicolon)
                {
                    return i;
                }
            }

            return tokens.Count;
        }

        private static int FindFirstTopLevel(IReadOnlyList<SqlToken> tokens, int startIndex, IReadOnlyList<string> keywords)
        {
            var depth = 0;
            for (var i = startIndex; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.EndOfFile || tokens[i].Type == SqlTokenType.Semicolon)
                {
                    return i;
                }

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

                if (depth == 0)
                {
                    for (var k = 0; k < keywords.Count; k++)
                    {
                        if (tokens[i].IsKeyword(keywords[k]))
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        private static int FindNextJoinBoundary(IReadOnlyList<SqlToken> tokens, int startIndex, int endExclusive)
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

                if (depth == 0 && IsJoinLead(tokens, i))
                {
                    return i;
                }
            }

            return endExclusive;
        }

        private static bool IsJoinLead(IReadOnlyList<SqlToken> tokens, int index)
        {
            return IsKeyword(tokens, index, "JOIN")
                   || IsKeyword(tokens, index, "INNER")
                   || IsKeyword(tokens, index, "LEFT")
                   || IsKeyword(tokens, index, "RIGHT")
                   || IsKeyword(tokens, index, "FULL")
                   || IsKeyword(tokens, index, "CROSS")
                   || IsKeyword(tokens, index, "OUTER");
        }

        private static bool IsReservedWord(SqlToken token)
        {
            if (token.Type != SqlTokenType.Identifier)
            {
                return false;
            }

            switch (token.Text.ToUpperInvariant())
            {
                case "SELECT":
                case "FROM":
                case "JOIN":
                case "UPDATE":
                case "DELETE":
                case "INSERT":
                case "MERGE":
                case "WHERE":
                case "SET":
                case "VALUES":
                case "INTO":
                case "USING":
                case "ON":
                case "GROUP":
                case "BY":
                case "ORDER":
                case "OFFSET":
                case "FETCH":
                case "ROW":
                case "ROWS":
                case "UNION":
                case "INTERSECT":
                case "EXCEPT":
                case "OUTER":
                case "CROSS":
                case "APPLY":
                case "WHEN":
                case "THEN":
                case "ELSE":
                case "END":
                case "AS":
                case "WITH":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsKeyword(IReadOnlyList<SqlToken> tokens, int index, string keyword)
            => index >= 0 && index < tokens.Count && tokens[index].IsKeyword(keyword);

        private static void SkipBalancedParenthesis(TokenCursor cursor)
        {
            if (cursor.Current.Type != SqlTokenType.OpenParen)
            {
                return;
            }

            var closeIndex = FindMatchingCloseParen(cursor.Tokens, cursor.Index);
            if (closeIndex > cursor.Index)
            {
                cursor.Index = closeIndex + 1;
            }
        }

        private static string SliceSql(string sql, IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            if (startInclusive >= endExclusive || startInclusive < 0 || endExclusive > tokens.Count)
            {
                return string.Empty;
            }

            var startPos = tokens[startInclusive].Start;
            var endPos = tokens[endExclusive - 1].End;
            return sql.Substring(startPos, endPos - startPos).Trim();
        }

        private sealed class TokenCursor
        {
            public TokenCursor(IReadOnlyList<SqlToken> tokens, string sql)
            {
                this.Tokens = tokens;
                this.Sql = sql;
                this.Index = 0;
            }

            public IReadOnlyList<SqlToken> Tokens { get; }

            public string Sql { get; }

            public int Index { get; set; }

            public SqlToken Current
                => this.Index >= 0 && this.Index < this.Tokens.Count
                    ? this.Tokens[this.Index]
                    : this.Tokens[this.Tokens.Count - 1];

            public void MoveNext()
            {
                if (this.Index < this.Tokens.Count - 1)
                {
                    this.Index++;
                }
            }
        }
    }
}
