using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

namespace SqExpress;

public static class TableComparisonExtensions
{
    public static TableListComparison? CompareWith(
        this IReadOnlyList<TableBase> thisList,
        IReadOnlyList<TableBase> otherList,
        Func<IExprTableFullName, object>? tableNameKeyExtractor = null)
        => CompareWith(thisList, otherList, TableComparisonFlags.Strict, tableNameKeyExtractor);

    public static TableListComparison? CompareWith(
        this IReadOnlyList<TableBase> thisList,
        IReadOnlyList<TableBase> otherList,
        TableComparisonFlags flags,
        Func<IExprTableFullName, object>? tableNameKeyExtractor = null)
    {
        if (otherList.Count < 1)
        {
            return thisList.Count < 1
                ? null
                : new TableListComparison(thisList, Array.Empty<TableBase>(), Array.Empty<DifferentTables>());
        }

        tableNameKeyExtractor ??= name => BuildTableKey(name, flags);

        var thisTables = thisList.ToDictionary(c => tableNameKeyExtractor(c.FullName), c => c);

        List<TableBase>? extraTables = null;
        List<TableBase>? missedTables = null;
        List<DifferentTables>? differentTables = null;

        var sameNameColumns = new HashSet<object>();

        foreach (var otherTable in otherList)
        {
            if (thisTables.TryGetValue(tableNameKeyExtractor(otherTable.FullName), out var thisTable))
            {
                sameNameColumns.Add(tableNameKeyExtractor(otherTable.FullName));
                var tableComparison = thisTable.CompareWith(otherTable, flags);
                if (tableComparison != null)
                {
                    differentTables ??= new List<DifferentTables>();
                    differentTables.Add(new(thisTable, otherTable, tableComparison));
                }
            }
            else
            {
                extraTables ??= new List<TableBase>();
                extraTables.Add(otherTable);
            }
        }

        if (sameNameColumns.Count != thisList.Count)
        {
            missedTables ??= new();
            foreach (var thisTable in thisList)
            {
                if (!sameNameColumns.Contains(tableNameKeyExtractor(thisTable.FullName)))
                {
                    missedTables.Add(thisTable);
                }
            }
        }

        if (missedTables == null && extraTables == null && differentTables == null)
        {
            return null;
        }

        return new TableListComparison(
            missedTables == null ? Array.Empty<TableBase>() : missedTables,
            extraTables == null ? Array.Empty<TableBase>() : extraTables,
            differentTables == null ? Array.Empty<DifferentTables>() : differentTables
        );
    }

    public static TableComparison? CompareWith(this TableBase thisList, TableBase otherList)
        => CompareWith(thisList, otherList, TableComparisonFlags.Strict);

    public static TableComparison? CompareWith(this TableBase thisList, TableBase otherList, TableComparisonFlags flags)
    {
        var thisColumns = thisList.Columns.ToDictionary(c => c.ColumnName, c => c);

        List<TableColumn>? extraColumns = null;
        List<TableColumn>? missedColumns = null;
        List<DifferentColumns>? differentColumns = null;

        var sameNameColumns = new HashSet<ExprColumnName>();

        foreach (var otherTableColumn in otherList.Columns)
        {
            if (thisColumns.TryGetValue(otherTableColumn.ColumnName, out var thisColumn))
            {
                sameNameColumns.Add(otherTableColumn.ColumnName);
                var tableColumnComparison = thisColumn.CompareWith(otherTableColumn, flags);
                if (tableColumnComparison != TableColumnComparison.Equal)
                {
                    differentColumns ??= new List<DifferentColumns>();
                    differentColumns.Add(new(thisColumn, otherTableColumn, tableColumnComparison));
                }
            }
            else
            {
                extraColumns ??= new List<TableColumn>();
                extraColumns.Add(otherTableColumn);
            }
        }

        if (sameNameColumns.Count != thisList.Columns.Count)
        {
            missedColumns ??= new();
            foreach (var thisColumn in thisList.Columns)
            {
                if (!sameNameColumns.Contains(thisColumn.ColumnName))
                {
                    missedColumns.Add(thisColumn);
                }
            }
        }

        var indexComparison = thisList.Indexes.CompareWith(otherList.Indexes, flags);

        if (HasFlag(flags, TableComparisonFlags.IgnoreMissingColumns))
        {
            missedColumns = null;
        }

        if (HasFlag(flags, TableComparisonFlags.IgnoreExtraColumns))
        {
            extraColumns = null;
        }

        if (missedColumns == null && extraColumns == null && differentColumns == null && indexComparison == null)
        {
            return null;
        }

        return new TableComparison(
            missedColumns == null ? Array.Empty<TableColumn>() : missedColumns,
            extraColumns == null ? Array.Empty<TableColumn>() : extraColumns,
            differentColumns == null ? Array.Empty<DifferentColumns>() : differentColumns,
            indexComparison
        );
    }

