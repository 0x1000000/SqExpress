import { describe, expect, it } from "vitest";
import {
  lit,
  column,
  compileSql,
  defineTable,
  exprBitwiseAnd,
  exprBitwiseOr,
  exprTypeInt32,
  hour,
  jsonArray,
  jsonObject,
  param,
  parseTSql,
  minute,
  right,
  second,
  select,
  sqlType,
  toExpr,
  toSql,
  year,
} from "../../src/index.js";

describe("dialect exporter slice", () => {
  it.each([true, false])("renders T-SQL Boolean comparisons without a cast: %s", (value) => {
    const users = defineTable({
      schema: "dbo",
      name: "Users",
      columns: { Active: column(sqlType.boolean) },
    });
    expect(select(users.Active).from(users).where(users.Active.eq(value)).toSql("tsql")).toBe(
      `SELECT [Users].[Active] FROM [dbo].[Users] WHERE [Users].[Active]=${value ? 1 : 0}`,
    );
  });
  it("renders T-SQL Boolean literals like SqExpress and preserves other dialects", () => {
    expect(select(true, false).toSql("tsql")).toBe("SELECT 1,0");
    expect(select(true, false).toSql("pgsql")).toBe("SELECT TRUE,FALSE");
    expect(select(true, false).toSql("mysql")).toBe("SELECT 1,0");
    expect(select(true, false).toSql("sqlite")).toBe("SELECT 1,0");
    expect(compileSql(select(true), { dialect: "tsql" })).toEqual({
      sql: "SELECT @p0",
      parameters: [{ name: "p0", value: true, type: "ExprBoolLiteral" }],
    });
  });
  it.each(["tsql", "pgsql", "mysql", "sqlite"] as const)(
    "matches the C# literal fixture for %s",
    (dialect) => expect(toSql(parseTSql("SELECT 1").ast, { dialect })).toBe("SELECT 1"),
  );
  it("quotes identifiers per dialect", () => {
    const ast = parseTSql("SELECT u.Id FROM dbo.Users u").ast;
    expect(toSql(ast, { dialect: "tsql" })).toBe("SELECT [u].[Id] FROM [dbo].[Users] [u]");
    expect(toSql(ast, { dialect: "pgsql" })).toBe('SELECT "u"."Id" FROM "dbo"."Users" "u"');
    expect(toSql(ast, { dialect: "mysql" })).toBe("SELECT `u`.`Id` FROM `Users` `u`");
  });
  it("renders joins and predicates with precedence", () =>
    expect(
      toSql(
        parseTSql(
          "SELECT u.Id FROM Users u JOIN Orders o ON o.UserId=u.Id WHERE u.Id=1 OR u.Id=2 AND o.Id>0",
        ).ast,
        { dialect: "tsql" },
      ),
    ).toBe(
      "SELECT [u].[Id] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id] WHERE [u].[Id]=1 OR [u].[Id]=2 AND [o].[Id]>0",
    ));
  it("renders parser-produced advanced expressions", () => {
    expect(
      toSql(
        parseTSql(
          "SELECT CAST(LOWER(u.Name) AS NVARCHAR(50)) AS Name, CASE WHEN u.Id=1 THEN 'one' ELSE 'other' END AS Label FROM Users u",
        ).ast,
        { dialect: "tsql" },
      ),
    ).toContain("CAST(LOWER([u].[Name]) AS [nvarchar](50))");
    expect(
      toSql(
        parseTSql(
          "SELECT u.Id FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId=u.Id)",
        ).ast,
        { dialect: "pgsql" },
      ),
    ).toContain("EXISTS(SELECT 1");
    expect(toSql(parseTSql("SELECT 1 UNION ALL SELECT 2").ast, { dialect: "sqlite" })).toBe(
      "SELECT 1 UNION ALL SELECT 2",
    );
    expect(
      toSql(parseTSql("WITH A AS (SELECT 1 AS Id) SELECT a.Id FROM A a").ast, { dialect: "tsql" }),
    ).toBe("WITH [A] AS(SELECT 1 [Id])SELECT [a].[Id] FROM [A] [a]");
  });
  it("compiles parameters with dialect placeholders and exact ordered values", () => {
    const ast = select({ value: param(9007199254740993n, "id") });
    expect(compileSql(ast, { dialect: "tsql" })).toEqual({
      sql: "SELECT @id [value]",
      parameters: [{ name: "id", value: 9007199254740993n, type: "ExprInt64Literal" }],
    });
    expect(compileSql(ast, { dialect: "pgsql" }).sql).toBe('SELECT $1 "value"');
    expect(compileSql(select({ ok: param(true) }), { dialect: "mysql" }).parameters[0]?.value).toBe(
      true,
    );
  });
  it("supports every SqExpress parameterization strategy and Boolean shorthand", () => {
    const ast = select(param(7, "value"));
    expect(compileSql(ast, { dialect: "tsql", parameterize: "none" })).toEqual({
      sql: "SELECT 7",
      parameters: [],
    });
    expect(compileSql(ast, { dialect: "tsql", parameterize: false })).toEqual({
      sql: "SELECT 7",
      parameters: [],
    });
    expect(compileSql(ast, { dialect: "tsql", parameterize: true }).parameters).toHaveLength(1);
    expect(
      compileSql(ast, { dialect: "tsql", parameterize: "throw-on-limit" }).parameters,
    ).toHaveLength(1);

    const overLimit = select(Array.from({ length: 2001 }, (_, index) => param(index)));
    expect(() =>
      compileSql(overLimit, { dialect: "tsql", parameterize: "throw-on-limit" }),
    ).toThrow("limit of 2000");
    const fallback = compileSql(overLimit, { dialect: "tsql", parameterize: "literal-fallback" });
    expect(fallback.parameters).toHaveLength(2000);
    expect(fallback.sql.endsWith(",2000")).toBe(true);
  });
  it("returns SQL and parameters from toSql when parameterization is requested", () => {
    const query = select({ id: param(42, "id") });
    expect(query.toSql({ dialect: "tsql", parameterize: "literal-fallback" })).toEqual({
      sql: "SELECT @id [id]",
      parameters: [{ name: "id", value: 42, type: "ExprInt32Literal" }],
    });
    expect(query.toSql({ dialect: "pgsql", parameterize: true })).toEqual({
      sql: 'SELECT $1 "id"',
      parameters: [{ name: "id", value: 42, type: "ExprInt32Literal" }],
    });
  });
  it("parameterizes ordinary literals like SqExpress", () => {
    const compiled = select(17, "value", true).toSql({
      dialect: "pgsql",
      parameterize: "throw-on-limit",
    });
    expect(compiled).toEqual({
      sql: "SELECT $1,$2,$3",
      parameters: [
        { name: "p0", value: 17, type: "ExprInt32Literal" },
        { name: "p1", value: "value", type: "ExprStringLiteral" },
        { name: "p2", value: true, type: "ExprBoolLiteral" },
      ],
    });
  });
  it("preserves C# bitwise AST grouping for dialects with different precedence", () => {
    const expression = exprBitwiseOr({
      left: toExpr(3),
      right: exprBitwiseAnd({ left: toExpr(5), right: toExpr(2) }),
    });
    expect(select(expression).toSql("sqlite")).toBe("SELECT 3|(5&2)");
  });
  it.each(["mysql", "sqlite"] as const)(
    "binds one positional value for every repeated JSON placeholder in %s",
    (dialect) => {
      const query = select({
        Constructed: jsonObject({
          nested: lit('{"ok":true}').jsonQuery(),
          items: jsonArray(1, null),
        }),
        Changed: lit('{"a":1,"remove":2}').jsonSet("$.a", 3).jsonRemove("$.remove"),
      });
      const compiled = query.toSql({ dialect, parameterize: "literal-fallback" });
      expect(compiled.parameters).toHaveLength(
        [...compiled.sql].filter((character) => character === "?").length,
      );
    },
  );
  it("guards typed SQLite JSON extraction against incompatible JSON kinds", () => {
    expect(
      select(lit('{"name":"Ada"}').jsonValue("$.name", exprTypeInt32)).toSql("sqlite"),
    ).toContain("json_type");
  });
  it("extracts typed T-SQL JSON scalars through their containing object", () => {
    expect(
      select(lit('{"active":true}').jsonValue("$.active", exprTypeInt32)).toSql("tsql"),
    ).toContain("FROM OPENJSON('{\"active\":true}','$') WHERE [key]='active'");
    expect(
      select(lit('{"items":[{"id":7}]}').jsonValue("$.items[0].id", exprTypeInt32)).toSql("tsql"),
    ).toContain("FROM OPENJSON('{\"items\":[{\"id\":7}]}','$.items[0]') WHERE [key]='id'");
  });
  it("keeps nested MariaDB JSON values structured", () => {
    expect(
      select(jsonObject({ nested: lit('{"ok":true}').jsonQuery() })).toSql({
        dialect: "mysql",
        mysqlFlavor: "mariadb",
      }),
    ).toContain("JSON_QUERY(CAST(CASE WHEN JSON_TYPE(");
  });
  it("contextually types string operands used by descriptor column methods", () => {
    const entity = defineTable({
      schema: "dbo",
      name: "Entity",
      columns: { Id: column(sqlType.guid) },
    });
    const compiled = select(entity.Id)
      .from(entity)
      .where(entity.Id.eq("58ad8253-4f8f-4c84-b930-4f58a8f25912"))
      .toSql({ dialect: "pgsql", parameterize: true });
    expect(compiled.parameters[0]?.type).toBe("ExprGuidLiteral");
  });
  it("parenthesizes a bound T-SQL TOP value", () => {
    const query = select(1, { top: 2 });
    expect(query.toSql({ dialect: "tsql", parameterize: true }).sql).toContain("TOP (@p0)");
  });
  it("uses MySQL's legal CAST target for integer values", () =>
    expect(select(lit(1).cast(exprTypeInt32)).toSql("mysql")).toBe("SELECT CAST(1 AS SIGNED)"));
  it("adapts portable time-part extraction", () => {
    expect(select(hour("2020-01-01T03:04:05")).toSql("sqlite")).toBe(
      "SELECT CAST(STRFTIME('%H','2020-01-01T03:04:05') AS INTEGER)",
    );
    expect(select(minute("2020-01-01T03:04:05")).toSql("pgsql")).toBe(
      "SELECT CAST(EXTRACT(MINUTE FROM CAST('2020-01-01T03:04:05' AS timestamp)) AS int4)",
    );
    expect(
      select(year("2020-01-01T03:04:05")).toSql({
        dialect: "pgsql",
        strictTemporalTyping: true,
      }),
    ).toBe("SELECT CAST(EXTRACT(YEAR FROM CAST('2020-01-01T03:04:05' AS timestamp)) AS int4)");
    expect(select(second("2020-01-01T03:04:05")).toSql("tsql")).toBe(
      "SELECT DATEPART(SECOND,'2020-01-01T03:04:05')",
    );
  });
  it("expands repeated positional placeholders with matching values", () => {
    const compiled = select(right("abc", 2)).toSql({
      dialect: "sqlite",
      parameterize: true,
    });
    expect(compiled.sql.match(/\?/g)?.length).toBe(compiled.parameters.length);
    expect(compiled.parameters.map((item) => item.value)).toEqual([2, "abc", 2]);
  });
  it("groups limited PostgreSQL set operands", () => {
    const ast = parseTSql("SELECT TOP 2 1 AS Value UNION SELECT TOP 3 2 AS Value").ast;
    expect(toSql(ast, { dialect: "pgsql" })).toBe(
      '(SELECT 1 "Value" LIMIT 2) UNION (SELECT 2 "Value" LIMIT 3)',
    );
  });
  it("reuses a shared literal parameter where PostgreSQL expression identity requires it", () => {
    const value = toExpr(2);
    const compiled = select(value, value).toSql({ dialect: "pgsql", parameterize: true });
    expect(compiled).toEqual({
      sql: "SELECT $1,$1",
      parameters: [{ name: "p0", value: 2, type: "ExprInt32Literal" }],
    });
  });
});
