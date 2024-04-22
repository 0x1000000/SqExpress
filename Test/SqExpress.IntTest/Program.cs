using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
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
            const string msSqlConnectionString = "Data Source=(local);Initial Catalog=TestDatabase;Integrated Security=True";
            const string pgSqlConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=test;Database=test";
            const string mySqlConnectionString = "server=127.0.0.1;uid=test;pwd=test;database=test";

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
                       .Then(new ScMerge())
                       .Then(new ScMergeExpr())
                       .Then(new ScModelSelector())
                       .Then(new ScCancellation())
                       .Then(new ScCte())
                       .Then(new ScCteCross())
                       .Then(new ScTreeClosure())
                       .Then(new ScBitwise())
                       .Then(new ScGetTables())
                    ;

                await ExecScenarioAll(
                    scenario: scenario,
                    msSqlConnectionString: msSqlConnectionString,
                    pgSqlConnectionString: pgSqlConnectionString,
                    mySqlConnectionString: mySqlConnectionString
                );

                await CompareMetadata(
                    msSqlConnectionString: msSqlConnectionString,
                    pgSqlConnectionString: pgSqlConnectionString,
                    mySqlConnectionString: mySqlConnectionString
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
            string mySqlConnectionString)
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

        private static async Task CompareMetadata(
            string msSqlConnectionString,
            string pgSqlConnectionString,
            string mySqlConnectionString)
        {
            Console.WriteLine("-Tables comparison (MS with PG)");

            using var msSqlDb = GetMsSqlDatabase(msSqlConnectionString, TSqlExporter.Default);
            using var pgSqlDb = GetPgSqlDatabase(pgSqlConnectionString, PgSqlExporter.Default);

            var msTables = await msSqlDb.GetTables();
            var pgTables = await pgSqlDb.GetTables();

            var comparison = msTables.CompareWith(pgTables, t => t.TableName);
            if (comparison != null)
            {
                foreach (var comparisonDifferentTable in comparison.DifferentTables)
                {
                    if (comparisonDifferentTable.TableComparison.ExtraColumns.Count > 0 ||
                        comparisonDifferentTable.TableComparison.ExtraColumns.Count > 0)
                    {
                        throw new Exception("Column set should be the same");
                    }

                    foreach (var differentColumn in comparisonDifferentTable.TableComparison.DifferentColumns)
                    {
                        if (!CompareLists(
                                differentColumn.Column.ColumnMeta?.ForeignKeyColumns,
                                differentColumn.OtherColumn.ColumnMeta?.ForeignKeyColumns,
                                (fk1, fk2) => fk1.ColumnName.Name == fk2.ColumnName.Name,
                                false
                            ))
                        {
                            //throw new Exception("Column FK should be the same");
                        }
                    }

                    var ic = comparisonDifferentTable.TableComparison.IndexComparison;
                    if (ic != null)
                    {
                        if ((ic.ExtraIndexes?.Count ?? 0) != (ic.MissedIndexes?.Count ?? 0))
                        {
                            throw new Exception("Indexes should be the same");
                        }

                        if (!CompareLists(
                                ic.ExtraIndexes,
                                ic.MissedIndexes,
                                (cl1, cl2) => CompareLists(
                                    cl1.Columns,
                                    cl2.Columns,
                                    (c1, c2) => c1.Column.ColumnName.Name == c2.Column.ColumnName.Name,
                                    true
                                ),
                                false
                            ))
                        {
                            throw new Exception("Indexes should equal");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Equal");
            }

            bool CompareLists<T>(IReadOnlyList<T>? l1, IReadOnlyList<T>? l2, Func<T, T, bool> comparer, bool order)
            {
                if (ReferenceEquals(l1, l2))
                {
                    return true;
                }

                if (l1?.Count != l2?.Count)
                {
                    return false;
                }

                for (var i = 0; i < l1!.Count; i++)
                {
                    if (order)
                    {
                        if (!comparer(l1[i], l2![i]))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        var iClose = i;
                        if (!l2!.Any(x => comparer(l1[iClose], x)))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        private static async Task ExecMsSql(IScenario scenario, string connectionString)
        {
            var sqlExporter = TSqlExporter.Default;
            using var database = GetMsSqlDatabase(connectionString, sqlExporter);

            await scenario.Exec(
                new ScenarioContext(
                    database,
                    SqlDialect.TSql,
                    () => GetMsSqlDatabase(connectionString, sqlExporter),
                    sqlExporter
                )
            );
        }

        private static async Task ExecNpgSql(IScenario scenario, string connectionString)
        {
            var sqlExporter =
                new PgSqlExporter(SqlBuilderOptions.Default.WithSchemaMap(new[] { new SchemaMap("dbo", "public") }));

            using var database = GetPgSqlDatabase(connectionString, sqlExporter);
            await scenario.Exec(
                new ScenarioContext(
                    database,
                    SqlDialect.PgSql,
                    () => GetPgSqlDatabase(connectionString, sqlExporter),
                    sqlExporter
                )
            );
        }

        private static async Task ExecMySql(IScenario scenario, string connectionString)
        {
            var sqlExporter = MySqlExporter.Default;

            using var database = GetMySqlDatabase(connectionString, sqlExporter);

            await scenario.Exec(
                new ScenarioContext(
                    database,
                    SqlDialect.MySql,
                    () => GetMySqlDatabase(connectionString, sqlExporter),
                    sqlExporter
                )
            );
        }

        private static ISqDatabase GetMsSqlDatabase(string connectionString, ISqlExporter sqlExporter)
            => new SqDatabase<SqlConnection>(
                new SqlConnection(connectionString),
                (conn, sql) => new SqlCommand(sql, conn),
                sqlExporter,
                disposeConnection: true
            );

        private static ISqDatabase GetPgSqlDatabase(string connectionString, ISqlExporter sqlExporter)
            => new SqDatabase<NpgsqlConnection>(
                new NpgsqlConnection(connectionString),
                (conn, sql) => new NpgsqlCommand(sql, conn),
                sqlExporter,
                disposeConnection: true
            );

        private static ISqDatabase GetMySqlDatabase(string connectionString, ISqlExporter sqlExporter)
            => new SqDatabase<MySqlConnection>(
                new MySqlConnection(connectionString),
                (conn, sql) => new MySqlCommand(sql, conn),
                sqlExporter,
                disposeConnection: true
            );
    }
}
