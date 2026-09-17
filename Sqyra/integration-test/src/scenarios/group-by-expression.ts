import {
  aggregate,
  column,
  dateTimeValue,
  defineTable,
  exprColumn,
  exprColumnName,
  exprDateTimeLiteral,
  exprUnsafeValue,
  insertInto,
  portable,
  select,
  sqlType,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

export const groupByExpressionScenario: Scenario = {
  source: "ScGroupByExpression",
  async run(context) {
    const table = defineTable({
      schema: null,
      name: "TmpGroupedSales",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        CreatedAt: column(sqlType.dateTime),
        Amount: column(sqlType.int32),
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
      const data = [
        [1, "2023-01-10", 10],
        [2, "2023-05-11", 20],
        [3, "2024-02-12", 30],
        [4, "2024-09-13", 25],
        [5, "2025-03-13", 40],
        [6, "2025-10-14", 35],
        [7, "2026-04-15", 50],
        [8, "2026-11-16", 45],
      ] as const;
      const [first, ...rest] = data.map(([Id, CreatedAt, Amount]) => ({
        Id,
        CreatedAt: exprDateTimeLiteral({ value: dateTimeValue(`${CreatedAt}T00:00:00`) }),
        Amount,
      }));
      if (first === undefined) throw new Error("Grouped sales fixture is empty.");
      await context.execute(insertInto(table).values(first, ...rest));
      const bucket = portable("Year", table.CreatedAt).modulo(
        exprUnsafeValue({ unsafeValue: "2" }),
      );
      const rows = await context.query(
        select({ CreatedYearModulo: bucket, TotalAmount: aggregate("SUM", table.Amount) })
          .from(table)
          .groupBy(bucket)
          .orderBy(
            exprColumn({ source: null, columnName: exprColumnName({ name: "CreatedYearModulo" }) }),
          ),
      );
      const actual = new Map(
        rows.map((row) => [Number(row.CreatedYearModulo), Number(row.TotalAmount)]),
      );
      if (actual.size !== rows.length)
        throw new Error("Grouped results contain duplicate bucket keys.");
      if (actual.get(0) !== 150 || actual.get(1) !== 105)
        throw new Error(`Unexpected grouped totals: ${JSON.stringify([...actual])}`);
    } finally {
      await context.database.executeScript(table.$script.dropIfExists().toSql(options));
    }
  },
};
