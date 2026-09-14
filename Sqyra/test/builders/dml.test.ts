import { describe, expect, it } from "vitest";
import { add, aliasTable, column, columnExpression, concat, defaultValue, defineTable, deleteFrom, dropAndCreateTemporaryTable, eq, exists, exprGetUtcDate, exprInt32Literal, exprQuerySpecification, inList, insertInto, not, nullableColumn, select, sqlType, tableSource, toSql, update, values } from "../../src/index.js";

describe("typed DML builders", () => {
  const users = aliasTable(defineTable({ schema: "dbo", name: "Users", columns: { Id: column(sqlType.int32), Name: nullableColumn(sqlType.string(100)) } }), "u");
  it("builds multi-row INSERT values", () => {
    const statement = insertInto(users).values({ Id: 1, Name: "A" }, { Id: 2, Name: null });
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("INSERT INTO [dbo].[Users]([Id],[Name]) VALUES (1,'A'),(2,NULL)");
  });
  it("ports mapped object data INSERT", () => {
    const table = defineTable({ schema: "dbo", name: "user", columns: { FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Email: column(sqlType.string()), RegDate: column(sqlType.dateTime) } });
    const statement = insertInto(table).values(
      { FirstName: "First0", LastName: "Last0", Email: "user0@company.com", RegDate: "2020-01-02" },
      { FirstName: "First1", LastName: "Last1", Email: "user1@company.com", RegDate: "2020-01-02" },
      { FirstName: "First2", LastName: "Last2", Email: "user2@company.com", RegDate: "2020-01-02" },
    );
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("INSERT INTO [dbo].[user]([FirstName],[LastName],[Email],[RegDate]) VALUES ('First0','Last0','user0@company.com','2020-01-02'),('First1','Last1','user1@company.com','2020-01-02'),('First2','Last2','user2@company.com','2020-01-02')");
  });
  it("ports chained INSERT values", () => {
    const table = defineTable({ schema: "dbo", name: "user", columns: { FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Modified: column(sqlType.dateTime), Version: column(sqlType.int32) } });
    const statement = insertInto(table).values(
      { FirstName: "FirstName", LastName: "LastName", Modified: exprGetUtcDate, Version: 1 },
      { FirstName: "FirstName2", LastName: "LastName2", Modified: "2022-07-10", Version: 2 },
    );
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("INSERT INTO [dbo].[user]([FirstName],[LastName],[Modified],[Version]) VALUES ('FirstName','LastName',GETUTCDATE(),1),('FirstName2','LastName2','2022-07-10',2)");
  });
  it("ports T-SQL identity INSERT values", () => {
    const table = defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()) } });
    const statement = insertInto(table).values({ UserId: 1, FirstName: "FirstName" }, { UserId: 2, FirstName: "FirstName2" }).identity("UserId");
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("SET IDENTITY_INSERT [dbo].[user] ON;INSERT INTO [dbo].[user]([UserId],[FirstName]) VALUES (1,'FirstName'),(2,'FirstName2');SET IDENTITY_INSERT [dbo].[user] OFF;");
  });
  it("ports query-based INSERT expressions", () => {
    const table = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Modified: column(sqlType.dateTime), Version: column(sqlType.int32) } }), "A0");
    const source = select({ FirstName: table.FirstName, LastName: table.LastName, Modified: exprGetUtcDate, Version: add(1, 1) }).from(table).done();
    expect(toSql(insertInto(table).from(source, "FirstName", "LastName", "Modified", "Version").ast, { dialect: "tsql" })).toBe("INSERT INTO [dbo].[user]([FirstName],[LastName],[Modified],[Version]) SELECT [A0].[FirstName] [FirstName],[A0].[LastName] [LastName],GETUTCDATE() [Modified],1+1 [Version] FROM [dbo].[user] [A0]");
  });
  it("ports PostgreSQL data identity INSERT reseeding", () => {
    const table = defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()) } });
    const statement = insertInto(table).values({ UserId: 0, FirstName: "First0" }).identity("UserId");
    expect(toSql(statement.ast, { dialect: "postgresql", schemaMap: [{ from: "dbo", to: "public" }] })).toBe("WITH \"__sqexpress_identity_insert\" AS (INSERT INTO \"public\".\"user\"(\"UserId\",\"FirstName\") OVERRIDING SYSTEM VALUE VALUES (0,'First0')  RETURNING \"UserId\") SELECT setval(pg_get_serial_sequence('\"public\".\"user\"','UserId'),GREATEST((SELECT MAX(\"UserId\") FROM \"public\".\"user\"),(SELECT MAX(\"UserId\") FROM \"__sqexpress_identity_insert\"))) FROM \"__sqexpress_identity_insert\" LIMIT 1");
  });
  it("ports data INSERT with extra values and OUTPUT", () => {
    const table = defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Email: column(sqlType.string()), RegDate: column(sqlType.dateTime), Version: column(sqlType.int32), Created: column(sqlType.dateTime) } });
    const data = values([["First0","Last0","user0@company.com","2020-01-02"],["First1","Last1","user1@company.com","2020-01-02"],["First2","Last2","user2@company.com","2020-01-02"]], "A0", { FirstName: column(sqlType.string()), LastName: column(sqlType.string()), Email: column(sqlType.string()), RegDate: column(sqlType.dateTime) });
    const source = select({ FirstName: data.FirstName, LastName: data.LastName, Email: data.Email, RegDate: data.RegDate, Version: 5, Created: "2020-01-02" }).from(data).done();
    const statement = insertInto(table).from(source, "FirstName", "LastName", "Email", "RegDate", "Version", "Created").output(table.UserId);
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("INSERT INTO [dbo].[user]([FirstName],[LastName],[Email],[RegDate],[Version],[Created]) OUTPUT INSERTED.[UserId] SELECT [A0].[FirstName] [FirstName],[A0].[LastName] [LastName],[A0].[Email] [Email],[A0].[RegDate] [RegDate],5 [Version],'2020-01-02' [Created] FROM (VALUES ('First0','Last0','user0@company.com','2020-01-02'),('First1','Last1','user1@company.com','2020-01-02'),('First2','Last2','user2@company.com','2020-01-02'))[A0]([FirstName],[LastName],[Email],[RegDate])");
    expect(toSql(statement.ast, { dialect: "mysql", mysqlFlavor: "mariadb" })).toContain("RETURNING `UserId`"); expect(() => toSql(statement.ast, { dialect: "mysql", mysqlFlavor: "oracle" })).toThrow(/Oracle MySQL/);
  });
  it("ports conditional data INSERT with dialect adaptation and OUTPUT", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()) } }), "A1");
    const data = values([["First0", "Last0"]], "A0", { FirstName: column(sqlType.string()), LastName: column(sqlType.string()) });
    const duplicate = exprQuerySpecification({ selectList: [exprInt32Literal({ value: 1 })], top: null, from: tableSource(target), where: eq(target.FirstName, data.FirstName).and(eq(target.LastName, data.LastName)), groupBy: null, distinct: false });
    const source = exprQuerySpecification({ selectList: [columnExpression(data.FirstName), columnExpression(data.LastName)], top: null, from: data.derivedSource, where: not(exists({ ast: duplicate })), groupBy: null, distinct: false });
    const statement = insertInto(target).from({ ast: source }, "FirstName", "LastName").output(target.UserId);
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("INSERT INTO [dbo].[user]([FirstName],[LastName]) OUTPUT INSERTED.[UserId] SELECT [A0].[FirstName],[A0].[LastName] FROM (VALUES ('First0','Last0'))[A0]([FirstName],[LastName]) WHERE NOT EXISTS(SELECT 1 FROM [dbo].[user] [A1] WHERE [A1].[FirstName]=[A0].[FirstName] AND [A1].[LastName]=[A0].[LastName])");
    const maria = toSql(statement.ast, { dialect: "mysql", mysqlFlavor: "mariadb" }); expect(maria).toContain("WITH CTE_Derived_Table_0(`FirstName`,`LastName`) AS(VALUES ('First0','Last0'))"); expect(maria).toContain("RETURNING `UserId`");
    expect(() => toSql(statement.ast, { dialect: "mysql", mysqlFlavor: "oracle" })).toThrow(/Oracle MySQL/);
  });
  it("builds UPDATE with an optional predicate", () => {
    const statement = update(users).set({ Name: "Renamed" }).where(eq(users.columns.Id, 1));
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("UPDATE [u] SET [u].[Name]='Renamed' WHERE [u].[Id]=1");
  });
  it("ports UPDATE FROM join chains", () => {
    const user = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()) } }), "A0");
    const customer = aliasTable(defineTable({ schema: "dbo", name: "Customer", columns: { UserId: column(sqlType.int32), CustomerId: column(sqlType.int32) } }), "A1");
    const statement = update(user).set({ FirstName: "First", LastName: "Last" }).from(user).innerJoin(customer, eq(customer.UserId, user.UserId)).crossJoin(user).where(inList(customer.CustomerId, 1));
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("UPDATE [A0] SET [A0].[FirstName]='First',[A0].[LastName]='Last' FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] CROSS JOIN [dbo].[user] [A0] WHERE [A1].[CustomerId] IN(1)");
  });
  it("ports PostgreSQL UPDATE FROM source extraction", () => {
    const user = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), LastName: column(sqlType.string()), RegDate: column(sqlType.dateTime) } }), "A0");
    const customer = aliasTable(defineTable({ schema: "dbo", name: "Customer", columns: { UserId: column(sqlType.int32), CustomerId: column(sqlType.int32) } }), "A1");
    const orders = aliasTable(defineTable({ schema: "dbo", name: "CustomerOrder", columns: { Id: column(sqlType.int32) } }), "A2");
    const statement = update(user).set({ FirstName: "First", LastName: concat(user.LastName, "(i)"), RegDate: defaultValue }).from(user).innerJoin(customer, eq(customer.UserId, user.UserId)).crossJoin(orders).where(inList(customer.CustomerId, 1));
    expect(toSql(statement.ast, { dialect: "postgresql", schemaMap: [{ from: "dbo", to: "public" }] })).toBe('UPDATE "public"."user" "A0" SET "FirstName"=\'First\',"LastName"="A0"."LastName"||\'(i)\',"RegDate"=DEFAULT FROM "public"."Customer" "A1","public"."CustomerOrder" "A2" WHERE "A1"."UserId"="A0"."UserId" AND "A1"."CustomerId" IN(1)');
  });
  it("ports data-driven UPDATE through derived values", () => {
    const target = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()), Modified: column(sqlType.dateTime) } }), "A0");
    const data = values([[1,"First0"],[2,"First1"],[3,"First2"]], "A1", { UserId: column(sqlType.int32), FirstName: column(sqlType.string()) });
    const statement = update(target).set({ FirstName: data.FirstName, Modified: exprGetUtcDate }).from(target).innerJoin(data, eq(target.UserId, data.UserId)).done();
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("UPDATE [A0] SET [A0].[FirstName]=[A1].[FirstName],[A0].[Modified]=GETUTCDATE() FROM [dbo].[user] [A0] JOIN (VALUES (1,'First0'),(2,'First1'),(3,'First2'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId]");
    const mysql = toSql(statement.ast, { dialect: "mysql" }); expect(mysql).toMatch(/^CREATE TEMPORARY TABLE `t[\da-z]{32}`/i); expect(mysql.replace(/t[\da-z]{32}/gi, "tmpTableName")).toBe("CREATE TEMPORARY TABLE `tmpTableName`(`UserId` int,`FirstName` varchar(6) character set utf8mb4,CONSTRAINT PRIMARY KEY (`UserId`));INSERT INTO `tmpTableName`(`UserId`,`FirstName`) VALUES (1,'First0'),(2,'First1'),(3,'First2');UPDATE `user` `A0` JOIN `tmpTableName` `A1` ON `A0`.`UserId`=`A1`.`UserId` SET `A0`.`FirstName`=`A1`.`FirstName`,`A0`.`Modified`=UTC_TIMESTAMP();DROP TABLE `tmpTableName`;");
  });
  it("builds DELETE with an optional predicate", () => {
    const statement = deleteFrom(users).where(eq(users.columns.Id, 1));
    expect(toSql(statement.ast, { dialect: "tsql" })).toBe("DELETE [u] FROM [dbo].[Users] [u] WHERE [u].[Id]=1");
  });
  it("ports DELETE FROM joins and nullable predicates", () => {
    const user = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } }), "A0");
    const customer = aliasTable(defineTable({ schema: "dbo", name: "Customer", columns: { UserId: column(sqlType.int32) } }), "A1");
    const base = deleteFrom(user).from(user);
    expect(toSql(base.done().ast, { dialect: "tsql" })).toBe("DELETE [A0] FROM [dbo].[user] [A0]");
    expect(toSql(base.done().ast, { dialect: "mysql" })).toBe("DELETE `A0` FROM `user` `A0`");
    const inner = base.innerJoin(customer, eq(customer.UserId, user.UserId));
    expect(toSql(inner.done().ast, { dialect: "tsql" })).toBe("DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]");
    expect(toSql(inner.done().ast, { dialect: "mysql" })).toBe("DELETE `A0` FROM `user` `A0` JOIN `Customer` `A1` ON `A1`.`UserId`=`A0`.`UserId`");
    expect(toSql(base.leftJoin(customer, eq(customer.UserId, user.UserId)).done().ast, { dialect: "tsql" })).toContain("LEFT JOIN");
    expect(toSql(base.fullJoin(customer, eq(customer.UserId, user.UserId)).done().ast, { dialect: "tsql" })).toContain("FULL JOIN");
    expect(toSql(base.crossJoin(customer).done().ast, { dialect: "tsql" })).toContain("CROSS JOIN");
    expect(toSql(inner.where(null).ast, { dialect: "tsql" })).toBe(toSql(inner.done().ast, { dialect: "tsql" }));
  });
  it("ports T-SQL DELETE OUTPUT", () => {
    const user = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } }), "A0");
    expect(toSql(deleteFrom(user).from(user).done().output(user.UserId).ast, { dialect: "tsql" })).toBe("DELETE [A0] OUTPUT DELETED.[UserId] FROM [dbo].[user] [A0]");
  });
  it("ports PostgreSQL DELETE RETURNING including USING joins", () => {
    const user = aliasTable(defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } }), "A0");
    const customer = aliasTable(defineTable({ schema: "dbo", name: "Customer", columns: { UserId: column(sqlType.int32) } }), "A1");
    const statement = deleteFrom(user).from(user).innerJoin(customer, eq(customer.UserId, user.UserId)).done().output(user.UserId);
    expect(toSql(statement.ast, { dialect: "postgresql", schemaMap: [{ from: "dbo", to: "public" }] })).toBe('DELETE FROM "public"."user" "A0" USING "public"."Customer" "A1" WHERE "A1"."UserId"="A0"."UserId" RETURNING "A0"."UserId"');
  });
  it("rejects generic DELETE OUTPUT for MySQL", () => {
    const user = defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } });
    const statement = deleteFrom(user).from(user).done().output(user.UserId);
    expect(() => toSql(statement.ast, { dialect: "mysql", mysqlFlavor: "oracle" })).toThrow(/Oracle MySQL does not support generic DELETE OUTPUT\/RETURNING/);
  });
  it("ports temporary-table drop and create scripts", () => {
    const name = `t -- mpU"s'er`; const columns = [{ name: "Id", type: "int32", identity: true, primaryKey: true }, { name: "Modified", type: "date" }] as const;
    expect(dropAndCreateTemporaryTable(name, columns, "postgresql")).toBe("DROP TABLE IF EXISTS \"t -- mpU\"\"s'er\";CREATE TEMP TABLE \"t -- mpU\"\"s'er\"(\"Id\" int4 NOT NULL  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),\"Modified\" date NOT NULL,CONSTRAINT \"PK_t -- mpU\"\"s'er\" PRIMARY KEY (\"Id\"));");
    expect(dropAndCreateTemporaryTable(name, columns, "tsql")).toBe("IF OBJECT_ID('tempdb..[#t -- mpU\"s''er]') IS NOT NULL DROP TABLE [#t -- mpU\"s'er]CREATE TABLE [#t -- mpU\"s'er]([Id] int NOT NULL  IDENTITY (1, 1),[Modified] date NOT NULL,CONSTRAINT [PK_t -- mpU\"s'er] PRIMARY KEY ([Id]));");
  });
});
