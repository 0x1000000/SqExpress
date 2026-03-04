using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

                var sqlText = NormalizeKeywords(statement.Trim());
                if (string.IsNullOrWhiteSpace(sqlText))
                {
                    sqlText = expr!.ToSql(TSqlExporter.Default);
                }

                formatted.Add(sqlText);
            }

            return string.Join(";" + Environment.NewLine, formatted);
        }

        private static string NormalizeKeywords(string statement)
        {
            var sql = statement;
            sql = Regex.Replace(sql, @"\s+", " ");
            sql = Regex.Replace(sql, @"\bselect\b", "SELECT", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bfrom\b", "FROM", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bwhere\b", "WHERE", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\border\s+by\b", "ORDER BY", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bas\b", "AS", RegexOptions.IgnoreCase);

            sql = Regex.Replace(sql, @"\s+FROM\s+", Environment.NewLine + "FROM ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\s+WHERE\s+", Environment.NewLine + "WHERE ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\s+ORDER BY\s+", Environment.NewLine + "ORDER BY ", RegexOptions.IgnoreCase);

            return sql.Trim();
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
