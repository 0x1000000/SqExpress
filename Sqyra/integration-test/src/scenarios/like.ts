import {
  column,
  defineTable,
  insertInto,
  select,
  sqlType,
  type Expr,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

export const likeScenario: Scenario = {
  source: "ScLike",
  async run(context) {
    const table = defineTable({
      schema: null,
      name: "TmpStr",
      temporary: true,
      columns: {
        Index: column(sqlType.int32, { primaryKey: true }),
        Text: column(sqlType.string(255, { unicode: true })),
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
    await context.execute(
      insertInto(table).values(
        { Index: 0, Text: "Some simple text" },
        { Index: 1, Text: "a%b%c" },
        { Index: 2, Text: "aabcc" },
      ),
    );
    await assertRows(
      context,
      select(table.Index).from(table).where(table.Text.like("%simple%")).orderBy(table.Index),
      [0],
    );
    const exact =
      context.dialect === "sqlite"
        ? table.Text.eq("a%b%c")
        : table.Text.like(context.dialect === "tsql" ? "a[%]b[%]c" : "a\\%b\\%c");
    await assertRows(
      context,
      select(table.Index).from(table).where(exact).orderBy(table.Index),
      [1],
    );
    await assertRows(
      context,
      select(table.Index).from(table).where(table.Text.like("a%b%c")).orderBy(table.Index),
      [1, 2],
    );
    await context.database.executeScript(table.$script.drop().toSql(options));
  },
};
async function assertRows(
  context: Parameters<Scenario["run"]>[0],
  query: Expr,
  expected: ReadonlyArray<number>,
): Promise<void> {
  const actual = (await context.query(query)).map((row) => Number(row.Index));
  if (actual.length !== expected.length || actual.some((value, index) => value !== expected[index]))
    throw new Error(`LIKE result ${actual.join(",")} did not match ${expected.join(",")}.`);
}
