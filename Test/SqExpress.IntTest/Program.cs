using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using SqExpress.DataAccess;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Scenarios;
using SqExpress.SqlExport;

namespace SqExpress.IntTest
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var options = RunnerOptions.Parse(args);

            if (options.Parametrizations.Contains(ParametrizationMode.None))
            {
                await RunTests(ParametrizationMode.None, options);
            }

            if (options.Parametrizations.Contains(ParametrizationMode.LiteralFallback))
            {
                await RunTests(ParametrizationMode.LiteralFallback, options);
            }

            if (options.Parametrizations.Contains(ParametrizationMode.ThrowOnLimit))
            {
                await RunTests(ParametrizationMode.ThrowOnLimit, options, new ScParametrizationLimitBoundary());
            }
        }

        private static async Task RunTests(ParametrizationMode parametrizationMode, RunnerOptions options, IScenario? customScenario = null)
        {
            const string msSqlConnectionString = "Data Source=(local);Initial Catalog=TestDatabase;Integrated Security=True";
            const string pgSqlConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=test;Database=test";
            const string oracleMySqlConnectionString = "server=127.0.0.1;port=3306;uid=test;pwd=test;database=test";
            const string mariaDbConnectionString = "server=127.0.0.1;port=3307;uid=test;pwd=test;database=test";
            var sqliteConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"sqexpress-inttest-{parametrizationMode}-{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            try
            {
                var scenario = customScenario ?? BuildScenario();

                await ExecScenarioSelected(
                    scenario: scenario,
                    msSqlConnectionString: msSqlConnectionString,
                    pgSqlConnectionString: pgSqlConnectionString,
                    oracleMySqlConnectionString: oracleMySqlConnectionString,
                    mariaDbConnectionString: mariaDbConnectionString,
                    sqliteConnectionString: sqliteConnectionString,
                    parametrizationMode: parametrizationMode,
                    options: options
                );

                if (options.ShouldRunCrossDbCompare)
                {
                    await ExecCrossDbScenario(
                        msSqlConnectionString: msSqlConnectionString,
                        pgSqlConnectionString: pgSqlConnectionString,
                        parametrizationMode: parametrizationMode
                    );
                }
            }
            catch (SqDatabaseCommandException commandException)
            {
                Console.WriteLine(commandException.CommandText);
                Console.WriteLine(commandException.InnerException);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static IScenario BuildScenario()
        {
            return new ScCreateTables()
                .Then(new ScInsertUserData())
                .Then(new ScSqlInjections())
                .Then(new ScLike())
                .Then(new ScDeleteCustomersByTopUser())
                .Then(new ScInsertCompanies())
                .Then(new ScUpdateUsers())
                .Then(new ScUpdateUserData())
                .Then(new ScSelectSeveralModelsWithPrefix())
                .Then(new ScAllColumnTypes())
                .Then(new ScAllColumnTypesExportImport())
                .Then(new ScSelectLogic())
                .Then(new ScSelectTop())
                .Then(new ScSelectSets())
                .Then(new ScTempTables())
                .Then(new ScGroupByExpression())
                .Then(new ScSelectValue())
                .Then(new ScCreateOrders())
                .Then(new ScAnalyticFunctionsOrders())
                .Then(new ScTransactions(false))
                .Then(new ScTransactions(true))
                .Then(new ScTransactionsAsync(false))
                .Then(new ScTransactionsAsync(true))
                .Then(new ScTransactionsDeadlock())
                .Then(new ScMerge())
                .Then(new ScPgMergeIdentityPolyfill())
                .Then(new ScParametrizationTypes())
                .Then(new ScParserParamsExprValues())
                .Then(new ScParserTypedParams())
                .Then(new ScParametrizationLimitBoundary())
                .Then(new ScMergeExpr())
                .Then(new ScMergeExprEdgeCases())
                .Then(new ScModelSelector())
                .Then(new ScCancellation())
                .Then(new ScCte())
                .Then(new ScCteCross())
                .Then(new ScTreeClosure())
                .Then(new ScBitwise())
                .Then(new ScJsonTableFunction())
                .Then(new ScGetTables())
                .Then(new ScDateDiff())
                .Then(new ScCreateDynamicTable())
                .Then(new ScPortableScalarFunctions());
        }

        private static async Task ExecScenarioSelected(
            IScenario scenario,
            string msSqlConnectionString,
            string pgSqlConnectionString,
            string oracleMySqlConnectionString,
            string mariaDbConnectionString,
            string sqliteConnectionString,
            ParametrizationMode parametrizationMode,
            RunnerOptions options)
        {
            Stopwatch stopwatch = new Stopwatch();

            foreach (var dialect in options.Dialects)
            {
                Console.WriteLine();
                Console.WriteLine(GetDialectBanner(dialect));
                stopwatch.Restart();

                switch (dialect)
                {
                    case SqlDialect.TSql:
                        await ExecMsSql(scenario, msSqlConnectionString, parametrizationMode);
                        break;
                    case SqlDialect.PgSql:
                        await ExecNpgSql(scenario, pgSqlConnectionString, parametrizationMode);
                        break;
                    case SqlDialect.OracleMySql:
                        await ExecMySql(scenario, oracleMySqlConnectionString, MySqlExporter.OracleDefault, SqlDialect.OracleMySql, parametrizationMode);
                        break;
                    case SqlDialect.MariaDb:
                        await ExecMySql(scenario, mariaDbConnectionString, MySqlExporter.MariaDbDefault, SqlDialect.MariaDb, parametrizationMode);
                        break;
                    case SqlDialect.Sqlite:
                        await ExecSqlite(scenario, sqliteConnectionString, parametrizationMode);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dialect), dialect, null);
                }

                stopwatch.Stop();
                Console.WriteLine($"{GetDialectFooter(dialect)}: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        private static string GetDialectBanner(SqlDialect dialect)
        {
            return dialect switch
            {
                SqlDialect.TSql => "-MS SQL Test-------",
                SqlDialect.PgSql => "-Postgres Test-----",
                SqlDialect.OracleMySql => "-Oracle MySQL Test-",
                SqlDialect.MariaDb => "-MariaDB Test------",
                SqlDialect.Sqlite => "-SQLite Test-------",
                _ => throw new ArgumentOutOfRangeException(nameof(dialect), dialect, null)
            };
        }

        private static string GetDialectFooter(SqlDialect dialect)
        {
            return dialect switch
            {
                SqlDialect.TSql => "-MS SQL Test End",
                SqlDialect.PgSql => "-Postgres Test End",
                SqlDialect.OracleMySql => "-Oracle MySQL Test End",
                SqlDialect.MariaDb => "-MariaDB Test End",
                SqlDialect.Sqlite => "-SQLite Test End",
                _ => throw new ArgumentOutOfRangeException(nameof(dialect), dialect, null)
            };
        }

        private static async Task ExecCrossDbScenario(
            string msSqlConnectionString,
            string pgSqlConnectionString,
            ParametrizationMode parametrizationMode)
        {
            using var msSqlDb = GetMsSqlDatabase(msSqlConnectionString, TSqlExporter.Default, parametrizationMode);
            using var pgSqlDb = GetPgSqlDatabase(pgSqlConnectionString, PgSqlExporter.Default, parametrizationMode);

            Console.WriteLine("-Tables comparison (MS with PG)");
            await Helpers.CompareDatabases(msSqlDb, pgSqlDb);
        }

        private static async Task ExecMsSql(IScenario scenario, string connectionString, ParametrizationMode parametrizationMode)
        {
            var sqlExporter = TSqlExporter.Default;
            await using var database = GetMsSqlDatabase(connectionString, sqlExporter, parametrizationMode);

            await scenario.Exec(
                new ScenarioContext(
                    database,
                    SqlDialect.TSql,
                    () => GetMsSqlDatabase(connectionString, sqlExporter, parametrizationMode),
                    sqlExporter,
                    parametrizationMode
                )
            );
        }

        private static async Task ExecNpgSql(IScenario scenario, string connectionString, ParametrizationMode parametrizationMode)
        {
            var sqlExporter =
                new PgSqlExporter(SqlBuilderOptions.Default.WithSchemaMap(new[] { new SchemaMap("dbo", "public") }));

            await using var database = GetPgSqlDatabase(connectionString, sqlExporter, parametrizationMode);
            await scenario.Exec(
                new ScenarioContext(
                    database,
                    SqlDialect.PgSql,
                    () => GetPgSqlDatabase(connectionString, sqlExporter, parametrizationMode),
                    sqlExporter,
                    parametrizationMode
                )
            );
        }

        private static async Task ExecMySql(IScenario scenario, string connectionString, MySqlExporter sqlExporter, SqlDialect dialect, ParametrizationMode parametrizationMode)
        {
            await using var database = GetMySqlDatabase(connectionString, sqlExporter, parametrizationMode);

            await scenario.Exec(
                new ScenarioContext(
                    database,
                    dialect,
                    () => GetMySqlDatabase(connectionString, sqlExporter, parametrizationMode),
                    sqlExporter,
                    parametrizationMode
                )
            );
        }

        private static async Task ExecSqlite(IScenario scenario, string connectionString, ParametrizationMode parametrizationMode)
        {
            var sqlExporter = SqliteExporter.Default;
            await using var database = GetSqliteDatabase(connectionString, sqlExporter, parametrizationMode);

            await scenario.Exec(
                new ScenarioContext(
                    database,
                    SqlDialect.Sqlite,
                    () => GetSqliteDatabase(connectionString, sqlExporter, parametrizationMode),
                    sqlExporter,
                    parametrizationMode
                )
            );
        }

        private static ISqDatabase GetMsSqlDatabase(string connectionString, ISqlExporter sqlExporter, ParametrizationMode parametrizationMode)
            => new SqDatabase<SqlConnection>(
                new SqlConnection(connectionString),
                (conn, sql) => new SqlCommand(sql, conn),
                sqlExporter,
                parametrizationMode,
                disposeConnection: true
            );

        private static ISqDatabase GetPgSqlDatabase(string connectionString, ISqlExporter sqlExporter, ParametrizationMode parametrizationMode)
            => new SqDatabase<NpgsqlConnection>(
                new NpgsqlConnection(connectionString),
                (conn, sql) => new NpgsqlCommand(sql, conn),
                sqlExporter,
                parametrizationMode,
                disposeConnection: true
            );

        private static ISqDatabase GetMySqlDatabase(string connectionString, ISqlExporter sqlExporter, ParametrizationMode parametrizationMode)
            => new SqDatabase<MySqlConnection>(
                new MySqlConnection(connectionString),
                (conn, sql) => new MySqlCommand(sql, conn),
                sqlExporter,
                parametrizationMode,
                disposeConnection: true
            );

        private static ISqDatabase GetSqliteDatabase(string connectionString, ISqlExporter sqlExporter, ParametrizationMode parametrizationMode)
            => new SqDatabase<SqliteConnection>(
                new SqliteConnection(connectionString),
                (conn, sql) => new SqliteCommand(sql, conn),
                sqlExporter,
                parametrizationMode,
                disposeConnection: true
            );

        private sealed class RunnerOptions
        {
            public IReadOnlyList<SqlDialect> Dialects { get; init; } = Array.Empty<SqlDialect>();
            public IReadOnlyList<ParametrizationMode> Parametrizations { get; init; } = Array.Empty<ParametrizationMode>();
            public bool ShouldRunCrossDbCompare { get; init; }

            public static RunnerOptions Parse(string[] args)
            {
                var dialects = ParseDialects(GetOptionValue(args, "--dialects")) ?? DefaultDialects();
                var parametrizations = ParseParametrizations(GetOptionValue(args, "--parametrization")) ?? DefaultParametrizations();
                var crossDbCompare = ParseBool(GetOptionValue(args, "--cross-db-compare"))
                    ?? (dialects.Contains(SqlDialect.TSql) && dialects.Contains(SqlDialect.PgSql));

                return new RunnerOptions
                {
                    Dialects = dialects,
                    Parametrizations = parametrizations,
                    ShouldRunCrossDbCompare = crossDbCompare
                };
            }

            private static string? GetOptionValue(string[] args, string name)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException($"Missing value for '{name}'.");
                    }

                    return args[i + 1];
                }

                return null;
            }

            private static IReadOnlyList<SqlDialect>? ParseDialects(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                var items = SplitList(raw);
                if (items.Contains("all", StringComparer.OrdinalIgnoreCase))
                {
                    return DefaultDialects();
                }

                return items.Select(ParseDialect).Distinct().ToArray();
            }

            private static SqlDialect ParseDialect(string raw)
            {
                return raw.Trim().ToLowerInvariant() switch
                {
                    "tsql" => SqlDialect.TSql,
                    "pgsql" => SqlDialect.PgSql,
                    "mariadb" => SqlDialect.MariaDb,
                    "mysql-oracle" => SqlDialect.OracleMySql,
                    "sqlite" => SqlDialect.Sqlite,
                    _ => throw new ArgumentException($"Unknown dialect '{raw}'.")
                };
            }

            private static IReadOnlyList<ParametrizationMode>? ParseParametrizations(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                var items = SplitList(raw);
                if (items.Contains("all", StringComparer.OrdinalIgnoreCase))
                {
                    return DefaultParametrizations();
                }

                return items.Select(ParseParametrization).Distinct().ToArray();
            }

            private static ParametrizationMode ParseParametrization(string raw)
            {
                return raw.Trim().ToLowerInvariant() switch
                {
                    "none" => ParametrizationMode.None,
                    "literal-fallback" => ParametrizationMode.LiteralFallback,
                    "throw-on-limit" => ParametrizationMode.ThrowOnLimit,
                    _ => throw new ArgumentException($"Unknown parametrization '{raw}'.")
                };
            }

            private static bool? ParseBool(string? raw)
            {
                if (raw == null)
                {
                    return null;
                }

                if (bool.TryParse(raw, out var value))
                {
                    return value;
                }

                throw new ArgumentException($"Unknown boolean value '{raw}'.");
            }

            private static string[] SplitList(string raw)
            {
                return raw
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            private static IReadOnlyList<SqlDialect> DefaultDialects()
            {
                return new[]
                {
                    SqlDialect.TSql,
                    SqlDialect.PgSql,
                    SqlDialect.OracleMySql,
                    SqlDialect.MariaDb,
                    SqlDialect.Sqlite
                };
            }

            private static IReadOnlyList<ParametrizationMode> DefaultParametrizations()
            {
                return new[]
                {
                    ParametrizationMode.None,
                    ParametrizationMode.LiteralFallback,
                    ParametrizationMode.ThrowOnLimit
                };
            }
        }
    }
}
