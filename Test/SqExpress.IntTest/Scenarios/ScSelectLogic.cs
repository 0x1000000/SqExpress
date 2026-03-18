using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScSelectLogic : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            await CaseWhenThen(context: context);
            await Arithmetic(context: context);
            await IsNullFunc(context: context);
            await CaseWhenThenIsNull(context: context);
            await CaseWhenThenCoalesce(context: context);
            await GetUtcDateFunc(context: context);
            await SelectAllColumns(context: context);
        }

        private static async Task CaseWhenThen(IScenarioContext context)
        {
            var rawResult = await Select(Cast(Case()
                        .When(Literal(1) + 2 == Literal(3))
                        .Then(17)
                        .Else(2),
                    SqlType.Int32))
                .QueryScalar(context.Database);
            var result = rawResult == null ? (int?)null : Convert.ToInt32(rawResult);
            if (result != 17)
            {
                throw new Exception("Something went wrong");
            }
        }

        private static async Task Arithmetic(IScenarioContext context)
        {
            var rawResult = await Select(Cast((Literal(7) + 3 - Literal(1)) * 2 / 6, SqlType.Double))
                .QueryScalar(context.Database);
            var doubleResult = rawResult == null ? (double?)null : Convert.ToDouble(rawResult);

            if (Math.Abs((doubleResult ?? 0) - 3.0) > 0)
            {
                throw new Exception("Something went wrong");
            }
        }

        private static async Task IsNullFunc(IScenarioContext context)
        {
            var result = (string?)await Select(IsNull(Null, "NotNull"))
                .QueryScalar(context.Database);

            if (result != "NotNull")
            {
                throw new Exception("Something went wrong");
            }
        }

        private static async Task CaseWhenThenIsNull(IScenarioContext context)
        {
            var result = (string?)await Select(Case().When(IsNull(Null)).Then(Literal("Tr") + "ue").Else("False"))
                .QueryScalar(context.Database);

            if (result != "True")
            {
                throw new Exception("Something went wrong");
            }
        }

        private static async Task CaseWhenThenCoalesce(IScenarioContext context)
        {
            var rawResult = await Select(Coalesce(Null, Null, 2.123456m))
                .QueryScalar(context.Database);
            var result = rawResult == null ? (decimal?)null : Convert.ToDecimal(rawResult);

            if (result != 2.123456m)
            {
                throw new Exception("Something went wrong");
            }
        }

        private static async Task GetUtcDateFunc(IScenarioContext context)
        {
            var result = await Select(GetUtcDate())
                .Query(context.Database, (DateTime?)null, (_, next)=> next.GetDateTime(0));
            Console.WriteLine($"Utc now: {result}");
        }

        private static async Task SelectAllColumns(IScenarioContext context)
        {
            void PrintColumns(ISqDataRecordReader r)
            {
                for (int i = 0; i < r.FieldCount; i++)
                {
                    context.Write(r.GetName(i) + ",");
                }

                context.WriteLine(null);
            }

            var tUser = AllTables.GetItUser(context.Dialect);
            var tCustomer = AllTables.GetItCustomer();

            await SelectTop(1, AllColumns()).From(tUser).Query(context.Database, PrintColumns);

            await SelectTop(1, tUser.AllColumns()).From(tUser).Query(context.Database, PrintColumns);

            await SelectTop(1, tUser.AllColumns(), tCustomer.AllColumns())
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Query(context.Database, PrintColumns);

        }
    }
}
