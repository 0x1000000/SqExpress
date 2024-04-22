using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScSelectTop : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tUser = AllTables.GetItUser(context.Dialect);

            var top2Users = await SelectTop(2, UserEmail.GetColumns(tUser))
                .From(tUser)
                .OrderBy(tUser.FirstName)
                .QueryList(context.Database, r => UserEmail.Read(r, tUser));

            Console.WriteLine(top2Users[0]);
            Console.WriteLine(top2Users[1]);

            if (context.Dialect != SqlDialect.TSql)
            {

                top2Users = await SelectTop(2, UserEmail.GetColumns(tUser))
                    .From(tUser)
                    .OrderBy(tUser.FirstName)
                    .Offset(5)
                    .QueryList(context.Database, r => UserEmail.Read(r, tUser));

                Console.WriteLine(top2Users[0].Email);
                Console.WriteLine(top2Users[1].Email);
            }
        }
    }
}