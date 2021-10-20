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
            try
            {
                var scenario = new ScCreateTables()
                    .Then(new ScInsertUserData())
                    .Then(new ScSqlInjections())
                    .Then(new ScLike())
                    .Then(new ScDeleteCustomersByTopUser())
                    .Then(new ScInsertCompanies())
                    .Then(new ScUpdateUsers())
                    .Then(new ScUpdateUserData())
                    .Then(new ScAllColumnTypes())
                    .Then(new ScAllColumnTypesExportImport())
                    .Then(new ScSelectLogic())
                    .Then(new ScSelectTop())
                    .Then(new ScSelectSets())
                    .Then(new ScTempTables())
                    .Then(new ScCreateOrders())
                    .Then(new ScAnalyticFunctionsOrders())
                    .Then(new ScTransactions(false))
                    .Then(new ScTransactions(true))
                    .Then(new ScMerge())
                    ;

                await ExecScenarioAll(
                    scenario: scenario,
                    msSqlConnectionString: "Data Source=(local);Initial Catalog=TestDatabase;Integrated Security=True",
                    pgSqlConnectionString: "Host=localhost;Port=5432;Username=postgres;Password=test;Database=test",
                    mySqlConnectionString: "server=127.0.0.1;uid=test;pwd=test;database=test");
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

        private static async Task ExecScenarioAll(IScenario scenario, string msSqlConnectionString, string pgSqlConnectionString, string mySqlConnectionString)
        {
            Stopwatch stopwatch = new Stopwatch();

            Console.WriteLine();
            Console.WriteLine("-MS SQL Test-------");
            stopwatch.Restart();
            await ExecMsSql(scenario, msSqlConnectionString);
            stopwatch.Stop();
            Console.WriteLine($"-MS SQL Test End: {stopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine("-Postgres Test-----");
            stopwatch.Start();
            await ExecNpgSql(scenario, pgSqlConnectionString);
            stopwatch.Stop();
            Console.WriteLine($"-Postgres Test End: {stopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine();
            Console.WriteLine("-MY SQL Test-------");
            stopwatch.Restart();
            await ExecMySql(scenario, mySqlConnectionString);
            stopwatch.Stop();
            Console.WriteLine($"-MY SQL Test End: {stopwatch.ElapsedMilliseconds} ms");
        }

        private static async Task ExecMsSql(IScenario scenario, string connectionString)
        {
            ISqDatabase Factory() =>
                new SqDatabase<SqlConnection>(new SqlConnection(connectionString),
                    (conn, sql) => new SqlCommand(sql, conn),
                    TSqlExporter.Default,
                    disposeConnection: true);

            using var database = Factory();

            await scenario.Exec(new ScenarioContext(database, SqlDialect.TSql, Factory));
        }

        private static async Task ExecNpgSql(IScenario scenario, string connectionString)
        {
            ISqDatabase Factory() =>
                new SqDatabase<NpgsqlConnection>(
                    new NpgsqlConnection(connectionString),
                    (conn, sql) => new NpgsqlCommand(sql, conn),
                    new PgSqlExporter(SqlBuilderOptions.Default.WithSchemaMap(new[] { new SchemaMap("dbo", "public") })),
                    disposeConnection: true);

            using var database = Factory();

            await scenario.Exec(new ScenarioContext(database, SqlDialect.PgSql, Factory));
        }

        private static async Task ExecMySql(IScenario scenario, string connectionString)
        {
            ISqDatabase Factory() =>
                new SqDatabase<MySqlConnection>(
                    new MySqlConnection(connectionString),
                    (conn, sql) => new MySqlCommand(sql, conn),
                    new MySqlExporter(SqlBuilderOptions.Default),
                    disposeConnection: true);

            using var database = Factory();
            await scenario.Exec(new ScenarioContext(database, SqlDialect.MySql, Factory));
        }
    }
}
