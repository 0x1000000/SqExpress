using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScSelectValue : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tUser = AllTables.GetItUser();
            var tUserSub = AllTables.GetItUser();

            var userId = (int)await SelectTop(1, tUser.UserId)
                .From(tUser)
                .QueryScalar(context.Database);

            var email = await Select(UserEmail.GetColumns(tUser))
                .From(tUser)
                .Where(tUser.UserId ==
                       ValueQuery(Select(tUserSub.UserId).From(tUserSub).Where(tUserSub.UserId == userId)))
                .QueryList(context.Database, r=> UserEmail.Read(r, tUser));

            if ((int)email[0].Id != userId)
            {
                throw new Exception("Incorrect value");
            }

            var res = await Select(
                    ValueQuery(Select(tUserSub.UserId)
                            .From(tUserSub)
                            .Where(tUserSub.UserId == userId))
                        .As("v"),
                    tUser.UserId)
                .From(tUser)
                .Where(tUser.UserId == userId)
                .QueryList(context.Database, r => (V: r.GetInt32("v"), UserId: tUser.UserId.Read(r)));

            if (res[0].V != res[0].UserId)
            {
                throw new Exception("Incorrect value");
            }
        }
    }
}