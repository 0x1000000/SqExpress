import { describe, expect, it } from "vitest";
import {
  add,
  aggregate,
  and,
  column,
  defineTable,
  deleteFrom,
  eq,
  forJson,
  inList,
  insertInto,
  jsonValue,
  lit,
  scalarSubquery,
  select,
  serializeAst,
  sqlType,
  unionAll,
  update,
  walk,
} from "../../src/index.js";

describe("fluent builders and standalone compatibility", () => {
  const users = defineTable({
    name: "Users",
    schema: "dbo",
    columns: { Id: column(sqlType.int32) },
  });
  it("keeps query combinators equivalent across dialects", () => {
    const left = select({ value: 1 });
    const right = select({ value: 2 });
    expect(serializeAst(left.unionAll(right))).toBe(serializeAst(unionAll(left, right)));
    expect(serializeAst(left.forJson())).toBe(serializeAst(forJson(left)));
    expect(serializeAst(left.scalarSubquery())).toBe(serializeAst(scalarSubquery(left)));
    for (const dialect of ["tsql", "pgsql", "mysql", "sqlite"] as const) {
      expect(left.forJson().toSql(dialect)).toBe(forJson(left).toSql(dialect));
      expect(left.unionAll(right).toSql(dialect)).toBe(unionAll(left, right).toSql(dialect));
    }
    expect(left.toSql("tsql")).toBe("SELECT 1 [value]");
    expect(left.union(right).intersect(left).except(right).kind).toBe("ExprQueryExpression");
  });
  it("orders and paginates set results without altering the operands", () => {
    const query = select(1).unionAll(select(2));
    expect(query.orderBy(lit(1).desc()).offsetFetch(0, 2).toSql("tsql")).toContain(
      "ORDER BY 1 DESC OFFSET 0 ROW FETCH NEXT 2 ROW ONLY",
    );
    expect(query.orderByList([lit(1).asc()]).toSql("pgsql")).toContain("ORDER BY 1");
    expect(() => query.orderBy()).toThrow(/at least one/);
    expect(() => select(1).from(users).orderBy(users.Id).forJson()).toThrow();
  });
  it("decorates scalar expressions and every predicate", () => {
    const value = lit(1).add(2).multiply(3).subtract(4).divide(5).modulo(2);
    expect(value.eq(1).or(value.neq(2)).not().kind).toBe("ExprBooleanNot");
    expect(serializeAst(lit(1).add(2))).toBe(serializeAst(add(1, 2)));
    expect(serializeAst(lit(1).eq(2).and(lit(1).inList(1, 2)))).toBe(
      serializeAst(and(eq(1, 2), inList(1, 1, 2))),
    );
    expect(jsonValue('{"x":1}', "$.x").gt(0).and(lit("x").like("%")).kind).toBe("ExprBooleanAnd");
    expect(lit(1).inQuery(select(1)).not().kind).toBe("ExprBooleanNot");
    expect(select(lit("a").concat("b").as("text")).toSql("tsql")).toContain("[text]");
    expect(select(aggregate("SUM", 1).as("total")).toSql("tsql")).toContain("SUM(1)");
    expect(select(aggregate("SUM", 1).over().as("total")).toSql("tsql")).toContain("OVER()");
  });
  it("exports executable DML stages and preserves compatibility aliases", () => {
    const insert = insertInto(users).values({ Id: 1 });
    expect(insert.toSql("tsql")).toContain("INSERT INTO");
    expect(insert.output(users.Id).toSql("tsql")).toContain("OUTPUT");
    expect(insert.identity("Id").toSql("tsql")).toContain("IDENTITY_INSERT");
    const change = update(users).set({ Id: 2 });
    expect(change.toSql("tsql")).toBe(change.done().toSql("tsql"));
    expect(change.from(users).toSql("tsql")).toBe(change.from(users).done().toSql("tsql"));
    expect(
      change.where(users.Id.eq(1)).toSql({ dialect: "tsql", parameterize: true }).sql,
    ).toContain("UPDATE");
    const remove = deleteFrom(users);
    expect(remove.toSql("tsql")).toBe(remove.done().toSql("tsql"));
    expect(remove.where(users.Id.eq(1)).output(users.Id).toSql("tsql")).toContain("OUTPUT");
  });
  it("escapes fluent member collisions and keeps AST operations unchanged", () => {
    const query = select({ forJson: 1, union: 2, scalarSubquery: 3 });
    expect(query.$forJson.name).toBe("forJson");
    expect(query.$union.name).toBe("union");
    expect(query.$scalarSubquery.name).toBe("scalarSubquery");
    expect(query.forJson({ withoutArrayWrapper: true, includeNullValues: false }).kind).toBe(
      "ExprQueryAsJson",
    );
    expect(query.$ast.modify((node) => node).forJson().kind).toBe("ExprQueryAsJson");
    expect([...walk(query)].length).toBeGreaterThan(3);
    expect(JSON.stringify(query)).not.toContain("toSql");
    expect(() => Reflect.set(query, "union", null)).not.toThrow();
    expect(Reflect.set(query, "union", null)).toBe(false);
  });
});
