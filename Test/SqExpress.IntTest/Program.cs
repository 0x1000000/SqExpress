using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
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
        static async Task Main()
        {
            await RunTests(ParametrizationMode.None);
            await RunTests(ParametrizationMode.LiteralFallback);
            await RunTests(ParametrizationMode.ThrowOnLimit, new ScParametrizationLimitBoundary());
        }

        private static async Task RunTests(ParametrizationMode parametrizationMode, IScenario? customScenario = null)
        {
            const string msSqlConnectionString = "Data Source=(local);Initial Catalog=TestDatabase;Integrated Security=True";
            const string pgSqlConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=test;Database=test";
            const string oracleMySqlConnectionString = "server=127.0.0.1;port=3306;uid=test;pwd=test;database=test";
            const string mariaDbConnectionString = "server=127.0.0.1;port=3307;uid=test;pwd=test;database=test";
            try
            {
                var scenario = customScenario ?? new ScCreateTables()
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
                        .Then(new ScParametrizationLimitBoundary())
                        .Then(new ScMergeExpr())
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

                await ExecScenarioAll(
                    scenario: scenario,
                    msSqlConnectionString: msSqlConnectionString,
                    pgSqlConnectionString: pgSqlConnectionString,
                    oracleMySqlConnectionString: oracleMySqlConnectionString,
                    mariaDbConnectionString: mariaDbConnectionString,
                    parametrizationMode
                );

                await ExecCrossDbScenario(
                    msSqlConnectionString: msSqlConnectionString,
                    pgSqlConnectionString: pgSqlConnectionString,
                    parametrizationMode
                );
            }
            catch (SqDatabaseCommandException commandException)
            {
                Console.WriteLine(commandException.CommandText);
                Console.WriteLine(commandException.InnerException);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task ExecScenarioAll(
            IScenario scenario,
            string msSqlConnectionString,
            string pgSqlConnectionString,
            string oracleMySqlConnectionString,
            string mariaDbConnectionString,
            ParametrizationMode parametrizationMode)
        {
            Stopwatch stopwatch = new Stopwatch();

            Console.WriteLine();
            Console.WriteLine("-MS SQL Test-------");
            stopwatch.Restart();
            await ExecMsSql(scenario, msSqlConnectionString, parametrizationMode);
            stopwatch.Stop();
            Console.WriteLine($"-MS SQL Test End: {stopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine("-Postgres Test-----");
            stopwatch.Restart();
            await ExecNpgSql(scenario, pgSqlConnectionString, parametrizationMode);
            stopwatch.Stop();
            Console.WriteLine($"-Postgres Test End: {stopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine();
            Console.WriteLine("-Oracle MySQL Test-");
            stopwatch.Restart();
            await ExecMySql(scenario, oracleMySqlConnectionString, MySqlExporter.OracleDefault, SqlDialect.OracleMySql, parametrizationMode);
            stopwatch.Stop();
            Console.WriteLine($"-Oracle MySQL Test End: {stopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine();
            Console.WriteLine("-MariaDB Test------");
            stopwatch.Restart();
            await ExecMySql(scenario, mariaDbConnectionString, MySqlExporter.MariaDbDefault, SqlDialect.MariaDb, parametrizationMode);
            stopwatch.Stop();
            Console.WriteLine($"-MariaDB Test End: {stopwatch.ElapsedMilliseconds} ms");
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
    }
}
