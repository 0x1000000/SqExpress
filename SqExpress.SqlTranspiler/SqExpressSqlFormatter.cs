using System;
using System.Collections.Generic;
using System.Text;
using SqExpress.SqlExport;
using SqExpress.SqlParser;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlFormatter : ISqExpressSqlFormatter
    {
        public string Format(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new SqExpressSqlTranspilerException("SQL text cannot be empty.");
            }

            var statements = SplitStatements(sql);
            if (statements.Count < 1)
            {
                throw new SqExpressSqlTranspilerException("SQL text cannot be empty.");
            }

            var formatted = new List<string>(statements.Count);
            foreach (var statement in statements)
            {
                if (!SqTSqlParser.TryParse(statement, out var expr, out var error))
                {
                    throw new SqExpressSqlTranspilerException($"Could not parse SQL:{Environment.NewLine}{error}");
                }

                var sqlText = expr!.ToSql(TSqlExporter.Default);
                sqlText = PrettyFormat(sqlText);
                formatted.Add(sqlText);
            }

            return string.Join(";" + Environment.NewLine, formatted);
        }

        private static string PrettyFormat(string sql)
        {
            var tokens = Tokenize(sql);
            if (tokens.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(sql.Length + 32);
            string? previousToken = null;
            var depth = 0;
            var i = 0;
            var lineStart = true;
            while (i < tokens.Count)
            {
                var breakLen = depth == 0 ? ClauseBreakLength(tokens, i) : 0;
                if (breakLen > 0)
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
                    {
                        sb.AppendLine();
                        lineStart = true;
                    }

                    for (var j = 0; j < breakLen; j++)
                    {
                        var current = tokens[i + j];
                        if (!lineStart && NeedSpace(previousToken, current))
                        {
                            sb.Append(' ');
                        }

                        sb.Append(current);
                        previousToken = current;
                        lineStart = false;
                    }

                    i += breakLen;
                    continue;
                }

                var token = tokens[i];
                if (!lineStart && NeedSpace(previousToken, token))
                {
                    sb.Append(' ');
                }

                sb.Append(token);
                if (token == "(")
                {
                    depth++;
                }
                else if (token == ")")
                {
                    depth = Math.Max(0, depth - 1);
                }

                previousToken = token;
                lineStart = false;
                i++;
            }

            return sb.ToString().Trim();
        }

        private static int ClauseBreakLength(IReadOnlyList<string> tokens, int index)
        {
            if (Match(tokens, index, "SELECT")) return 1;
            if (Match(tokens, index, "FROM")) return 1;
            if (Match(tokens, index, "WHERE")) return 1;
            if (Match(tokens, index, "GROUP", "BY")) return 2;
            if (Match(tokens, index, "HAVING")) return 1;
            if (Match(tokens, index, "ORDER", "BY")) return 2;
            if (Match(tokens, index, "OFFSET")) return 1;
            if (Match(tokens, index, "FETCH")) return 1;
            if (Match(tokens, index, "UNION", "ALL")) return 2;
            if (Match(tokens, index, "UNION")) return 1;
            if (Match(tokens, index, "EXCEPT")) return 1;
            if (Match(tokens, index, "INTERSECT")) return 1;
            if (Match(tokens, index, "INNER", "JOIN")) return 2;
            if (Match(tokens, index, "LEFT", "JOIN")) return 2;
            if (Match(tokens, index, "RIGHT", "JOIN")) return 2;
            if (Match(tokens, index, "FULL", "JOIN")) return 2;
            if (Match(tokens, index, "CROSS", "JOIN")) return 2;
            if (Match(tokens, index, "CROSS", "APPLY")) return 2;
            if (Match(tokens, index, "OUTER", "APPLY")) return 2;
            if (Match(tokens, index, "ON")) return 1;
            if (Match(tokens, index, "WHEN", "MATCHED")) return 2;
            if (Match(tokens, index, "WHEN", "NOT", "MATCHED")) return 3;
            if (Match(tokens, index, "THEN")) return 1;
            if (Match(tokens, index, "VALUES")) return 1;
            if (Match(tokens, index, "SET")) return 1;

            return 0;
        }

        private static bool Match(IReadOnlyList<string> tokens, int index, params string[] words)
        {
            if (index + words.Length > tokens.Count)
            {
                return false;
            }

            for (var i = 0; i < words.Length; i++)
            {
                if (!string.Equals(tokens[index + i], words[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<string> Tokenize(string sql)
        {
            var tokens = new List<string>(sql.Length / 2);
            for (var i = 0; i < sql.Length;)
            {
                var ch = sql[i];
                if (char.IsWhiteSpace(ch))
                {
                    i++;
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

                    tokens.Add(sql.Substring(start, i - start));
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

                    tokens.Add(sql.Substring(start, i - start));
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

                    tokens.Add(sql.Substring(start, i - start));
                    continue;
                }

                if (IsWordStart(ch))
                {
                    var start = i++;
                    while (i < sql.Length && IsWordPart(sql[i]))
                    {
                        i++;
                    }

                    tokens.Add(sql.Substring(start, i - start));
                    continue;
                }

                if ((ch == '<' || ch == '>' || ch == '!') && i + 1 < sql.Length && sql[i + 1] == '=')
                {
                    tokens.Add(sql.Substring(i, 2));
                    i += 2;
                    continue;
                }

                if (ch == '<' && i + 1 < sql.Length && sql[i + 1] == '>')
                {
                    tokens.Add("<>");
                    i += 2;
                    continue;
                }

                tokens.Add(ch.ToString());
                i++;
            }

            return tokens;
        }

        private static bool NeedSpace(string? previousToken, string currentToken)
        {
            if (string.IsNullOrEmpty(previousToken))
            {
                return false;
            }

            if (currentToken == "," || currentToken == ")" || currentToken == "." || currentToken == ";")
            {
                return false;
            }

            if (previousToken == "(" || previousToken == "." || previousToken == ",")
            {
                return false;
            }

            return true;
        }

        private static bool IsWordStart(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || ch == '@' || ch == '#';
        }

        private static bool IsWordPart(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || ch == '@' || ch == '#';
        }

        private static List<string> SplitStatements(string sql)
        {
            var result = new List<string>();
            var current = new StringBuilder(sql.Length);
            bool inString = false;
            bool inBracket = false;
            bool inLineComment = false;
            bool inBlockComment = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char ch = sql[i];
                char next = i + 1 < sql.Length ? sql[i + 1] : '\0';

                if (inLineComment)
                {
                    current.Append(ch);
                    if (ch == '\n')
                    {
                        inLineComment = false;
                    }

                    continue;
                }

                if (inBlockComment)
                {
                    current.Append(ch);
                    if (ch == '*' && next == '/')
                    {
                        current.Append('/');
                        i++;
                        inBlockComment = false;
                    }

                    continue;
                }

                if (!inString && !inBracket && ch == '-' && next == '-')
                {
                    current.Append(ch);
                    current.Append(next);
                    i++;
                    inLineComment = true;
                    continue;
                }

                if (!inString && !inBracket && ch == '/' && next == '*')
                {
                    current.Append(ch);
                    current.Append(next);
                    i++;
                    inBlockComment = true;
                    continue;
                }

                if (ch == '\'' && !inBracket)
                {
                    current.Append(ch);
                    if (inString && next == '\'')
                    {
                        current.Append(next);
                        i++;
                    }
                    else
                    {
                        inString = !inString;
                    }

                    continue;
                }

                if (!inString)
                {
                    if (ch == '[')
                    {
                        inBracket = true;
                    }
                    else if (ch == ']')
                    {
                        inBracket = false;
                    }
                }

                if (!inString && !inBracket && ch == ';')
                {
                    AddStatement(result, current);
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            AddStatement(result, current);
            return result;
        }

        private static void AddStatement(List<string> list, StringBuilder sb)
        {
            var value = sb.ToString().Trim();
            if (value.Length > 0)
            {
                list.Add(value);
            }
        }
    }
}
