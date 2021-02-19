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
            string[] ids =
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
                "\"\\\"'\'\\'*`\\`;"
            };

            var expr = Select(ids.Select(id => Literal(id).As(id)).ToList()).Done();
            var res = await expr.Query(context.Database, new List<string>(),
                (acc,r) =>
                {
                    foreach (var id in ids)
                    {
                        acc.Add(r.GetString(id));
                    }

                    return acc;
                });

            for (var index = 0; index < ids.Length; index++)
            {
                if (res[index] != ids[index])
                {
                    context.WriteLine(context.Dialect.GetExporter().ToSql(expr));
                    throw new Exception("Sql Injection!");
                }
            }
        }
    }
}