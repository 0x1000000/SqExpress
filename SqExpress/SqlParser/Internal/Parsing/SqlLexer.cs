using System;
using System.Collections.Generic;

namespace SqExpress.SqlParser.Internal.Parsing
{
    internal static class SqlLexer
    {
        public static List<SqlToken> Tokenize(string sql)
        {
            if (!TryTokenize(sql, out var result, out var error))
            {
                throw new InvalidOperationException(error ?? "SQL tokenization failed.");
            }

            return result!;
        }

        public static bool TryTokenize(string sql, out List<SqlToken>? result, out string? error)
        {
            result = new List<SqlToken>();
            var index = 0;

            while (index < sql.Length)
            {
                var ch = sql[index];
                if (char.IsWhiteSpace(ch))
                {
                    index++;
                    continue;
                }

                if (ch == '-' && (index + 1) < sql.Length && sql[index + 1] == '-')
                {
                    index += 2;
                    while (index < sql.Length && sql[index] != '\r' && sql[index] != '\n')
                    {
                        index++;
                    }

                    continue;
                }

                if (ch == '/' && (index + 1) < sql.Length && sql[index + 1] == '*')
                {
                    index += 2;
                    var closed = false;
                    while ((index + 1) < sql.Length)
                    {
                        if (sql[index] == '*' && sql[index + 1] == '/')
                        {
                            index += 2;
                            closed = true;
                            break;
                        }

                        index++;
                    }

                    if (!closed)
                    {
                        result = null;
                        error = "Syntax error: unterminated block comment.";
                        return false;
                    }

                    continue;
                }

                if (ch == '[')
                {
                    var start = index;
                    index++;
                    while (index < sql.Length)
                    {
                        if (sql[index] == ']')
                        {
                            if ((index + 1) < sql.Length && sql[index + 1] == ']')
                            {
                                index += 2;
                                continue;
                            }

                            index++;
                            break;
                        }

                        index++;
                    }

                    result.Add(new SqlToken(SqlTokenType.BracketIdentifier, sql.Substring(start, index - start), start, index - start));
                    continue;
                }

                if ((ch == 'N' || ch == 'n') && (index + 1) < sql.Length && sql[index + 1] == '\'')
                {
                    if (!TryReadStringLiteral(sql, ref index, true, out var token, out error))
                    {
                        result = null;
                        return false;
                    }

                    result.Add(token);
                    continue;
                }

                if (ch == '\'')
                {
                    if (!TryReadStringLiteral(sql, ref index, false, out var token, out error))
                    {
                        result = null;
                        return false;
                    }

                    result.Add(token);
                    continue;
                }

                if (char.IsDigit(ch))
                {
                    var start = index;
                    index++;
                    while (index < sql.Length && char.IsDigit(sql[index]))
                    {
                        index++;
                    }

                    if (index < sql.Length && sql[index] == '.')
                    {
                        index++;
                        while (index < sql.Length && char.IsDigit(sql[index]))
                        {
                            index++;
                        }
                    }

                    result.Add(new SqlToken(SqlTokenType.NumberLiteral, sql.Substring(start, index - start), start, index - start));
                    continue;
                }

                if (IsIdentifierStart(ch))
                {
                    var start = index;
                    index++;
                    while (index < sql.Length && IsIdentifierPart(sql[index]))
                    {
                        index++;
                    }

                    result.Add(new SqlToken(SqlTokenType.Identifier, sql.Substring(start, index - start), start, index - start));
                    continue;
                }

                switch (ch)
                {
                    case ',':
                        result.Add(new SqlToken(SqlTokenType.Comma, ",", index, 1));
                        index++;
                        break;
                    case '.':
                        result.Add(new SqlToken(SqlTokenType.Dot, ".", index, 1));
                        index++;
                        break;
                    case '(':
                        result.Add(new SqlToken(SqlTokenType.OpenParen, "(", index, 1));
                        index++;
                        break;
                    case ')':
                        result.Add(new SqlToken(SqlTokenType.CloseParen, ")", index, 1));
                        index++;
                        break;
                    case ';':
                        result.Add(new SqlToken(SqlTokenType.Semicolon, ";", index, 1));
                        index++;
                        break;
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '%':
                    case '=':
                    case '<':
                    case '>':
                    case '!':
                    case '|':
                    case '&':
                    case '^':
                        result.Add(new SqlToken(SqlTokenType.Operator, ch.ToString(), index, 1));
                        index++;
                        break;
                    default:
                        result.Add(new SqlToken(SqlTokenType.Symbol, ch.ToString(), index, 1));
                        index++;
                        break;
                }
            }

            result.Add(new SqlToken(SqlTokenType.EndOfFile, string.Empty, sql.Length, 0));
            error = null;
            return true;
        }

        private static bool IsIdentifierStart(char ch)
            => char.IsLetter(ch) || ch == '_' || ch == '#' || ch == '@' || ch == '$';

        private static bool IsIdentifierPart(char ch)
            => char.IsLetterOrDigit(ch) || ch == '_' || ch == '#' || ch == '@' || ch == '$';

        private static bool TryReadStringLiteral(string sql, ref int index, bool hasUnicodePrefix, out SqlToken token, out string? error)
        {
            var start = index;
            index += hasUnicodePrefix ? 2 : 1;
            while (index < sql.Length)
            {
                if (sql[index] == '\'')
                {
                    if ((index + 1) < sql.Length && sql[index + 1] == '\'')
                    {
                        index += 2;
                        continue;
                    }

                    index++;
                    token = new SqlToken(SqlTokenType.StringLiteral, sql.Substring(start, index - start), start, index - start);
                    error = null;
                    return true;
                }

                index++;
            }

            token = default;
            error = "Syntax error: unterminated string literal.";
            return false;
        }
    }
}
