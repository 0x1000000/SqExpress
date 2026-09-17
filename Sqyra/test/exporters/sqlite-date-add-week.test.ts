import { describe, expect, it } from "vitest";
import { dateTimeValue, exprDateAdd, exprDateTimeLiteral, toSql } from "../../src/index.js";

describe("SQLite DATEADD week adaptation", () => {
  it("uses seven-day units supported by SQLite", () => {
    const date = exprDateTimeLiteral({ value: dateTimeValue("2020-10-17T00:00:00") });
    expect(toSql(exprDateAdd({ datePart: "Week", number: 3, date }), "sqlite")).toBe(
      "DATETIME('2020-10-17','+21 days')",
    );
  });
  it("preserves milliseconds using SQLite fractional-second formatting", () => {
    const date = exprDateTimeLiteral({ value: dateTimeValue("2020-10-17T00:00:00") });
    expect(toSql(exprDateAdd({ datePart: "Millisecond", number: 3, date }), "sqlite")).toBe(
      "STRFTIME('%Y-%m-%d %H:%M:%f','2020-10-17','+0.003 seconds')",
    );
  });
  it("uses MySQL microseconds for a millisecond DATEADD", () => {
    const date = exprDateTimeLiteral({ value: dateTimeValue("2020-10-17T00:00:00") });
    expect(toSql(exprDateAdd({ datePart: "Millisecond", number: 3, date }), "mysql")).toBe(
      "DATE_ADD('2020-10-17',INTERVAL 3000 MICROSECOND)",
    );
  });
});
