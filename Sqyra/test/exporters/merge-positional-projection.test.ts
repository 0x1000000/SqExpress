import { expect, it } from "vitest";
import {
  column,
  defineTable,
  exprGetUtcDate,
  lit,
  mergeInto,
  select,
  sqlType,
} from "../../src/index.js";

it("exports MERGE from positional mixed-name projections with explicitly declared derived columns", () => {
  const target = defineTable({
    schema: "dbo",
    name: "Target",
    columns: { Id: column(sqlType.int32), Value: column(sqlType.string(2)) },
  });
  const query = select(lit(1), lit("AA").as("BB"), target.Id, exprGetUtcDate).from(target);
  const source = query.as("S", {
    Expr1: column(sqlType.int32),
    BB: column(sqlType.string(2)),
    Id: column(sqlType.int32),
    Expr4: column(sqlType.dateTime),
  });
  const statement = mergeInto(target, source)
    .on(target.Id.eq(source.Id))
    .whenMatchedUpdate({ Value: source.BB })
    .whenNotMatchedInsert({ Id: source.Id, Value: source.BB });
  for (const dialect of ["mysql", "sqlite"] as const) {
    const sql = statement.toSql(dialect);
    expect(sql).toContain("CREATE TEMP");
    expect(sql).toContain(
      dialect === "mysql"
        ? "(`Expr1`,`BB`,`Id`,`Expr4`) SELECT 1,'AA'"
        : '("Expr1","BB","Id","Expr4") SELECT 1,\'AA\'',
    );
  }
});
