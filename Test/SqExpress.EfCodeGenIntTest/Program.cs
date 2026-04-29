using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.SqlExport;

namespace SqExpress.EfCodeGenIntTest;

public static class Program
{
    public static async Task Main()
    {
        await RecreateEfDatabase();
        await CompareGeneratedTablesWithDatabase();
    }

    private static async Task RecreateEfDatabase()
    {
        await using var context = new EfCodeGenDbContext();
        Console.WriteLine("Recreating EFTest from EF Core model...");
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private static async Task CompareGeneratedTablesWithDatabase()
    {
        var exporter = TSqlExporter.Default;
        await using var database = new SqDatabase<SqlConnection>(
            new SqlConnection(EfCodeGenDbContext.ConnectionString),
            (connection, sql) => new SqlCommand(sql, connection),
            exporter,
            ParametrizationMode.None,
            disposeConnection: true);

        var generatedTables = LoadGeneratedTables();
        var databaseTables = await database.GetTables();

        var comparison = generatedTables.CompareWith(
            databaseTables,
            TableComparisonFlags.Strict,
            table => $"{table.SchemaName}.{table.TableName}".ToLowerInvariant());

        if (comparison != null)
        {
            PrintComparison(comparison, exporter);
            throw new InvalidOperationException("Generated EF table descriptors differ from EFTest database metadata.");
        }

        Console.WriteLine($"Generated EF table descriptors match EFTest database metadata. Tables: {generatedTables.Count}.");
    }

    private static IReadOnlyList<TableBase> LoadGeneratedTables()
    {
        var allTablesType = Assembly.GetExecutingAssembly().GetType("SqExpress.EfCodeGenIntTest.Tables.AllTables")
            ?? throw new InvalidOperationException("Could not find generated AllTables class.");

        var buildMethod = allTablesType.GetMethod("BuildAllTableList", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not find generated AllTables.BuildAllTableList method.");

        return buildMethod.Invoke(null, Array.Empty<object>()) as IReadOnlyList<TableBase>
               ?? throw new InvalidOperationException("Generated AllTables.BuildAllTableList did not return table descriptors.");
    }

    private static void PrintComparison(TableListComparison comparison, ISqlExporter exporter)
    {
        if (comparison.MissedTables.Count > 0)
        {
            Console.WriteLine("Missed tables:");
            foreach (var table in comparison.MissedTables)
            {
                Console.WriteLine($"  {table.FullName}");
            }
        }

        if (comparison.ExtraTables.Count > 0)
        {
            Console.WriteLine("Extra tables:");
            foreach (var table in comparison.ExtraTables)
            {
                Console.WriteLine($"  {table.FullName}");
            }
        }

        foreach (var differentTable in comparison.DifferentTables)
        {
            Console.WriteLine($"Different table: {differentTable.Table.FullName}");
            Console.WriteLine(exporter.ToSql(differentTable.Table.Script.Create()));
            Console.WriteLine("-vs-");
            Console.WriteLine(exporter.ToSql(differentTable.OtherTable.Script.Create()));

            foreach (var column in differentTable.TableComparison.DifferentColumns)
            {
                Console.WriteLine($"  Different column {column.Column.ColumnName.Name}: {column.ColumnComparison}");
            }

            foreach (var column in differentTable.TableComparison.MissedColumns)
            {
                Console.WriteLine($"  Missed column {column.ColumnName.Name}");
            }

            foreach (var column in differentTable.TableComparison.ExtraColumns)
            {
                Console.WriteLine($"  Extra column {column.ColumnName.Name}");
            }

            var indexComparison = differentTable.TableComparison.IndexComparison;
            if (indexComparison == null)
            {
                continue;
            }

            foreach (var index in indexComparison.MissedIndexes ?? Array.Empty<IndexMeta>())
            {
                Console.WriteLine($"  Missed index {index.Name}");
            }

            foreach (var index in indexComparison.ExtraIndexes ?? Array.Empty<IndexMeta>())
            {
                Console.WriteLine($"  Extra index {index.Name}");
            }
        }
    }
}
