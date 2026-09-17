import { describe, expect, it } from "vitest";
import {
  aliasTable,
  column,
  cte,
  defineTable,
  insertInto,
  modify,
  select,
  sqlType,
  toSql,
  update,
} from "../../src/index.js";

describe("typed CTE builders", () => {
  it("builds a simple CTE", () => {
    const numbers = cte("Numbers", { Num: column(sqlType.int32) }, () => select({ Num: 1 }));
    expect(select({ Num: numbers.Num }).from(numbers).toSql({ dialect: "tsql" })).toBe(
      "WITH [Numbers] AS(SELECT 1 [Num])SELECT [Numbers].[Num] [Num] FROM [Numbers]",
    );
  });
  it("builds a recursive CTE and emits RECURSIVE for portable dialects", () => {
    const numbers = cte("Numbers", { Num: column(sqlType.int32) }, (self) =>
      select({ Num: 1 }).unionAll(
        select({ Num: self.Num.add(1) })
          .from(self)
          .where(self.Num.lt(10)),
      ),
    );
    const query = select({ Num: numbers.Num }).from(numbers);
    expect(query.toSql({ dialect: "tsql" })).toContain(
      "WITH [Numbers] AS(SELECT 1 [Num] UNION ALL",
    );
    expect(query.toSql({ dialect: "pgsql" })).toContain("WITH RECURSIVE");
    expect(query.toSql({ dialect: "mysql" })).toContain("WITH RECURSIVE");
    expect(modify(numbers.$metadata.source, (node) => node)).toBe(numbers.$metadata.source);
  });
  it("ports dependent, joined, and unioned CTE graphs", () => {
    const base = cte("Base", { Num: column(sqlType.int32) }, (self) =>
      select({ Num: 1 }).unionAll(
        select({ Num: self.Num.add(1) })
          .from(self)
          .where(self.Num.lt(3)),
      ),
    );
    const scaled = cte("Scaled", { Num: column(sqlType.int32) }, () =>
      select({ Num: base.Num.multiply(10) }).from(base),
    );
    const joined = cte(
      "Joined",
      { Left: column(sqlType.int32), Right: column(sqlType.int32) },
      () => select({ Left: base.Num, Right: scaled.Num }).from(base).crossJoin(scaled),
    );
    const unioned = select({ Num: scaled.Num })
      .from(scaled)
      .unionAll(select({ Num: base.Num }).from(base));
    const sql = select({ Left: joined.Left, Right: joined.Right })
      .from(joined)
      .toSql({ dialect: "pgsql" });
    expect(sql).toContain('WITH RECURSIVE "Base" AS(');
    expect(sql).toContain('"Scaled" AS(');
    expect(sql).toContain('"Joined" AS(');
    expect(sql).toContain("CROSS JOIN");
    expect(unioned.toSql({ dialect: "tsql" })).toContain("UNION ALL");
  });
  it("ports CTE-backed INSERT and immutable CTE renaming", () => {
    const numbers = cte("Numbers", { Num: column(sqlType.int32) }, () => select({ Num: 1 }));
    const target = defineTable({
      schema: "dbo",
      name: "Target",
      columns: { Val: column(sqlType.int32) },
    });
    const insert = insertInto(target).from(select({ Num: numbers.Num }).from(numbers), "Val");
    expect(insert.toSql({ dialect: "tsql" })).toBe(
      "WITH [Numbers] AS(SELECT 1 [Num])INSERT INTO [dbo].[Target]([Val]) SELECT [Numbers].[Num] [Num] FROM [Numbers]",
    );
    const renamed = modify(numbers.$metadata.source, (node) =>
      node.kind === "ExprCteQuery" ? { ...node, name: "Renamed" } : node,
    );
    expect(renamed).not.toBe(numbers.$metadata.source);
    if (renamed === null) throw new Error("CTE was removed");
    expect(toSql(renamed, { dialect: "tsql" })).toContain("[Renamed]");
  });
  it("ports a recursive CTE seeded from another CTE with pagination", () => {
    const simple = cte("SimpleCte", { Num: column(sqlType.int32) }, () => select({ Num: 1 }));
    const recursive = cte("SimpleFromRecursive", { Num: column(sqlType.int32) }, (self) =>
      select({ Num: simple.Num })
        .from(simple)
        .unionAll(
          select({ Num: self.Num.add(1) })
            .from(self)
            .where(self.Num.lt(10)),
        ),
    );
    const sql = select({ Num: recursive.Num })
      .from(recursive)
      .orderBy(recursive.Num)
      .offsetFetch(2, 3)
      .toSql({ dialect: "tsql" });
    expect(sql).toBe(
      "WITH [SimpleCte] AS(SELECT 1 [Num]),[SimpleFromRecursive] AS(SELECT [SimpleCte].[Num] [Num] FROM [SimpleCte] UNION ALL SELECT [SimpleFromRecursive].[Num]+1 [Num] FROM [SimpleFromRecursive] WHERE [SimpleFromRecursive].[Num]<10)SELECT [SimpleFromRecursive].[Num] [Num] FROM [SimpleFromRecursive] ORDER BY [SimpleFromRecursive].[Num] OFFSET 2 ROW FETCH NEXT 3 ROW ONLY",
    );
  });
  it("ports UPDATE joined to a dependent recursive CTE", () => {
    const numbers = cte("SimpleRecursiveCte", { Num: column(sqlType.int32) }, (self) =>
      select({ Num: 1 }).unionAll(
        select({ Num: self.Num.add(1) })
          .from(self)
          .where(self.Num.lt(10)),
      ),
    );
    const scaled = cte(
      "RefSimpleRecursiveWithOriginalCte",
      { OriginalNum: column(sqlType.int32), Num: column(sqlType.int32) },
      () =>
        select({ OriginalNum: numbers.Num, Num: numbers.Num.multiply(10) })
          .from(numbers)
          .where(numbers.Num.lt(10)),
    );
    const target = aliasTable(
      defineTable({ schema: "dbo", name: "TargetTable", columns: { Val: column(sqlType.int32) } }),
      "A0",
    );
    const statement = update(target)
      .set({ Val: scaled.Num })
      .from(target)
      .innerJoin(scaled, scaled.Num.eq(target.Val));
    const tsql = statement.toSql({ dialect: "tsql" });
    expect(tsql).toContain("WITH [SimpleRecursiveCte] AS(");
    expect(tsql).toContain("[RefSimpleRecursiveWithOriginalCte] AS(");
    expect(tsql).toContain("UPDATE [A0] SET [A0].[Val]=[RefSimpleRecursiveWithOriginalCte].[Num]");
    const mysql = statement.toSql({ dialect: "mysql" });
    expect(mysql).toContain("UPDATE `TargetTable` `A0` JOIN (WITH RECURSIVE");
    expect(mysql).toContain("SET `A0`.`Val`=");
  });
});
