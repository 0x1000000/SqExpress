using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Scenarios
{
    public class ScTempTables : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tbl = new IdModified();

            await context.Database.Statement(tbl.Script.Create());

            await SqQueryBuilder.InsertDataInto(tbl, GetItems())
                .MapData(s => s.Set(s.Target.Modified, s.Source))
                .Exec(context.Database);

            context.WriteLine("Data from temporary table:");

            await SqQueryBuilder.Select(tbl.Id, tbl.Modified)
                .From(tbl)
                .Query(context.Database,
                    (object?) null,
                    (seed, next) =>
                    {
                        context.WriteLine($"{tbl.Id.Read(next)}, {tbl.Modified.Read(next)}");
                        return seed;
                    });

            await context.Database.Statement(tbl.Script.Drop());
            await context.Database.Statement(tbl.Script.DropIfExist());

            IEnumerable<DateTime> GetItems()
            {
                DateTime now = new DateTime(2020, 10, 17);

                for (int i = 0; i < 10; i++)
                {
                    now = now.AddDays(-1);
                    yield return now;
                }
            }
        }

        private class IdModified : TempTableBase
        {
            public Int32TableColumn Id { get; }

            public DateTimeTableColumn Modified { get; }

            public IdModified(Alias alias = default) : base("t -- mpU\"s'er", alias)
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.Identity().PrimaryKey());
                this.Modified = this.CreateDateTimeColumn("Modified", true);
            }
        }
    }
}