import {
  column,
  defineTable,
  exprGetUtcDate,
  lit,
  mergeInto,
  nullableColumn,
  select,
  sqlType,
  values,
  type InlineExportOptions,
} from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const mergeExprEdgeCasesScenario: Scenario = {
  source: "ScMergeExprEdgeCases",
  async run(context) {
    const target = defineTable({
      schema: null,
      name: "TargetTable",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        Value: column(sqlType.int32),
        Version: column(sqlType.int32, { default: 0 }),
        Extra: nullableColumn(sqlType.string(255, { unicode: true })),
      },
    });
    const script: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(target.$script.dropIfExists().toSql(script));
    await context.database.executeScript(target.$script.create().toSql(script));
    try {
      const initial = values([[1, -1]], "initial", {
        Id: column(sqlType.int32),
        Value: column(sqlType.int32),
      });
      await context.execute(
        mergeInto(target, initial)
          .on(target.Id.eq(initial.Id))
          .whenNotMatchedInsert({ Id: initial.Id, Value: initial.Value }),
      );
      const { user } = defineIntegrationTables(context.dialect);
      const query = select(lit(1), lit("AA").as("BB"), user.UserId, exprGetUtcDate)
        .from(user)
        .where(lit(1).eq(1).and(user.UserId.inList(1, 2)));
      const source = query.as("S", {
        Expr1: column(sqlType.int32),
        BB: column(sqlType.string(2)),
        UserId: column(sqlType.int32),
        Expr4: column(sqlType.dateTime),
      });
      await context.execute(
        mergeInto(target, source)
          .on(target.Id.eq(source.UserId))
          .whenMatchedUpdate({
            Value: source.UserId,
            Extra: source.BB,
            Version: target.Version.add(1),
          })
          .whenNotMatchedInsert({ Id: source.UserId, Value: source.UserId, Extra: source.BB }),
      );
      const rows = await context.query(
        select(target.Id, target.Value, target.Version, target.Extra)
          .from(target)
          .orderBy(target.Id),
      );
      const actual = rows
        .map(
          (row) =>
            `${String(row.Id)},${String(row.Value)},${String(row.Version)},${row.Extra === null ? "NULL" : String(row.Extra)}`,
        )
        .join(";");
      if (actual !== "1,1,1,AA;2,2,0,AA")
        throw new Error(`Incorrect merge edge-case result: ${actual}.`);
    } finally {
      await context.database.executeScript(target.$script.dropIfExists().toSql(script));
    }
  },
};
