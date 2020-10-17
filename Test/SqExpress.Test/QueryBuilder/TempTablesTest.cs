using System;
using NUnit.Framework;
using SqExpress.SqlExport;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class TempTablesTest
    {
        [Test]
        public void Create()
        {
            var tbl = new IdModified();

            var actual = PgSqlExporter.Default.ToSql(tbl.Script.DropAndCreate());
            var expected = "DROP TABLE IF EXISTS \"t -- mpU\"\"s'er\";CREATE TEMP TABLE \"t -- mpU\"\"s'er\"(\"Id\" int4 NOT NULL  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),\"Modified\" date NOT NULL,CONSTRAINT \"PK_t -- mpU\"\"s'er\" PRIMARY KEY (\"Id\"));";
            Assert.AreEqual(expected, actual);

            actual = TSqlExporter.Default.ToSql(tbl.Script.DropAndCreate());
            expected = "IF OBJECT_ID('tempdb..[#t -- mpU\"s''er]') IS NOT NULL DROP TABLE [#t -- mpU\"s'er]CREATE TABLE [#t -- mpU\"s'er]([Id] int NOT NULL  IDENTITY (1, 1),[Modified] date NOT NULL,CONSTRAINT [PK_t -- mpU\"s'er] PRIMARY KEY ([Id]));";
            Assert.AreEqual(expected, actual);
            Console.WriteLine(actual);
        }

        private class IdModified : TempTableBase
        {
            public Int32TableColumn Id { get; }

            public DateTimeTableColumn Modified { get; }

            public IdModified(Alias alias = default) : base("t -- mpU\"s'er", alias)
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.Identity().PrimaryKey());
                this.Modified = this.CreateDateTimeColumn("Modified", true);
            }
        }
    }
}