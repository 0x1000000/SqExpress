using System;
using System.Data;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScTransactions : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var guid = Guid.Parse("58AD8253-4F8F-4C84-B930-4F58A8F25912");
            var tCompany = AllTables.GetItCompany();
            var data = new[] { (Name: "TestCompany", ExtId: guid) };
            var exprInsert = InsertDataInto(tCompany, data)
                .MapData(m => m.Set(m.Target.CompanyName, m.Source.Name).Set(m.Target.ExternalId, m.Source.ExtId))
                .AlsoInsert(m => m
                    .Set(m.Target.Modified, GetUtcDate())
                    .Set(m.Target.Created, GetUtcDate())
                    .Set(m.Target.Version, 1)
                ).Done();

            using (context.Database.BeginTransaction())
            {
                await exprInsert.Exec(context.Database);

                if (!await CheckExistence())
                {
                    throw new Exception("Inside transaction the data should be visible");
                }
            }

            if (await CheckExistence())
            {
                throw new Exception("Transaction was not committed, so it should return nothing");
            }

            using (var t = context.Database.BeginTransaction())
            {
                await exprInsert.Exec(context.Database);
                t.Commit();
            }

            if (!await CheckExistence())
            {
                throw new Exception("Transaction was committed, so it should return something");
            }

            using (var t = context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                await Delete(tCompany).Where(tCompany.ExternalId == guid).Exec(context.Database);
                t.Commit();
            }

            if (await CheckExistence())
            {
                throw new Exception("The row is suppose to be deleted");
            }

            async Task<bool> CheckExistence()
            {
                var col = await Select(Literal(1).As("Col"))
                    .From(tCompany)
                    .Where(tCompany.ExternalId == guid)
                    .QueryList(context.Database, r => r.GetInt32("Col"));

                return col.Count > 0;
            }
        }
    }
}