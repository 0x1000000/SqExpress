using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal class DbManager : IDisposable
    {
        protected readonly IDbStrategy Database;

        private readonly DbConnection _connection;

        private readonly GenTablesOptions _options;

        public DbManager(IDbStrategy database, DbConnection connection, GenTablesOptions options)
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
                await this._connection.CloseAsync();
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public async Task<IReadOnlyList<TableModel>> SelectTables()
        {
            var columnsRaw = await this.Database.LoadColumns();
            var indexes = await this.Database.LoadIndexes();
            var fk = await this.Database.LoadForeignKeys();

            var acc = new Dictionary<TableRef, Dictionary<ColumnRef, ColumnModel>>();

            foreach (var rawColumn in columnsRaw)
            {
                var table = rawColumn.DbName.Table;
                if (!acc.TryGetValue(table, out var colList))
                {
                    colList = new Dictionary<ColumnRef, ColumnModel>();
                    acc.Add(table, colList);
                }

                var colModel = BuildColumnModel(
                    rawColumn,
                    indexes.Pks.TryGetValue(table, out var pkCols) ? pkCols.Columns : null,
                    fk.TryGetValue(rawColumn.DbName, out var fkList) ? fkList : null);

                colList.Add(colModel.DbName, colModel);
            }

            var sortedTables = SortTablesByForeignKeys(acc: acc);

            var result = sortedTables.Select(t =>
                    new TableModel(
                        name: ToTableCrlName(tableRef: t),
                        dbName: t,
                        columns: acc[key: t]
                            .Select(p => p.Value)
                            .OrderBy(c => c.Pk?.Index ?? 10000)
                            .ThenBy(c => c.OrdinalPosition)
                            .ToList(),
                        indexes: indexes.Indexes.TryGetValue(key: t, value: out var tIndexes)
                            ? tIndexes
                            : new List<IndexModel>(capacity: 0)))
                .ToList();

            EnsureTableNamesAreUnique(result, this.Database.DefaultSchemaName);

            return result;

        }

        private ColumnModel BuildColumnModel(ColumnRawModel rawColumn, List<IndexColumnModel>? pkCols, List<ColumnRef>? fkList)
        {
            string clrName = ToColCrlName(rawColumn.DbName);

            var pkIndex = pkCols?.FindIndex(c=>c.DbName.Equals(rawColumn.DbName));

            PkInfo? pkInfo = null;
            if (pkIndex >= 0 && pkCols != null)
            {
                pkInfo = new PkInfo(pkIndex.Value, pkCols[pkIndex.Value].IsDescending);
            }

            return new ColumnModel(
                name: clrName,
                dbName: rawColumn.DbName,
                ordinalPosition: rawColumn.OrdinalPosition,
                columnType: this.Database.GetColType(raw: rawColumn),
                pk: pkInfo,
                identity: rawColumn.Identity,
                defaultValue: this.Database.ParseDefaultValue(rawColumn.DefaultValue),
                fk: fkList);
        }

        private static string ToColCrlName(ColumnRef columnRef)
        {
            return StringHelper.DeSnake(columnRef.Name);
        }

        private string ToTableCrlName(TableRef tableRef)
        {
            return this._options.TableClassPrefix + StringHelper.DeSnake(tableRef.Name);
        }

        private static IReadOnlyList<TableRef> SortTablesByForeignKeys(Dictionary<TableRef, Dictionary<ColumnRef, ColumnModel>> acc)
        {
            var tableGraph = new Dictionary<TableRef, int>();
            var maxValue = 0;

            foreach (var pair in acc)
            {
                CountTable(pair.Key, pair.Value, 1);
            }

            return acc
                .Keys
                .OrderByDescending(k => tableGraph.TryGetValue(k, out var value) ? value : maxValue)
                .ThenBy(k => k)
                .ToList();

            void CountTable(TableRef table, Dictionary<ColumnRef, ColumnModel> columns, int value)
            {
                var parentTables = columns.Values
                    .Where(c => c.Fk != null)
                    .SelectMany(c => c.Fk!)
                    .Select(f => f.Table)
                    .Distinct()
                    .Where(pt => !pt.Equals(table))//Self ref
                    .ToList();

                bool hasParents = false;
                foreach (var parentTable in parentTables)
                {
                    if (tableGraph.TryGetValue(parentTable, out int oldValue))
                    {
                        if (value >= 1000)
                        {
                            throw new SqExpressCodeGenException("Cycle in tables");
                        }

                        if (oldValue < value)
                        {
                            tableGraph[parentTable] = value;
                        }
                    }
                    else
                    {
                        tableGraph.Add(parentTable, value);
                    }

                    if (maxValue < value)
                    {
                        maxValue = value;
                    }

                    CountTable(parentTable, acc[parentTable], value + 1);
                    hasParents = true;
                }

                if (hasParents && !tableGraph.ContainsKey(columns.Keys.First().Table))
                {
                    tableGraph.Add(table, 0);
                }
            }
        }

        private void EnsureTableNamesAreUnique(List<TableModel> result, string defaultSchema)
        {
            if (result.Count < 2)
            {
                return;
            }

            var dic = result
                .Select((table,origIndex)=>
                {
                    EnsureColumnNamesAreUnique(table);
                    return (table, origIndex);
                })
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

                    duplicateIndex ??= 1;//Second

                    var duplicateRes = pair.Value[duplicateIndex.Value];

                    var newTable = duplicateRes.table.WithNewName(newName);

                    result[duplicateRes.origIndex] = newTable;

                    dic.Add(newTable.Name, new List<(TableModel table, int origIndex)>(1){(newTable, duplicateRes.origIndex)});

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

                    dict.Add(newColumn.Name, new List<(ColumnModel, int)>(1){(newColumn, duplicate.originalIndex) });

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
}