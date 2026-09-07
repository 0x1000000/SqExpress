using System;
using System.Collections.Generic;
using System.Text;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlFormatter : ISqExpressSqlFormatter
    {
        private static readonly TSqlExporter FormattedExporter = new TSqlExporter(
            SqlBuilderOptions.Default.WithFormatting(SqlFormattingProfile.Spacious));

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
                if (!SqTSqlParser.TryParse(statement.Text, out var expr, out var error))
                {
                    throw new SqExpressSqlTranspilerException($"Could not parse SQL:{Environment.NewLine}{error}");
                }

                var formattedStatement = BindParametersForRendering(expr!).ToSql(FormattedExporter);
                if (formattedStatement.EndsWith(";", StringComparison.Ordinal))
                {
                    formattedStatement = formattedStatement.Substring(0, formattedStatement.Length - 1);
                }

                formatted.Add(formattedStatement);
            }

            var result = string.Join(";" + Environment.NewLine, formatted);
            return statements[statements.Count - 1].IsTerminated ? result + ";" : result;
        }

        private static IExpr BindParametersForRendering(IExpr expr)
            => expr.SyntaxTree().Modify<ExprParameter>(parameter =>
                ReferenceEquals(parameter.ReplacedValue, null)
                    ? new ExprParameter(SqQueryBuilder.Literal(0), parameter.TagName)
                    : parameter) as IExpr ?? expr;

        private static List<SqlStatementText> SplitStatements(string sql)
        {
            var result = new List<SqlStatementText>();
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
                    current.Append(ch);
                    AddStatement(result, current, true);
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            AddStatement(result, current, false);
            return result;
        }

        private static void AddStatement(List<SqlStatementText> list, StringBuilder sb, bool isTerminated)
        {
            var value = sb.ToString().Trim();
            if (value.Length > 0)
            {
                list.Add(new SqlStatementText(value, isTerminated));
            }
        }

        private readonly struct SqlStatementText
        {
            public SqlStatementText(string text, bool isTerminated)
            {
                this.Text = text;
                this.IsTerminated = isTerminated;
            }

            public string Text { get; }

            public bool IsTerminated { get; }
        }
    }
}
