import { column, defineTable, insertInto, mergeInto, select, sqlType, values } from "sqyra";
import type { Scenario } from "./types.js";

export const pgMergeIdentityPolyfillScenario: Scenario = {
  source: "ScPgMergeIdentityPolyfill",
  async run(context) {
    if (context.dialect !== "pgsql") return;
    const script = {
      dialect: "pgsql" as const,
      ...(context.database.schemaMap === undefined
        ? {}
        : { schemaMap: context.database.schemaMap }),
    };
    const identity = defineTable({
      schema: null,
      name: "PgIdentityInsertPolyfillTable",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true, identity: true }),
        Value: column(sqlType.int32),
      },
    });
    await context.database.executeScript(identity.$script.dropIfExists().toSql(script));
    await context.database.executeScript(identity.$script.create().toSql(script));
    try {
      await context.execute(insertInto(identity).columns("Value").values({ Value: 10 }));
      const explicit = insertInto(identity)
        .values({ Id: 200, Value: 20 }, { Id: 201, Value: 21 })
        .identity("Id");
      const sql = context.compile(explicit).sql;
      if (sql.includes(";"))
        throw new Error("PostgreSQL identity insert polyfill should be a single SQL statement.");
      await context.execute(explicit);
      await context.execute(insertInto(identity).columns("Value").values({ Value: 30 }));
      const rows = await context.query(
        select(identity.Id, identity.Value).from(identity).orderBy(identity.Id),
      );
      const actual = rows.map((row) => `${String(row.Id)},${String(row.Value)}`).join(";");
      if (actual !== "1,10;200,20;201,21;202,30")
        throw new Error(`Identity insert polyfill failed: ${actual}.`);
    } finally {
      await context.database.executeScript(identity.$script.dropIfExists().toSql(script));
    }

    const target = defineTable({
      schema: null,
      name: "TestMergeTmpTable",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        Value: column(sqlType.int32),
        Version: column(sqlType.int32, { default: 0 }),
      },
    });
    await context.database.executeScript(target.$script.dropIfExists().toSql(script));
    await context.database.executeScript(target.$script.create().toSql(script));
    try {
      await context.execute(
        insertInto(target)
          .columns("Id", "Value")
          .values({ Id: 1, Value: 10 }, { Id: 2, Value: 20 }),
      );
      const source = values(
        [
          [1, 100],
          [3, 300],
        ],
        "MergeSource",
        { Id: column(sqlType.int32), Value: column(sqlType.int32) },
      );
      const merge = mergeInto(target, source)
        .on(target.Id.eq(source.Id))
        .whenMatchedUpdate({
          Value: source.Value,
          Version: target.Version.add(1),
        })
        .whenNotMatchedInsert({ Id: source.Id, Value: source.Value });
      const sql = context.compile(merge).sql;
      if (/CREATE TEMP TABLE|DROP TABLE/i.test(sql) || sql.includes(";"))
        throw new Error(
          "PostgreSQL MERGE polyfill must be a single statement without temporary-table setup.",
        );
      await context.execute(merge);
      const rows = await context.query(
        select(target.Id, target.Value, target.Version).from(target).orderBy(target.Id),
      );
      const actual = rows
        .map((row) => `${String(row.Id)},${String(row.Value)},${String(row.Version)}`)
        .join(";");
      if (actual !== "1,100,1;2,20,0;3,300,0")
        throw new Error(`PostgreSQL MERGE polyfill failed: ${actual}.`);
    } finally {
      await context.database.executeScript(target.$script.dropIfExists().toSql(script));
    }
  },
};
