using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScUpdateUsers : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tUser = Tables.TableList.User();
            var tCustomer = Tables.TableList.Customer();

            var maxVersion = (int) await Select(Max(tUser.Version))
                .From(tUser)
                .QueryScalar(context.Database);

            var countBefore = (long)await Select(Cast(CountOne(), SqlType.Int64))
                .From(tUser)
                .Where(tUser.Version == maxVersion & Exists(SelectOne().From(tCustomer).Where(tCustomer.UserId == tUser.UserId)))
                .QueryScalar(context.Database);

            await Update(tUser)
                .Set(tUser.Version, tUser.Version + 1)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .All()
                .Exec(context.Database);

            var countAfter = (long)await Select(Cast(CountOne(), SqlType.Int64))
                .From(tUser)
                .Where(tUser.Version == maxVersion + 1)
                .QueryScalar(context.Database);

            if (countBefore != countAfter)
            {
                throw new Exception($"Something went wrong: count before {countBefore}, count after {countAfter}");
            }

            Console.WriteLine();
            Console.WriteLine($"{countAfter} items were updated.");
            Console.WriteLine();
        }
    }
}