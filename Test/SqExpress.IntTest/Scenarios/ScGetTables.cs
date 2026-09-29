using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;

namespace SqExpress.IntTest.Scenarios;

public class ScGetTables : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var declaredTables = AllTables.BuildAllTableList(context.Dialect);
        AssertCycleForeignKey(declaredTables, "FkCycleA", "FkCycleBId", "FkCycleB");
        AssertCycleForeignKey(declaredTables, "FkCycleB", "FkCycleAId", "FkCycleA");

        var actualTables = await context.Database.GetTables();
        AssertCycleForeignKey(actualTables, "FkCycleA", "FkCycleBId", "FkCycleB");
        AssertCycleForeignKey(actualTables, "FkCycleB", "FkCycleAId", "FkCycleA");

        //SQLite introspection collapses several declared types/defaults to affinity-level metadata,
        //so this comparison keeps structure strict while relaxing type/default round-tripping.
        var comparisonFlags = context.Dialect.IsSqlite()
            ? TableComparisonFlags.IgnoreColumnTypes |
              TableComparisonFlags.IgnoreColumnTypeArguments |
              TableComparisonFlags.IgnoreColumnDefaultValues
            : TableComparisonFlags.Strict;

        var tableListComparison = declaredTables.CompareWith(
            actualTables,
            comparisonFlags,
            t => t.TableName.ToLower());
        if (tableListComparison != null)
        {
            PrintComparison(context, tableListComparison);

            if (tableListComparison.DifferentTables.Count > 0 || tableListComparison.MissedTables.Count > 0)
            {
                throw new Exception("Different tables");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Hierarchy:");

        var hierarchy = actualTables.BuildHierarchy();
        var printed = new HashSet<ExprTableFullName>();
        foreach (var actualTable in actualTables.Where(t => !t.GetParentTables().Any()))
        {
            Print(actualTable.FullName.AsExprTableFullName(), "", new HashSet<ExprTableFullName>());
        }

        foreach (var actualTable in actualTables)
        {
            var name = actualTable.FullName.AsExprTableFullName();
            if (!printed.Contains(name))
            {
                Print(name, "", new HashSet<ExprTableFullName>());
            }
        }

        void Print(ExprTableFullName table, string prefix, HashSet<ExprTableFullName> path)
        {
            if (!path.Add(table))
            {
                context.WriteLine($"{prefix} - {table.TableName.Name} (cycle)");
                return;
            }

            printed.Add(table);
            if (hierarchy.TryGetValue(table, out var children))
            {
                context.WriteLine($"{prefix} - {table.TableName.Name} is referenced by:");
                foreach (var child in children)
                {
                    Print(child, prefix + "  ", path);
                }
            }
            else
            {
                context.WriteLine($"{prefix} - {table.TableName.Name}");
            }

            path.Remove(table);
        }
    }

    private static void AssertCycleForeignKey(
        IReadOnlyList<TableBase> tables,
        string tableName,
        string columnName,
        string referencedTableName)
    {
        var table = tables.Single(t => string.Equals(t.FullName.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        var column = table.Columns.Single(c => string.Equals(c.ColumnName.Name, columnName, StringComparison.OrdinalIgnoreCase));
        var foreignKeys = column.ColumnMeta?.ForeignKeys;
        if (foreignKeys?.Count != 1 ||
            !string.Equals(foreignKeys[0].ReferencedColumn.Table.FullName.TableName,
                referencedTableName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(foreignKeys[0].ReferencedColumn.ColumnName.Name, "Id", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"{tableName}.{columnName} should reference {referencedTableName}.Id");
        }
    }

    public static void PrintComparison(IScenarioContext context, TableListComparison tableListComparison)
    {
        context.WriteLine("Tables are different");

        if (tableListComparison.MissedTables.Count > 0)
        {
            context.WriteLine("Missed tables:");
            foreach (var missedTable in tableListComparison.MissedTables)
            {
                context.WriteLine(missedTable.FullName.TableName);
            }
        }

        if (tableListComparison.ExtraTables.Count > 0)
        {
            context.WriteLine("Extra tables:");
            foreach (var missedTable in tableListComparison.ExtraTables)
            {
                context.WriteLine(missedTable.FullName.TableName);
            }
        }

        if (tableListComparison.DifferentTables.Count > 0)
        {
            context.WriteLine("Different tables:");
            foreach (var differentTable in tableListComparison.DifferentTables)
            {
                context.WriteLine(context.SqlExporter.ToSql(differentTable.Table.Script.Create()));
                context.WriteLine("-vs-");
                context.WriteLine(context.SqlExporter.ToSql(differentTable.OtherTable.Script.Create()));

                context.WriteLine($"{differentTable.Table.FullName.TableName}");
                string prefix = "    -";
                string prefix2 = "       -";
                string prefix3 = "           -";
                if (differentTable.TableComparison.IndexComparison != null)
                {
                    context.WriteLine(prefix + "Different Indexes");

                    var ic = differentTable.TableComparison.IndexComparison;

                    if (ic.MissedIndexes is { Count: > 0 })
                    {
                        context.WriteLine(prefix2 + "Missed indexes:");
                        foreach (var mi in ic.MissedIndexes)
                        {
                            context.WriteLine($"{prefix2}Name:{mi.Name} U:{mi.Unique} C:{mi.Clustered}");
                            context.WriteLine($"{prefix3}Columns:");
                            foreach (var indexMetaColumn in mi.Columns)
                            {
                                context.WriteLine(
                                    $"{prefix3}{indexMetaColumn.Column.ColumnName.Name} D:{indexMetaColumn.Descending}"
                                );
                            }
                        }
                    }

                    if (ic.ExtraIndexes is { Count: > 0 })
                    {
                        context.WriteLine(prefix2 + "Extra indexes:");
                        foreach (var ei in ic.ExtraIndexes)
                        {
                            context.WriteLine($"{prefix2}Name:{ei.Name} U:{ei.Unique} C:{ei.Clustered}");
                            context.WriteLine($"{prefix3}Columns:");
                            foreach (var indexMetaColumn in ei.Columns)
                            {
                                context.WriteLine(
                                    $"{prefix3}{indexMetaColumn.Column.ColumnName.Name} D:{indexMetaColumn.Descending}"
                                );
                            }
                        }
                    }
                }

                if (differentTable.TableComparison.DifferentColumns.Count > 0)
                {
                    context.WriteLine(prefix + "Different Columns");
                    foreach (var differentColumn in differentTable.TableComparison.DifferentColumns)
                    {
                        context.WriteLine(
                            $"{prefix2}{differentColumn.Column.ColumnName.Name} - {differentColumn.ColumnComparison}"
                        );

                        if ((differentColumn.ColumnComparison & TableColumnComparison.DifferentArguments) > 0 ||
                            (differentColumn.ColumnComparison & TableColumnComparison.DifferentType) > 0)
                        {
                            context.WriteLine(
                                $"{context.SqlExporter.ToSql(differentColumn.Column.SqlType)} - vs - {context.SqlExporter.ToSql(differentColumn.OtherColumn.SqlType)}"
                            );
                        }

                        if ((differentColumn.ColumnComparison & TableColumnComparison.DifferentMeta) > 0)
                        {
                            var metaComparison =
                                differentColumn.Column.ColumnMeta.CompareWith(differentColumn.OtherColumn.ColumnMeta);
                            context.WriteLine($"{prefix2}{metaComparison}");
                        }
                    }
                }

                if (differentTable.TableComparison.ExtraColumns.Count > 0)
                {
                    context.WriteLine(prefix + "Extra Columns");
                }

                if (differentTable.TableComparison.MissedColumns.Count > 0)
                {
                    context.WriteLine(prefix + "Missed Columns");
                }
            }
        }
    }
}
