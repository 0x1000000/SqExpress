using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScPgMergeIdentityPolyfill : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            if (context.Dialect != SqlDialect.PgSql)
            {
                context.WriteLine("Skipped: PostgreSQL-only polyfill scenario");
                return;
            }

            await TestIdentityInsertPolyfill(context);
            await TestMergePolyfill(context);
        }

        private static async Task TestIdentityInsertPolyfill(IScenarioContext context)
        {
            var table = new IdentityInsertPolyfillTable();

            await context.Database.Statement(table.Script.DropIfExist());
            await context.Database.Statement(table.Script.Create());

            await InsertInto(table, table.Value)
                .Values(10)
                .DoneWithValues()
                .Exec(context.Database);

            var identityInsert = IdentityInsertInto(table, table.Id, table.Value)
                .Values(200, 20)
                .Values(201, 21)
                .DoneWithValues();

            var sql = context.SqlExporter.ToSql(identityInsert);
            if (sql.Contains(";", StringComparison.Ordinal))
            {
                throw new Exception("Identity insert polyfill should be a single SQL statement");
            }

            await identityInsert.Exec(context.Database);

            await InsertInto(table, table.Value)
                .Values(30)
                .DoneWithValues()
                .Exec(context.Database);

            var rows = await Select(table.Id, table.Value)
                .From(table)
                .OrderBy(table.Id)
                .QueryList(context.Database, r => (Id: table.Id.Read(r), Value: table.Value.Read(r)));

            var actual = string.Join(';', rows.Select(r => $"{r.Id},{r.Value}"));
            const string expected = "1,10;200,20;201,21;202,30";
            if (actual != expected)
            {
                throw new Exception($"Identity insert polyfill failed. Expected: {expected}. Actual: {actual}");
            }

            await context.Database.Statement(table.Script.Drop());
        }

        private static async Task TestMergePolyfill(IScenarioContext context)
        {
            var table = new TestMergeTmpTable();

            await context.Database.Statement(table.Script.DropIfExist());
            await context.Database.Statement(table.Script.Create());

            await InsertInto(table, table.Id, table.Value)
                .Values(1, 10)
                .Values(2, 20)
                .DoneWithValues()
                .Exec(context.Database);

            var mergeSource = new[]
            {
                new TestMergeData(1, 100),
                new TestMergeData(3, 300)
            };

            var merge = MergeDataInto(table, mergeSource)
                .MapDataKeys(TestMergeData.GetUpdateKeyMapping)
                .MapData(TestMergeData.GetUpdateMapping)
                .WhenMatchedThenUpdate()
                .AlsoSet(s => s.Set(s.Target.Version, s.Target.Version + 1))
                .WhenNotMatchedByTargetThenInsert()
                .Done();

            var mergeSql = context.SqlExporter.ToSql(merge);
            if (mergeSql.Contains("CREATE TEMP TABLE", StringComparison.OrdinalIgnoreCase)
                || mergeSql.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("MERGE polyfill should not use CREATE/DROP temp table in PostgreSQL");
            }

            if (mergeSql.Contains(";", StringComparison.Ordinal))
            {
                throw new Exception("MERGE polyfill should be a single SQL statement");
            }

            await merge.Exec(context.Database);

            var rows = await Select(TestMergeDataRow.GetColumns(table))
                .From(table)
                .OrderBy(table.Id)
                .QueryList(context.Database, r => TestMergeDataRow.Read(r, table));

            var actual = string.Join(';', rows.Select(r => $"{r.Id},{r.Value},{r.Version}"));
            const string expected = "1,100,1;2,20,0;3,300,0";
            if (actual != expected)
            {
                throw new Exception($"MERGE polyfill failed. Expected: {expected}. Actual: {actual}");
            }

            await context.Database.Statement(table.Script.Drop());
        }

        private class IdentityInsertPolyfillTable : TempTableBase
        {
            public IdentityInsertPolyfillTable(Alias alias = default) : base("PgIdentityInsertPolyfillTable", alias)
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey().Identity());
                this.Value = this.CreateInt32Column("Value");
            }

            public Int32TableColumn Id { get; }

            public Int32TableColumn Value { get; }
        }
    }
}
