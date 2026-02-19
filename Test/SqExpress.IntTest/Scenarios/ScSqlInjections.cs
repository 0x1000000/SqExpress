using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScSqlInjections : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            string[] literalPayloads =
            {
                "admin'--",
                "10; DROP TABLE members /*",
                "Line1\r\n\tLine2\\%%\b",
                "--",
                "\'",
                "\\",
                "\\'",
                "\\\\",
                "`\"'",
                "``\\`\\\\`\"\\\"\\\\\"'\\'\\\\'",
                "\"\\\"'\'\\'*`\\`;",
                "'; EXEC xp_cmdshell('whoami');--",
                "1); WAITFOR DELAY '00:00:05'--",
                "');SELECT * FROM users WHERE '1'='1",
                "/*comment*/' OR 'x'='x",
                "UNION SELECT username,password FROM members--",
                "abc]];DROP TABLE [T];--",
                "abc``;DROP TABLE `T`;--",
                "abc\"\";DROP TABLE \"T\";--",
                "json:{\"a\":\"' OR 1=1 --\"}",
                "line1\nline2\n--",
                "'';BEGIN TRAN;ROLLBACK;--",
                "%_[]^"
            };

            string[] identifierPayloads =
            {
                "alias'--",
                "alias; DROP TABLE members /*",
                "x y",
                "x.y",
                "x/y",
                "x\\y",
                "x--comment",
                "x/*comment*/",
                "from",
                "select",
                "@@version",
                "[x]",
                "`x`",
                "\"x\"",
                "0leading",
                "a]b",
                "a`b",
                "a\"b",
                "a,b",
                "a:b"
            };

            await AssertLiteralRoundTrip(context, literalPayloads);
            await AssertIdentifierRoundTrip(context, identifierPayloads);
        }

        private static Task AssertLiteralRoundTrip(IScenarioContext context, IReadOnlyList<string> payloads)
            => AssertRoundTrip(
                context,
                payloads.Select((payload, i) => (Alias: $"v{i}", Value: payload)).ToArray(),
                "literal");

        private static Task AssertIdentifierRoundTrip(IScenarioContext context, IReadOnlyList<string> payloads)
            => AssertRoundTrip(
                context,
                payloads.Select(payload => (Alias: payload, Value: payload)).ToArray(),
                "identifier");

        private static async Task AssertRoundTrip(
            IScenarioContext context,
            IReadOnlyList<(string Alias, string Value)> payloads,
            string payloadType)
        {
            var expr = Select(payloads.Select(s => Literal(s.Value).As(s.Alias)).ToList()).Done();

            var res = await expr.Query(
                context.Database,
                new List<string>(),
                (acc, r) =>
                {
                    foreach (var payload in payloads)
                    {
                        acc.Add(r.GetString(payload.Alias));
                    }

                    return acc;
                });

            for (var index = 0; index < payloads.Count; index++)
            {
                if (res[index] != payloads[index].Value)
                {
                    context.WriteLine(context.Dialect.GetExporter().ToSql(expr));
                    throw new Exception(
                        $"Sql injection test failed ({payloadType}) at index {index}. " +
                        $"Alias:'{payloads[index].Alias}', Value:'{payloads[index].Value}'.");
                }
            }
        }
    }
}