    private static IndexComparison? CompareWith(
        this IReadOnlyList<IndexMeta> thisIndexes,
        IReadOnlyList<IndexMeta> otherIndexes)
        => CompareWith(thisIndexes, otherIndexes, TableComparisonFlags.Strict);

    private static IndexComparison? CompareWith(
        this IReadOnlyList<IndexMeta> thisIndexes,
        IReadOnlyList<IndexMeta> otherIndexes,
        TableComparisonFlags flags)
    {
        if (HasFlag(flags, TableComparisonFlags.IgnoreIndexes))
        {
            return null;
        }

        List<IndexMeta>? extraIndexes = null;
        List<IndexMeta>? missedIndexes = null;

        var matchedThisIndexes = new bool[thisIndexes.Count];

        foreach (var otherIndex in otherIndexes)
        {
            var matched = false;
            for (var i = 0; i < thisIndexes.Count; i++)
            {
                if (matchedThisIndexes[i])
                {
                    continue;
                }

                if (!IndexEquals(thisIndexes[i], otherIndex, flags))
                {
                    continue;
                }

                matchedThisIndexes[i] = true;
                matched = true;
                break;
            }

            if (!matched)
            {
                extraIndexes ??= new();
                extraIndexes.Add(otherIndex);
            }
        }

        for (var i = 0; i < thisIndexes.Count; i++)
        {
            if (matchedThisIndexes[i])
            {
                continue;
            }

            missedIndexes ??= new List<IndexMeta>();
            missedIndexes.Add(thisIndexes[i]);
        }

        if (extraIndexes == null && missedIndexes == null)
        {
            return null;
        }

        return new IndexComparison(missedIndexes, extraIndexes);
    }

    public static TableColumnComparison CompareWith(this TableColumn thisColumn, TableColumn otherColumn)
        => CompareWith(thisColumn, otherColumn, TableComparisonFlags.Strict);

    public static TableColumnComparison CompareWith(
        this TableColumn thisColumn,
        TableColumn otherColumn,
        TableComparisonFlags flags)
    {
        var result = TableColumnComparison.Equal;

        if (!thisColumn.ColumnName.Equals(otherColumn.ColumnName))
        {
            result |= TableColumnComparison.DifferentName;
        }

        result |= ApplyColumnShapeFlags(
            thisColumn.SqlType.Accept(ExprTypeComparer.Instance, otherColumn.SqlType),
            flags
        );

        if (!HasFlag(flags, TableComparisonFlags.IgnoreColumnNullability) &&
            thisColumn.IsNullable != otherColumn.IsNullable)
        {
            result |= TableColumnComparison.DifferentNullability;
        }

        if (CompareWith(thisColumn.ColumnMeta, otherColumn.ColumnMeta, flags) != ColumnMetaComparison.Equal)
        {
            result |= TableColumnComparison.DifferentMeta;
        }

        return result;
    }

