import { describe, expect, it } from "vitest";
import { lit } from "../../src/index.js";
import {
  aliasTable,
  column,
  columnExpression,
  compileSql,
  cte,
  defaultValue,
  defineTable,
  deleteFrom,
  dropAndCreateTemporaryTable,
  exists,
  exprGetUtcDate,
  exprInt32Literal,
  exprQuerySpecification,
  insertInto,
  nullableColumn,
  select,
  sqlType,
  tableSource,
  update,
  values,
} from "../../src/index.js";

describe("typed DML builders", () => {
  const users = aliasTable(
    defineTable({
      schema: "dbo",
      name: "Users",
      columns: { Id: column(sqlType.int32), Name: nullableColumn(sqlType.string(100)) },
    }),
    "u",
  );
  it("builds partial-column INSERT values", () => {
    const statement = insertInto(users).columns("Name").values({ Name: "Ada" });
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "INSERT INTO [dbo].[Users]([Name]) VALUES ('Ada')",
    );
  });
  it("preserves descriptor value types in INSERT and UPDATE parameters", () => {
    const typed = defineTable({
      schema: "dbo",
      name: "Typed",
      columns: {
        Id: column(sqlType.guid),
        Amount: column(sqlType.decimal(10, 3)),
        At: column(sqlType.dateTime),
      },
    });
    const inserted = compileSql(
      insertInto(typed).values({
        Id: "11111111-2222-3333-4444-555555555555",
        Amount: "12.340",
        At: "2024-01-02T03:04:05",
      }).ast,
      { dialect: "pgsql", parameterize: true },
    );
    expect(inserted.parameters.map((item) => item.type)).toEqual([
      "ExprGuidLiteral",
      "ExprDecimalLiteral",
      "ExprDateTimeLiteral",
    ]);
    const updated = compileSql(
      update(typed).set({
        Id: "22222222-3333-4444-5555-666666666666",
        Amount: "0.125",
        At: "2025-01-01",
      }).ast,
      { dialect: "pgsql", parameterize: true },
    );
    expect(updated.parameters.map((item) => item.type)).toEqual([
      "ExprGuidLiteral",
      "ExprDecimalLiteral",
      "ExprDateTimeLiteral",
    ]);
  });
  it("builds multi-row INSERT values", () => {
    const statement = insertInto(users).values({ Id: 1, Name: "A" }, { Id: 2, Name: null });
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "INSERT INTO [dbo].[Users]([Id],[Name]) VALUES (1,'A'),(2,NULL)",
    );
  });
  it("ports mapped object data INSERT", () => {
    const table = defineTable({
      schema: "dbo",
      name: "user",
      columns: {
        FirstName: column(sqlType.string()),
        LastName: column(sqlType.string()),
        Email: column(sqlType.string()),
        RegDate: column(sqlType.dateTime),
      },
    });
    const statement = insertInto(table).values(
      { FirstName: "First0", LastName: "Last0", Email: "user0@company.com", RegDate: "2020-01-02" },
      { FirstName: "First1", LastName: "Last1", Email: "user1@company.com", RegDate: "2020-01-02" },
      { FirstName: "First2", LastName: "Last2", Email: "user2@company.com", RegDate: "2020-01-02" },
    );
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "INSERT INTO [dbo].[user]([FirstName],[LastName],[Email],[RegDate]) VALUES ('First0','Last0','user0@company.com','2020-01-02'),('First1','Last1','user1@company.com','2020-01-02'),('First2','Last2','user2@company.com','2020-01-02')",
    );
  });
  it("ports chained INSERT values", () => {
    const table = defineTable({
      schema: "dbo",
      name: "user",
      columns: {
        FirstName: column(sqlType.string()),
        LastName: column(sqlType.string()),
        Modified: column(sqlType.dateTime),
        Version: column(sqlType.int32),
      },
    });
    const statement = insertInto(table).values(
      { FirstName: "FirstName", LastName: "LastName", Modified: exprGetUtcDate, Version: 1 },
      { FirstName: "FirstName2", LastName: "LastName2", Modified: "2022-07-10", Version: 2 },
    );
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "INSERT INTO [dbo].[user]([FirstName],[LastName],[Modified],[Version]) VALUES ('FirstName','LastName',GETUTCDATE(),1),('FirstName2','LastName2','2022-07-10',2)",
    );
  });
  it("ports T-SQL identity INSERT values", () => {
    const table = defineTable({
      schema: "dbo",
      name: "user",
      columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()) },
    });
    const statement = insertInto(table)
      .values({ UserId: 1, FirstName: "FirstName" }, { UserId: 2, FirstName: "FirstName2" })
      .identity("UserId");
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "SET IDENTITY_INSERT [dbo].[user] ON;INSERT INTO [dbo].[user]([UserId],[FirstName]) VALUES (1,'FirstName'),(2,'FirstName2');SET IDENTITY_INSERT [dbo].[user] OFF;",
    );
  });
  it("ports query-based INSERT expressions", () => {
    const table = aliasTable(
      defineTable({
        schema: "dbo",
        name: "user",
        columns: {
          FirstName: column(sqlType.string()),
          LastName: column(sqlType.string()),
          Modified: column(sqlType.dateTime),
          Version: column(sqlType.int32),
        },
      }),
      "A0",
    );
    const source = select({
      FirstName: table.FirstName,
      LastName: table.LastName,
      Modified: exprGetUtcDate,
      Version: lit(1).add(1),
    }).from(table);
    expect(
      insertInto(table)
        .from(source, "FirstName", "LastName", "Modified", "Version")
        .toSql({ dialect: "tsql" }),
    ).toBe(
      "INSERT INTO [dbo].[user]([FirstName],[LastName],[Modified],[Version]) SELECT [A0].[FirstName] [FirstName],[A0].[LastName] [LastName],GETUTCDATE() [Modified],1+1 [Version] FROM [dbo].[user] [A0]",
    );
  });
  it("ports PostgreSQL data identity INSERT reseeding", () => {
    const table = defineTable({
      schema: "dbo",
      name: "user",
      columns: { UserId: column(sqlType.int32), FirstName: column(sqlType.string()) },
    });
    const statement = insertInto(table)
      .values({ UserId: 0, FirstName: "First0" })
      .identity("UserId");
    expect(statement.toSql({ dialect: "pgsql", schemaMap: [{ from: "dbo", to: "public" }] })).toBe(
      'WITH "__sqexpress_identity_insert" AS (INSERT INTO "public"."user"("UserId","FirstName") OVERRIDING SYSTEM VALUE VALUES (0,\'First0\')  RETURNING "UserId") SELECT setval(pg_get_serial_sequence(\'"public"."user"\',\'UserId\'),GREATEST((SELECT MAX("UserId") FROM "public"."user"),(SELECT MAX("UserId") FROM "__sqexpress_identity_insert"))) FROM "__sqexpress_identity_insert" LIMIT 1',
    );
  });
  it("combines PostgreSQL source CTEs with the identity reseed CTE", () => {
    const table = defineTable({
      schema: null,
      name: "target",
      columns: { Id: column(sqlType.int32), Value: column(sqlType.int32) },
    });
    const source = cte("source", { Id: column(sqlType.int32), Value: column(sqlType.int32) }, () =>
      select({ Id: 1, Value: 10 }),
    );
    const sql = insertInto(table)
      .from(select(source.Id, source.Value).from(source), "Id", "Value")
      .identity("Id")
      .toSql("pgsql");
    expect(sql).toContain('WITH "source" AS(');
    expect(sql).toContain(',"__sqexpress_identity_insert" AS (INSERT INTO');
    expect(sql).not.toContain(')WITH "__sqexpress_identity_insert"');
    const mysql = insertInto(table)
      .from(select(source.Id, source.Value).from(source), "Id", "Value")
      .identity("Id")
      .toSql("mysql");
    expect(mysql).toContain("INSERT INTO `target`(`Id`,`Value`) WITH `source` AS(");
  });
  it("ports data INSERT with extra values and OUTPUT", () => {
    const table = defineTable({
      schema: "dbo",
      name: "user",
      columns: {
        UserId: column(sqlType.int32),
        FirstName: column(sqlType.string()),
        LastName: column(sqlType.string()),
        Email: column(sqlType.string()),
        RegDate: column(sqlType.dateTime),
        Version: column(sqlType.int32),
        Created: column(sqlType.dateTime),
      },
    });
    const data = values(
      [
        ["First0", "Last0", "user0@company.com", "2020-01-02"],
        ["First1", "Last1", "user1@company.com", "2020-01-02"],
        ["First2", "Last2", "user2@company.com", "2020-01-02"],
      ],
      "A0",
      {
        FirstName: column(sqlType.string()),
        LastName: column(sqlType.string()),
        Email: column(sqlType.string()),
        RegDate: column(sqlType.dateTime),
      },
    );
    const source = select({
      FirstName: data.FirstName,
      LastName: data.LastName,
      Email: data.Email,
      RegDate: data.RegDate,
      Version: 5,
      Created: "2020-01-02",
    }).from(data);
    const statement = insertInto(table)
      .from(source, "FirstName", "LastName", "Email", "RegDate", "Version", "Created")
      .output(table.UserId);
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "INSERT INTO [dbo].[user]([FirstName],[LastName],[Email],[RegDate],[Version],[Created]) OUTPUT INSERTED.[UserId] SELECT [A0].[FirstName] [FirstName],[A0].[LastName] [LastName],[A0].[Email] [Email],[A0].[RegDate] [RegDate],5 [Version],'2020-01-02' [Created] FROM (VALUES ('First0','Last0','user0@company.com','2020-01-02'),('First1','Last1','user1@company.com','2020-01-02'),('First2','Last2','user2@company.com','2020-01-02'))[A0]([FirstName],[LastName],[Email],[RegDate])",
    );
    expect(statement.toSql({ dialect: "mysql", mysqlFlavor: "mariadb" })).toContain(
      "RETURNING `UserId`",
    );
    expect(() => statement.toSql({ dialect: "mysql", mysqlFlavor: "oracle" })).toThrow(
      /Oracle MySQL/,
    );
  });
  it("ports conditional data INSERT with dialect adaptation and OUTPUT", () => {
    const target = aliasTable(
      defineTable({
        schema: "dbo",
        name: "user",
        columns: {
          UserId: column(sqlType.int32),
          FirstName: column(sqlType.string()),
          LastName: column(sqlType.string()),
        },
      }),
      "A1",
    );
    const data = values([["First0", "Last0"]], "A0", {
      FirstName: column(sqlType.string()),
      LastName: column(sqlType.string()),
    });
    const duplicate = exprQuerySpecification({
      selectList: [exprInt32Literal({ value: 1 })],
      top: null,
      from: tableSource(target),
      where: target.FirstName.eq(data.FirstName).and(target.LastName.eq(data.LastName)),
      groupBy: null,
      distinct: false,
    });
    const source = exprQuerySpecification({
      selectList: [columnExpression(data.FirstName), columnExpression(data.LastName)],
      top: null,
      from: data.$metadata.source,
      where: exists(duplicate).not(),
      groupBy: null,
      distinct: false,
    });
    const statement = insertInto(target)
      .from(source, "FirstName", "LastName")
      .output(target.UserId);
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "INSERT INTO [dbo].[user]([FirstName],[LastName]) OUTPUT INSERTED.[UserId] SELECT [A0].[FirstName],[A0].[LastName] FROM (VALUES ('First0','Last0'))[A0]([FirstName],[LastName]) WHERE NOT EXISTS(SELECT 1 FROM [dbo].[user] [A1] WHERE [A1].[FirstName]=[A0].[FirstName] AND [A1].[LastName]=[A0].[LastName])",
    );
    const maria = statement.toSql({ dialect: "mysql", mysqlFlavor: "mariadb" });
    expect(maria).toContain(
      "WITH CTE_Derived_Table_0(`FirstName`,`LastName`) AS(VALUES ('First0','Last0'))",
    );
    expect(maria).toContain("RETURNING `UserId`");
    expect(() => statement.toSql({ dialect: "mysql", mysqlFlavor: "oracle" })).toThrow(
      /Oracle MySQL/,
    );
  });
  it("builds UPDATE with an optional predicate", () => {
    const statement = update(users).set({ Name: "Renamed" }).where(users.Id.eq(1));
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "UPDATE [u] SET [u].[Name]='Renamed' WHERE [u].[Id]=1",
    );
  });
  it("ports UPDATE FROM join chains", () => {
    const user = aliasTable(
      defineTable({
        schema: "dbo",
        name: "user",
        columns: {
          UserId: column(sqlType.int32),
          FirstName: column(sqlType.string()),
          LastName: column(sqlType.string()),
        },
      }),
      "A0",
    );
    const customer = aliasTable(
      defineTable({
        schema: "dbo",
        name: "Customer",
        columns: {
          UserId: column(sqlType.int32, { references: user.UserId }),
          CustomerId: column(sqlType.int32),
        },
      }),
      "A1",
    );
    const statement = update(user)
      .set({ FirstName: "First", LastName: "Last" })
      .from(user)
      .innerJoin(customer)
      .crossJoin(user)
      .where(customer.CustomerId.inList(1));
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "UPDATE [A0] SET [A0].[FirstName]='First',[A0].[LastName]='Last' FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] CROSS JOIN [dbo].[user] [A0] WHERE [A1].[CustomerId] IN(1)",
    );
  });
  it("ports PostgreSQL UPDATE FROM source extraction", () => {
    const user = aliasTable(
      defineTable({
        schema: "dbo",
        name: "user",
        columns: {
          UserId: column(sqlType.int32),
          FirstName: column(sqlType.string()),
          LastName: column(sqlType.string()),
          RegDate: column(sqlType.dateTime),
        },
      }),
      "A0",
    );
    const customer = aliasTable(
      defineTable({
        schema: "dbo",
        name: "Customer",
        columns: {
          UserId: column(sqlType.int32, { references: user.UserId }),
          CustomerId: column(sqlType.int32),
        },
      }),
      "A1",
    );
    const orders = aliasTable(
      defineTable({ schema: "dbo", name: "CustomerOrder", columns: { Id: column(sqlType.int32) } }),
      "A2",
    );
    const statement = update(user)
      .set({
        FirstName: "First",
        LastName: user.LastName.concat("(i)"),
        RegDate: defaultValue,
      })
      .from(user)
      .innerJoin(customer)
      .crossJoin(orders)
      .where(customer.CustomerId.inList(1));
    expect(statement.toSql({ dialect: "pgsql", schemaMap: [{ from: "dbo", to: "public" }] })).toBe(
      'UPDATE "public"."user" "A0" SET "FirstName"=\'First\',"LastName"="A0"."LastName"||\'(i)\',"RegDate"=DEFAULT FROM "public"."Customer" "A1","public"."CustomerOrder" "A2" WHERE "A1"."UserId"="A0"."UserId" AND "A1"."CustomerId" IN(1)',
    );
  });
  it("ports data-driven UPDATE through derived values", () => {
    const target = aliasTable(
      defineTable({
        schema: "dbo",
        name: "user",
        columns: {
          UserId: column(sqlType.int32),
          FirstName: column(sqlType.string()),
          Modified: column(sqlType.dateTime),
        },
      }),
      "A0",
    );
    const data = values(
      [
        [1, "First0"],
        [2, "First1"],
        [3, "First2"],
      ],
      "A1",
      { UserId: column(sqlType.int32), FirstName: column(sqlType.string()) },
    );
    const statement = update(target)
      .set({ FirstName: data.FirstName, Modified: exprGetUtcDate })
      .from(target)
      .innerJoin(data, target.UserId.eq(data.UserId));
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "UPDATE [A0] SET [A0].[FirstName]=[A1].[FirstName],[A0].[Modified]=GETUTCDATE() FROM [dbo].[user] [A0] JOIN (VALUES (1,'First0'),(2,'First1'),(3,'First2'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId]",
    );
    const mysql = statement.toSql({ dialect: "mysql" });
    expect(mysql).toMatch(/^CREATE TEMPORARY TABLE `t[\da-z]{32}`/i);
    expect(mysql.replace(/t[\da-z]{32}/gi, "tmpTableName")).toBe(
      "CREATE TEMPORARY TABLE `tmpTableName`(`UserId` int,`FirstName` varchar(6) character set utf8mb4,CONSTRAINT PRIMARY KEY (`UserId`));INSERT INTO `tmpTableName`(`UserId`,`FirstName`) VALUES (1,'First0'),(2,'First1'),(3,'First2');UPDATE `user` `A0` JOIN `tmpTableName` `A1` ON `A0`.`UserId`=`A1`.`UserId` SET `A0`.`FirstName`=`A1`.`FirstName`,`A0`.`Modified`=UTC_TIMESTAMP();DROP TABLE `tmpTableName`;",
    );
  });
  it("builds DELETE with an optional predicate", () => {
    const statement = deleteFrom(users).where(users.Id.eq(1));
    expect(statement.toSql({ dialect: "tsql" })).toBe(
      "DELETE [u] FROM [dbo].[Users] [u] WHERE [u].[Id]=1",
    );
  });
  it("ports DELETE FROM joins and nullable predicates", () => {
    const user = aliasTable(
      defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } }),
      "A0",
    );
    const customer = aliasTable(
      defineTable({
        schema: "dbo",
        name: "Customer",
        columns: { UserId: column(sqlType.int32, { references: user.UserId }) },
      }),
      "A1",
    );
    const base = deleteFrom(user).from(user);
    expect(base.toSql({ dialect: "tsql" })).toBe("DELETE [A0] FROM [dbo].[user] [A0]");
    expect(base.toSql({ dialect: "mysql" })).toBe("DELETE `A0` FROM `user` `A0`");
    const inner = base.innerJoin(customer);
    expect(inner.toSql({ dialect: "tsql" })).toBe(
      "DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]",
    );
    expect(inner.toSql({ dialect: "mysql" })).toBe(
      "DELETE `A0` FROM `user` `A0` JOIN `Customer` `A1` ON `A1`.`UserId`=`A0`.`UserId`",
    );
    expect(base.leftJoin(customer).toSql({ dialect: "tsql" })).toContain("LEFT JOIN");
    expect(base.fullJoin(customer).toSql({ dialect: "tsql" })).toContain("FULL JOIN");
    expect(base.crossJoin(customer).toSql({ dialect: "tsql" })).toContain("CROSS JOIN");
    expect(inner.where(null).toSql({ dialect: "tsql" })).toBe(inner.toSql({ dialect: "tsql" }));
  });
  it("ports T-SQL DELETE OUTPUT", () => {
    const user = aliasTable(
      defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } }),
      "A0",
    );
    expect(deleteFrom(user).from(user).output(user.UserId).toSql({ dialect: "tsql" })).toBe(
      "DELETE [A0] OUTPUT DELETED.[UserId] FROM [dbo].[user] [A0]",
    );
  });
  it("ports PostgreSQL DELETE RETURNING including USING joins", () => {
    const user = aliasTable(
      defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } }),
      "A0",
    );
    const customer = aliasTable(
      defineTable({
        schema: "dbo",
        name: "Customer",
        columns: { UserId: column(sqlType.int32, { references: user.UserId }) },
      }),
      "A1",
    );
    const statement = deleteFrom(user).from(user).innerJoin(customer).output(user.UserId);
    expect(statement.toSql({ dialect: "pgsql", schemaMap: [{ from: "dbo", to: "public" }] })).toBe(
      'DELETE FROM "public"."user" "A0" USING "public"."Customer" "A1" WHERE "A1"."UserId"="A0"."UserId" RETURNING "A0"."UserId"',
    );
  });
  it("rejects generic DELETE OUTPUT for MySQL", () => {
    const user = defineTable({
      schema: "dbo",
      name: "user",
      columns: { UserId: column(sqlType.int32) },
    });
    const statement = deleteFrom(user).from(user).output(user.UserId);
    expect(() => statement.toSql({ dialect: "mysql", mysqlFlavor: "oracle" })).toThrow(
      /Oracle MySQL does not support generic DELETE OUTPUT\/RETURNING/,
    );
  });
  it("ports temporary-table drop and create scripts", () => {
    const name = `t -- mpU"s'er`;
    const columns = [
      { name: "Id", type: "int32", identity: true, primaryKey: true },
      { name: "Modified", type: "date" },
    ] as const;
    expect(dropAndCreateTemporaryTable(name, columns, "pgsql")).toBe(
      'DROP TABLE IF EXISTS "t -- mpU""s\'er";CREATE TEMP TABLE "t -- mpU""s\'er"("Id" int4 NOT NULL  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),"Modified" date NOT NULL,CONSTRAINT "PK_t -- mpU""s\'er" PRIMARY KEY ("Id"));',
    );
    expect(dropAndCreateTemporaryTable(name, columns, "tsql")).toBe(
      "IF OBJECT_ID('tempdb..[#t -- mpU\"s''er]') IS NOT NULL DROP TABLE [#t -- mpU\"s'er]CREATE TABLE [#t -- mpU\"s'er]([Id] int NOT NULL  IDENTITY (1, 1),[Modified] date NOT NULL,CONSTRAINT [PK_t -- mpU\"s'er] PRIMARY KEY ([Id]));",
    );
  });
});
