using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScPortableScalarFunctions : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var baseDate = new DateTime(2020, 2, 3, 4, 5, 6);
        var monthEnd = new DateTime(2020, 1, 31);
        const string unicodeSample = "\u0416";
        var leapDay = new DateTime(2020, 2, 29);
        // T-SQL OctetLength maps to DATALENGTH; for parameterized strings SQL Server uses nvarchar,
        // so ASCII "abc" is 6 bytes (2 bytes per char) instead of 3 bytes for a non-Unicode literal.
        var expectedOctetLenAscii = context.Dialect == SqlDialect.TSql && context.ParametrizationMode != ParametrizationMode.None
            ? 6
            : 3;

        Dictionary<string, object?> q = await Select(
                NullIf("A", "A").As("NullIfV"),
                NullIf("A", "B").As("NullIfKeepV"),
                Abs(-12).As("AbsV"),
                Lower("AbC").As("LowerV"),
                Upper("aBc").As("UpperV"),
                Trim("  x  ").As("TrimV"),
                LTrim("  x").As("LTrimV"),
                RTrim("x  ").As("RTrimV"),
                Replace("abc", "b", "z").As("ReplaceV"),
                Substring("abcdef", 2, 3).As("SubstringV"),
                Round(12.345m, 2).As("RoundV"),
                Floor(12.9m).As("FloorV"),
                Ceiling(12.1m).As("CeilingV"),
                Concat("a", "b", "c").As("ConcatV"),
                IndexOf("bc", "abcdef").As("IndexOfV"),
                IndexOf("zz", "abcdef").As("IndexOfMissingV"),
                Left("abcdef", 3).As("LeftV"),
                Left("abc", 10).As("LeftOverflowV"),
                Left("abc", 0).As("LeftZeroV"),
                Right("abcdef", 3).As("RightV"),
                Right("abc", 10).As("RightOverflowV"),
                Right("abc", 0).As("RightZeroV"),
                Repeat("ab", 3).As("RepeatV"),
                Repeat("ab", 0).As("RepeatZeroV"),
                Len("abc").As("CharLenV"),
                Len("").As("CharLenEmptyV"),
                Len(unicodeSample).As("CharLenUnicodeV"),
                DataLength("abc").As("OctetLenV"),
                DataLength("").As("OctetLenEmptyV"),
                DataLength(unicodeSample).As("OctetLenUnicodeV"),
                Year(baseDate).As("YearV"),
                Month(baseDate).As("MonthV"),
                Day(baseDate).As("DayV"),
                Hour(baseDate).As("HourV"),
                Minute(baseDate).As("MinuteV"),
                Second(baseDate).As("SecondV"),
                GetDate().As("CurrentDateV"),
                GetDate().As("CurrentTimeV"),
                GetUtcDate().As("CurrentTimestampV"),
                AddYears(1, baseDate).As("AddYearsV"),
                AddMonths(1, baseDate).As("AddMonthsV"),
                AddDays(1, baseDate).As("AddDaysV"),
                AddHours(1, baseDate).As("AddHoursV"),
                AddMinutes(1, baseDate).As("AddMinutesV"),
                AddSeconds(1, baseDate).As("AddSecondsV"),
                AddMonths(1, monthEnd).As("AddMonthsEdgeV"),
                AddYears(1, leapDay).As("AddYearsEdgeV"),
                DiffYears(baseDate, AddYears(1, baseDate)).As("DiffYearsV"),
                DiffMonths(baseDate, AddMonths(1, baseDate)).As("DiffMonthsV"),
                DiffDays(baseDate, AddDays(1, baseDate)).As("DiffDaysV"),
                DiffHours(baseDate, AddHours(1, baseDate)).As("DiffHoursV"),
                DiffMinutes(baseDate, AddMinutes(1, baseDate)).As("DiffMinutesV"),
                DiffSeconds(baseDate, AddSeconds(1, baseDate)).As("DiffSecondsV"),
                DiffSeconds(baseDate, AddSeconds(-5, baseDate)).As("DiffSecondsNegativeV"),
                DiffDays(AddDays(-1, baseDate), baseDate).As("DiffDaysEdgeV")
            )
            .Query(context.Database, new Dictionary<string, object?>(), (acc, r) =>
            {
                var total = r.FieldCount;
                for (int i = 0; i < total; i++)
                {
                    var colName = r.GetName(i);
                    var value = r.GetValue(i);
                    acc[colName] = value == DBNull.Value ? null : value;
                }

                return acc;
            });

        AssertResult(q, "NullIfV", null);
        AssertResult(q, "NullIfKeepV", "A");
        AssertIntResult(q, "AbsV", 12);
        AssertResult(q, "LowerV", "abc");
        AssertResult(q, "UpperV", "ABC");
        AssertResult(q, "TrimV", "x");
        AssertResult(q, "LTrimV", "x");
        AssertResult(q, "RTrimV", "x");
        AssertResult(q, "ReplaceV", "azc");
        AssertResult(q, "SubstringV", "bcd");
        AssertDecimalResult(q, "RoundV", 12.35m);
        AssertIntResult(q, "FloorV", 12);
        AssertIntResult(q, "CeilingV", 13);
        AssertResult(q, "ConcatV", "abc");
        AssertIntResult(q, "IndexOfV", 2);
        AssertIntResult(q, "IndexOfMissingV", 0);
        AssertResult(q, "LeftV", "abc");
        AssertResult(q, "LeftOverflowV", "abc");
        AssertResult(q, "LeftZeroV", "");
        AssertResult(q, "RightV", "def");
        AssertResult(q, "RightOverflowV", "abc");
        AssertResult(q, "RightZeroV", "");
        AssertResult(q, "RepeatV", "ababab");
        AssertResult(q, "RepeatZeroV", "");
        AssertIntResult(q, "CharLenV", 3);
        AssertIntResult(q, "CharLenEmptyV", 0);
        AssertIntResult(q, "CharLenUnicodeV", 1);
        AssertIntResult(q, "OctetLenV", expectedOctetLenAscii);
        AssertIntResult(q, "OctetLenEmptyV", 0);
        AssertIntResult(q, "OctetLenUnicodeV", 2);
        AssertIntResult(q, "YearV", 2020);
        AssertIntResult(q, "MonthV", 2);
        AssertIntResult(q, "DayV", 3);
        AssertIntResult(q, "HourV", 4);
        AssertIntResult(q, "MinuteV", 5);
        AssertIntResult(q, "SecondV", 6);

        AssertNotNull(q, "CurrentDateV");
        AssertNotNull(q, "CurrentTimeV");
        AssertNotNull(q, "CurrentTimestampV");

        AssertDateTimeResult(q, "AddYearsV", baseDate.AddYears(1));
        AssertDateTimeResult(q, "AddMonthsV", baseDate.AddMonths(1));
        AssertDateTimeResult(q, "AddDaysV", baseDate.AddDays(1));
        AssertDateTimeResult(q, "AddHoursV", baseDate.AddHours(1));
        AssertDateTimeResult(q, "AddMinutesV", baseDate.AddMinutes(1));
        AssertDateTimeResult(q, "AddSecondsV", baseDate.AddSeconds(1));
        AssertDateTimeResult(q, "AddMonthsEdgeV", monthEnd.AddMonths(1));
        AssertDateTimeResult(q, "AddYearsEdgeV", leapDay.AddYears(1));

        AssertIntResult(q, "DiffYearsV", 1);
        AssertIntResult(q, "DiffMonthsV", 1);
        AssertIntResult(q, "DiffDaysV", 1);
        AssertIntResult(q, "DiffHoursV", 1);
        AssertIntResult(q, "DiffMinutesV", 1);
        AssertIntResult(q, "DiffSecondsV", 1);
        AssertIntResult(q, "DiffSecondsNegativeV", -5);
        AssertIntResult(q, "DiffDaysEdgeV", 1);
    }

    private static string? PrintValue(object? o)
    {
        if (o == null)
        {
            return "<null>";
        }

        if (o  is string s && string.IsNullOrEmpty(s))
        {
            return "<empty string>";
        }

        if (o  is string s2 && string.IsNullOrWhiteSpace(s2))
        {
            return "<white space>";
        }

        return o.ToString();
    }

    private static void AssertResult(Dictionary<string, object?> actual, string name, object? expected)
    {
        var actualValue = actual[name];
        if (!Equals(actualValue, expected))
        {
            throw new Exception($"{name}: expected {PrintValue(expected)} but was {PrintValue(actualValue)}");
        }
    }

    private static void AssertNotNull(Dictionary<string, object?> actual, string name)
    {
        var actualValue = actual[name];
        if (actualValue == null || actualValue is DBNull)
        {
            throw new Exception($"{name}: expected non-null value");
        }
    }

    private static void AssertIntResult(Dictionary<string, object?> actual, string name, int expected)
    {
        var actualValue = Convert.ToInt32(actual[name]);
        if (actualValue != expected)
        {
            throw new Exception($"{name}: expected {PrintValue(expected)} but was {PrintValue(actualValue)}");
        }
    }

    private static void AssertDecimalResult(Dictionary<string, object?> actual, string name, decimal expected)
    {
        var actualValue = Convert.ToDecimal(actual[name]);
        if (actualValue != expected)
        {
            throw new Exception($"{name}: expected {PrintValue(expected)} but was {PrintValue(actualValue)}");
        }
    }

    private static void AssertDateTimeResult(Dictionary<string, object?> actual, string name, DateTime expected)
    {
        var raw = actual[name];
        if (raw == null || raw is DBNull)
        {
            throw new Exception($"{name}: expected DateTime but was null");
        }

        DateTime actualValue;
        if (raw is DateTime dateTime)
        {
            actualValue = dateTime;
        }
        else if (raw is DateTimeOffset dateTimeOffset)
        {
            actualValue = dateTimeOffset.DateTime;
        }
        else if (raw is string str && DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            actualValue = parsed;
        }
        else
        {
            throw new Exception($"{name}: expected DateTime but was {raw.GetType().FullName}");
        }

        if (actualValue != expected)
        {
            throw new Exception($"{name}: expected {expected:O} but was {actualValue:O}");
        }
    }
}
