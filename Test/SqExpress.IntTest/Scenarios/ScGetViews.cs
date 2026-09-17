using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.DbMetadata;
using SqExpress.IntTest.Context;
using SqExpress.Syntax;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.IntTest.Scenarios;

public sealed class ScGetViews : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        const string tableName = "sqexpress_view_source";
        const string directName = "sqexpress_view_direct";
        const string subsetName = "sqexpress_view_subset";
        var schema = context.Dialect switch
        {
            SqlDialect.TSql => "dbo",
            SqlDialect.PgSql => "public",
            SqlDialect.Sqlite => "dbo",
            _ => "test"
        };
        string Name(string name) => context.Dialect switch
        {
            SqlDialect.TSql => $"[dbo].[{name}]",
            SqlDialect.PgSql => $"public.\"{name}\"",
            SqlDialect.MariaDb or SqlDialect.OracleMySql => $"`{name}`",
            _ => $"\"{name}\""
        };
        var stringType = context.Dialect == SqlDialect.TSql ? "NVARCHAR(40)" : "VARCHAR(40)";
        var tableSuffix = context.Dialect is SqlDialect.MariaDb or SqlDialect.OracleMySql
            ? " DEFAULT CHARACTER SET utf8mb4" : "";
        var created = new Stack<(string Kind, string Name)>();
        try
        {
            await ExecRaw($"CREATE TABLE {Name(tableName)} (id INT NOT NULL PRIMARY KEY, label {stringType} NOT NULL, amount DECIMAL(12,2) NOT NULL DEFAULT 1.5, optional_id INT NULL){tableSuffix}");
            created.Push(("TABLE", tableName));
            // Each CREATE VIEW is its own command, as required by SQL Server.
            await ExecRaw($"CREATE VIEW {Name(directName)} AS SELECT id, label, amount, optional_id FROM {Name(tableName)}");
            created.Push(("VIEW", directName));
            await ExecRaw($"CREATE VIEW {Name(subsetName)} AS SELECT label AS renamed_label, optional_id AS renamed_id FROM {Name(tableName)}");
            created.Push(("VIEW", subsetName));

            var baseline = await context.Database.GetTables();
            AssertTablesOnly(baseline);
            AssertTablesOnly(await context.Database.GetTables(skipUnknownColumnTypes: false));
            AssertTablesOnly(await context.Database.GetTables(skipUnknownColumnTypes: true));
            AssertTablesOnly(await context.Database.GetTables(new SqGetTablesOptions()));
            var source = baseline.Single(t => t.FullName.TableName == tableName);

            // PostgreSQL and SQLite report view columns as nullable, even for NOT NULL source columns.
            var nullable = context.Dialect is SqlDialect.PgSql or SqlDialect.Sqlite;
            var sqlite = context.Dialect == SqlDialect.Sqlite;
            const bool unicode = true;
            // MySQL/MariaDB report the source default directly in INFORMATION_SCHEMA.COLUMNS for this view.
            // Preserve reported metadata rather than fabricating or discarding defaults.
            ColumnMeta? amountMeta = context.Dialect is SqlDialect.MariaDb or SqlDialect.OracleMySql
                ? ColumnMeta.DefaultValue(new ExprUnsafeValue("1.50")) : null;
            var expectedDirect = SqTable.Create(schema, directName, c => new TableColumn[]
            {
                nullable ? c.CreateNullableInt32Column("id") : c.CreateInt32Column("id"),
                nullable ? c.CreateNullableStringColumn("label", 40, unicode, sqlite) : c.CreateStringColumn("label", 40, unicode),
                nullable ? c.CreateNullableDecimalColumn("amount", new DecimalPrecisionScale(12, 2)) : c.CreateDecimalColumn("amount", new DecimalPrecisionScale(12, 2), amountMeta),
                c.CreateNullableInt32Column("optional_id")
            });
            var expectedSubset = SqTable.Create(schema, subsetName, c => new TableColumn[]
            {
                nullable ? c.CreateNullableStringColumn("renamed_label", 40, unicode, sqlite) : c.CreateStringColumn("renamed_label", 40, unicode),
                c.CreateNullableInt32Column("renamed_id")
            });

            foreach (var skipUnknown in new[] { false, true })
            {
                var discovered = await context.Database.GetTables(new SqGetTablesOptions
                {
                    IncludeViews = true,
                    SkipUnknownColumnTypes = skipUnknown
                });
                if (discovered.Select(t => (t.FullName.SchemaName, t.FullName.TableName)).Distinct().Count() != discovered.Count)
                {
                    throw new Exception("Discovery returned duplicate objects.");
                }
                AssertMetadata(expectedDirect, discovered.Single(t => t.FullName.TableName == directName));
                AssertMetadata(expectedSubset, discovered.Single(t => t.FullName.TableName == subsetName));
                AssertMetadata(source, discovered.Single(t => t.FullName.TableName == tableName));
            }
        }
        finally
        {
            // Stack order removes both views before their backing table, including partial creation failures.
            while (created.Count > 0)
            {
                var item = created.Pop();
                await ExecRaw($"DROP {item.Kind} {Name(item.Name)}");
            }
        }

        Task ExecRaw(string sql) => context.Database.Exec(new RawCommand(sql));

        void AssertTablesOnly(IReadOnlyList<SqTable> tables)
        {
            if (tables.Count(t => t.FullName.TableName == tableName) != 1 ||
                tables.Any(t => t.FullName.TableName is directName or subsetName))
            {
                throw new Exception("Default discovery must include the backing table and exclude views.");
            }
        }
    }

    private static void AssertMetadata(TableBase expected, TableBase actual)
    {
        var comparison = expected.CompareWith(actual);
        if (expected.FullName.SchemaName != actual.FullName.SchemaName || expected.FullName.TableName != actual.FullName.TableName ||
            !expected.Columns.Select(c => c.ColumnName.Name).SequenceEqual(actual.Columns.Select(c => c.ColumnName.Name)) ||
            comparison != null)
        {
            var differences = string.Join(", ", expected.Columns.Zip(actual.Columns,
                (e, a) => $"{e.ColumnName.Name}: {e.CompareWith(a)}, default: {a.ColumnMeta?.ColumnDefaultValue?.ToSql(SqExpress.SqlExport.TSqlExporter.Default)}"));
            throw new Exception($"Unexpected metadata for {actual.FullName.TableName}: {differences}");
        }
    }

    // Test-only adapter for executing dialect-specific DDL without adding view syntax to the library.
    private sealed class RawCommand(string sql) : IExprExec
    {
        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprUnsafeValue(new ExprUnsafeValue(sql), arg);
    }
}
