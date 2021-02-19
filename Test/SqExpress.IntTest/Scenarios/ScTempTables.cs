using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Functions.Known;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScTempTables : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tbl = new IdModified();
            DateTime now = new DateTime(2020, 10, 18);

            await context.Database.Statement(tbl.Script.Create());

            await InsertDataInto(tbl, GetItems())
                .MapData(s => s.Set(s.Target.Modified, s.Source))
                .Exec(context.Database);

            context.WriteLine("Data from temporary table:");

            var clYear = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Year));
            var clMonth = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Month));
            var clWeek = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Week));
            var clDay = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Day));
            var clHour = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Hour));
            var clMinute = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Minute));
            var clSecond = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Second));
            var clMillisecond = CustomColumnFactory.DateTime(nameof(DateAddDatePart.Millisecond));

            var number = 3;
            var result = await Select(
                    tbl.Id,
                    tbl.Modified,
                    DateAdd(DateAddDatePart.Year, number, tbl.Modified).As(clYear),
                    DateAdd(DateAddDatePart.Month, number, tbl.Modified).As(clMonth),
                    DateAdd(DateAddDatePart.Week, number, tbl.Modified).As(clWeek),
                    DateAdd(DateAddDatePart.Day, number, tbl.Modified).As(clDay),
                    DateAdd(DateAddDatePart.Hour, number, tbl.Modified).As(clHour),
                    DateAdd(DateAddDatePart.Minute, number, tbl.Modified).As(clMinute),
                    DateAdd(DateAddDatePart.Second, number, tbl.Modified).As(clSecond),
                    DateAdd(DateAddDatePart.Millisecond, number, tbl.Modified).As(clMillisecond)
                )
                .From(tbl)
                .QueryList(context.Database,
                    r => new
                    {
                        Original = tbl.Modified.Read(r), 
                        Year = clYear.Read(r), 
                        Month = clMonth.Read(r),
                        Week = clWeek.Read(r), 
                        Day = clDay.Read(r), 
                        Hour = clHour.Read(r), 
                        Minute = clMinute.Read(r),
                        Second = clSecond.Read(r), 
                        Millisecond = clMillisecond.Read(r)
                    });

            //Checking Items
            foreach (var r in result)
            {
                AssertDatesEqual(r.Original.AddYears(number), r.Year, "Year");
                AssertDatesEqual(r.Original.AddMonths(number), r.Month, "Month");
                AssertDatesEqual(r.Original.AddDays(number*7), r.Week, "Week");
                AssertDatesEqual(r.Original.AddDays(number), r.Day, "Day");
                AssertDatesEqual(r.Original.AddHours(number), r.Hour, "Hour");
                AssertDatesEqual(r.Original.AddMinutes(number), r.Minute, "Minute");
                AssertDatesEqual(r.Original.AddSeconds(number), r.Second, "Second");
                AssertDatesEqual(r.Original.AddMilliseconds(number), r.Millisecond, "Millisecond");
            }

            context.WriteLine("All dates are correct!");

            await context.Database.Statement(tbl.Script.Drop());
            await context.Database.Statement(tbl.Script.DropIfExist());

            IEnumerable<DateTime> GetItems()
            {

                for (int i = 0; i < 10; i++)
                {
                    now = now.AddDays(-1);
                    yield return now;
                }
            }

            void AssertDatesEqual(DateTime expected, DateTime actual, string mode)
            {
                if (expected != actual)
                {
                    throw new SqExpressException($"{context.Dialect} {mode} - Expected '{expected:O}' does not equal to actual '{actual:O}'");
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
                this.Modified = this.CreateDateTimeColumn("Modified");
            }
        }
    }
}