using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Derived;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScCreateOrders : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tOrder = TableList.Order();
            var tOrderSub2 = TableList.Order();
            var vwCustomer = new CustomerName();

            var numbers = Values(Enumerable.Range(1, 10).Select(Literal).ToList()).AsColumns("Num");

            var getNum = numbers.Column(context.Dialect == SqlDialect.MySql ? "1" : "Num");
            await InsertInto(tOrder, tOrder.CustomerId, tOrder.Notes)
                .From(
                    Select(vwCustomer.CustomerId, ("Notes for " + vwCustomer.Name + " No:" + Cast(getNum, SqlType.String(5))).As("Notes"))
                        .From(vwCustomer)
                        .CrossJoin(numbers)
                        .OrderBy(vwCustomer.CustomerId, getNum)
                )
                .Exec(context.Database);

            var count = await Select(Count(1)).From(tOrder).QueryScalar(context.Database);

            context.WriteLine("Orders are inserted: " + count);

            //Delete % 7
            await Delete(tOrder)
                .Where(tOrder.OrderId.In(Select(tOrderSub2.OrderId)
                    .From(tOrderSub2)
                    .Where(tOrderSub2.OrderId % 7 == 0)))
                .Exec(context.Database);

            count = await Select(Count(1)).From(tOrder).QueryScalar(context.Database);
            context.WriteLine("Some Orders are deleted. Current count: " + count);

            //Delete JOIN
            await Delete(tOrder)
                .From(tOrder)
                .InnerJoin(vwCustomer, on: vwCustomer.CustomerId == tOrder.CustomerId)
                .Where(tOrder.CustomerId % 7 + 1 == 1)
                .Exec(context.Database);

            //For my SQL number is different since Auto Increment is not reset on delete (ItCustomer)

            count = await Select(Count(1)).From(tOrder).QueryScalar(context.Database);
            context.WriteLine("Some Orders are deleted. Current count: " + count);

            await Update(tOrder)
                .Set(tOrder.Notes, tOrder.Notes + " (Updated 17)")
                .Where(tOrder.OrderId % 17 == 0)
                .Exec(context.Database);

            await Update(tOrder)
                .Set(tOrder.Notes, tOrder.Notes + " (Updated 19)")
                .From(tOrder)
                .Where(tOrder.OrderId % 19 == 0)
                .Exec(context.Database);

            await Update(tOrder)
                .Set(tOrder.Notes, tOrder.Notes + " (Updated 23)")
                .From(tOrder)
                .InnerJoin(vwCustomer, on: vwCustomer.CustomerId == tOrder.CustomerId)
                .Where(tOrder.OrderId % 23 == 0)
                .Exec(context.Database);
        }
    }
}