import { describe, expect, it } from "vitest";
import { compileSql, param, parseTSql, select, toSql } from "../../src/index.js";

describe("dialect exporter slice", () => {
  it.each(["tsql", "postgresql", "mysql", "sqlite"] as const)("matches the C# literal fixture for %s", (dialect) => expect(toSql(parseTSql("SELECT 1").ast, { dialect })).toBe("SELECT 1"));
  it("quotes identifiers per dialect", () => {
    const ast = parseTSql("SELECT u.Id FROM dbo.Users u").ast;
    expect(toSql(ast, { dialect: "tsql" })).toBe("SELECT [u].[Id] FROM [dbo].[Users] [u]");
    expect(toSql(ast, { dialect: "postgresql" })).toBe('SELECT "u"."Id" FROM "dbo"."Users" "u"');
    expect(toSql(ast, { dialect: "mysql" })).toBe("SELECT `u`.`Id` FROM `Users` `u`");
  });
  it("renders joins and predicates with precedence", () => expect(toSql(parseTSql("SELECT u.Id FROM Users u JOIN Orders o ON o.UserId=u.Id WHERE u.Id=1 OR u.Id=2 AND o.Id>0").ast, { dialect: "tsql" })).toBe("SELECT [u].[Id] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id] WHERE [u].[Id]=1 OR [u].[Id]=2 AND [o].[Id]>0"));
  it("renders parser-produced advanced expressions", () => {
    expect(toSql(parseTSql("SELECT CAST(LOWER(u.Name) AS NVARCHAR(50)) AS Name, CASE WHEN u.Id=1 THEN 'one' ELSE 'other' END AS Label FROM Users u").ast, { dialect: "tsql" })).toContain("CAST(LOWER([u].[Name]) AS [nvarchar](50))");
    expect(toSql(parseTSql("SELECT u.Id FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId=u.Id)").ast, { dialect: "postgresql" })).toContain("EXISTS(SELECT 1");
    expect(toSql(parseTSql("SELECT 1 UNION ALL SELECT 2").ast, { dialect: "sqlite" })).toBe("SELECT 1 UNION ALL SELECT 2");
    expect(toSql(parseTSql("WITH A AS (SELECT 1 AS Id) SELECT a.Id FROM A a").ast, { dialect: "tsql" })).toBe("WITH [A] AS(SELECT 1 [Id])SELECT [a].[Id] FROM [A] [a]");
  });
  it("compiles parameters with dialect placeholders and exact ordered values", () => {
    const ast = select({ value: param(9007199254740993n, "id") }).done().ast;
    expect(compileSql(ast, { dialect: "tsql" })).toEqual({ sql: "SELECT @id [value]", parameters: [{ name: "id", value: 9007199254740993n, type: "ExprInt64Literal" }] });
    expect(compileSql(ast, { dialect: "postgresql" }).sql).toBe('SELECT $1 "value"');
    expect(compileSql(select({ ok: param(true) }).done().ast, { dialect: "mysql" }).parameters[0]?.value).toBe(true);
  });
});
