import {
  column,
  defineTable,
  insertInto,
  nullableColumn,
  select,
  sqlType,
  stringAgg,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";
const separator = "'|";
export const stringAggScenario: Scenario = {
  source: "ScStringAgg",
  async run(context) {
    const table = defineTable({
      schema: null,
      name: "TmpStringAggItems",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        GroupId: column(sqlType.int32),
        SortKey: column(sqlType.int32),
        Value: nullableColumn(sqlType.string(50)),
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
        { Id: 1, GroupId: 1, SortKey: 2, Value: "Beta" },
        { Id: 2, GroupId: 1, SortKey: 1, Value: "Alpha" },
        { Id: 3, GroupId: 1, SortKey: 3, Value: null },
        { Id: 4, GroupId: 2, SortKey: 1, Value: "Solo" },
        { Id: 5, GroupId: 3, SortKey: 1, Value: null },
      ),
    );
    const rows = await context.query(
      select({
        GroupId: table.GroupId,
        OrderedValues: stringAgg(table.Value, separator).orderBy(table.SortKey),
        UnorderedValues: stringAgg(table.Value, separator),
      })
        .from(table)
        .groupBy(table.GroupId)
        .orderBy(table.GroupId),
    );
    const byGroup = new Map(rows.map((row) => [Number(row.GroupId), row]));
    if (byGroup.size !== rows.length)
      throw new Error("STRING_AGG returned duplicate group keys; C# ToDictionary rejects them.");
    if (
      byGroup.get(1)?.OrderedValues !== "Alpha'|Beta" ||
      byGroup.get(2)?.OrderedValues !== "Solo" ||
      byGroup.get(3)?.OrderedValues !== null
    )
      throw new Error("Ordered STRING_AGG returned unexpected values.");
    assertUnordered(byGroup.get(1)?.UnorderedValues, ["Alpha", "Beta"]);
    assertUnordered(byGroup.get(2)?.UnorderedValues, ["Solo"]);
    if (byGroup.get(3)?.UnorderedValues !== null)
      throw new Error("All-null STRING_AGG must return null.");
    await context.database.executeScript(table.$script.dropIfExists().toSql(options));
  },
};
function assertUnordered(actual: unknown, expected: ReadonlyArray<string>): void {
  if (
    typeof actual !== "string" ||
    actual.split(separator).sort().join("\0") !== [...expected].sort().join("\0")
  )
    throw new Error(`Unexpected unordered STRING_AGG value: ${String(actual)}.`);
}
