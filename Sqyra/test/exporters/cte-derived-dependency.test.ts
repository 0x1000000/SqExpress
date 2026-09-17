import { expect, it } from "vitest";
import { column, cte, defineTable, deleteFrom, select, sqlType } from "../../src/index.js";

it("orders CTE dependencies reached through a joined derived query before their consumers", () => {
  const base = cte("Base", { Id: column(sqlType.int32) }, () => select({ Id: 1 }));
  const dependent = cte("Dependent", { Id: column(sqlType.int32) }, () =>
    select({ Id: base.Id }).from(base),
  );
  const other = cte("Other", { Id: column(sqlType.int32) }, () => select({ Id: 1 }));
  const target = defineTable({
    schema: "dbo",
    name: "Target",
    columns: { Id: column(sqlType.int32) },
  });
  const derived = select({ Id: dependent.Id }).from(dependent);
  const statement = deleteFrom(target)
    .from(target)
    .innerJoin(other, other.Id.eq(target.Id))
    .innerJoin(derived, derived.Id.eq(other.Id));
  const sql = statement.toSql("tsql");
  expect(sql).toContain("[Base] AS(");
  expect(sql.indexOf("[Base] AS(")).toBeLessThan(sql.indexOf("[Dependent] AS("));
  const mysql = statement.toSql("mysql");
  expect(mysql).toContain("JOIN (WITH");
  expect(mysql).toContain("`Other` AS(");
  expect(mysql).toContain("`Dependent` AS(");
});

it("orders every level of a CTE dependency chain", () => {
  const base = cte("Base", { Id: column(sqlType.int32) }, () => select({ Id: 1 }));
  const middle = cte("Middle", { Id: column(sqlType.int32) }, () =>
    select({ Id: base.Id }).from(base),
  );
  const outer = cte("Outer", { Id: column(sqlType.int32) }, () =>
    select({ Id: middle.Id }).from(middle),
  );
  const sql = select(outer.Id).from(outer).toSql("tsql");
  expect(sql.indexOf("[Base] AS(")).toBeLessThan(sql.indexOf("[Middle] AS("));
  expect(sql.indexOf("[Middle] AS(")).toBeLessThan(sql.indexOf("[Outer] AS("));
});
