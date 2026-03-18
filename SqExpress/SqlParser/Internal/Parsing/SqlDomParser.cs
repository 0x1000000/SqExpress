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
            IReadOnlyList<SqlToken> tokens;
            try
            {
                tokens = SqlLexer.Tokenize(rawSql);
            }
            catch (InvalidOperationException ex)
            {
                statement = null;
                errors = new[] { ex.Message };
                return false;
            }

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

            var topLevelSelect = ParseTopLevelSelectIfAny(rawSql, tokens, cursor.Index, kind);

            if (TryDetectBasicSyntaxError(tokens, cursor.Index, kind, topLevelSelect, out var syntaxError))
            {
                statement = null;
                errors = new[] { syntaxError };
                return false;
            }

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
            SqlDomSelectClause? topLevelSelect,
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

            if (TryDetectSelectClauseError(topLevelSelect, out error))
            {
                return true;
            }

            if (HasDanglingTailToken(tokens))
            {
                error = "Syntax error: unexpected end of statement.";
                return true;
            }

            if (HasInvalidJoinSyntax(tokens))
            {
                error = "Syntax error: JOIN clause must contain ON condition.";
                return true;
            }

            if (HasUnexpectedOnAfterCrossOrApply(tokens))
            {
                error = "Syntax error: CROSS/ APPLY join cannot contain ON condition.";
                return true;
            }

            if (kind == SqlDomStatementKind.Select && IsSelectProjectionMissing(tokens, statementStartIndex))
            {
                error = "Syntax error: SELECT list is missing.";
                return true;
            }

            if (HasEmptyInPredicate(tokens))
            {
                error = "Syntax error: IN predicate list cannot be empty.";
                return true;
            }

            if (kind == SqlDomStatementKind.Update && !ContainsTopLevelKeyword(tokens, statementStartIndex + 1, "SET"))
            {
                error = "Syntax error: UPDATE statement must contain SET clause.";
                return true;
            }

            if (kind == SqlDomStatementKind.Update && TryDetectInvalidUpdateSetClause(tokens, statementStartIndex, out error))
            {
                return true;
            }

            if (kind == SqlDomStatementKind.Delete && !HasDeleteTarget(tokens, statementStartIndex))
            {
                error = "Syntax error: DELETE statement must contain target.";
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

        private static bool TryDetectSelectClauseError(SqlDomSelectClause? selectClause, [NotNullWhen(true)] out string? error)
        {
            if (selectClause == null)
            {
                error = null;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(selectClause.HavingSql))
            {
                error = "Feature 'HAVING' is not supported by SqExpress parser.";
                return true;
            }

            if (TryDetectInvalidWildcardProjectionAlias(selectClause.Items, out error))
            {
                return true;
            }

            if (selectClause.HasFromClause && selectClause.From == null)
            {
                error = "Syntax error: FROM clause is invalid.";
                return true;
            }

            if (selectClause.GroupBySql != null && !IsValidTopLevelCommaSeparatedClause(selectClause.GroupBySql))
            {
                error = "Syntax error: GROUP BY clause is invalid.";
                return true;
            }

            if (selectClause.OrderBySql != null && !IsValidTopLevelCommaSeparatedClause(selectClause.OrderBySql))
            {
                error = "Syntax error: ORDER BY clause is invalid.";
                return true;
            }

            if (ContainsTopLevelKeyword(selectClause.OrderBySql, "FETCH"))
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            if (TryDetectOffsetFetchError(selectClause.OrderBySql, selectClause.OffsetFetchSql, out error))
            {
                return true;
            }

            error = null;
            return false;
        }

        private static bool TryDetectInvalidWildcardProjectionAlias(
            IReadOnlyList<SqlDomSelectItem> items,
            [NotNullWhen(true)] out string? error)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (string.IsNullOrWhiteSpace(item.Alias))
                {
                    continue;
                }

                if (IsWildcardProjectionAlias(item.Sql, item.Alias!))
                {
                    error = $"Syntax error: incorrect syntax near '{item.Alias}'.";
                    return true;
                }
            }

            error = null;
            return false;
        }

        private static bool IsWildcardProjectionAlias(string itemSql, string alias)
        {
            var tokens = GetMeaningfulTokens(itemSql);
            if (tokens.Count < 2)
            {
                return false;
            }

            var endExclusive = tokens.Count;
            var last = tokens[endExclusive - 1];

            if (last.Type == SqlTokenType.StringLiteral)
            {
                if (!string.Equals(ParseAliasToken(last), alias, StringComparison.Ordinal))
                {
                    return false;
                }

                endExclusive--;
                if (endExclusive > 0 && tokens[endExclusive - 1].IsKeyword("AS"))
                {
                    endExclusive--;
                }
            }
            else if (last.IsIdentifierLike)
            {
                if (!string.Equals(last.IdentifierValue, alias, StringComparison.Ordinal))
                {
                    return false;
                }

                endExclusive--;
                if (endExclusive > 0 && tokens[endExclusive - 1].IsKeyword("AS"))
                {
                    endExclusive--;
                }
            }
            else
            {
                return false;
            }

            if (endExclusive == 1 && tokens[0].Type == SqlTokenType.Operator && tokens[0].Text == "*")
            {
                return true;
            }

            return endExclusive == 3
                   && tokens[0].IsIdentifierLike
                   && tokens[1].Type == SqlTokenType.Dot
                   && tokens[2].Type == SqlTokenType.Operator
                   && tokens[2].Text == "*";
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
                   || tokens[last].Type == SqlTokenType.OpenParen
                   || tokens[last].IsKeyword("WHERE")
                   || tokens[last].IsKeyword("ON")
                   || tokens[last].IsKeyword("AND")
                   || tokens[last].IsKeyword("OR")
                   || tokens[last].IsKeyword("SET")
                   || tokens[last].IsKeyword("FROM")
                   || tokens[last].IsKeyword("USING")
                   || tokens[last].IsKeyword("WHEN")
                   || tokens[last].IsKeyword("THEN")
                   || tokens[last].IsKeyword("BY");
        }

        private static bool HasEmptyInPredicate(IReadOnlyList<SqlToken> tokens)
        {
            for (var i = 0; i < tokens.Count - 2; i++)
            {
                if (!tokens[i].IsKeyword("IN"))
                {
                    continue;
                }

                if (tokens[i + 1].Type == SqlTokenType.OpenParen
                    && tokens[i + 2].Type == SqlTokenType.CloseParen)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryDetectInvalidUpdateSetClause(
            IReadOnlyList<SqlToken> tokens,
            int statementStartIndex,
            [NotNullWhen(true)] out string? error)
        {
            var setIndex = FindTopLevelKeywordIndex(tokens, statementStartIndex + 1, "SET");
            if (setIndex < 0)
            {
                error = null;
                return false;
            }

            var end = FindFirstTopLevel(tokens, setIndex + 1, new[] { "FROM", "WHERE", "OUTPUT" });
            if (end < 0)
            {
                end = FindStatementEnd(tokens, setIndex + 1);
            }

            if (end <= setIndex + 1)
            {
                error = "Syntax error: UPDATE SET clause is invalid.";
                return true;
            }

            if (!IsValidTopLevelCommaSeparatedClause(tokens, setIndex + 1, end))
            {
                error = "Syntax error: UPDATE SET clause is invalid.";
                return true;
            }

            error = null;
            return false;
        }

        private static bool HasDeleteTarget(IReadOnlyList<SqlToken> tokens, int statementStartIndex)
        {
            var cursor = statementStartIndex + 1;
            if (cursor >= tokens.Count)
            {
                return false;
            }

            if (cursor < tokens.Count && tokens[cursor].IsKeyword("TOP"))
            {
                cursor++;
                if (cursor >= tokens.Count)
                {
                    return false;
                }

                if (tokens[cursor].Type == SqlTokenType.OpenParen)
                {
                    var close = FindMatchingCloseParen(tokens, cursor);
                    if (close < 0)
                    {
                        return false;
                    }

                    cursor = close + 1;
                }
                else
                {
                    cursor++;
                }

                if (cursor < tokens.Count && tokens[cursor].IsKeyword("PERCENT"))
                {
                    cursor++;
                }
            }

            if (cursor >= tokens.Count)
            {
                return false;
            }

            if (tokens[cursor].IsKeyword("FROM"))
            {
                cursor++;
                return cursor < tokens.Count
                       && tokens[cursor].Type != SqlTokenType.EndOfFile
                       && tokens[cursor].Type != SqlTokenType.Semicolon
                       && !tokens[cursor].IsKeyword("WHERE")
                       && !tokens[cursor].IsKeyword("OUTPUT");
            }

            return tokens[cursor].Type != SqlTokenType.EndOfFile
                   && tokens[cursor].Type != SqlTokenType.Semicolon
                   && !tokens[cursor].IsKeyword("WHERE")
                   && !tokens[cursor].IsKeyword("OUTPUT")
                   && !tokens[cursor].IsKeyword("FROM");
        }

        private static bool IsValidTopLevelCommaSeparatedClause(string? sql)
        {
            if (sql == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                return false;
            }

            var meaningfulTokens = GetMeaningfulTokens(sql);
            if (meaningfulTokens.Count < 1)
            {
                return false;
            }

            var depth = 0;
            var segmentHasToken = false;
            for (var i = 0; i < meaningfulTokens.Count; i++)
            {
                var token = meaningfulTokens[i];
                if (token.Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    segmentHasToken = true;
                    continue;
                }

                if (token.Type == SqlTokenType.CloseParen)
                {
                    if (depth > 0)
                    {
                        depth--;
                    }

                    segmentHasToken = true;
                    continue;
                }

                if (depth == 0 && token.Type == SqlTokenType.Comma)
                {
                    if (!segmentHasToken)
                    {
                        return false;
                    }

                    segmentHasToken = false;
                    continue;
                }

                segmentHasToken = true;
            }

            return segmentHasToken;
        }

        private static bool IsValidTopLevelCommaSeparatedClause(IReadOnlyList<SqlToken> tokens, int startInclusive, int endExclusive)
        {
            if (endExclusive <= startInclusive)
            {
                return false;
            }

            var depth = 0;
            var segmentHasToken = false;
            for (var i = startInclusive; i < endExclusive; i++)
            {
                var token = tokens[i];
                if (token.Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    segmentHasToken = true;
                    continue;
                }

                if (token.Type == SqlTokenType.CloseParen)
                {
                    if (depth > 0)
                    {
                        depth--;
                    }

                    segmentHasToken = true;
                    continue;
                }

                if (depth == 0 && token.Type == SqlTokenType.Comma)
                {
                    if (!segmentHasToken)
                    {
                        return false;
                    }

                    segmentHasToken = false;
                    continue;
                }

                segmentHasToken = true;
            }

            return segmentHasToken;
        }

        private static bool ContainsTopLevelKeyword(string? sql, string keyword)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return false;
            }

            var tokens = SqlLexer.Tokenize(sql!);
            return ContainsTopLevelKeyword(tokens, 0, keyword);
        }

        private static bool TryDetectOffsetFetchError(string? orderBySql, string? offsetFetchSql, [NotNullWhen(true)] out string? error)
        {
            if (string.IsNullOrWhiteSpace(offsetFetchSql))
            {
                error = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(orderBySql))
            {
                error = "Syntax error: OFFSET requires ORDER BY clause.";
                return true;
            }

            var tokens = GetMeaningfulTokens(offsetFetchSql!);
            if (tokens.Count < 3 || !tokens[0].IsKeyword("OFFSET"))
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            var index = 1;
            if (!TryReadClauseExpression(tokens, ref index, "ROW", "ROWS"))
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            if (index >= tokens.Count)
            {
                error = null;
                return false;
            }

            if (!tokens[index].IsKeyword("FETCH"))
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            index++;
            if (index >= tokens.Count || (!tokens[index].IsKeyword("NEXT") && !tokens[index].IsKeyword("FIRST")))
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            index++;
            if (!TryReadClauseExpression(tokens, ref index, "ROW", "ROWS"))
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            if (index >= tokens.Count || !tokens[index].IsKeyword("ONLY"))
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            index++;
            if (index != tokens.Count)
            {
                error = "Syntax error: OFFSET/FETCH clause is invalid.";
                return true;
            }

            error = null;
            return false;
        }

        private static List<SqlToken> GetMeaningfulTokens(string sql)
        {
            var meaningfulTokens = new List<SqlToken>();
            var tokens = SqlLexer.Tokenize(sql);
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type != SqlTokenType.EndOfFile)
                {
                    meaningfulTokens.Add(tokens[i]);
                }
            }

            return meaningfulTokens;
        }

        private static bool TryReadClauseExpression(IReadOnlyList<SqlToken> tokens, ref int index, string terminalKeyword1, string terminalKeyword2)
        {
            var depth = 0;
            var expressionStart = index;

            while (index < tokens.Count)
            {
                if (tokens[index].Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    index++;
                    continue;
                }

                if (tokens[index].Type == SqlTokenType.CloseParen)
                {
                    if (depth == 0)
                    {
                        break;
                    }

                    depth--;
                    index++;
                    continue;
                }

                if (depth == 0 && (tokens[index].IsKeyword(terminalKeyword1) || tokens[index].IsKeyword(terminalKeyword2)))
                {
                    break;
                }

                index++;
            }

            if (index <= expressionStart || index >= tokens.Count)
            {
                return false;
            }

            if (!tokens[index].IsKeyword(terminalKeyword1) && !tokens[index].IsKeyword(terminalKeyword2))
            {
                return false;
            }

            index++;
            return true;
        }

        private static bool HasInvalidJoinSyntax(IReadOnlyList<SqlToken> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.EndOfFile)
                {
                    break;
                }

                if (!IsKeyword(tokens, i, "INNER")
                    && !IsKeyword(tokens, i, "LEFT")
                    && !IsKeyword(tokens, i, "RIGHT")
                    && !IsKeyword(tokens, i, "FULL"))
                {
                    continue;
                }

                if (i + 1 >= tokens.Count || !IsKeyword(tokens, i + 1, "JOIN"))
                {
                    continue;
                }

                var boundary = FindNextJoinBoundary(tokens, i + 2, tokens.Count);
                var hasOn = false;
                var depth = 0;
                for (var j = i + 2; j < boundary; j++)
                {
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

                    if (depth == 0 && IsKeyword(tokens, j, "ON"))
                    {
                        hasOn = true;
                        break;
                    }
                }

                if (!hasOn)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnexpectedOnAfterCrossOrApply(IReadOnlyList<SqlToken> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.EndOfFile)
                {
                    break;
                }

                var isCrossJoin = IsKeyword(tokens, i, "CROSS") && i + 1 < tokens.Count && IsKeyword(tokens, i + 1, "JOIN");
                var isCrossApply = IsKeyword(tokens, i, "CROSS") && i + 1 < tokens.Count && IsKeyword(tokens, i + 1, "APPLY");
                var isOuterApply = IsKeyword(tokens, i, "OUTER") && i + 1 < tokens.Count && IsKeyword(tokens, i + 1, "APPLY");
                if (!isCrossJoin && !isCrossApply && !isOuterApply)
                {
                    continue;
                }

                var boundary = FindNextJoinBoundary(tokens, i + 2, tokens.Count);
                var depth = 0;
                for (var j = i + 2; j < boundary; j++)
                {
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

                    if (depth == 0 && IsKeyword(tokens, j, "ON"))
                    {
                        return true;
                    }
                }
            }

            return false;
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
            var hasFromClause = false;

            var current = selectEnd;
            if (current >= 0 && tokens[current].IsKeyword("FROM"))
            {
                hasFromClause = true;
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
                hasFromClause,
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
            if (last.Type == SqlTokenType.StringLiteral)
            {
                var prevString = tokens[endExclusive - 2];
                return prevString.IsKeyword("AS") ? ParseAliasToken(last) : null;
            }

            if (!last.IsIdentifierLike)
            {
                return null;
            }

            if (last.IdentifierValue.StartsWith("@", StringComparison.Ordinal))
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

        private static string ParseAliasToken(SqlToken token)
        {
            if (token.Type != SqlTokenType.StringLiteral)
            {
                return token.IdentifierValue;
            }

            return token.Text.Length >= 3 && (token.Text[0] == 'N' || token.Text[0] == 'n') && token.Text[1] == '\''
                ? token.Text.Substring(2, token.Text.Length - 3).Replace("''", "'")
                : token.Text.Length >= 2
                    ? token.Text.Substring(1, token.Text.Length - 2).Replace("''", "'")
                    : string.Empty;
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

            if (IsReservedTableFactorLeadKeyword(tokens[index]))
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

        private static bool IsReservedTableFactorLeadKeyword(SqlToken token)
        {
            return token.IsKeyword("FROM")
                   || token.IsKeyword("WHERE")
                   || token.IsKeyword("GROUP")
                   || token.IsKeyword("HAVING")
                   || token.IsKeyword("ORDER")
                   || token.IsKeyword("OFFSET")
                   || token.IsKeyword("UNION")
                   || token.IsKeyword("INTERSECT")
                   || token.IsKeyword("EXCEPT")
                   || token.IsKeyword("ON")
                   || token.IsKeyword("JOIN")
                   || token.IsKeyword("INNER")
                   || token.IsKeyword("LEFT")
                   || token.IsKeyword("RIGHT")
                   || token.IsKeyword("FULL")
                   || token.IsKeyword("CROSS")
                   || token.IsKeyword("OUTER")
                   || token.IsKeyword("WHEN")
                   || token.IsKeyword("THEN");
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

        private static int FindTopLevelKeywordIndex(IReadOnlyList<SqlToken> tokens, int startIndex, string keyword)
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
                    return i;
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
                case "INNER":
                case "LEFT":
                case "RIGHT":
                case "FULL":
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
                case "TOP":
                case "DISTINCT":
                case "PERCENT":
                case "HAVING":
                case "AND":
                case "OR":
                case "NOT":
                case "LIKE":
                case "NULL":
                case "CASE":
                case "OVER":
                case "PARTITION":
                case "ASC":
                case "DESC":
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