    public static ColumnMetaComparison CompareWith(this ColumnMeta? thisMeta, ColumnMeta? otherMeta)
        => CompareWith(thisMeta, otherMeta, TableComparisonFlags.Strict);

    public static ColumnMetaComparison CompareWith(
        this ColumnMeta? thisMeta,
        ColumnMeta? otherMeta,
        TableComparisonFlags flags)
    {
        var result = ColumnMetaComparison.Equal;

        if (thisMeta == null && otherMeta == null)
        {
            return result;
        }

        if (!(thisMeta != null && otherMeta != null))
        {
            return HasFlag(flags, TableComparisonFlags.IgnoreColumnMeta)
                ? ColumnMetaComparison.Equal
                : ColumnMetaComparison.DifferentExistence;
        }

        if (!ShouldIgnoreColumnIdentity(flags) && thisMeta.IsIdentity != otherMeta.IsIdentity)
        {
            result |= ColumnMetaComparison.DifferentIdentity;
        }

        if (!ShouldIgnoreColumnPrimaryKey(flags) && thisMeta.IsPrimaryKey != otherMeta.IsPrimaryKey)
        {
            result |= ColumnMetaComparison.DifferentPrimaryKey;
        }

        if (!ShouldIgnoreColumnForeignKeys(flags) && !AreColumnListsEqual(
                thisMeta.ForeignKeyColumns,
                otherMeta.ForeignKeyColumns
            ))
        {
            result |= ColumnMetaComparison.DifferentFk;
        }

        if (!ShouldIgnoreColumnDefaultValues(flags))
        {
            if (!ReferenceEquals(thisMeta.ColumnDefaultValue, null) &&
                !ReferenceEquals(otherMeta.ColumnDefaultValue, null))
            {
                if (TSqlExporter.Default.ToSql(thisMeta.ColumnDefaultValue).Trim('\'') !=
                    TSqlExporter.Default.ToSql(otherMeta.ColumnDefaultValue).Trim('\''))
                {
                    result |= ColumnMetaComparison.DifferentDefaultValues;
                }
            }
            else if (!(ReferenceEquals(thisMeta.ColumnDefaultValue, null) &&
                       ReferenceEquals(otherMeta.ColumnDefaultValue, null)))
            {
                result |= ColumnMetaComparison.DifferentDefaultValues;
            }
        }

        return result;
    }

    public static bool Includes(
        this IReadOnlyList<TableBase> thisList,
        IReadOnlyList<TableBase> otherList,
        TableComparisonFlags flags = TableComparisonFlags.Strict,
        Func<IExprTableFullName, object>? tableNameKeyExtractor = null)
    {
        var comparison = thisList.CompareWith(
            otherList,
            flags | TableComparisonFlags.IgnoreMissingColumns | TableComparisonFlags.IgnoreIndexes |
            TableComparisonFlags.IgnoreColumnMeta,
            tableNameKeyExtractor
        );

        if (comparison == null)
        {
            return true;
        }

        return comparison.ExtraTables.Count == 0
               && comparison.DifferentTables.Count == 0;
    }

    private static object BuildTableKey(IExprTableFullName fullName, TableComparisonFlags flags)
    {
        var table = fullName.AsExprTableFullName();
        var parts = new List<string>(3);

        if (!HasFlag(flags, TableComparisonFlags.IgnoreDatabase))
        {
            parts.Add(table.DbSchema?.Database?.Name ?? string.Empty);
        }

        if (!HasFlag(flags, TableComparisonFlags.IgnoreSchema))
        {
            parts.Add(table.DbSchema?.Schema.Name ?? string.Empty);
        }

        parts.Add(table.TableName.Name);
        return string.Join("|", parts).ToUpperInvariant();
    }

