using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.Utils;

namespace SqExpress.DbMetadata.Internal.DbManagers;

internal class DbManager : IDisposable
{
    protected readonly IDbStrategy Database;

    private readonly DbConnection _connection;

    private readonly DbManagerOptions _options;

    public DbManager(IDbStrategy database, DbConnection connection, DbManagerOptions options)
    {
        this.Database = database;
        this._connection = connection;
        this._options = options;
    }

    public async Task<string?> TryOpenConnection()
    {
        try
        {
            await this._connection.OpenAsync();
#if NETSTANDARD
            this._connection.Close();
#else
                await this._connection.CloseAsync();
#endif
            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    public Task<IReadOnlyList<TableModel>> SelectTables()
        => this.SelectTables(skipUnknownColumnTypes: false);

    public async Task<IReadOnlyList<TableModel>> SelectTables(bool skipUnknownColumnTypes, bool includeViews = false)
    {
        var (columnsRaw, indexes, fk) = await this.Database.LoadRawModels(includeViews);

        var acc = new Dictionary<TableRef, Dictionary<ColumnRef, ColumnModel>>();
        var skippedColumns = new HashSet<ColumnRef>();
        var tablesWithBrokenPrimaryKey = new HashSet<TableRef>();

        foreach (var rawColumn in columnsRaw)
        {
            var table = rawColumn.DbName.Table;
            if (!acc.TryGetValue(table, out var colList))
            {
                colList = new Dictionary<ColumnRef, ColumnModel>();
                acc.Add(table, colList);
            }

            var columnRefs = fk.TryGetValue(rawColumn.DbName, out var fkList) 
                ? fkList 
                : null;

            var pkColumns = indexes.Pks.TryGetValue(table, out var pkCols) ? pkCols.Columns : null;
            if (!this.TryBuildColumnModel(
                    rawColumn,
                    pkColumns,
                    columnRefs,
                    out var colModel))
            {
                if (skipUnknownColumnTypes)
                {
                    skippedColumns.Add(rawColumn.DbName);
                    if (pkColumns != null && pkColumns.Any(c => c.DbName.Equals(rawColumn.DbName)))
                    {
                        tablesWithBrokenPrimaryKey.Add(table);
                    }
                    continue;
                }

                throw new SqExpressException(
                    $"Unsupported column type \"{rawColumn.TypeName}\" for {rawColumn.DbName.Schema}.{rawColumn.DbName.TableName}.{rawColumn.DbName.Name}. Consider using the option to skip unknown columns.");
            }

            colList.Add(colModel.DbName, colModel);
        }

        var sortedTables = SortTablesByForeignKeys(acc: acc);

        var result = sortedTables.Select(
                t =>
                {
                    var columns = acc[key: t]
                        .Select(p => p.Value)
                        .OrderBy(c => c.Pk?.Index ?? 10000)
                        .ThenBy(c => c.OrdinalPosition)
                        .ToList();

                    if (tablesWithBrokenPrimaryKey.Contains(t))
                    {
                        columns = columns
                            .Select(c => new ColumnModel(
                                name: c.Name,
                                dbName: c.DbName,
                                ordinalPosition: c.OrdinalPosition,
                                columnType: c.ColumnType,
                                pk: null,
                                identity: c.Identity,
                                defaultValue: c.DefaultValue,
                                fk: c.Fk))
                            .ToList();
                    }

                    var tableIndexes = indexes.Indexes.TryGetValue(key: t, value: out var tIndexes)
                        ? tIndexes
                        : [];

                    if (skipUnknownColumnTypes && skippedColumns.Count > 0)
                    {
                        tableIndexes = tableIndexes
                            .Where(i => i.Columns.All(c => !skippedColumns.Contains(c.DbName)))
                            .ToList();
                    }

                    return new TableModel(
                        name: this.ToTableCrlName(tableRef: t),
                        dbName: t,
                        columns: columns,
                        indexes: tableIndexes
                    );
                })
            .ToList();

        this.EnsureTableNamesAreUnique(result, this.Database.DefaultSchemaName);

        return result;
    }

    private bool TryBuildColumnModel(
        ColumnRawModel rawColumn,
        List<IndexColumnModel>? pkCols,
        List<ForeignKeyModel>? fkList,
        out ColumnModel columnModel)
    {
        string clrName = ToColCrlName(rawColumn.DbName);

        var pkIndex = pkCols?.FindIndex(c => c.DbName.Equals(rawColumn.DbName));

        PkInfo? pkInfo = null;
        if (pkIndex >= 0 && pkCols != null)
        {
            pkInfo = new PkInfo(pkIndex.Value, pkCols[pkIndex.Value].IsDescending);
        }

        var columnType = this.Database.TryGetColType(raw: rawColumn);
        if (columnType == null)
        {
            columnModel = default!;
            return false;
        }

        columnModel = new ColumnModel(
            name: clrName,
            dbName: rawColumn.DbName,
            ordinalPosition: rawColumn.OrdinalPosition,
            columnType: columnType,
            pk: pkInfo,
            identity: rawColumn.Identity,
            defaultValue: this.Database.ParseDefaultValue(rawColumn.DefaultValue, columnType),
            fk: fkList
        );
        return true;
    }

    private static string ToColCrlName(ColumnRef columnRef)
    {
        return StringHelper.DeSnake(columnRef.Name);
    }

    private string ToTableCrlName(TableRef tableRef)
    {
        return this._options.TableClassPrefix + StringHelper.DeSnake(tableRef.Name);
    }

    private static IReadOnlyList<TableRef> SortTablesByForeignKeys(
        Dictionary<TableRef, Dictionary<ColumnRef, ColumnModel>> acc)
    {
        var parents = acc.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Values
                .Where(c => c.Fk != null)
                .SelectMany(c => c.Fk!)
                .Select(f => f.Column.Table)
                .Distinct()
                .Where(parent => !parent.Equals(pair.Key)) // Self references do not affect ordering.
                .OrderBy(parent => parent)
                .ToList());

        var children = acc.Keys.ToDictionary(table => table, _ => new List<TableRef>());
        foreach (var pair in parents)
        {
            foreach (var parent in pair.Value)
            {
                if (!children.TryGetValue(parent, out var parentChildren))
                {
                    throw new SqExpressException($"Foreign key references unknown table {parent}");
                }

                parentChildren.Add(pair.Key);
            }
        }

        // Kosaraju's two passes are iterative so deep, valid FK chains do not exhaust the stack.
        var visited = new HashSet<TableRef>();
        var finished = new List<TableRef>(acc.Count);
        foreach (var table in acc.Keys.OrderBy(table => table))
        {
            if (!visited.Add(table))
            {
                continue;
            }

            var path = new Stack<(TableRef Table, int NextParent)>();
            path.Push((table, 0));
            while (path.Count > 0)
            {
                var (current, nextParent) = path.Pop();
                if (nextParent == parents[current].Count)
                {
                    finished.Add(current);
                    continue;
                }

                path.Push((current, nextParent + 1));
                var parent = parents[current][nextParent];
                if (visited.Add(parent))
                {
                    path.Push((parent, 0));
                }
            }
        }

        var componentByTable = new Dictionary<TableRef, int>(acc.Count);
        var components = new List<List<TableRef>>();
        for (var index = finished.Count - 1; index >= 0; index--)
        {
            var table = finished[index];
            if (componentByTable.ContainsKey(table))
            {
                continue;
            }

            var component = new List<TableRef>();
            var componentIndex = components.Count;
            var pending = new Stack<TableRef>();
            pending.Push(table);
            componentByTable.Add(table, componentIndex);
            while (pending.Count > 0)
            {
                var member = pending.Pop();
                component.Add(member);
                foreach (var child in children[member])
                {
                    if (!componentByTable.ContainsKey(child))
                    {
                        componentByTable.Add(child, componentIndex);
                        pending.Push(child);
                    }
                }
            }

            component.Sort();
            components.Add(component);
        }

        var componentParents = Enumerable.Range(0, components.Count)
            .Select(_ => new HashSet<int>()).ToArray();
        var childCounts = new int[components.Count];
        var ranks = new int[components.Count];
        var hasDependency = new bool[components.Count];
        foreach (var pair in parents)
        {
            var childComponent = componentByTable[pair.Key];
            foreach (var parent in pair.Value)
            {
                var parentComponent = componentByTable[parent];
                hasDependency[childComponent] = true;
                hasDependency[parentComponent] = true;
                if (childComponent != parentComponent && componentParents[childComponent].Add(parentComponent))
                {
                    childCounts[parentComponent]++;
                }
            }
        }

        var ready = new Queue<int>(Enumerable.Range(0, components.Count).Where(i => childCounts[i] == 0));
        while (ready.Count > 0)
        {
            var child = ready.Dequeue();
            foreach (var parent in componentParents[child])
            {
                ranks[parent] = Math.Max(ranks[parent], ranks[child] + 1);
                if (--childCounts[parent] == 0)
                {
                    ready.Enqueue(parent);
                }
            }
        }

        var maxRank = ranks.Length == 0 ? 0 : ranks.Max();
        return Enumerable.Range(0, components.Count)
            .OrderByDescending(i => hasDependency[i] ? ranks[i] : maxRank)
            .ThenBy(i => components[i][0])
            .SelectMany(i => components[i])
            .ToList();
    }

    private void EnsureTableNamesAreUnique(List<TableModel> result, string defaultSchema)
    {
        if (result.Count < 2)
        {
            return;
        }

        var dic = result
            .Select(
                (table, origIndex) =>
                {
                    EnsureColumnNamesAreUnique(table);
                    return (table, origIndex);
                }
            )
            //C# class names is case-sensitive but windows file system is not, so there might be class overwriting.
            .GroupBy(t => t.table.Name, StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(t => t.Key, t => t.ToList(), StringComparer.InvariantCultureIgnoreCase);

        foreach (var pair in dic.ToList())
        {
            while (pair.Value.Count > 1)
            {
                var newName = pair.Key;

                //Try add schema prefix
                int? duplicateIndex = null;
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    var next = pair.Value[i];
                    if (!string.Equals(next.table.DbName.Schema, defaultSchema, StringComparison.InvariantCultureIgnoreCase))
                    {
                        duplicateIndex = i;
                        break;
                    }
                }

                if (duplicateIndex != null)
                {
                    var duplicate = pair.Value[duplicateIndex.Value];

                    var tableName = duplicate.table.Name;

                    if (!string.IsNullOrEmpty(this._options.TableClassPrefix))
                    {
                        tableName = tableName.Substring(this._options.TableClassPrefix.Length);
                    }

                    newName = this._options.TableClassPrefix + StringHelper.DeSnake(duplicate.table.DbName.Schema) + tableName;
                }

                newName = StringHelper.AddNumberUntilUnique(newName, "No", nn => !dic.ContainsKey(nn));

                duplicateIndex ??= 1; //Second

                var duplicateRes = pair.Value[duplicateIndex.Value];

                var newTable = duplicateRes.table.WithNewName(newName);

                result[duplicateRes.origIndex] = newTable;

                dic.Add(newTable.Name, [(newTable, duplicateRes.origIndex)]);

                pair.Value.RemoveAt(duplicateIndex.Value);
            }
        }
    }

    private static void EnsureColumnNamesAreUnique(TableModel result)
    {
        var dict = result.Columns.Select((column, originalIndex) => (column, originalIndex))
            .GroupBy(c => c.column.Name)
            .ToDictionary(i => i.Key, i => i.ToList());

        foreach (var pair in dict.ToList())
        {
            while (pair.Value.Count > 1)
            {
                var duplicateIndex = 1;

                var duplicate = pair.Value[duplicateIndex];

                var newName = StringHelper.AddNumberUntilUnique(duplicate.column.Name, "No", n => !dict.ContainsKey(n));

                var newColumn = duplicate.column.WithName(newName);
                result.Columns[duplicate.originalIndex] = newColumn;

                dict.Add(newColumn.Name, [(newColumn, duplicate.originalIndex)]);

                pair.Value.RemoveAt(duplicateIndex);
            }
        }
    }

    public void Dispose()
    {
        try
        {
            this.Database.Dispose();
        }
        finally
        {
            this._connection.Dispose();
        }
    }
}
