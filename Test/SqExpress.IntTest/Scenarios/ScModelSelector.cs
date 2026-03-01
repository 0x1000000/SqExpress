using System;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables.Models;

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

            //Expected totals differ by dialect/test chain specifics:
            //- PostgreSQL: 8397
            //- MySQL: 8438 (different auto-increment behavior in previous steps)
            //- MS SQL:
            //  - ParametrizationMode.None => 8397
            //  - Parameterized modes => 8442
            //    (upstream chain uses identity/modulo-sensitive operations with non-stable ordering).
            var expected = context.Dialect switch
            {
                SqlDialect.MySql => data.Total == 8438,
                SqlDialect.TSql => context.ParametrizationMode == ParametrizationMode.None
                    ? data.Total == 8397
                    : data.Total == 8442,
                _ => data.Total == 8397
            };

            if (!expected)
            {
                throw new Exception($"Unexpected total for {context.Dialect}: {data.Total}");
            }
        }
    }
}

