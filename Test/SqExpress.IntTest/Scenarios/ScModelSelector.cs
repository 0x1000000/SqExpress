using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;
using SqExpress.ModelSelect;

namespace SqExpress.IntTest.Scenarios
{
    public class ScModelSelector : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var data = await SqModelSelectBuilder
                .Select(Customer.GetReader())
                .InnerJoin(
                    OrderDateCreated.GetReader(),
                    on: t => t.Table.CustomerId == t.JoinedTable1.CustomerId)
                .LeftJoin(
                    UserEmail.GetReader(),
                    on: t => t.Table.UserId == t.JoinedTable2.UserId)
                .LeftJoin(
                    CompanyName.GetReader(),
                    on: t => t.Table.CompanyId == t.JoinedTable3.CompanyId)
                .Find(0,
                    10,
                    null,
                    t => SqQueryBuilder.Desc(SqQueryBuilder.IsNull(t.JoinedTable2.FirstName, t.JoinedTable3.CompanyName)).ThenBy(t.JoinedTable1.DateCreated),
                    d =>
                        new
                        {
                            Client = d.JoinedModel2?.Email ?? d.JoinedModel3?.Name ?? "Unknown",
                            Date = d.JoinedModel1.DateCreated.ToString("s")
                        })
                .QueryPage(context.Database);

            if (data.Total != 8397 && data.Total != 8438/*MySQL*/)
            {
                throw new Exception($"8397 is expected but was {data.Total}");
            }
        }
    }
}