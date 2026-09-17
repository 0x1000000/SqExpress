import { describe, expect, it } from "vitest";
import {
  bindTSqlParameters,
  column,
  defineTable,
  parseTSql,
  sqlType,
  toSql,
  tryParseTSql,
} from "../../src/index.js";

describe("public T-SQL parser", () => {
  it("immutably binds every named parser parameter", () => {
    const parsed = parseTSql("SELECT @value AS Value WHERE @id = 7").ast;
    const bound = bindTSqlParameters(parsed, { "@value": "Hello", id: 7 });
    expect(toSql(bound, "tsql")).toBe("SELECT 'Hello' [Value] WHERE 7=7");
    expect(toSql(parsed, "tsql")).toBe("SELECT @value [Value] WHERE @id=7");
    expect(() => bindTSqlParameters(parsed, { value: "missing id" })).toThrow(
      "Could not find parameter id",
    );
  });
  it("parses a select literal into generated AST", () =>
    expect(parseTSql("SELECT 1").ast).toMatchObject({
      kind: "ExprQuerySpecification",
      selectList: [{ kind: "ExprInt32Literal", value: 1 }],
      distinct: false,
    }));
  it("preserves explicit null default schema", () =>
    expect(parseTSql("SELECT u.Id FROM Users u", { defaultSchema: null }).tables[0]).toMatchObject({
      schema: null,
      name: "Users",
      alias: "u",
    }));
  it("parses predicates and named parameters", () =>
    expect(parseTSql("SELECT u.Id FROM Users u WHERE u.Id=@id").ast).toMatchObject({
      where: { kind: "ExprBooleanEq", right: { kind: "ExprParameter", tagName: "id" } },
    }));
  it("rejects trailing unsupported input", () => {
    const result = tryParseTSql("SELECT 1 garbage more");
    expect(result.success).toBe(false);
  });
  it("returns lexer diagnostics unchanged", () => {
    const result = tryParseTSql("SELECT 'x");
    expect(result.success ? null : result.error.message).toBe(
      "Syntax error: unterminated string literal.",
    );
  });
  it("preserves Boolean precedence and comparison operators", () =>
    expect(
      parseTSql("SELECT u.Id FROM Users u WHERE u.Id>=1 OR u.Id<3 AND u.Id<>2").ast,
    ).toMatchObject({ where: { kind: "ExprBooleanOr", right: { kind: "ExprBooleanAnd" } } }));
  it("parses joins, aliases, arithmetic, IN, LIKE, and IS NULL", () => {
    expect(
      parseTSql(
        "SELECT u.Id+1 AS NextId FROM Users u INNER JOIN Orders o ON o.UserId=u.Id WHERE u.Name LIKE 'A%' AND o.Code IN (1,2) OR u.Name IS NULL",
      ).ast,
    ).toMatchObject({
      from: { kind: "ExprJoinedTable" },
      selectList: [{ kind: "ExprAliasedSelecting", value: { kind: "ExprSum" } }],
    });
  });
  it("parses DISTINCT, TOP, grouping, ordering, and pagination", () =>
    expect(
      parseTSql(
        "SELECT DISTINCT TOP (10) u.Id FROM Users u GROUP BY u.Id ORDER BY u.Id DESC OFFSET 2 ROWS FETCH NEXT 3 ROWS ONLY",
      ).ast,
    ).toMatchObject({
      kind: "ExprSelectOffsetFetch",
      selectQuery: { distinct: true, top: { value: 10 }, groupBy: [{ kind: "ExprColumn" }] },
    }));
  it("extracts columns and validates supplied descriptors", () => {
    const users = defineTable({
      schema: "dbo",
      name: "Users",
      columns: { Id: column(sqlType.int32) },
    });
    expect(
      parseTSql("SELECT u.Id FROM Users u", { existingTables: [users] }).tables[0]?.columns,
    ).toEqual(["Id"]);
    const bad = tryParseTSql("SELECT u.Missing FROM Users u", { existingTables: [users] });
    expect(bad.success ? null : bad.error.message).toContain("Column 'Missing'");
  });
  it("ports scalar functions, CAST, and searched CASE expressions", () => {
    const result = parseTSql(
      "SELECT CAST(LOWER(u.Name) AS NVARCHAR(50)) AS Name, CASE WHEN u.Id=1 THEN 'one' ELSE 'other' END AS Label FROM Users u",
    );
    expect(result.ast).toMatchObject({
      selectList: [
        {
          value: {
            kind: "ExprCast",
            expression: { kind: "ExprPortableScalarFunction" },
            sqlType: { kind: "ExprTypeString", size: 50 },
          },
        },
        { value: { kind: "ExprCase" } },
      ],
    });
  });
  it("maps portable, null-handling, date, and aggregate functions to semantic nodes", () => {
    expect(
      parseTSql(
        "SELECT LOWER(Name), ISNULL(Name,'x'), COALESCE(Name,'x','y'), GETDATE(), COUNT(DISTINCT Id) FROM Users GROUP BY Name",
      ).ast,
    ).toMatchObject({
      selectList: [
        { kind: "ExprPortableScalarFunction", portableFunction: "Lower" },
        { kind: "ExprFuncIsNull" },
        { kind: "ExprFuncCoalesce" },
        { kind: "ExprGetDate" },
        { kind: "ExprAggregateFunction", isDistinct: true },
      ],
    });
  });
  it("maps BETWEEN and fails closed on ambiguous or duplicate bindings", () => {
    expect(parseTSql("SELECT u.Id FROM Users u WHERE u.Id NOT BETWEEN 1 AND 3").ast).toMatchObject({
      where: { kind: "ExprBooleanNot", expr: { kind: "ExprBooleanAnd" } },
    });
    expect(tryParseTSql("SELECT Id FROM Users u JOIN Orders o ON u.Id=o.UserId").success).toBe(
      false,
    );
    expect(tryParseTSql("SELECT u.Id FROM Users u JOIN Orders u ON u.Id=u.UserId").success).toBe(
      false,
    );
  });
  it("ports simple CASE, scalar subqueries, unary arithmetic, and bitwise operators", () => {
    expect(
      parseTSql(
        "SELECT CASE u.Id WHEN 1 THEN 'one' ELSE 'other' END, (SELECT 1), -u.Id, u.Id&3|4^1 FROM Users u",
      ).ast,
    ).toMatchObject({
      selectList: [
        { kind: "ExprCase", cases: [{ condition: { kind: "ExprBooleanEq" } }] },
        { kind: "ExprValueQuery" },
        { kind: "ExprAliasedSelecting", value: { kind: "ExprSub" } },
        { kind: "ExprBitwiseXor" },
      ],
    });
  });
  it("ports aggregate and analytic windows", () => {
    expect(
      parseTSql(
        "SELECT SUM(o.Total) OVER (PARTITION BY o.UserId ORDER BY o.Id), ROW_NUMBER() OVER (ORDER BY o.Id) FROM Orders o",
      ).ast,
    ).toMatchObject({
      selectList: [{ kind: "ExprAggregateOverFunction" }, { kind: "ExprAnalyticFunction" }],
    });
  });
  it("ports STRING_AGG with optional WITHIN GROUP ordering", () => {
    const parsed = parseTSql(
      "SELECT STRING_AGG(u.Name,',') WITHIN GROUP (ORDER BY u.Name DESC) AS Names FROM Users u",
    );
    expect(parsed.ast).toMatchObject({
      selectList: [
        { value: { kind: "ExprStringAgg", orderBy: { orderList: [{ descendant: true }] } } },
      ],
    });
    expect(toSql(parsed.ast, { dialect: "tsql" })).toBe(
      "SELECT STRING_AGG([u].[Name],',') WITHIN GROUP (ORDER BY [u].[Name] DESC) [Names] FROM [dbo].[Users] [u]",
    );
    expect(toSql(parsed.ast, { dialect: "pgsql" })).toContain("STRING_AGG");
    for (const dialect of ["mysql", "sqlite"] as const)
      expect(toSql(parsed.ast, { dialect })).toContain("GROUP_CONCAT");
    expect(tryParseTSql("SELECT STRING_AGG(Name) FROM Users").success).toBe(false);
    expect(tryParseTSql("SELECT STRING_AGG(DISTINCT Name,',') FROM Users").success).toBe(false);
  });
  it("ports portable JSON scalar and construction functions fail-closed", () => {
    expect(
      parseTSql(
        "SELECT JSON_VALUE('{\"a\":1}','$.a'), JSON_QUERY('{}','$.a'), JSON_MODIFY('{}','$.a',1), JSON_ARRAY(1,'a',NULL NULL ON NULL), JSON_OBJECT('a':1,'b':'x' NULL ON NULL)",
      ).ast,
    ).toMatchObject({
      selectList: [
        { kind: "ExprJsonValue" },
        { kind: "ExprJsonQuery" },
        { kind: "ExprJsonSet" },
        { kind: "ExprJsonArray" },
        { kind: "ExprJsonObject" },
      ],
    });
    expect(tryParseTSql("SELECT JSON_VALUE('{}',@path)").success).toBe(false);
    expect(tryParseTSql("SELECT JSON_OBJECT('a':1,'a':2)").success).toBe(false);
  });
  it("ports FOR JSON PATH options", () =>
    expect(
      parseTSql("SELECT 1 AS [id] FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER").ast,
    ).toMatchObject({
      kind: "ExprQueryAsJson",
      includeNullValues: true,
      withoutArrayWrapper: true,
    }));
  it("ports typed OPENJSON and rejects bare OPENJSON", () => {
    expect(
      parseTSql(
        "SELECT j.Id,j.Data FROM OPENJSON('[{\"id\":1}]','$') WITH (Id int '$.id',Data nvarchar(max) '$.data' AS JSON) j",
      ).ast,
    ).toMatchObject({
      from: {
        kind: "ExprJsonTable",
        columns: [{ kind: "ExprJsonTableValueColumn" }, { kind: "ExprJsonTableQueryColumn" }],
      },
    });
    expect(tryParseTSql("SELECT j.value FROM OPENJSON('[]') j").success).toBe(false);
  });
  it("ports comma joins and VALUES-derived tables", () => {
    expect(
      parseTSql("SELECT u.Id,o.Id FROM Users u,Orders o WHERE u.Id=o.UserId").ast,
    ).toMatchObject({ from: { kind: "ExprCrossedTable" } });
    expect(parseTSql("SELECT v.Id FROM (VALUES (1),(2)) v(Id)").ast).toMatchObject({
      from: {
        kind: "ExprDerivedTableValues",
        values: { items: [{ items: [{ value: 1 }] }, { items: [{ value: 2 }] }] },
      },
    });
  });
  it("ports EXISTS, IN subqueries, derived tables, and APPLY", () => {
    expect(
      parseTSql(
        "SELECT u.Id FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId=u.Id)",
      ).ast,
    ).toMatchObject({ where: { kind: "ExprExists" } });
    expect(
      parseTSql("SELECT u.Id FROM Users u WHERE u.Id NOT IN (SELECT o.UserId FROM Orders o)").ast,
    ).toMatchObject({ where: { kind: "ExprBooleanNot", expr: { kind: "ExprInSubQuery" } } });
    expect(parseTSql("SELECT d.Id FROM (SELECT u.Id FROM Users u) d").ast).toMatchObject({
      from: { kind: "ExprDerivedTableQuery" },
    });
    expect(
      parseTSql(
        "SELECT u.Id FROM Users u OUTER APPLY (SELECT o.UserId FROM Orders o WHERE o.UserId=u.Id) d",
      ).ast,
    ).toMatchObject({ from: { kind: "ExprLateralCrossedTable", outer: true } });
    expect(
      tryParseTSql(
        "SELECT u.Id FROM Users u CROSS JOIN (SELECT o.UserId FROM Orders o WHERE o.UserId=u.Id) d",
      ).success,
    ).toBe(false);
  });
  it.each([
    ["UNION", "Union"],
    ["UNION ALL", "UnionAll"],
    ["INTERSECT", "Intersect"],
    ["EXCEPT", "Except"],
  ] as const)("ports set operation %s", (operator, kind) =>
    expect(parseTSql(`SELECT 1 ${operator} SELECT 2`).ast).toMatchObject({
      kind: "ExprQueryExpression",
      queryExpressionType: kind,
    }),
  );
  it("ports parenthesized and deeply nested set-operation shapes", () => {
    expect(parseTSql("SELECT 1 UNION ALL (SELECT 2 INTERSECT SELECT 2)").ast).toMatchObject({
      kind: "ExprQueryExpression",
      right: { kind: "ExprQueryExpression" },
    });
    expect(parseTSql("(SELECT 1 UNION ALL SELECT 2) ORDER BY 1").ast).toMatchObject({
      kind: "ExprSelect",
      selectQuery: { kind: "ExprQueryExpression" },
    });
    expect(
      parseTSql(
        "((SELECT 1 EXCEPT SELECT 2) INTERSECT (SELECT 1 UNION SELECT 3)) UNION ALL SELECT 4",
      ).ast,
    ).toMatchObject({ kind: "ExprQueryExpression", left: { kind: "ExprQueryExpression" } });
    expect(
      parseTSql("(SELECT 1 UNION ALL SELECT 2) ORDER BY 1 OFFSET 0 ROW FETCH NEXT 1 ROW ONLY").ast,
    ).toMatchObject({ kind: "ExprSelectOffsetFetch" });
  });
  it("ports non-recursive CTEs and keeps physical table artifacts", () => {
    const result = parseTSql(
      "WITH ActiveUsers AS (SELECT u.Id FROM Users u WHERE u.Id>0) SELECT a.Id FROM ActiveUsers a",
    );
    expect(result.ast).toMatchObject({
      from: { kind: "ExprCteQuery", name: "ActiveUsers", alias: { alias: { name: "a" } } },
    });
    expect(result.tables.some((table) => table.name === "Users")).toBe(true);
  });
});
