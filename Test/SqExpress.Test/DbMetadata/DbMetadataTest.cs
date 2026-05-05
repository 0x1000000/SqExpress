using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.DbMetadata.Internal;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.SqlExport;

namespace SqExpress.Test.DbMetadata;

[TestFixture]
public class DbMetadataTest
{
    [Test]
    public void BasicTest()
    {
        var tbl = SqTable.Create(
            "schema",
            "table",
            b => b
                .AppendInt32Column("Id", ColumnMeta.PrimaryKey().Identity())
                .AppendStringColumn("Value", 255, true)
                .AppendBooleanColumn("IsActive", ColumnMeta.DefaultValue(false)),
            i => i
                .AppendIndex(i.Asc("Id"), i.Desc("Value"))
                .AppendIndex(i.Asc("Value"))
        );

        var createScript = tbl.Script.Create().ToSql(PgSqlExporter.Default);
        var expected = "CREATE TABLE \"schema\".\"table\"(\"Id\" int4 NOT NULL  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),\"Value\" character varying(255) NOT NULL,\"IsActive\" bool NOT NULL DEFAULT (false),CONSTRAINT \"PK_schema_table\" PRIMARY KEY (\"Id\"));CREATE INDEX \"IX_schema_table_Id_Value_DESC\" ON \"schema\".\"table\"(\"Id\",\"Value\" DESC);CREATE INDEX \"IX_schema_table_Value\" ON \"schema\".\"table\"(\"Value\");";
        Assert.AreEqual(expected, createScript);


        tbl = tbl.With(
            tbl.FullName.WithSchemaName("schema2").WithTableName("table2"),
            (cols, app) => app.AppendColumns(cols.Where(c => c.ColumnName.Name != "IsActive"))
                .AppendDateTimeOffsetColumn("modifyDate"),
            (indexes, app) => app.AppendIndexes(indexes.Where(i=>i.Columns.Count > 1)).AddUniqueIndex(app.Desc("modifyDate"))
        );

        createScript = tbl.Script.Create().ToSql(TSqlExporter.Default);
        expected = "CREATE TABLE [schema2].[table2]([Id] int NOT NULL  IDENTITY (1, 1),[Value] [nvarchar](255) NOT NULL,[modifyDate] datetimeoffset NOT NULL,CONSTRAINT [PK_schema2_table2] PRIMARY KEY ([Id]),INDEX [IX_schema2_table2_Id_Value_DESC]([Id],[Value] DESC),INDEX [IX_schema2_table2_modifyDate_DESC] UNIQUE([modifyDate] DESC));";
        Assert.AreEqual(expected, createScript);
    }

    [Test]
    public void DbModelMapper_ToSqDbTables_SupportsCyclicForeignKeys()
    {
        var tableA = new TableRef("dbo", "TableA");
        var tableB = new TableRef("dbo", "TableB");
        var tableModels = new List<TableModel>
        {
            new TableModel(
                "TableA",
                tableA,
                new List<ColumnModel>
                {
                    new ColumnModel("Id", new ColumnRef("dbo", "TableA", "Id"), 1, new Int32ColumnType(false), new PkInfo(0, false), false, null, null),
                    new ColumnModel("BId", new ColumnRef("dbo", "TableA", "BId"), 2, new Int32ColumnType(false), null, false, null, new List<ColumnRef> { new ColumnRef("dbo", "TableB", "Id") })
                },
                new List<IndexModel>()),
            new TableModel(
                "TableB",
                tableB,
                new List<ColumnModel>
                {
                    new ColumnModel("Id", new ColumnRef("dbo", "TableB", "Id"), 1, new Int32ColumnType(false), new PkInfo(0, false), false, null, null),
                    new ColumnModel("AId", new ColumnRef("dbo", "TableB", "AId"), 2, new Int32ColumnType(false), null, false, null, new List<ColumnRef> { new ColumnRef("dbo", "TableA", "Id") })
                },
                new List<IndexModel>())
        };

        var tables = DbModelMapper.ToSqDbTables(tableModels, false);

        var sqTableA = tables.Single(t => t.FullName.TableName == "TableA");
        var sqTableB = tables.Single(t => t.FullName.TableName == "TableB");
        var tableAForeignKey = sqTableA.GetColumn("BId").ColumnMeta?.ForeignKeyColumns?.Single();
        var tableBForeignKey = sqTableB.GetColumn("AId").ColumnMeta?.ForeignKeyColumns?.Single();

        Assert.NotNull(tableAForeignKey);
        Assert.NotNull(tableBForeignKey);
        Assert.AreEqual("TableB", tableAForeignKey!.Table.FullName.TableName);
        Assert.AreEqual("Id", tableAForeignKey.ColumnName.Name);
        Assert.AreEqual("TableA", tableBForeignKey!.Table.FullName.TableName);
        Assert.AreEqual("Id", tableBForeignKey.ColumnName.Name);
    }
}
