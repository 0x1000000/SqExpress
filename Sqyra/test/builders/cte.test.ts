import { describe, expect, it } from "vitest";
import { add, aliasTable, column, cte, defineTable, eq, insertInto, lt, modify, multiply, select, sqlType, toSql, unionAll, update } from "../../src/index.js";

describe("typed CTE builders", () => {
  it("builds a simple CTE", () => {
    const numbers = cte("Numbers", { Num: column(sqlType.int32) }, () => select({ Num: 1 }).done());
    expect(toSql(select({ Num: numbers.columns.Num }).from(numbers).done().ast, { dialect: "tsql" })).toBe("WITH [Numbers] AS(SELECT 1 [Num])SELECT [Numbers].[Num] [Num] FROM [Numbers]");
  });
  it("builds a recursive CTE and emits RECURSIVE for portable dialects", () => {
    const numbers = cte("Numbers", { Num: column(sqlType.int32) }, (self) => unionAll(select({ Num: 1 }).done(), select({ Num: add(self.columns.Num, 1) }).from(self).where(lt(self.columns.Num, 10)).done()));
    const query = select({ Num: numbers.columns.Num }).from(numbers).done().ast;
    expect(toSql(query, { dialect: "tsql" })).toContain("WITH [Numbers] AS(SELECT 1 [Num] UNION ALL");
    expect(toSql(query, { dialect: "postgresql" })).toContain("WITH RECURSIVE");
    expect(toSql(query, { dialect: "mysql" })).toContain("WITH RECURSIVE");
    expect(modify(numbers.derivedSource, (node) => node)).toBe(numbers.derivedSource);
  });
  it("ports dependent, joined, and unioned CTE graphs", () => {
    const base = cte("Base", { Num: column(sqlType.int32) }, self => unionAll(select({ Num: 1 }).done(), select({ Num: add(self.Num, 1) }).from(self).where(lt(self.Num, 3)).done()));
    const scaled = cte("Scaled", { Num: column(sqlType.int32) }, () => select({ Num: multiply(base.Num, 10) }).from(base).done());
    const joined = cte("Joined", { Left: column(sqlType.int32), Right: column(sqlType.int32) }, () => select({ Left: base.Num, Right: scaled.Num }).from(base).crossJoin(scaled).done());
    const unioned = unionAll(select({ Num: scaled.Num }).from(scaled).done(), select({ Num: base.Num }).from(base).done());
    const sql = toSql(select({ Left: joined.Left, Right: joined.Right }).from(joined).done().ast, { dialect: "postgresql" });
    expect(sql).toContain('WITH RECURSIVE "Base" AS('); expect(sql).toContain('"Scaled" AS('); expect(sql).toContain('"Joined" AS('); expect(sql).toContain("CROSS JOIN");
    expect(toSql(unioned.ast, { dialect: "tsql" })).toContain("UNION ALL");
  });
  it("ports CTE-backed INSERT and immutable CTE renaming", () => {
    const numbers = cte("Numbers", { Num: column(sqlType.int32) }, () => select({ Num: 1 }).done());
    const target = defineTable({ schema: "dbo", name: "Target", columns: { Val: column(sqlType.int32) } });
    const insert = insertInto(target).from(select({ Num: numbers.Num }).from(numbers).done(), "Val");
    expect(toSql(insert.ast, { dialect: "tsql" })).toBe("WITH [Numbers] AS(SELECT 1 [Num])INSERT INTO [dbo].[Target]([Val]) SELECT [Numbers].[Num] [Num] FROM [Numbers]");
    const renamed = modify(numbers.derivedSource, node => node.kind === "ExprCteQuery" ? { ...node, name: "Renamed" } : node);
    expect(renamed).not.toBe(numbers.derivedSource); if (renamed === null) throw new Error("CTE was removed"); expect(toSql(renamed, { dialect: "tsql" })).toContain("[Renamed]");
  });
  it("ports a recursive CTE seeded from another CTE with pagination", () => {
    const simple = cte("SimpleCte", { Num: column(sqlType.int32) }, () => select({ Num: 1 }).done());
    const recursive = cte("SimpleFromRecursive", { Num: column(sqlType.int32) }, self => unionAll(select({ Num: simple.Num }).from(simple).done(), select({ Num: add(self.Num, 1) }).from(self).where(lt(self.Num, 10)).done()));
    const sql = toSql(select({ Num: recursive.Num }).from(recursive).orderBy(recursive.Num).offsetFetch(2, 3).done().ast, { dialect: "tsql" });
    expect(sql).toBe("WITH [SimpleCte] AS(SELECT 1 [Num]),[SimpleFromRecursive] AS(SELECT [SimpleCte].[Num] [Num] FROM [SimpleCte] UNION ALL SELECT [SimpleFromRecursive].[Num]+1 [Num] FROM [SimpleFromRecursive] WHERE [SimpleFromRecursive].[Num]<10)SELECT [SimpleFromRecursive].[Num] [Num] FROM [SimpleFromRecursive] ORDER BY [SimpleFromRecursive].[Num] OFFSET 2 ROW FETCH NEXT 3 ROW ONLY");
  });
  it("ports UPDATE joined to a dependent recursive CTE", () => {
    const numbers = cte("SimpleRecursiveCte", { Num: column(sqlType.int32) }, self => unionAll(select({ Num: 1 }).done(), select({ Num: add(self.Num, 1) }).from(self).where(lt(self.Num, 10)).done()));
    const scaled = cte("RefSimpleRecursiveWithOriginalCte", { OriginalNum: column(sqlType.int32), Num: column(sqlType.int32) }, () => select({ OriginalNum: numbers.Num, Num: multiply(numbers.Num, 10) }).from(numbers).where(lt(numbers.Num, 10)).done());
    const target = aliasTable(defineTable({ schema: "dbo", name: "TargetTable", columns: { Val: column(sqlType.int32) } }), "A0");
    const statement = update(target).set({ Val: scaled.Num }).from(target).innerJoin(scaled, eq(scaled.Num, target.Val)).done();
    const tsql = toSql(statement.ast, { dialect: "tsql" }); expect(tsql).toContain("WITH [SimpleRecursiveCte] AS("); expect(tsql).toContain("[RefSimpleRecursiveWithOriginalCte] AS("); expect(tsql).toContain("UPDATE [A0] SET [A0].[Val]=[RefSimpleRecursiveWithOriginalCte].[Num]");
    const mysql = toSql(statement.ast, { dialect: "mysql" }); expect(mysql).toContain("UPDATE `TargetTable` `A0` JOIN (WITH RECURSIVE"); expect(mysql).toContain("SET `A0`.`Val`=");
  });
});
