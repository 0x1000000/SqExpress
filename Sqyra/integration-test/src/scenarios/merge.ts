import {
  column,
  defineTable,
  mergeInto,
  select,
  sqlType,
  values,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

export const mergeScenario: Scenario = {
  source: "ScMerge",
  async run(context) {
    const table = defineTable({
      schema: null,
      name: "TestMergeTmpTable",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        Value: column(sqlType.int32),
        Version: column(sqlType.int32, { default: 0 }),
      },
    });
    const script: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(table.$script.dropIfExists().toSql(script));
    await context.database.executeScript(table.$script.create().toSql(script));
    const rows = async () =>
      (
        await context.query(
          select(table.Id, table.Value, table.Version).from(table).orderBy(table.Id),
        )
      )
        .map((row) => `${String(row.Id)},${String(row.Value)},${String(row.Version)}`)
        .join(";");
    const source = (data: ReadonlyArray<readonly [number, number]>) =>
      values(
        data.map(([id, value]) => [id, value]),
        "MergeSource",
        { Id: column(sqlType.int32), Value: column(sqlType.int32) },
      );
    try {
      const first = source([
        [1, 10],
        [2, 11],
      ]);
      await context.execute(
        mergeInto(table, first)
          .on(table.Id.eq(first.Id))
          .whenMatchedUpdate({
            Value: first.Value,
            Version: table.Version.add(1),
          })
          .whenNotMatchedInsert({ Id: first.Id, Value: first.Value }),
      );
      if ((await rows()) !== "1,10,0;2,11,0")
        throw new Error("Initial MERGE INSERT rows were incorrect.");
      const second = source([
        [1, 100],
        [2, 11],
        [3, 12],
      ]);
      await context.execute(
        mergeInto(table, second)
          .on(table.Id.eq(second.Id))
          .whenMatchedUpdate(
            { Value: second.Value, Version: table.Version.add(1) },
            table.Value.neq(second.Value),
          )
          .whenNotMatchedInsert({ Id: second.Id, Value: second.Value }),
      );
      const secondRows = await rows();
      if (secondRows !== "1,100,1;2,11,0;3,12,0")
        throw new Error(`Conditional MERGE UPDATE/INSERT rows were incorrect: ${secondRows}.`);
      const third = source([[1, 17]]);
      await context.execute(
        mergeInto(table, third)
          .on(table.Id.eq(third.Id))
          .whenMatchedUpdate(
            { Value: third.Value, Version: table.Version.add(1) },
            table.Value.neq(third.Value),
          )
          .whenNotMatchedBySourceUpdate({ Version: table.Version.add(10) }, table.Value.eq(12)),
      );
      if ((await rows()) !== "1,17,2;2,11,0;3,12,10")
        throw new Error("MERGE WHEN NOT MATCHED BY SOURCE UPDATE rows were incorrect.");
      await context.execute(
        mergeInto(table, third)
          .on(table.Id.eq(third.Id))
          .whenMatchedUpdate({
            Value: third.Value,
            Version: table.Version.add(1),
          })
          .whenNotMatchedBySourceDelete(table.Value.eq(12)),
      );
      if ((await rows()) !== "1,17,3;2,11,0")
        throw new Error("MERGE WHEN NOT MATCHED BY SOURCE DELETE rows were incorrect.");
      await context.execute(mergeInto(table, third).on(table.Id.eq(third.Id)).whenMatchedDelete());
      if ((await rows()) !== "2,11,0")
        throw new Error("MERGE WHEN MATCHED DELETE row was incorrect.");
    } finally {
      await context.database.executeScript(table.$script.dropIfExists().toSql(script));
    }
  },
};
