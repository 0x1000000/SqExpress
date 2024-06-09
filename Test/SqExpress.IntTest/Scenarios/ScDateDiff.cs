using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Functions.Known;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScDateDiff : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var tbl = new DateRangeTable();

        await tbl.Script.Create().Exec(context.Database);

        var start = new DateTime(2023, 11, 6, 01, 33, 45);
        var end = start.AddYears(2);
        var stepMin = TimeSpan.FromSeconds(1);
        var stepMax = TimeSpan.FromDays(2);

        foreach (var chunk in GenerateRange(start, end, stepMin, stepMax).Chunk(1000))
        {
            await InsertDataInto(tbl, chunk).MapData(m => m.Set(m.Target.End, m.Source)).Exec(context.Database);
        }

        var asyncEnum = Select(
                DateDiff(DateDiffDatePart.Year, start, tbl.End),
                DateDiff(DateDiffDatePart.Month, start, tbl.End),
                DateDiff(DateDiffDatePart.Day, start, tbl.End),
                DateDiff(DateDiffDatePart.Hour, start, tbl.End),
                DateDiff(DateDiffDatePart.Minute, start, tbl.End),
                DateDiff(DateDiffDatePart.Second, start, tbl.End),

                tbl.End
                )
            .From(tbl)
            .OrderBy(tbl.End)
            .Query(context.Database)
            .Select(r=> new
            {
                Year = r.GetInt32(0),
                Month = r.GetInt32(1),
                Day = r.GetInt32(2),
                Hour = r.GetInt32(3),
                Minute = r.GetInt32(4),
                Second = r.GetInt32(5),
                End = tbl.End.Read(r)
            });

        await foreach (var i in asyncEnum)
        {
            //context.WriteLine($"Y:{i.Year};M:{i.Month};W:{i.Week};D:{i.Day};H:{i.Hour};m:{i.Minute};S:{i.Second}");

          Check(start, i.End, DateDiffDatePart.Year, i.Year);
          Check(start, i.End, DateDiffDatePart.Month, i.Month);
          Check(start, i.End, DateDiffDatePart.Day, i.Day);
          Check(start, i.End, DateDiffDatePart.Hour, i.Hour);
          Check(start, i.End, DateDiffDatePart.Minute, i.Minute);
          Check(start, i.End, DateDiffDatePart.Second, i.Second);

        }

        await tbl.Script.Drop().Exec(context.Database);

        static void Check(DateTime startDate, DateTime endDate, DateDiffDatePart dateDiffDatePart, int value)
        {
            int expectedDiff = dateDiffDatePart switch
            {
                DateDiffDatePart.Year => endDate.Year - startDate.Year,
                DateDiffDatePart.Month => (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month,
                DateDiffDatePart.Day => (int)(DateTrunc(DateDiffDatePart.Day, endDate) - DateTrunc(DateDiffDatePart.Day, startDate)).TotalDays,
                DateDiffDatePart.Hour => (int)(DateTrunc(DateDiffDatePart.Hour, endDate) - DateTrunc(DateDiffDatePart.Hour, startDate)).TotalHours,
                DateDiffDatePart.Minute => (int)(DateTrunc(DateDiffDatePart.Minute, endDate) - DateTrunc(DateDiffDatePart.Minute, startDate)).TotalMinutes,
                DateDiffDatePart.Second => (int)(DateTrunc(DateDiffDatePart.Second, endDate) - DateTrunc(DateDiffDatePart.Second, startDate)).TotalSeconds,
                DateDiffDatePart.Millisecond => (int)(DateTrunc(DateDiffDatePart.Millisecond, endDate) - DateTrunc(DateDiffDatePart.Millisecond, startDate)).TotalMilliseconds,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (expectedDiff != value)
            {
                throw new Exception($"Incorrect Difference: expected: {expectedDiff}, actual: {value} for {dateDiffDatePart}. Start: {startDate.ToString("O")}; End: {endDate.ToString("O")}");
            }
        }

        static DateTime DateTrunc(DateDiffDatePart dateDiffDatePart, DateTime source)
        {
            return dateDiffDatePart switch
            {
                DateDiffDatePart.Year => new DateTime(source.Year, 1, 1),
                DateDiffDatePart.Month => new DateTime(source.Year, source.Month, 1),
                DateDiffDatePart.Day => new DateTime(source.Year, source.Month, source.Day),
                DateDiffDatePart.Hour => new DateTime(source.Year, source.Month, source.Day, source.Hour, 0, 0),
                DateDiffDatePart.Minute => new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute, 0),
                DateDiffDatePart.Second => new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute, source.Second),
                DateDiffDatePart.Millisecond => new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute, source.Second, source.Millisecond),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    IEnumerable<DateTime> GenerateRange(DateTime start, DateTime end, TimeSpan initialStep, TimeSpan stepMax)
    {
        var acc = start;

        var step = initialStep;

        while (acc < end)
        {
            acc += step;
            step *= 2;
            if (step > stepMax)
            {
                step = initialStep;
            }
            yield return acc;
        }
    }

    class DateRangeTable : TempTableBase
    {
        public DateRangeTable(Alias alias = default) : base("TempTable", alias)
        {
            this.End = this.CreateDateTimeColumn("End");
        }

        public DateTimeTableColumn End { get; }
    }
}
