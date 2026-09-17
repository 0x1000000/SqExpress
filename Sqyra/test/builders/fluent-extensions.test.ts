import { describe, expect, it } from "vitest";
import {
  aggregate,
  caseWhen,
  cast,
  columnExpression,
  derivedTable,
  exists,
  jsonValue,
  column,
  defineTable,
  exprTypeInt32,
  lit,
  mergeInto,
  nullableColumn,
  select,
  serializeAst,
  sqlType,
  stringAgg,
  update,
} from "../../src/index.js";

describe("extended fluent builders", () => {
  const table = defineTable({
    schema: "dbo",
    name: "Users",
    columns: { Id: column(sqlType.int32), Name: nullableColumn(sqlType.string()) },
  });
  it("uses physical and derived columns directly", () => {
    expect(select(table.Id.as("id"), table.Name.as("name")).from(table).toSql("tsql")).toContain(
      "[Id] [id]",
    );
    expect(table.Id.add(1).multiply(2).gt(0).and(table.Name.like("A%")).kind).toBe(
      "ExprBooleanAnd",
    );
    expect(table.Id.inList(1, 2).or(table.Id.inQuery(select(1))).kind).toBe("ExprBooleanOr");
    expect(serializeAst(table.Id.desc())).toBe(serializeAst(columnExpression(table.Id).desc()));
    const projected = select({ Id: table.Id }).from(table);
    expect(select(projected.Id.add(1).as("next")).from(projected).toSql("tsql")).toContain(
      "[A0].[Id]+1",
    );
    expect(Object.keys(table.Id)).toEqual(["name", "sourceAlias", "nullable"]);
    expect(Object.isFrozen(table.Id)).toBe(true);
  });
  it("builds immutable CASE chains with legacy-equivalent SQL", () => {
    const first = caseWhen(table.Id.eq(1)).then("one");
    const extended = first.when(table.Id.eq(2)).then("two");
    expect(serializeAst(extended.else("other"))).toBe(
      serializeAst(caseWhen([table.Id.eq(1), "one"], [table.Id.eq(2), "two"]).else("other")),
    );
    expect(select(first.else(null).as("label")).from(table).toSql("tsql")).not.toContain("'two'");
    expect(extended.else("other").as("label").kind).toBe("ExprAliasedSelecting");
  });
  it("supports scalar cast and JSON transforms", () => {
    expect(serializeAst(lit("1").cast(exprTypeInt32))).toBe(serializeAst(cast("1", exprTypeInt32)));
    expect(serializeAst(lit('{"id":1}').jsonValue("$.id"))).toBe(
      serializeAst(jsonValue('{"id":1}', "$.id")),
    );
    expect(table.Name.jsonQuery().jsonSet("$.id", 1).jsonRemove("$.id").kind).toBe(
      "ExprJsonRemove",
    );
    expect(() => lit("{}").jsonValue("invalid")).toThrow(/Invalid portable JSON path/);
    expect(() => lit("{}").jsonRemove("$")).toThrow(/root/);
  });
  it("converts queries through exists and as", () => {
    const query = select({ Id: table.Id }).from(table);
    expect(serializeAst(query.exists())).toBe(serializeAst(exists(query)));
    expect(serializeAst(query.as("d").$metadata.source)).toBe(
      serializeAst(derivedTable(query, "d").$metadata.source),
    );
    const typed = query.as("d", { Id: column(sqlType.int32) });
    expect(select(typed.Id.as("id")).from(typed).toSql("tsql")).toContain("[d].[Id]");
    const collisions = select({ as: 1, exists: 2, modify: 3 });
    expect(collisions.$as.name).toBe("as");
    expect(collisions.$exists.name).toBe("exists");
    expect(collisions.modify.name).toBe("modify");
    expect(collisions.$ast.modify((node) => node).exists().kind).toBe("ExprExists");
    expect(() => query.as("d", {})).toThrow(/Number of declared columns/);
  });
  it("configures window expressions without mutating earlier stages", () => {
    const base = aggregate("SUM", table.Id).over();
    const partitioned = base.partitionBy(table.Name);
    const ordered = partitioned.orderBy(table.Id.desc());
    expect(serializeAst(ordered)).toBe(
      serializeAst(
        aggregate("SUM", table.Id).over({ partitionBy: [table.Name], orderBy: [table.Id.desc()] }),
      ),
    );
    expect(select(base).from(table).toSql("tsql")).not.toContain("PARTITION BY");
    expect(select(ordered.as("total")).from(table).toSql("tsql")).toContain("PARTITION BY");
    const stringBase = stringAgg(table.Name, ",");
    expect(serializeAst(stringBase.orderBy(table.Id.desc()))).toBe(
      serializeAst(stringAgg(table.Name, ",", table.Id.desc())),
    );
    expect(select(stringBase).from(table).toSql("pgsql")).not.toContain("ORDER BY");
  });
  it("exports MERGE and OUTPUT without done", () => {
    const source = table.as("s");
    const merge = mergeInto(table, source).on(table.Id.eq(source.Id));
    expect(() => merge.toSql("tsql")).toThrow(/at least one action/);
    merge.whenMatchedUpdate({ Name: source.Name });
    expect(merge.toSql("tsql")).toBe(merge.done().toSql("tsql"));
    expect(merge.output({ inserted: [table.Id] }).toSql("tsql")).toBe(
      merge
        .done()
        .output({ inserted: [table.Id] })
        .toSql("tsql"),
    );
    expect(merge.toSql({ dialect: "tsql", parameterize: true }).sql).toContain("MERGE");
  });
  it("exposes fluent AST operations without changing enumerable AST fields", () => {
    const query = select({ answer: 1 });
    expect(query.$ast.find((node) => node.kind === "ExprInt32Literal")?.kind).toBe(
      "ExprInt32Literal",
    );
    expect([...query.$ast.walk()].map((node) => node.kind)).toContain("ExprInt32Literal");
    expect([...query.$ast.descendants()]).toHaveLength([...query.$ast.walk()].length - 1);
    expect([...query.$ast.descendantsOfType("ExprInt32Literal")]).toHaveLength(1);
    expect([
      ...query.$ast.descendantsOfType("ExprInt32Literal", (node) => node.value === 1),
    ]).toHaveLength(1);
    expect([...query.$ast.walkWithParent()][0]?.parent).toBeNull();
    expect(query.$ast.serialize()).toBe(serializeAst(query));
    expect(Object.keys(query)).not.toContain("$ast");

    const changed = query.$ast.modify((node) =>
      node.kind === "ExprInt32Literal" ? lit(42) : node,
    );
    expect(changed.toSql("pgsql")).toBe('SELECT 42 "answer"');
    expect(lit(1).$ast.modify(() => lit(2)).kind).toBe("ExprInt32Literal");
    expect(
      lit(1)
        .eq(1)
        .$ast.find((node) => node.kind === "ExprBooleanEq")?.kind,
    ).toBe("ExprBooleanEq");

    const statement = update(table).set({ Name: "Ada" });
    expect(statement.$ast.find((node) => node.kind === "ExprUpdate")?.kind).toBe("ExprUpdate");
    expect(statement.$ast.modify((node) => node).toSql("tsql")).toBe(statement.toSql("tsql"));
    expect(Object.keys(statement)).not.toContain("$ast");
  });
});
