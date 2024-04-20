using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;

namespace SqExpress.IntTest.Scenarios;

public class ScGetTables : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        if (context.Dialect != SqlDialect.PgSql)
        {
            return;
        }

        var tables = await context.Database.GetTables();

        var allTables = AllTables.BuildAllTableList(context.Dialect);

        var tableListComparison = allTables.CompareWith(tables, t => t.TableName);
        if (tableListComparison != null)
        {
            PrintComparison(context.WriteLine, tableListComparison);

            if (tableListComparison.DifferentTables.Count > 0 || tableListComparison.MissedTables.Count > 0)
            {
                throw new Exception("Different tables");
            }
        }
    }

    public static void PrintComparison(Action<string> writeLine, TableListComparison tableListComparison)
    {
        writeLine("Tables are different");

        if (tableListComparison.MissedTables.Count > 0)
        {
            writeLine("Missed tables:");
            foreach (var missedTable in tableListComparison.MissedTables)
            {
                writeLine(missedTable.FullName.TableName);
            }
        }

        if (tableListComparison.ExtraTables.Count > 0)
        {
            writeLine("Extra tables:");
            foreach (var missedTable in tableListComparison.ExtraTables)
            {
                writeLine(missedTable.FullName.TableName);
            }
        }

        if (tableListComparison.DifferentTables.Count > 0)
        {
            writeLine("Different tables:");
            foreach (var differentTable in tableListComparison.DifferentTables)
            {
                writeLine($"{differentTable.Table.FullName.TableName}");
                string prefix = "    -";
                if (differentTable.TableComparison.IndexComparison != null)
                {
                    writeLine(prefix + "Different Indexes");
                }

                if (differentTable.TableComparison.DifferentColumns.Count > 0)
                {
                    writeLine(prefix + "Different Columns");
                    string prefix2 = "       -";
                    foreach (var differentColumn in differentTable.TableComparison.DifferentColumns)
                    {
                        writeLine($"{prefix2}{differentColumn.Column.ColumnName.Name} - {differentColumn.ColumnComparison}");

                        if ((differentColumn.ColumnComparison & TableColumnComparison.DifferentMeta) > 0)
                        {
                            var metaComparison = differentColumn.Column.ColumnMeta.CompareWith(differentColumn.OtherColumn.ColumnMeta);
                            writeLine($"{prefix2}{metaComparison}");
                        }
                    }
                }

                if (differentTable.TableComparison.ExtraColumns.Count > 0)
                {
                    writeLine(prefix + "Extra Columns");
                }

                if (differentTable.TableComparison.MissedColumns.Count > 0)
                {
                    writeLine(prefix + "Missed Columns");
                }
            }
        }
    }
}
