import { describe, expect, it } from "vitest";
import { column, defineTable, jsonOutput, select, selectJson, sqlType } from "../../src/index.js";

describe("nested FOR JSON value queries", () => {
  it("accepts a JSON output query as a scalar subquery", () => {
    const nested = selectJson(jsonOutput(1, "$.id")).forJson().scalarSubquery();
    expect(nested.kind).toBe("ExprValueQuery");
    if (nested.kind !== "ExprValueQuery")
      throw new Error("Expected a scalar value-query AST node.");
    expect(nested.query.kind).toBe("ExprQueryAsJson");
    const sql = selectJson(jsonOutput(nested, "$.book")).forJson().toSql("sqlite");
    expect(sql).toContain("json_group_array");
    expect(sql).toContain("'book'");
    expect(sql).toContain('json(J0."book")');
  });
  it("keeps outer correlation visible in MariaDB nested JSON SQL", () => {
    const parent = defineTable({
      schema: null,
      name: "Parent",
      columns: { Id: column(sqlType.int32) },
    });
    const child = defineTable({
      schema: null,
      name: "Child",
      columns: { ParentId: column(sqlType.int32), Id: column(sqlType.int32) },
    });
    const children = selectJson(jsonOutput(child.Id, "$.id"))
      .from(child)
      .where(child.ParentId.eq(parent.Id));
    const parents = select(
      parent.Id,
      jsonOutput(children.forJson().scalarSubquery(), "$.children"),
    ).from(parent);
    const sql = parents.forJson().toSql({ dialect: "mysql", mysqlFlavor: "mariadb" });
    expect(sql).toContain("FROM `Child` WHERE `Child`.`ParentId`=`Parent`.`Id`");
    expect(sql).toContain("JSON_ARRAYAGG(JSON_OBJECT('id',`Child`.`Id`))");
  });
});
