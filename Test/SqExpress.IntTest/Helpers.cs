using SqExpress.IntTest.Context;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DataAccess;

namespace SqExpress.IntTest;

public static class Helpers
{
    public static bool IsUnicode(bool value, SqlDialect dialect)
    {
        return dialect == SqlDialect.PgSql || dialect == SqlDialect.MySql || value;
    }

    public static int? ArrayLimit(int? value, SqlDialect dialect)
    {
        return dialect == SqlDialect.PgSql ? null : value;
    }

    public static async Task CompareDatabases(ISqDatabase db1, ISqDatabase db2)
    {
        var db1Tables = await db1.GetTables();
        var db2Tables = await db2.GetTables();

        var comparison = db1Tables.CompareWith(db2Tables, t => t.TableName);

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
                    if (!Helpers.CompareLists(
                            differentColumn.Column.ColumnMeta?.ForeignKeyColumns,
                            differentColumn.OtherColumn.ColumnMeta?.ForeignKeyColumns,
                            (fk1, fk2) => fk1.ColumnName.Name == fk2.ColumnName.Name,
                            false
                        ))
                    {
                        throw new Exception("Column FK should be the same");
                    }
                }

                var ic = comparisonDifferentTable.TableComparison.IndexComparison;
                if (ic != null)
                {
                    if ((ic.ExtraIndexes?.Count ?? 0) != (ic.MissedIndexes?.Count ?? 0))
                    {
                        throw new Exception("Indexes should be the same");
                    }

                    if (!Helpers.CompareLists(
                            ic.ExtraIndexes,
                            ic.MissedIndexes,
                            (cl1, cl2) => Helpers.CompareLists(
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
    }

    public static bool CompareLists<T>(IReadOnlyList<T>? l1, IReadOnlyList<T>? l2, Func<T, T, bool> comparer, bool order)
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
