import {
  column,
  columnExpression,
  defineTable,
  exprDateAdd,
  insertInto,
  select,
  sqlType,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

const dateParts = [
  "Year",
  "Month",
  "Week",
  "Day",
  "Hour",
  "Minute",
  "Second",
  "Millisecond",
] as const;
type DatePart = (typeof dateParts)[number];

function expectedDate(original: Date, part: DatePart, count: number): Date {
  const result = new Date(original.getTime());
  switch (part) {
    case "Year":
      result.setUTCFullYear(result.getUTCFullYear() + count);
      break;
    case "Month":
      result.setUTCMonth(result.getUTCMonth() + count);
      break;
    case "Week":
      result.setUTCDate(result.getUTCDate() + count * 7);
      break;
    case "Day":
      result.setUTCDate(result.getUTCDate() + count);
      break;
    case "Hour":
      result.setUTCHours(result.getUTCHours() + count);
      break;
    case "Minute":
      result.setUTCMinutes(result.getUTCMinutes() + count);
      break;
    case "Second":
      result.setUTCSeconds(result.getUTCSeconds() + count);
      break;
    case "Millisecond":
      result.setUTCMilliseconds(result.getUTCMilliseconds() + count);
      break;
  }
  return result;
}

function timestamp(value: string): number {
  const parsed = Date.parse(`${value.replace(" ", "T").replace(/Z$/, "")}Z`);
  if (!Number.isFinite(parsed)) throw new Error(`Invalid normalized date/time value: ${value}.`);
  return parsed;
}

export const tempTablesScenario: Scenario = {
  source: "ScTempTables",
  async run(context) {
    const table = defineTable({
      schema: null,
      name: "t -- mpU\"s'er",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { identity: true, primaryKey: true }),
        Modified: column(sqlType.dateTime),
      },
    });
    const options: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(table.$script.dropIfExists().toSql(options));
    await context.database.executeScript(table.$script.create().toSql(options));
    try {
      const originals: string[] = [];
      for (let day = 17; day >= 8; day--)
        originals.push(`2020-10-${String(day).padStart(2, "0")}T00:00:00`);
      const inputs = originals.map((Modified) => ({ Modified }));
      const first = inputs[0];
      if (first === undefined) throw new Error("The source temporary-table test data is empty.");
      await context.execute(
        insertInto(table)
          .columns("Modified")
          .values(first, ...inputs.slice(1)),
      );
      const expression = columnExpression(table.Modified);
      const projection = Object.fromEntries(
        dateParts.map((part) => [
          part,
          exprDateAdd({ datePart: part, number: 3, date: expression }),
        ]),
      );
      const query = select({ Id: table.Id, Modified: table.Modified, ...projection })
        .from(table)
        .orderBy(table.Id);
      const rows = await context.query(query);
      if (rows.length !== originals.length)
        throw new Error(
          `Expected ${originals.length} temporary table rows, received ${rows.length}.`,
        );
      for (const row of rows) {
        const original = new Date(timestamp(String(row.Modified)));
        for (const part of dateParts) {
          const expected = expectedDate(original, part, 3).getTime();
          if (row[part] === null || row[part] === undefined)
            throw new Error(
              `${context.database.dialect} ${part} returned ${String(row[part])} for ${String(row.Modified)}.`,
            );
          const actual = timestamp(String(row[part]));
          if (actual !== expected)
            throw new Error(
              `${context.database.dialect} ${part}: expected ${new Date(expected).toISOString()}, received ${String(row[part])}.`,
            );
        }
      }
    } finally {
      await context.database.executeScript(table.$script.drop().toSql(options));
      await context.database.executeScript(table.$script.dropIfExists().toSql(options));
    }
  },
};
