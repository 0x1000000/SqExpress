using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;

namespace SqExpress.IntTest.Scenarios;

public class ScMergeExpr : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var tUser = AllTables.GetItUser();

        var userNames = await SqModelSelectBuilder
            .Select(UserName.GetReader())
            .Get(null, null, i => i)
            .QueryList(context.Database);


        var valTable = SqQueryBuilder.ValueTable(userNames.Take(10).Concat(new []{new UserName((EntUser)(-1777), "New User", "Last Name")}), s => s.Set(tUser.UserId, (int)s.Item.Id).Set(tUser.FirstName, "Mode " + s.Item.FirstName));

        await SqQueryBuilder.MergeInto(tUser, valTable)
            .On(tUser.UserId == valTable.Column(tUser.UserId))
            .WhenMatched()
                .ThenUpdate()
                .Set(tUser.FirstName, valTable.Column(tUser.FirstName))
            .WhenNotMatchedByTarget()
                .ThenInsert()
                .Set(tUser.FirstName, valTable.Column(tUser.FirstName))
                .Set(tUser.LastName, "Last Name")
                .Set(tUser.ExternalId, Guid.NewGuid())
                .Set(tUser.Email, "LastName@email.com")
                .Set(tUser.RegDate, DateTime.Now)
            .WhenNotMatchedBySource()
                .ThenUpdate()
                .Set(tUser.Version, tUser.Version + 1)
            .Exec(context.Database);


        var nuCount = (await SqQueryBuilder.Select(SqQueryBuilder.Count(tUser.UserId)).From(tUser).Where(tUser.FirstName.Like("Mode %")).QueryScalar(context.Database))!.ToString();
        if (nuCount != "11")
        {
            throw new Exception($"Incorrect num ({nuCount})");
        }

    }

    private static string RowDataToString(IReadOnlyList<TestMergeDataRow> data)
        => string.Join(';', data.Select(d=>$"{d.Id},{d.Value},{d.Version}"));
}