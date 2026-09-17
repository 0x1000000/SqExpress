import { describe, expect, it } from "vitest";
import { parseTSql, toSql } from "../../src/index.js";

describe("DML parser slice", () => {
  it("parses and exports INSERT VALUES", () => {
    const ast = parseTSql("INSERT INTO dbo.Users(Id,Name) VALUES(1,'A'),(2,NULL)").ast;
    expect(ast.kind).toBe("ExprInsert");
    expect(toSql(ast, { dialect: "tsql" })).toBe(
      "INSERT INTO [dbo].[Users]([Id],[Name]) VALUES (1,'A'),(2,NULL)",
    );
  });
  it("parses and exports INSERT SELECT", () => {
    const ast = parseTSql(
      "INSERT INTO dbo.Archive(Id) SELECT u.Id FROM dbo.Users u WHERE u.Id>0",
    ).ast;
    expect(ast).toMatchObject({ kind: "ExprInsert", source: { kind: "ExprInsertQuery" } });
    expect(toSql(ast, { dialect: "pgsql" })).toContain(
      'INSERT INTO "dbo"."Archive"("Id") SELECT "u"."Id"',
    );
  });
  it("parses and exports UPDATE", () => {
    const ast = parseTSql("UPDATE dbo.Users SET Name='A' WHERE Id=1").ast;
    expect(ast.kind).toBe("ExprUpdate");
    expect(toSql(ast, { dialect: "tsql" })).toContain("SET [Name]='A' WHERE [Id]=1");
  });
  it("parses and exports DELETE", () => {
    const ast = parseTSql("DELETE FROM dbo.Users WHERE Id=1").ast;
    expect(ast.kind).toBe("ExprDelete");
    expect(toSql(ast, { dialect: "tsql" })).toBe("DELETE [dbo].[Users] WHERE [Id]=1");
  });
  it("parses MERGE update, insert, and source-delete actions", () => {
    const ast = parseTSql(
      "MERGE Users t USING UsersStaging s ON t.UserId=s.UserId WHEN MATCHED THEN UPDATE SET t.Name=s.Name WHEN NOT MATCHED THEN INSERT(UserId,Name) VALUES(s.UserId,s.Name) WHEN NOT MATCHED BY SOURCE THEN DELETE;",
    ).ast;
    expect(ast).toMatchObject({
      kind: "ExprMerge",
      whenMatched: { kind: "ExprMergeMatchedUpdate" },
      whenNotMatchedByTarget: { kind: "ExprExprMergeNotMatchedInsert" },
      whenNotMatchedBySource: { kind: "ExprMergeMatchedDelete" },
    });
    expect(toSql(ast, { dialect: "tsql" })).toContain("WHEN NOT MATCHED BY SOURCE THEN  DELETE;");
  });
});
