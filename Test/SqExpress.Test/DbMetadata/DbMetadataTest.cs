using System;
using System.Linq;
using NUnit.Framework;
using SqExpress.DbMetadata;
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
}
