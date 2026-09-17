import { describe, expect, it } from "vitest";
import {
  column,
  createColumnRef,
  defineTable,
  indexDesc,
  sqlType,
  tableIndex,
  type ColumnRef,
} from "../src/index.js";

describe("table metadata and scripts", () => {
  it("ports cyclic foreign-key ownership assertions from DbModelMapper_ToSqDbTables_SupportsCyclicForeignKeys", () => {
    const a = defineTable({
      schema: "dbo",
      name: "TableA",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        BId: column(sqlType.int32, {
          references: (): ColumnRef<"Id", number, false, "TableB"> => b.Id,
        }),
      },
    });
    const b = defineTable({
      schema: "dbo",
      name: "TableB",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        AId: column(sqlType.int32, { references: a.Id }),
      },
    });
    const aSql = a.$script.create().toSql("tsql"),
      bSql = b.$script.create().toSql("tsql");
    expect(aSql).toContain("FOREIGN KEY ([BId]) REFERENCES [dbo].[TableB]([Id])");
    expect(bSql).toContain("FOREIGN KEY ([AId]) REFERENCES [dbo].[TableA]([Id])");
    expect(a.$script.create().toSql("tsql")).toBe(aSql);
  });
  it("ports TableFk3Ab's multiple references and composite foreign-key grouping", () => {
    const a = defineTable({
      schema: "dbo",
      name: "Fk1A",
      columns: { Id: column(sqlType.int32, { primaryKey: true }) },
    });
    const b = defineTable({
      schema: "dbo",
      name: "Fk1B",
      columns: { Id: column(sqlType.int32, { primaryKey: true }) },
    });
    const ab = defineTable({
      schema: "dbo",
      name: "Fk2AB",
      columns: {
        ParentA: column(sqlType.int32, { primaryKey: true }),
        ParentB: column(sqlType.int32, { primaryKey: true }),
      },
    });
    const child = defineTable({
      schema: "dbo",
      name: "Fk3AB",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        ParentA: column(sqlType.int32, { references: [a.Id, ab.ParentA] }),
        ParentB: column(sqlType.int32, { references: [b.Id, ab.ParentB] }),
      },
    });
    expect(ab.$script.create().toSql("tsql")).toContain("PRIMARY KEY ([ParentA],[ParentB])");
    const sql = child.$script.create().toSql("tsql");
    expect(sql).toContain("FOREIGN KEY ([ParentA]) REFERENCES [dbo].[Fk1A]([Id])");
    expect(sql).toContain("FOREIGN KEY ([ParentB]) REFERENCES [dbo].[Fk1B]([Id])");
    expect(sql).toContain(
      "FOREIGN KEY ([ParentA],[ParentB]) REFERENCES [dbo].[Fk2AB]([ParentA],[ParentB])",
    );
  });
  it("preserves fixed-size ANSI and Unicode string types used by TableItAllColumnTypes", () => {
    const table = defineTable({
      schema: "dbo",
      name: "FixedStrings",
      columns: {
        Ansi: column(sqlType.string(3, { unicode: false, fixed: true })),
        Unicode: column(sqlType.string(3, { unicode: true, fixed: true })),
      },
    });
    expect(table.$script.create().toSql("tsql")).toBe(
      "CREATE TABLE [dbo].[FixedStrings]([Ansi] [char](3) NOT NULL,[Unicode] [nchar](3) NOT NULL);",
    );
  });
  it("ports DbMetadataTest.BasicTest create-script behavior", () => {
    const table = defineTable({
      schema: "schema",
      name: "table",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true, identity: true }),
        Value: column(sqlType.string(255)),
        IsActive: column(sqlType.boolean, { default: false }),
      },
      indexes: (t) => [tableIndex([t.Id, indexDesc(t.Value)]), tableIndex(t.Value)],
    });

    expect(table.$script.create().toSql({ dialect: "pgsql" })).toBe(
      'CREATE TABLE "schema"."table"("Id" int4 NOT NULL  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),"Value" character varying(255) NOT NULL,"IsActive" bool NOT NULL DEFAULT (false),CONSTRAINT "PK_schema_table" PRIMARY KEY ("Id"));CREATE INDEX "IX_schema_table_Id_Value_DESC" ON "schema"."table"("Id","Value" DESC);CREATE INDEX "IX_schema_table_Value" ON "schema"."table"("Value");',
    );
    expect(table.$metadata.indexes).toHaveLength(2);
    expect(table.$script.create().toSql("pgsql")).toBe(
      table.$script.create().toSql({ dialect: "pgsql" }),
    );
    expect(table("u").$script.create().toSql({ dialect: "pgsql" })).toBe(
      table.$script.create().toSql({ dialect: "pgsql" }),
    );
  });

  it("ports the transformed T-SQL script assertion from DbMetadataTest.BasicTest", () => {
    const table = defineTable({
      schema: "schema2",
      name: "table2",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true, identity: true }),
        Value: column(sqlType.string(255)),
        modifyDate: column(sqlType.dateTimeOffset),
      },
      indexes: (t) => [
        tableIndex([t.Id, indexDesc(t.Value)]),
        tableIndex(indexDesc(t.modifyDate), { unique: true }),
      ],
    });
    expect(table.$script.create().toSql({ dialect: "tsql" })).toBe(
      "CREATE TABLE [schema2].[table2]([Id] int NOT NULL  IDENTITY (1, 1),[Value] [nvarchar](255) NOT NULL,[modifyDate] datetimeoffset NOT NULL,CONSTRAINT [PK_schema2_table2] PRIMARY KEY ([Id]),INDEX [IX_schema2_table2_Id_Value_DESC]([Id],[Value] DESC),INDEX [IX_schema2_table2_modifyDate_DESC] UNIQUE([modifyDate] DESC));",
    );
  });

  it("ports QueryBuilder.TempTablesTest.Create", () => {
    const table = defineTable({
      schema: null,
      name: "t -- mpU\"s'er",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true, identity: true }),
        Modified: column(sqlType.date),
      },
    });
    expect(table.$script.dropAndCreate().toSql({ dialect: "pgsql" })).toBe(
      'DROP TABLE IF EXISTS "t -- mpU""s\'er";CREATE TEMP TABLE "t -- mpU""s\'er"("Id" int4 NOT NULL  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),"Modified" date NOT NULL,CONSTRAINT "PK_t -- mpU""s\'er" PRIMARY KEY ("Id"));',
    );
    expect(table.$script.dropAndCreate().toSql({ dialect: "tsql" })).toBe(
      "IF OBJECT_ID('tempdb..[#t -- mpU\"s''er]') IS NOT NULL DROP TABLE [#t -- mpU\"s'er]CREATE TABLE [#t -- mpU\"s'er]([Id] int NOT NULL  IDENTITY (1, 1),[Modified] date NOT NULL,CONSTRAINT [PK_t -- mpU\"s'er] PRIMARY KEY ([Id]));",
    );
  });

  it("renders MySQL and SQLite metadata without a database dependency", () => {
    const table = defineTable({
      schema: "app",
      name: "items",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true, identity: true }),
        Name: column(sqlType.string(50)),
      },
      indexes: (t) => [tableIndex(t.Name, { unique: true })],
    });
    expect(table.$script.create().toSql({ dialect: "mysql" })).toBe(
      "CREATE TABLE `items`(`Id` int NOT NULL AUTO_INCREMENT,`Name` varchar(50) NOT NULL,PRIMARY KEY (`Id`),UNIQUE KEY `IX_app_items_Name`(`Name`));",
    );
    expect(table.$script.create().toSql({ dialect: "sqlite" })).toBe(
      'CREATE TABLE "items"("Id" INTEGER PRIMARY KEY AUTOINCREMENT,"Name" text NOT NULL);CREATE UNIQUE INDEX "IX_app_items_Name" ON "items"("Name");',
    );
  });

  it("keeps foreign-key metadata typed and renders it from physical column ownership", () => {
    const parents = defineTable({
      schema: "dbo",
      name: "Parent",
      columns: { Id: column(sqlType.int32, { primaryKey: true }) },
    });
    const children = defineTable({
      schema: "dbo",
      name: "Child",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        ParentId: column(sqlType.int32, { references: parents.Id }),
      },
    });
    expect(children.$script.create().toSql({ dialect: "tsql" })).toContain(
      "CONSTRAINT [FK_dbo__Child_to_dbo__Parent] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Parent]([Id])",
    );
    expect(children("c").$script.create().toSql({ dialect: "tsql" })).toBe(
      children.$script.create().toSql({ dialect: "tsql" }),
    );
  });

  it("copies binary defaults and validates index metadata", () => {
    const bytes = new Uint8Array([1, 2, 255]);
    const definition = column(sqlType.binary(), { default: bytes });
    bytes[0] = 9;
    expect(definition.options.default).toEqual(new Uint8Array([1, 2, 255]));
    expect(() =>
      defineTable({
        schema: "dbo",
        name: "T",
        columns: { Id: column(sqlType.int32) },
        indexes: () => [tableIndex(createColumnRef("Missing", "T", false))],
      }),
    ).toThrow("unknown column 'Missing'");
  });
  it("uses MySQL large-object storage for unbounded binary columns", () => {
    const table = defineTable({
      schema: null,
      name: "Payload",
      columns: { Data: column(sqlType.binary()) },
    });
    expect(table.$script.create().toSql("mysql")).toBe(
      "CREATE TABLE `Payload`(`Data` longblob NOT NULL);",
    );
  });
  it("ports the script-producing portion of ScCreateDynamicTable with indexes", () => {
    const table = defineTable({
      schema: "dbo",
      name: "DynamicTable",
      columns: {
        Id: column(sqlType.int32, { primaryKey: true, identity: true }),
        Value: column(sqlType.string(255, { unicode: true })),
        IsActive: column(sqlType.boolean, { default: true }),
      },
      indexes: (t) => {
        const id = t.$metadata.columns.Id!,
          value = t.$metadata.columns.Value!;
        return [tableIndex([id, indexDesc(value)]), tableIndex(value)];
      },
    });
    expect(table.$script.create().toSql({ dialect: "tsql" })).toContain(
      "INDEX [IX_dbo_DynamicTable_Id_Value_DESC]([Id],[Value] DESC)",
    );
    expect(
      table.$script
        .create()
        .toSql({ dialect: "pgsql", schemaMap: [{ from: "dbo", to: "public" }] }),
    ).toContain('CREATE INDEX "IX_public_DynamicTable_Value" ON "public"."DynamicTable"("Value")');
    expect(table.$script.create().toSql({ dialect: "sqlite" })).toContain(
      'CREATE INDEX "IX_dbo_DynamicTable_Value" ON "DynamicTable"("Value")',
    );
  });
});
