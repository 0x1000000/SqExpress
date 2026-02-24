using System;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

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

            var parser = new TSql160Parser(initialQuotedIdentifiers: true);
            using var reader = new StringReader(sql);
            var fragment = parser.Parse(reader, out var errors);
            if (errors.Count > 0)
            {
                var details = string.Join(
                    Environment.NewLine,
                    errors.Select(e => $"({e.Line},{e.Column}) {e.Message}"));
                throw new SqExpressSqlTranspilerException($"Could not parse SQL:{Environment.NewLine}{details}");
            }

            var generator = new Sql160ScriptGenerator(new SqlScriptGeneratorOptions
            {
                KeywordCasing = KeywordCasing.Uppercase
            });

            generator.GenerateScript(fragment, out var formattedSql);
            return formattedSql.Trim();
        }
    }
}
