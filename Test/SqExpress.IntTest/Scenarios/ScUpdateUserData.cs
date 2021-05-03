using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;

namespace SqExpress.IntTest.Scenarios
{
    public class ScUpdateUserData : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tUser = AllTables.GetItUser();

            var users = await SqQueryBuilder.Select(UserName.GetColumns(tUser))
                .From(tUser)
                .OrderBy(tUser.FirstName)
                .OffsetFetch(0, 10)
                .QueryList(context.Database, r => UserName.Read(r, tUser));

            var modifiedUsers = users.Select(u => u.WithFirstName(u.FirstName + "Modified")).ToList();

            await SqQueryBuilder.UpdateData(tUser, modifiedUsers)
                .MapDataKeys(UserName.GetUpdateKeyMapping)
                .MapData(UserName.GetUpdateMapping)
                .AlsoSet(s => s.Set(s.Target.Version, s.Target.Version + 1))
                .Exec(context.Database);

            var usersAfterMod = await SqQueryBuilder.Select(UserName.GetColumns(tUser))
                .From(tUser)
                .OrderBy(tUser.FirstName)
                .OffsetFetch(0, 10)
                .QueryList(context.Database, r => UserName.Read(r, tUser));

            for (var index = 0; index < usersAfterMod.Count; index++)
            {
                if (usersAfterMod[index].FirstName != modifiedUsers[index].FirstName)
                {
                    throw new Exception("Name was not updated");
                }
                if (usersAfterMod[index].LastName != modifiedUsers[index].LastName)
                {
                    throw new Exception("Name was not updated");
                }
            }

            await SqQueryBuilder.UpdateData(tUser, users)
                .MapDataKeys(UserName.GetUpdateKeyMapping)
                .MapData(UserName.GetUpdateMapping)
                .AlsoSet(s => s.Set(s.Target.Version, s.Target.Version + 1))
                .Done()
                .Exec(context.Database);

            var usersAfterMod2 = await SqQueryBuilder.Select(UserName.GetColumns(tUser))
                .From(tUser)
                .OrderBy(tUser.FirstName)
                .OffsetFetch(0, 10)
                .QueryList(context.Database, r => UserName.Read(r, tUser));

            for (var index = 0; index < usersAfterMod2.Count; index++)
            {
                if (usersAfterMod2[index].FirstName != users[index].FirstName)
                {
                    throw new Exception("Name was not updated");
                }
                if (usersAfterMod2[index].LastName != users[index].LastName)
                {
                    throw new Exception("Name was not updated");
                }
            }
        }
    }
}