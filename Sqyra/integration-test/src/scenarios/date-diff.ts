import {
  column,
  columnExpression,
  dateTimeValue,
  defineTable,
  exprDateDiff,
  exprDateTimeLiteral,
  insertInto,
  select,
  sqlType,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

const parts = ["Year", "Month", "Day", "Hour", "Minute", "Second"] as const;
export const dateDiffScenario: Scenario = {
  source: "ScDateDiff",
  async run(context) {
    const table = defineTable({
      schema: null,
      name: "DateRangeTemp",
      temporary: true,
      columns: { End: column(sqlType.dateTime) },
    });
    const options: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(table.$script.dropIfExists().toSql(options));
    await context.database.executeScript(table.$script.create().toSql(options));
    const start = new Date("2023-11-06T01:33:45Z"),
      end = new Date("2025-11-06T01:33:45Z");
    const dates = generateRange(start, end);
    for (let index = 0; index < dates.length; index += 1000) {
      const rows = dates
        .slice(index, index + 1000)
        .map((value) => ({ End: exprDateTimeLiteral({ value: dateTimeValue(isoLocal(value)) }) }));
      const first = rows[0];
      if (first !== undefined)
        await context.execute(
          insertInto(table)
            .columns("End")
            .values(first, ...rows.slice(1)),
        );
    }
    const startExpr = exprDateTimeLiteral({ value: dateTimeValue(isoLocal(start)) });
    const endExpr = columnExpression(table.End);
    const difference = (datePart: (typeof parts)[number]) =>
      exprDateDiff({ startDate: startExpr, endDate: endExpr, datePart });
    const rows = await context.query(
      select({
        Year: difference("Year"),
        Month: difference("Month"),
        Day: difference("Day"),
        Hour: difference("Hour"),
        Minute: difference("Minute"),
        Second: difference("Second"),
        End: table.End,
      })
        .from(table)
        .orderBy(table.End),
    );
    if (rows.length !== dates.length)
      throw new Error(`Expected ${dates.length} date ranges but received ${rows.length}.`);
    for (const row of rows) {
      const text = String(row.End).replace(" ", "T");
      const actualEnd = new Date(/(?:Z|[+-]\d{2}:\d{2})$/.test(text) ? text : `${text}Z`);
      for (const part of parts) {
        const expected = dateDiff(part, start, actualEnd);
        if (Number(row[part]) !== expected)
          throw new Error(`${part}: expected ${expected}, got ${String(row[part])}.`);
      }
    }
    await context.database.executeScript(table.$script.drop().toSql(options));
  },
};
function generateRange(start: Date, end: Date): Date[] {
  const result: Date[] = [];
  let current = start.getTime(),
    step = 1000;
  while (current < end.getTime()) {
    current += step;
    step *= 2;
    if (step > 172800000) step = 1000;
    result.push(new Date(current));
  }
  return result;
}
function truncate(part: (typeof parts)[number], value: Date): number {
  const y = value.getUTCFullYear(),
    m = value.getUTCMonth(),
    d = value.getUTCDate(),
    h = value.getUTCHours(),
    min = value.getUTCMinutes(),
    s = value.getUTCSeconds();
  return Date.UTC(
    y,
    part === "Year" ? 0 : m,
    part === "Year" || part === "Month" ? 1 : d,
    ["Year", "Month", "Day"].includes(part) ? 0 : h,
    ["Year", "Month", "Day", "Hour"].includes(part) ? 0 : min,
    part === "Second" ? s : 0,
  );
}
function dateDiff(part: (typeof parts)[number], start: Date, end: Date): number {
  if (part === "Year") return end.getUTCFullYear() - start.getUTCFullYear();
  if (part === "Month")
    return (
      (end.getUTCFullYear() - start.getUTCFullYear()) * 12 + end.getUTCMonth() - start.getUTCMonth()
    );
  const unit =
    part === "Day" ? 86400000 : part === "Hour" ? 3600000 : part === "Minute" ? 60000 : 1000;
  return Math.trunc((truncate(part, end) - truncate(part, start)) / unit);
}
function isoLocal(value: Date): string {
  return value.toISOString().slice(0, 19);
}
