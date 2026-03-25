using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.TableDecalationAttributes;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScGroupByExpression : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var table = new TempGroupedSales();

            await context.Database.Statement(table.Script.DropIfExist());
            await context.Database.Statement(table.Script.Create());

            try
            {
                var items = new[]
                {
                    new {Id = 1, CreatedAt = new DateTime(2023, 01, 10), Amount = 10},
                    new {Id = 2, CreatedAt = new DateTime(2023, 05, 11), Amount = 20},
                    new {Id = 3, CreatedAt = new DateTime(2024, 02, 12), Amount = 30},
                    new {Id = 4, CreatedAt = new DateTime(2024, 09, 13), Amount = 25},
                    new {Id = 5, CreatedAt = new DateTime(2025, 03, 13), Amount = 40},
                    new {Id = 6, CreatedAt = new DateTime(2025, 10, 14), Amount = 35},
                    new {Id = 7, CreatedAt = new DateTime(2026, 04, 15), Amount = 50},
                    new {Id = 8, CreatedAt = new DateTime(2026, 11, 16), Amount = 45}
                };

                await InsertDataInto(table, items)
                    .MapData(s => s
                        .Set(s.Target.Id, s.Source.Id)
                        .Set(s.Target.CreatedAt, s.Source.CreatedAt)
                        .Set(s.Target.Amount, s.Source.Amount))
                    .Exec(context.Database);

                var clYearModulo = CustomColumnFactory.Int32("CreatedYearModulo");
                var clTotalAmount = CustomColumnFactory.Int32("TotalAmount");

                ExprValue yearModuloBucket = Year(table.CreatedAt) % UnsafeValue("2");

                var baseQuery = Select(
                        yearModuloBucket.As(clYearModulo),
                        Sum(table.Amount).As(clTotalAmount))
                    .From(table)
                    .GroupBy(table.CreatedAt)
                    .OrderBy(Asc(clYearModulo))
                    .Done();

                var query = baseQuery.WithSelectQuery(
                    ((ExprQuerySpecification)baseQuery.SelectQuery).WithGroupBy(new[] { yearModuloBucket }));

                var result = await query.QueryList(context.Database, r => new
                {
                    YearModulo = clYearModulo.Read(r),
                    TotalAmount = clTotalAmount.Read(r)
                });

                var actual = result.ToDictionary(i => i.YearModulo, i => i.TotalAmount);

                AssertTotal(actual, 0, 150);
                AssertTotal(actual, 1, 105);

                context.WriteLine("Grouped rows:");
                foreach (var item in result)
                {
                    context.WriteLine($"{item.YearModulo}: {item.TotalAmount}");
                }
            }
            finally
            {
                await context.Database.Statement(table.Script.DropIfExist());
            }
        }

        private static void AssertTotal(System.Collections.Generic.IReadOnlyDictionary<int, int> actual, int year, int expected)
        {
            if (!actual.TryGetValue(year, out var value) || value != expected)
            {
                throw new SqExpressException($"Expected grouped year modulo {year} to have total amount {expected}, actual: {(actual.TryGetValue(year, out var current) ? current.ToString() : "<missing>")}");
            }
        }
    }

    [TempTableDescriptor("TmpGroupedSales")]
    [Int32Column("Id", Pk = true)]
    [DateTimeColumn("CreatedAt")]
    [Int32Column("Amount")]
    public partial class TempGroupedSales
    {
    }
}
