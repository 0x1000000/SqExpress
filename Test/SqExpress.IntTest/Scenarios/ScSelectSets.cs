using System;
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
            var tUser = TableList.User();
            var tCompany = TableList.Company();

            var unionResult = await SelectTop(2, (tUser.FirstName + "-" + tUser.LastName).As("Name")).From(tUser)
                .Union(SelectTop(2, tCompany.CompanyName.As("Name")).From(tCompany))
                .QueryList(context.Database, r => r.GetString("Name"));

            Console.WriteLine("Union");
            foreach (var name in unionResult)
            {
                Console.WriteLine(name);
            }

            var exceptSet = Values(unionResult
                    .Where((i, index) => index % 2 == 0)
                    .Select(i => new ExprValue[] {Literal(i)})
                    .ToList())
                .As("EX", "Name");

            var unionExceptResult = await SelectTop(2, (tUser.FirstName + "-" + tUser.LastName).As("Name")).From(tUser)
                .Union(SelectTop(2, tCompany.CompanyName.As("Name")).From(tCompany))
                .Except(Select(exceptSet.Alias.AllColumns()).From(exceptSet))
                .QueryList(context.Database, r => r.GetString("Name"));

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