    private static TableColumnComparison ApplyColumnShapeFlags(
        TableColumnComparison comparison,
        TableComparisonFlags flags)
    {
        if (HasFlag(flags, TableComparisonFlags.IgnoreColumnShape))
        {
            comparison &= ~(TableColumnComparison.DifferentType | TableColumnComparison.DifferentArguments |
                            TableColumnComparison.DifferentNullability);
        }
        else
        {
            if (HasFlag(flags, TableComparisonFlags.IgnoreColumnTypes))
            {
                comparison &= ~(TableColumnComparison.DifferentType | TableColumnComparison.DifferentArguments);
            }
            else if (HasFlag(flags, TableComparisonFlags.IgnoreColumnTypeArguments))
            {
                comparison &= ~TableColumnComparison.DifferentArguments;
            }
        }

        return comparison;
    }

    private static bool IndexEquals(IndexMeta x, IndexMeta y, TableComparisonFlags flags)
    {
        if (x.Unique != y.Unique || x.Clustered != y.Clustered)
        {
            return false;
        }

        if (x.Columns.Count != y.Columns.Count)
        {
            return false;
        }

        for (var i = 0; i < x.Columns.Count; i++)
        {
            if (!IndexColumnEquals(x.Columns[i], y.Columns[i], flags))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IndexColumnEquals(IndexMetaColumn x, IndexMetaColumn y, TableComparisonFlags flags)
    {
        if (x.GetType() != y.GetType())
        {
            return false;
        }

        if (x.Column.CompareWith(y.Column, flags) != TableColumnComparison.Equal)
        {
            return false;
        }

        return x.Descending == y.Descending;
    }

    private static bool HasFlag(TableComparisonFlags flags, TableComparisonFlags value)
        => (flags & value) == value;

    private static bool ShouldIgnoreColumnPrimaryKey(TableComparisonFlags flags)
        => HasFlag(flags, TableComparisonFlags.IgnoreColumnMeta) ||
           HasFlag(flags, TableComparisonFlags.IgnoreColumnPrimaryKey);

    private static bool ShouldIgnoreColumnIdentity(TableComparisonFlags flags)
        => HasFlag(flags, TableComparisonFlags.IgnoreColumnMeta) ||
           HasFlag(flags, TableComparisonFlags.IgnoreColumnIdentity);

    private static bool ShouldIgnoreColumnForeignKeys(TableComparisonFlags flags)
        => HasFlag(flags, TableComparisonFlags.IgnoreColumnMeta) || HasFlag(
            flags,
            TableComparisonFlags.IgnoreColumnForeignKeys
        );

    private static bool ShouldIgnoreColumnDefaultValues(TableComparisonFlags flags)
        => HasFlag(flags, TableComparisonFlags.IgnoreColumnMeta) || HasFlag(
            flags,
            TableComparisonFlags.IgnoreColumnDefaultValues
        );

    private static bool AreColumnListsEqual(IReadOnlyList<TableColumn>? list1, IReadOnlyList<TableColumn>? list2)
    {
        if (list1 == null && list2 == null)
        {
            return true;
        }

        if (!(list1 != null && list2 != null) || list1.Count != list2.Count)
        {
            return false;
        }

        var hs = new HashSet<ExprColumnName>(list1.Select(x => x.ColumnName));
        foreach (var column in list2)
        {
            hs.Remove(column.ColumnName);
        }

        return hs.Count == 0;
    }
}

[Flags]
public enum TableComparisonFlags
{
    Strict = 0,
    IgnoreDatabase = 1 << 0,
    IgnoreSchema = 1 << 1,
    IgnoreMissingColumns = 1 << 2,
    IgnoreExtraColumns = 1 << 3,
    IgnoreIndexes = 1 << 4,
    IgnoreColumnTypes = 1 << 5,
    IgnoreColumnTypeArguments = 1 << 6,
    IgnoreColumnNullability = 1 << 7,
    IgnoreColumnPrimaryKey = 1 << 8,
    IgnoreColumnIdentity = 1 << 9,
    IgnoreColumnForeignKeys = 1 << 10,
    IgnoreColumnDefaultValues = 1 << 11,

    IgnoreColumnMeta = IgnoreColumnPrimaryKey | IgnoreColumnIdentity | IgnoreColumnForeignKeys |
                       IgnoreColumnDefaultValues,
    IgnoreColumnShape = IgnoreColumnTypes | IgnoreColumnTypeArguments | IgnoreColumnNullability
}

public record struct DifferentTables(TableBase Table, TableBase OtherTable, TableComparison TableComparison);

public class TableListComparison
{
    public IReadOnlyList<TableBase> MissedTables { get; }

    public IReadOnlyList<TableBase> ExtraTables { get; }

    public IReadOnlyList<DifferentTables> DifferentTables { get; }

    public TableListComparison(
        IReadOnlyList<TableBase> missedTables,
        IReadOnlyList<TableBase> extraTables,
        IReadOnlyList<DifferentTables> differentTables)
    {
        this.MissedTables = missedTables;
        this.ExtraTables = extraTables;
        this.DifferentTables = differentTables;
    }
}

public record struct DifferentColumns(
    TableColumn Column,
    TableColumn OtherColumn,
    TableColumnComparison ColumnComparison);

public class TableComparison
{
    public IReadOnlyList<TableColumn> MissedColumns { get; }

    public IReadOnlyList<TableColumn> ExtraColumns { get; }

    public IReadOnlyList<DifferentColumns> DifferentColumns { get; }

    public IndexComparison? IndexComparison { get; }

    internal TableComparison(
        IReadOnlyList<TableColumn> missedColumns,
        IReadOnlyList<TableColumn> extraColumns,
        IReadOnlyList<DifferentColumns> differentColumns,
        IndexComparison? indexComparison)
    {
        this.MissedColumns = missedColumns;
        this.ExtraColumns = extraColumns;
        this.DifferentColumns = differentColumns;
        this.IndexComparison = indexComparison;
    }
}

public class IndexComparison
{
    public IndexComparison(IReadOnlyList<IndexMeta>? missedIndexes, IReadOnlyList<IndexMeta>? extraIndexes)
    {
        this.MissedIndexes = missedIndexes;
        this.ExtraIndexes = extraIndexes;
    }

    public IReadOnlyList<IndexMeta>? MissedIndexes { get; }

    public IReadOnlyList<IndexMeta>? ExtraIndexes { get; }
}

[Flags]
public enum TableColumnComparison
{
    Equal = 0,
    DifferentName = 1,
    DifferentNullability = 2,
    DifferentType = 4,
    DifferentArguments = 8,
    DifferentMeta = 16
}

[Flags]
public enum ColumnMetaComparison
{
    Equal = 0,
    DifferentPrimaryKey = 1,
    DifferentIdentity = 2,
    DifferentDefaultValues = 4,
    DifferentFk = 8,
    DifferentExistence = 16,
}

internal class ExprTypeComparer : IExprTypeVisitor<TableColumnComparison, ExprType>
{
    private ExprTypeComparer()
    {
    }

    public static readonly ExprTypeComparer Instance = new();

    public TableColumnComparison VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, ExprType arg)
    {
        if (arg is ExprTypeBoolean)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeByte(ExprTypeByte exprType, ExprType arg)
    {
        if (arg is ExprTypeByte)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeByteArray(ExprTypeByteArray exprType, ExprType arg)
    {
        if (arg is ExprTypeByteArray typed)
        {
            if (typed.Size != exprType.Size)
            {
                return TableColumnComparison.DifferentArguments;
            }

            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprType, ExprType arg)
    {
        if (arg is ExprTypeFixSizeByteArray typed)
        {
            if (typed.Size != exprType.Size)
            {
                return TableColumnComparison.DifferentArguments;
            }

            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeInt16(ExprTypeInt16 exprType, ExprType arg)
    {
        if (arg is ExprTypeInt16)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeInt32(ExprTypeInt32 exprType, ExprType arg)
    {
        if (arg is ExprTypeInt32)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeInt64(ExprTypeInt64 exprType, ExprType arg)
    {
        if (arg is ExprTypeInt64)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeDecimal(ExprTypeDecimal exprType, ExprType arg)
    {
        if (arg is ExprTypeDecimal typed)
        {
            if (typed.PrecisionScale?.Scale != exprType.PrecisionScale?.Scale ||
                typed.PrecisionScale?.Precision != exprType.PrecisionScale?.Precision)
            {
                return TableColumnComparison.DifferentArguments;
            }

            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeDouble(ExprTypeDouble exprType, ExprType arg)
    {
        if (arg is ExprTypeDouble)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeDateTime(ExprTypeDateTime exprType, ExprType arg)
    {
        if (arg is ExprTypeDateTime typed)
        {
            if (typed.IsDate != exprType.IsDate)
            {
                return TableColumnComparison.DifferentArguments;
            }

            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprType, ExprType arg)
    {
        if (arg is ExprTypeDateTimeOffset)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeGuid(ExprTypeGuid exprType, ExprType arg)
    {
        if (arg is ExprTypeGuid)
        {
            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeString(ExprTypeString exprType, ExprType arg)
    {
        if (arg is ExprTypeString typed)
        {
            if (typed.Size != exprType.Size || typed.IsUnicode != exprType.IsUnicode ||
                typed.IsText != exprType.IsText)
            {
                return TableColumnComparison.DifferentArguments;
            }

            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeFixSizeString(ExprTypeFixSizeString exprType, ExprType arg)
    {
        if (arg is ExprTypeFixSizeString typed)
        {
            if (typed.Size != exprType.Size || typed.IsUnicode != exprType.IsUnicode)
            {
                return TableColumnComparison.DifferentArguments;
            }

            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }

    public TableColumnComparison VisitExprTypeXml(ExprTypeXml exprType, ExprType arg)
    {
        if (arg is ExprTypeXml typed)
        {
            if (typed.Size != exprType.Size)
            {
                return TableColumnComparison.DifferentArguments;
            }

            return TableColumnComparison.Equal;
        }

        return TableColumnComparison.DifferentType;
    }
}

internal class IndexMetaEqualityComparer : IEqualityComparer<IndexMeta?>
{
    public static readonly IEqualityComparer<IndexMeta?> Instance = new IndexMetaEqualityComparer();

    public bool Equals(IndexMeta? x, IndexMeta? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;

        if (!ReferenceEquals(x.Columns, y.Columns))
        {
            if (x.Columns.Count != y.Columns.Count)
            {
                return false;
            }

            for (var index = 0; index < x.Columns.Count; index++)
            {
                if (!ColumnEquals(x.Columns[index], y.Columns[index]))
                {
                    return false;
                }
            }
        }

        return x.Unique == y.Unique && x.Clustered == y.Clustered;
    }

    public int GetHashCode(IndexMeta? obj)
    {
        if (ReferenceEquals(obj, null)) return 0;

        unchecked
        {
            var hashCode = obj.Unique.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.Clustered.GetHashCode();
            if (!ReferenceEquals(obj.Columns, null))
            {
                foreach (var column in obj.Columns)
                {
                    if (!ReferenceEquals(column, null))
                    {
                        hashCode = (hashCode * 397) ^ ColumnGetHashCode(column);
                    }
                }
            }

            return hashCode;
        }
    }

    private static bool ColumnEquals(IndexMetaColumn x, IndexMetaColumn y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;

        if (x.Column.CompareWith(y.Column) != TableColumnComparison.Equal) return false;
        return x.Descending == y.Descending;
    }

    private static int ColumnGetHashCode(IndexMetaColumn obj)
    {
        unchecked
        {
            return (obj.Column.ColumnName.GetHashCode() * 397) ^ obj.Descending.GetHashCode();
        }
    }
}
