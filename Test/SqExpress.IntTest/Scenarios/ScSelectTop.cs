using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScSelectTop : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tUser = TableList.User();

            var top2Users = await SelectTop(2, tUser.FirstName)
                .From(tUser)
                .OrderBy(tUser.FirstName)
                .QueryList(context.Database, r => tUser.FirstName.Read(r));

            Console.WriteLine(top2Users[0]);
            Console.WriteLine(top2Users[1]);

            if (context.Dialect != SqlDialect.TSql)
            {

                top2Users = await SelectTop(2, tUser.FirstName)
                    .From(tUser)
                    .OrderBy(tUser.FirstName)
                    .Offset(5)
                    .QueryList(context.Database, r => tUser.FirstName.Read(r));

                Console.WriteLine(top2Users[0]);
                Console.WriteLine(top2Users[1]);
            }
        }
    }
}