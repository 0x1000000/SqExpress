import {
  dateTimeValue,
  decimalValue,
  exprDateAdd,
  exprDateDiff,
  exprDateTimeLiteral,
  exprDecimalLiteral,
  exprGetDate,
  exprGetUtcDate,
  portable,
  select,
  type ExprValue,
} from "sqyra";
import type { CanonicalRow } from "../types.js";
import type { Scenario } from "./types.js";

export const portableScalarFunctionsScenario: Scenario = {
  source: "ScPortableScalarFunctions",
  async run(context) {
    const date = (value: string) => exprDateTimeLiteral({ value: dateTimeValue(value) });
    const base = date("2020-02-03T04:05:06"),
      monthEnd = date("2020-01-31T00:00:00"),
      leap = date("2020-02-29T00:00:00");
    const add = (
      part: "Year" | "Month" | "Day" | "Hour" | "Minute" | "Second",
      number: number,
      value = base,
    ) => exprDateAdd({ date: value, datePart: part, number });
    const diff = (
      part: "Year" | "Month" | "Day" | "Hour" | "Minute" | "Second",
      start: ExprValue,
      end: ExprValue,
    ) => exprDateDiff({ startDate: start, endDate: end, datePart: part });
    const row = (
      await context.query(
        select({
          NullIfV: portable("NullIf", "A", "A"),
          NullIfKeepV: portable("NullIf", "A", "B"),
          AbsV: portable("Abs", -12),
          LowerV: portable("Lower", "AbC"),
          UpperV: portable("Upper", "aBc"),
          TrimV: portable("Trim", "  x  "),
          LTrimV: portable("LTrim", "  x"),
          RTrimV: portable("RTrim", "x  "),
          ReplaceV: portable("Replace", "abc", "b", "z"),
          SubstringV: portable("Substring", "abcdef", 2, 3),
          RoundV: portable("Round", exprDecimalLiteral({ value: decimalValue("12.345") }), 2),
          FloorV: portable("Floor", 12.9),
          CeilingV: portable("Ceiling", 12.1),
          ConcatV: "a".concat("b").concat("c"),
          IndexOfV: portable("IndexOf", "bc", "abcdef"),
          IndexOfMissingV: portable("IndexOf", "zz", "abcdef"),
          LeftV: portable("Left", "abcdef", 3),
          LeftOverflowV: portable("Left", "abc", 10),
          LeftZeroV: portable("Left", "abc", 0),
          RightV: portable("Right", "abcdef", 3),
          RightOverflowV: portable("Right", "abc", 10),
          RightZeroV: portable("Right", "abc", 0),
          RepeatV: portable("Repeat", "ab", 3),
          RepeatZeroV: portable("Repeat", "ab", 0),
          CharLenV: portable("Len", "abc"),
          CharLenEmptyV: portable("Len", ""),
          CharLenUnicodeV: portable("Len", "Ж"),
          OctetLenV: portable("DataLen", "abc"),
          OctetLenEmptyV: portable("DataLen", ""),
          OctetLenUnicodeV: portable("DataLen", "Ж"),
          YearV: portable("Year", base),
          MonthV: portable("Month", base),
          DayV: portable("Day", base),
          HourV: portable("Hour", base),
          MinuteV: portable("Minute", base),
          SecondV: portable("Second", base),
          CurrentDateV: exprGetDate,
          CurrentTimeV: exprGetDate,
          CurrentTimestampV: exprGetUtcDate,
          AddYearsV: add("Year", 1),
          AddMonthsV: add("Month", 1),
          AddDaysV: add("Day", 1),
          AddHoursV: add("Hour", 1),
          AddMinutesV: add("Minute", 1),
          AddSecondsV: add("Second", 1),
          AddMonthsEdgeV: add("Month", 1, monthEnd),
          AddYearsEdgeV: add("Year", 1, leap),
          DiffYearsV: diff("Year", base, add("Year", 1)),
          DiffMonthsV: diff("Month", base, add("Month", 1)),
          DiffDaysV: diff("Day", base, add("Day", 1)),
          DiffHoursV: diff("Hour", base, add("Hour", 1)),
          DiffMinutesV: diff("Minute", base, add("Minute", 1)),
          DiffSecondsV: diff("Second", base, add("Second", 1)),
          DiffSecondsNegativeV: diff("Second", base, add("Second", -5)),
          DiffDaysEdgeV: diff("Day", add("Day", -1), base),
        }),
      )
    )[0];
    assertPortable(row, context.dialect === "tsql" && context.parameterization !== "none" ? 6 : 3);
  },
};

function assertPortable(row: CanonicalRow | undefined, octetAscii: number): void {
  if (row === undefined) throw new Error("Portable scalar query returned no row.");
  const exact: Readonly<Record<string, string | number | null>> = {
    NullIfV: null,
    NullIfKeepV: "A",
    AbsV: 12,
    LowerV: "abc",
    UpperV: "ABC",
    TrimV: "x",
    LTrimV: "x",
    RTrimV: "x",
    ReplaceV: "azc",
    SubstringV: "bcd",
    RoundV: "12.35",
    FloorV: 12,
    CeilingV: 13,
    ConcatV: "abc",
    IndexOfV: 2,
    IndexOfMissingV: 0,
    LeftV: "abc",
    LeftOverflowV: "abc",
    LeftZeroV: "",
    RightV: "def",
    RightOverflowV: "abc",
    RightZeroV: "",
    RepeatV: "ababab",
    RepeatZeroV: "",
    CharLenV: 3,
    CharLenEmptyV: 0,
    CharLenUnicodeV: 1,
    OctetLenV: octetAscii,
    OctetLenEmptyV: 0,
    OctetLenUnicodeV: 2,
    YearV: 2020,
    MonthV: 2,
    DayV: 3,
    HourV: 4,
    MinuteV: 5,
    SecondV: 6,
    DiffYearsV: 1,
    DiffMonthsV: 1,
    DiffDaysV: 1,
    DiffHoursV: 1,
    DiffMinutesV: 1,
    DiffSecondsV: 1,
    DiffSecondsNegativeV: -5,
    DiffDaysEdgeV: 1,
  };
  for (const [name, expected] of Object.entries(exact)) {
    const actual = row[name];
    const equal =
      typeof expected === "number"
        ? Number(actual) === expected
        : expected === null
          ? actual === null
          : String(actual) === expected;
    if (!equal) throw new Error(`${name}: expected ${String(expected)} but was ${String(actual)}.`);
  }
  for (const name of ["CurrentDateV", "CurrentTimeV", "CurrentTimestampV"])
    if (row[name] === null || row[name] === undefined)
      throw new Error(`${name}: expected non-null value.`);
  const dates = {
    AddYearsV: "2021-02-03T04:05:06",
    AddMonthsV: "2020-03-03T04:05:06",
    AddDaysV: "2020-02-04T04:05:06",
    AddHoursV: "2020-02-03T05:05:06",
    AddMinutesV: "2020-02-03T04:06:06",
    AddSecondsV: "2020-02-03T04:05:07",
    AddMonthsEdgeV: "2020-02-29T00:00:00",
    AddYearsEdgeV: "2021-02-28T00:00:00",
  };
  for (const [name, expected] of Object.entries(dates)) {
    const actual = String(row[name]).replace(" ", "T");
    if (
      !actual.startsWith(expected) &&
      !(expected.endsWith("T00:00:00") && actual === expected.slice(0, 10))
    )
      throw new Error(`${name}: expected ${expected} but was ${String(row[name])}.`);
  }
}
