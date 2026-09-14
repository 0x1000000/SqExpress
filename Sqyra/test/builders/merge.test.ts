import { describe, expect, it } from "vitest";
import { add, aliasTable, column, columnExpression, defaultValue, defineTable, dynamicDerivedTable, eq, exprAliasedSelecting, exprColumn, exprColumnAlias, exprColumnName, exprGetUtcDate, exprInt32Literal, exprQuerySpecification, exprStringLiteral, gt, lit, mergeInto, neq, sqlType, toSql, values } from "../../src/index.js";

const user = defineTable({ schema: "dbo", name: "user", columns: {
  UserId: column(sqlType.int32), FirstName: column(sqlType.string(255)), Modified: column(sqlType.dateTime)
} });

describe("MERGE builder", () => {
  it("builds update, insert, and source-missing delete actions", () => {
    const target = aliasTable(user, "T");
    const source = aliasTable(user, "S");
    const statement = mergeInto(target, source)
      .on(eq(target.columns.UserId, source.columns.UserId))
      .whenMatchedUpdate({ FirstName: source.columns.FirstName })
      .whenNotMatchedInsert({ UserId: source.columns.UserId, FirstName: source.columns.FirstName })
      .whenNotMatchedBySourceDelete()
      .done();
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("MERGE [dbo].[user] [T] USING [dbo].[user] [S] ON [T].[UserId]=[S].[UserId] WHEN MATCHED THEN UPDATE SET [T].[FirstName]=[S].[FirstName] WHEN NOT MATCHED THEN INSERT([UserId],[FirstName]) VALUES([S].[UserId],[S].[FirstName]) WHEN NOT MATCHED BY SOURCE THEN  DELETE;");
  });

  it("supports guarded deletes and INSERT DEFAULT VALUES", () => {
    const target = aliasTable(user, "T");
    const source = aliasTable(user, "S");
    const statement = mergeInto(target, source).on(eq(target.columns.UserId, source.columns.UserId))
      .whenMatchedDelete(eq(source.columns.FirstName, "deleted"))
      .whenNotMatchedInsertDefault(eq(source.columns.UserId, 7)).done();
    expect(toSql(statement.ast, { dialect: "tsql" })).toContain("WHEN MATCHED AND [S].[FirstName]='deleted' THEN  DELETE WHEN NOT MATCHED AND [S].[UserId]=7 THEN INSERT DEFAULT VALUES");
  });

  it("rejects an actionless merge and empty assignments", () => {
    const target = aliasTable(user, "T"); const source = aliasTable(user, "S");
    const builder = mergeInto(target, source).on(eq(target.columns.UserId, source.columns.UserId));
    expect(() => builder.done()).toThrow(/action/);
    expect(() => builder.whenMatchedUpdate({})).toThrow(/assignment/);
    expect(lit(1).kind).toBe("ExprInt32Literal");
  });
  it("ports a value-table MERGE with all action families", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string(255)), Modified: column(sqlType.dateTime), Created: column(sqlType.dateTime) } }), "A0");
    const source = values([[1, "Alice"], [2, "Bob"]], "A1", { UserId: column(sqlType.int32), FirstName: column(sqlType.string(255)) });
    const statement = mergeInto(target, source).on(eq(target.UserId, source.UserId))
      .whenMatchedUpdate({ FirstName: source.FirstName, Modified: exprGetUtcDate }, neq(target.FirstName, source.FirstName))
      .whenNotMatchedInsert({ UserId: source.UserId, FirstName: source.FirstName, Modified: exprGetUtcDate, Created: exprGetUtcDate })
      .whenNotMatchedBySourceUpdate({ Modified: exprGetUtcDate }).done();
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("MERGE [dbo].[user] [A0] USING (VALUES (1,'Alice'),(2,'Bob'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId] WHEN MATCHED AND [A0].[FirstName]!=[A1].[FirstName] THEN UPDATE SET [A0].[FirstName]=[A1].[FirstName],[A0].[Modified]=GETUTCDATE() WHEN NOT MATCHED THEN INSERT([UserId],[FirstName],[Modified],[Created]) VALUES([A1].[UserId],[A1].[FirstName],GETUTCDATE(),GETUTCDATE()) WHEN NOT MATCHED BY SOURCE THEN UPDATE SET [A0].[Modified]=GETUTCDATE();");
  });
  it("ports matched and source-missing MERGE deletes", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string(255)) } }), "A0");
    const source = values([[1, "Alice"], [2, "Bob"]], "A1", { UserId: column(sqlType.int32), FirstName: column(sqlType.string(255)) });
    const statement = mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenMatchedDelete().whenNotMatchedBySourceDelete(gt(target.UserId, 100)).done();
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("MERGE [dbo].[user] [A0] USING (VALUES (1,'Alice'),(2,'Bob'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId] WHEN MATCHED THEN  DELETE WHEN NOT MATCHED BY SOURCE AND [A0].[UserId]>100 THEN  DELETE;");
  });
  it("ports default and explicit MERGE insert/update values", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Version: column(sqlType.int32), RegDate: column(sqlType.dateTime) } }), "T");
    const source = values([[1, "First", "Last"]], "A0", { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()) });
    expect(toSql(mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenNotMatchedInsertDefault(neq(source.UserId, 7)).done().ast, { dialect: "tsql" })).toContain("WHEN NOT MATCHED AND [A0].[UserId]!=7 THEN INSERT DEFAULT VALUES;");
    const statement = mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenMatchedUpdate({ FirstName: source.FirstName, LastName: source.LastName, Version: -1, RegDate: defaultValue }).whenNotMatchedInsert({ FirstName: source.FirstName, LastName: source.LastName, RegDate: defaultValue }).done();
    expect(toSql(statement.ast, { dialect: "tsql" })).toContain("[T].[RegDate]=DEFAULT WHEN NOT MATCHED THEN INSERT([FirstName],[LastName],[RegDate]) VALUES([A0].[FirstName],[A0].[LastName],DEFAULT);");
    const excluded = mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenNotMatchedInsert({ UserId: source.UserId, FirstName: source.FirstName }, neq(source.UserId, 8)).done();
    expect(toSql(excluded.ast, { dialect: "tsql" })).toContain("WHEN NOT MATCHED AND [A0].[UserId]!=8 THEN INSERT([UserId],[FirstName]) VALUES([A0].[UserId],[A0].[FirstName]);");
    const sourceMissing = mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenNotMatchedBySourceUpdate({ Version: -1 }, eq(target.UserId, 7)).done();
    expect(toSql(sourceMissing.ast, { dialect: "tsql" })).toContain("WHEN NOT MATCHED BY SOURCE AND [T].[UserId]=7 THEN UPDATE SET [T].[Version]=-1;");
  });
  it("ports keys-only MERGE variants and rejects an empty matched update", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "CustomerOrder", columns: { CustomerId: column(sqlType.int32), OrderId: column(sqlType.int32) } }), "A0");
    const source = values([[1, 2], [3, 4]], "A1", { CustomerId: column(sqlType.int32), OrderId: column(sqlType.int32) }); const on = eq(target.CustomerId, source.CustomerId).and(eq(target.OrderId, source.OrderId));
    const base = () => mergeInto(target, source).on(on);
    expect(toSql(base().whenNotMatchedInsert({ CustomerId: source.CustomerId, OrderId: source.OrderId }).whenNotMatchedBySourceDelete().done().ast, { dialect: "tsql" })).toContain("WHEN NOT MATCHED THEN INSERT([CustomerId],[OrderId]) VALUES([A1].[CustomerId],[A1].[OrderId]) WHEN NOT MATCHED BY SOURCE THEN  DELETE;");
    expect(toSql(base().whenMatchedDelete().whenNotMatchedInsert({ CustomerId: source.CustomerId, OrderId: source.OrderId }).whenNotMatchedBySourceDelete().done().ast, { dialect: "tsql" })).toContain("WHEN MATCHED THEN  DELETE");
    expect(toSql(base().whenMatchedUpdate({ CustomerId: 0 }).whenNotMatchedInsert({ CustomerId: source.CustomerId, OrderId: source.OrderId }).whenNotMatchedBySourceDelete().done().ast, { dialect: "tsql" })).toContain("WHEN MATCHED THEN UPDATE SET [A0].[CustomerId]=0");
    expect(() => base().whenMatchedUpdate({})).toThrow(/assignment/);
  });
  it("rejects empty MERGE data", () => {
    expect(() => values([], "A1", { UserId: column(sqlType.int32) })).toThrow(/at least one row/);
  });
  it("ports MERGE OUTPUT columns, inserted/deleted values, and action", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), LastName: column(sqlType.string()) } }), "A0");
    const source = values([[1]], "A1", { UserId: column(sqlType.int32) });
    const statement = mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenMatchedDelete().whenNotMatchedBySourceDelete().done().output({ columns: [source.UserId], inserted: [[target.LastName, "LN"]], deleted: [target.UserId], action: "Act" });
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("MERGE [dbo].[user] [A0] USING (VALUES (1))[A1]([UserId]) ON [A0].[UserId]=[A1].[UserId] WHEN MATCHED THEN  DELETE WHEN NOT MATCHED BY SOURCE THEN  DELETE OUTPUT [A1].[UserId],INSERTED.[LastName] [LN],DELETED.[UserId],$ACTION [Act];");
  });
  it("ports PostgreSQL anonymous MERGE source-column naming", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), Version: column(sqlType.int32) } }), "T");
    const sourceQuery = exprQuerySpecification({ selectList: [exprInt32Literal({ value: 1 }), exprAliasedSelecting({ value: exprStringLiteral({ value: "AA" }), alias: exprColumnAlias({ name: "BB" }) }), exprColumn({ source: null, columnName: exprColumnName({ name: "UserId" }) }), exprGetUtcDate], top: null, from: null, where: null, groupBy: null, distinct: false });
    const source = dynamicDerivedTable({ ast: sourceQuery }, "S");
    const statement = mergeInto(target, source).on(eq(target.UserId, columnExpression(source.columns.UserId!))).whenMatchedUpdate({ FirstName: columnExpression(source.columns.BB!), Version: 1 }).done();
    const sql = toSql(statement.ast, { dialect: "postgresql" }); expect(sql).toContain('WITH "__sqexpress_merge_source"("Expr1","BB","UserId","Expr4") AS('); expect(sql).toContain('FROM "__sqexpress_merge_source" "S"');
  });
  it("ports SQLite MERGE delete and insert/update polyfills", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), Modified: column(sqlType.dateTime) } }), "A0"); const source = values([[1, "Alice"], [2, "Bob"]], "A1", { UserId: column(sqlType.int32), FirstName: column(sqlType.string()) });
    const deletes = toSql(mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenMatchedDelete().whenNotMatchedBySourceDelete().done().ast, { dialect: "sqlite" });
    expect(deletes).toContain('CREATE TEMP TABLE "tmpMergeDataSource"'); expect(deletes).toContain('DELETE FROM "user" WHERE EXISTS('); expect(deletes).toContain('DELETE FROM "user" WHERE NOT EXISTS('); expect(deletes).not.toContain('DELETE FROM "user" "A0"');
    const changes = toSql(mergeInto(target, source).on(eq(target.UserId, source.UserId)).whenMatchedUpdate({ FirstName: source.FirstName, Modified: exprGetUtcDate }).whenNotMatchedInsert({ UserId: source.UserId, FirstName: source.FirstName, Modified: exprGetUtcDate }).whenNotMatchedBySourceUpdate({ Modified: exprGetUtcDate }).done().ast, { dialect: "sqlite" });
    expect(changes).toContain('UPDATE "user" SET "FirstName"="A0"."FirstName","Modified"=CURRENT_TIMESTAMP FROM "tmpMergeDataSource" "A0"'); expect(changes).toContain('INSERT INTO "user"("UserId","FirstName","Modified")'); expect(changes).toContain('UPDATE "user" SET "Modified"=CURRENT_TIMESTAMP WHERE NOT EXISTS(');
  });
  it("ports the full data-driven MERGE composition", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Email: column(sqlType.string()), RegDate: column(sqlType.dateTime), Version: column(sqlType.int32), Modified: column(sqlType.dateTime), Created: column(sqlType.dateTime) } }), "A0");
    const source = values([[0,"First0","Last0","user0@company.com","2020-01-02",0],[1,"First1","Last1","user1@company.com","2020-01-02",1],[0,"First2","Last2","user2@company.com","2020-01-02",2]], "A1", { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Email: column(sqlType.string()), RegDate: column(sqlType.dateTime), Index: column(sqlType.int32) });
    const stamp = "2020-10-03T10:17:12.131";
    const statement = mergeInto(target, source).on(eq(target.UserId, source.UserId).and(neq(source.UserId, 0)))
      .whenMatchedUpdate({ FirstName: source.FirstName, LastName: source.LastName, Email: source.Email, RegDate: source.RegDate, Version: add(target.Version, 1), Modified: stamp })
      .whenNotMatchedInsert({ FirstName: source.FirstName, RegDate: source.RegDate, LastName: "Fake", Created: stamp, Modified: stamp, Version: 1 })
      .whenNotMatchedBySourceDelete().done().output({ items: [{ kind: "inserted", value: [target.UserId, "InsertedUserId"] }, { kind: "inserted", value: [target.UserId, "DeletedUserId"] }, { kind: "column", value: source.Index }, { kind: "action", alias: "Action" }] });
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("MERGE [dbo].[user] [A0] USING (VALUES (0,'First0','Last0','user0@company.com','2020-01-02',0),(1,'First1','Last1','user1@company.com','2020-01-02',1),(0,'First2','Last2','user2@company.com','2020-01-02',2))[A1]([UserId],[FirstName],[LastName],[Email],[RegDate],[Index]) ON [A0].[UserId]=[A1].[UserId] AND [A1].[UserId]!=0 WHEN MATCHED THEN UPDATE SET [A0].[FirstName]=[A1].[FirstName],[A0].[LastName]=[A1].[LastName],[A0].[Email]=[A1].[Email],[A0].[RegDate]=[A1].[RegDate],[A0].[Version]=[A0].[Version]+1,[A0].[Modified]='2020-10-03T10:17:12.131' WHEN NOT MATCHED THEN INSERT([FirstName],[RegDate],[LastName],[Created],[Modified],[Version]) VALUES([A1].[FirstName],[A1].[RegDate],'Fake','2020-10-03T10:17:12.131','2020-10-03T10:17:12.131',1) WHEN NOT MATCHED BY SOURCE THEN  DELETE OUTPUT INSERTED.[UserId] [InsertedUserId],INSERTED.[UserId] [DeletedUserId],[A1].[Index],$ACTION [Action];");
  });
});
