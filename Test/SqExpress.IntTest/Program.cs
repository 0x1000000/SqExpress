using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
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
            try
            {
                var scenario = new ScCreateTables()
                    .Then(new ScInsertUserData())
                    .Then(new ScDeleteCustomersByTopUser())
                    .Then(new ScInsertCompanies())
                    .Then(new ScUpdateUsers())
                    .Then(new ScAllColumnTypes())
                    .Then(new ScSelectLogic());

                await ExecScenarioAll(scenario: scenario);
                //After warming
                //await ExecScenarioAll(scenario: scenario);
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

        private static async Task ExecScenarioAll(IScenario scenario)
        {
            Stopwatch stopwatch = new Stopwatch();

            Console.WriteLine("-Postgres Test-----");
            stopwatch.Start();
            await ExecNpgSql(scenario);
            stopwatch.Stop();
            Console.WriteLine($"-Postgres Test End: {stopwatch.ElapsedMilliseconds} ms");


            Console.WriteLine();
            Console.WriteLine("-MS SQL Test-------");
            stopwatch.Restart();
            await ExecMsSql(scenario);
            stopwatch.Stop();
            Console.WriteLine($"-MS SQL Test End: {stopwatch.ElapsedMilliseconds} ms");
        }

        private static async Task ExecMsSql(IScenario scenario)
        {
            await using var connection = new SqlConnection("Data Source=(local);Initial Catalog=TestDatabase;Integrated Security=True");
            using var database = new SqDatabase<SqlConnection>(connection, (conn, sql) =>
            {
                return new SqlCommand(sql, conn) {Transaction = null};
            }, new TSqlExporter(SqlBuilderOptions.Default.WithSchemaMap(new []{new SchemaMap("public", "dbo") })));
            await scenario.Exec(new ScenarioContext(database, false));
        }

        private static async Task ExecNpgSql(IScenario scenario)
        {
            await using var connection = new NpgsqlConnection("Host=localhost;Port=5432;Username=postgres;Password=test;Database=test");
            using var database = new SqDatabase<NpgsqlConnection>(connection, (conn, sql) => new NpgsqlCommand(sql, conn) { Transaction = null }, PgSqlExporter.Default);
            await scenario.Exec(new ScenarioContext(database, true));
        }
    }
}
