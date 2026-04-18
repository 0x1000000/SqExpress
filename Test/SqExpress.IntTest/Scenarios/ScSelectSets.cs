using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Value;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScSelectSets : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tUser = AllTables.GetItUser(context.Dialect);
            var tCompany = AllTables.GetItCompany(context.Dialect);

            IReadOnlyList<string> unionResult;
            //SQLite does not accept the same TOP-inside-set-operation shape used by the other dialects,
            //so this scenario materializes each side first and unions the value sets afterward.
            if (context.Dialect == SqlDialect.Sqlite)
            {
                var topUsers = await SelectTop(2, (tUser.FirstName + "-" + tUser.LastName).As("Name")).From(tUser)
                    .QueryList(context.Database, r => r.GetString("Name"));

                var topCompanies = await SelectTop(2, tCompany.CompanyName.As("Name")).From(tCompany)
                    .QueryList(context.Database, r => r.GetString("Name"));

                var leftSet = Values(topUsers.Select(i => new ExprValue[] { Literal(i) }).ToList())
                    .As("US", "Name");

                var rightSet = Values(topCompanies.Select(i => new ExprValue[] { Literal(i) }).ToList())
                    .As("CP", "Name");

                unionResult = await Select(leftSet.Alias.AllColumns()).From(leftSet)
                    .Union(Select(rightSet.Alias.AllColumns()).From(rightSet))
                    .QueryList(context.Database, r => r.GetString("Name"));
            }
            else
            {
                unionResult = await SelectTop(2, (tUser.FirstName + "-" + tUser.LastName).As("Name")).From(tUser)
                    .Union(SelectTop(2, tCompany.CompanyName.As("Name")).From(tCompany))
                    .QueryList(context.Database, r => r.GetString("Name"));
            }

            Console.WriteLine("Union");
            foreach (var name in unionResult)
            {
                Console.WriteLine(name);
            }

            var exceptSet = Values(unionResult
                    .Where((i, index) => index % 2 == 0)
                    .Select(i => new ExprValue[] { Literal(i) })
                    .ToList())
                .As("EX", "Name");

            IReadOnlyList<string> unionExceptResult;
            //For SQLite, run EXCEPT over the already materialized UNION set to avoid the unsupported
            //TOP-with-set-operation composition used in the generic branch.
            if (context.Dialect == SqlDialect.Sqlite)
            {
                var unionSet = Values(unionResult.Select(i => new ExprValue[] { Literal(i) }).ToList())
                    .As("UN", "Name");

                unionExceptResult = await Select(unionSet.Alias.AllColumns()).From(unionSet)
                    .Except(Select(exceptSet.Alias.AllColumns()).From(exceptSet))
                    .QueryList(context.Database, r => r.GetString("Name"));
            }
            else
            {
                unionExceptResult = await SelectTop(2, (tUser.FirstName + "-" + tUser.LastName).As("Name")).From(tUser)
                    .Union(SelectTop(2, tCompany.CompanyName.As("Name")).From(tCompany))
                    .Except(Select(exceptSet.Alias.AllColumns()).From(exceptSet))
                    .QueryList(context.Database, r => r.GetString("Name"));
            }

            Console.WriteLine();
            Console.WriteLine("Union Except");
            foreach (var name in unionExceptResult)
            {
                Console.WriteLine(name);
            }

            for (int i = 0; i < unionResult.Count; i++)
            {
                if (i % 2 != 0)
                {
                    if (unionResult[i] != unionExceptResult[i / 2])
                    {
                        throw new Exception(unionResult[i] + " != " + unionExceptResult[i / 2]);
                    }
                }
            }
        }
    }
}
