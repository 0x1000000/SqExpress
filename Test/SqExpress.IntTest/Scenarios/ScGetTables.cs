using System;
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

        var tableListComparison = allTables.CompareWith(tables);
        if (tableListComparison != null)
        {
            context.WriteLine("Tables are different");

            if (tableListComparison.MissedTables.Count > 0)
            {
                context.WriteLine("Missed tables");
                foreach (var missedTable in tableListComparison.MissedTables)
                {
                    context.WriteLine(missedTable.FullName.TableName);
                }
            }

            if (tableListComparison.ExtraTables.Count > 0)
            {
                context.WriteLine("Extra tables");
                foreach (var missedTable in tableListComparison.ExtraTables)
                {
                    context.WriteLine(missedTable.FullName.TableName);
                }
            }

            if (tableListComparison.DifferentTables.Count > 0)
            {
                context.WriteLine("Different tables");
                foreach (var differentTable in tableListComparison.DifferentTables)
                {
                    context.WriteLine($"{differentTable.Table.FullName.TableName}");
                    string prefix = "    -";
                    if (differentTable.TableComparison.IndexComparison != null)
                    {
                        context.WriteLine(prefix + "Different Indexes");
                    }
                    if (differentTable.TableComparison.DifferentColumns.Count > 0)
                    {
                        context.WriteLine(prefix + "Different Columns");
                        string prefix2 = "       -";
                        foreach (var differentColumn in differentTable.TableComparison.DifferentColumns)
                        {
                            context.WriteLine($"{prefix2}{differentColumn.Column.ColumnName.Name} - {differentColumn.ColumnComparison}");
                            if ((differentColumn.ColumnComparison & TableColumnComparison.DifferentMeta) > 0)
                            {
                                var metaComparison = differentColumn.Column.ColumnMeta.CompareWith(differentColumn.OtherColumn.ColumnMeta);
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

            if (tableListComparison.DifferentTables.Count > 0 || tableListComparison.MissedTables.Count > 0)
            {
                throw new Exception("Different tables");
            }
        }
    }
}