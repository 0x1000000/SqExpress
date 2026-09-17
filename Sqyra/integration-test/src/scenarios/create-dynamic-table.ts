import {
  column,
  defineTable,
  indexDesc,
  select,
  sqlType,
  tableIndex,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

export const createDynamicTableScenario: Scenario = {
  source: "ScCreateDynamicTable",
  async run(context) {
    // The C# source excludes Oracle MySQL for this scenario.
    if (context.dialect === "mysql-oracle") return;
    const table = defineTable({
      schema: "dbo",
      name: "DynamicTable",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true, identity: true }),
        Value: column(sqlType.string(255, { unicode: true })),
        IsActive: column(sqlType.boolean, { default: true }),
      },
      indexes: (t) => {
        const id = t.$metadata.columns.Id!,
          value = t.$metadata.columns.Value!;
        return [tableIndex([id, indexDesc(value)]), tableIndex(value)];
      },
    });
    const options: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
      ...(context.database.schemaMap === undefined
        ? {}
        : { schemaMap: context.database.schemaMap }),
    };
    await context.database.executeScript(table.$script.dropIfExists().toSql(options));
    await context.database.executeScript(table.$script.create().toSql(options));
    try {
      const rows = await context.query(select({ Id: 1 }).from(table));
      if (rows.length !== 0) throw new Error("A newly created dynamic table should be empty.");
      // The remaining C# assertion compares discovered DatabaseMetadata to
      // the descriptor; live metadata discovery is outside Sqyra's API.
    } finally {
      await context.database.executeScript(table.$script.drop().toSql(options));
    }
  },
};
