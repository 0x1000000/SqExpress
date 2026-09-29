using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.Syntax;
using SqExpress.Syntax.Value;

namespace SqExpress.IntTest.Tables;

public static class ForeignKeyTables
{
    private const string AConstraint = "FK_FkCycleA_FkCycleB";
    private const string BConstraint = "FK_FkCycleB_FkCycleA";

    public static TableBase[] BuildTableList() =>
    [
        GetFk0(Alias.Empty), GetFk1A(Alias.Empty), GetFk1B(Alias.Empty),
        GetFk2AB(Alias.Empty), GetFk3AB(Alias.Empty),
        GetFkCycleA(Alias.Empty), GetFkCycleB(Alias.Empty)
    ];

    public static bool IsCycleTable(TableBase table) => table is TableFkCycleA or TableFkCycleB;

    public static TableFk0 GetFk0(Alias alias) => new TableFk0(alias);
    public static TableFk0 GetFk0() => new TableFk0(Alias.Auto);
    public static TableFk1A GetFk1A(Alias alias) => new TableFk1A(alias);
    public static TableFk1A GetFk1A() => new TableFk1A(Alias.Auto);
    public static TableFk1B GetFk1B(Alias alias) => new TableFk1B(alias);
    public static TableFk1B GetFk1B() => new TableFk1B(Alias.Auto);
    public static TableFk2AB GetFk2AB(Alias alias) => new TableFk2AB(alias);
    public static TableFk2AB GetFk2AB() => new TableFk2AB(Alias.Auto);
    public static TableFk3AB GetFk3AB(Alias alias) => new TableFk3AB(alias);
    public static TableFk3AB GetFk3AB() => new TableFk3AB(Alias.Auto);
    public static TableFkCycleA GetFkCycleA(Alias alias) => new TableFkCycleA(alias);
    public static TableFkCycleB GetFkCycleB(Alias alias) => new TableFkCycleB(alias);

    public static async Task DropCycleTables(IScenarioContext context)
    {
        var a = GetFkCycleA(Alias.Empty);
        var b = GetFkCycleB(Alias.Empty);
        var aName = context.SqlExporter.ToSql(a.FullName);

        switch (context.Dialect)
        {
            case SqlDialect.TSql:
                await ExecRaw(context,
                    $"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{AConstraint}' AND parent_object_id = OBJECT_ID('dbo.FkCycleA')) ALTER TABLE {aName} DROP CONSTRAINT {AConstraint}");
                break;
            case SqlDialect.PgSql:
                await ExecRaw(context, $"ALTER TABLE IF EXISTS {aName} DROP CONSTRAINT IF EXISTS {AConstraint}");
                break;
            case SqlDialect.OracleMySql:
            case SqlDialect.MariaDb:
                await ExecRaw(context, "SET FOREIGN_KEY_CHECKS = 0");
                try
                {
                    await b.Script.DropIfExist().Exec(context.Database);
                    await a.Script.DropIfExist().Exec(context.Database);
                }
                finally
                {
                    await ExecRaw(context, "SET FOREIGN_KEY_CHECKS = 1");
                }
                return;
            case SqlDialect.Sqlite:
                break;
        }

        await b.Script.DropIfExist().Exec(context.Database);
        await a.Script.DropIfExist().Exec(context.Database);
    }

    public static async Task CreateCycleTables(IScenarioContext context)
    {
        var a = GetFkCycleA(Alias.Empty);
        var b = GetFkCycleB(Alias.Empty);
        var aName = context.SqlExporter.ToSql(a.FullName);
        var bName = context.SqlExporter.ToSql(b.FullName);
        var aId = context.SqlExporter.ToSql(a.Id.ColumnName);
        var bId = context.SqlExporter.ToSql(b.Id.ColumnName);
        var aFk = context.SqlExporter.ToSql(a.FkCycleBId.ColumnName);
        var bFk = context.SqlExporter.ToSql(b.FkCycleAId.ColumnName);
        var sqlite = context.Dialect == SqlDialect.Sqlite;
        var aConstraint = $"CONSTRAINT {AConstraint} FOREIGN KEY ({aFk}) REFERENCES {bName} ({bId})";
        var bConstraint = $"CONSTRAINT {BConstraint} FOREIGN KEY ({bFk}) REFERENCES {aName} ({aId})";

        await ExecRaw(context,
            $"CREATE TABLE {aName} ({aId} INTEGER NOT NULL PRIMARY KEY, {aFk} INTEGER NULL{(sqlite ? ", " + aConstraint : "")})");
        await ExecRaw(context,
            $"CREATE TABLE {bName} ({bId} INTEGER NOT NULL PRIMARY KEY, {bFk} INTEGER NULL{(sqlite ? ", " + bConstraint : "")})");

        if (!sqlite)
        {
            await ExecRaw(context, $"ALTER TABLE {aName} ADD {aConstraint}");
            await ExecRaw(context, $"ALTER TABLE {bName} ADD {bConstraint}");
        }
    }

    private static Task ExecRaw(IScenarioContext context, string sql)
        => context.Database.Exec(new RawCommand(sql));

    private sealed class RawCommand(string sql) : IExprExec
    {
        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprUnsafeValue(new ExprUnsafeValue(sql), arg);
    }
}
