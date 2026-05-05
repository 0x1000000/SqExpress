using System.Collections.Generic;
using System.Linq;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal;

internal static class DbModelMapper
{
    public static List<SqTable> ToSqDbTables(IReadOnlyList<TableModel> tableModels, bool skipUnknownColumnTypes)
    {
        Dictionary<ColumnRef, TableColumn> refColStorage = new();

        var result = tableModels
            .Select(tableModel => new TableMapping(tableModel, new SqTable(tableModel.DbName.Schema, tableModel.DbName.Name)))
            .ToList();

        TableColumn GetTableColumn(ColumnRef columnRef) =>
            refColStorage.TryGetValue(columnRef, out var tableColumn)
                ? tableColumn
                : throw new SqExpressException("Could not create consistent foreign column references");

        foreach (var tableMapping in result)
        {
            foreach (var columnModel in tableMapping.Model.Columns)
            {
                refColStorage.Add(
                    columnModel.DbName,
                    tableMapping.Table.CreateColumn(
                        RemoveForeignKeys(columnModel),
                        GetTableColumn));
            }
        }

        foreach (var tableMapping in result)
        {
            var columns = new List<TableColumn>();
            foreach (var columnModel in tableMapping.Model.Columns)
            {
                if (!refColStorage.ContainsKey(columnModel.DbName))
                {
                    continue;
                }

                var columnToAdd = skipUnknownColumnTypes
                    ? SanitizeForeignKeys(columnModel, refColStorage)
                    : columnModel;

                var addedColumn = tableMapping.Table.CreateColumn(columnToAdd, GetTableColumn);
                columns.Add(addedColumn);
                refColStorage[columnModel.DbName] = addedColumn;
            }

            tableMapping.Table.AddColumns(columns);
        }

        foreach (var tableMapping in result)
        {
            tableMapping.Table.AddIndexes(tableMapping.Model.Indexes
                .Where(im => !skipUnknownColumnTypes || im.Columns.All(imc => refColStorage.ContainsKey(imc.DbName)))
                .Select(im =>
                    new IndexMeta(
                        im.Columns.Select(imc => new IndexMetaColumn(GetTableColumn(imc.DbName), imc.IsDescending))
                            .ToList(), im.Name, im.IsUnique, im.IsClustered))
                .ToList());
        }

        return result.Select(i => i.Table).ToList();
    }

    private static ColumnModel SanitizeForeignKeys(ColumnModel columnModel, IReadOnlyDictionary<ColumnRef, TableColumn> storage)
    {
        if (columnModel.Fk == null || columnModel.Fk.Count == 0)
        {
            return columnModel;
        }

        var filteredForeignKeys = columnModel.Fk
            .Where(storage.ContainsKey)
            .ToList();

        if (filteredForeignKeys.Count == columnModel.Fk.Count)
        {
            return columnModel;
        }

        return new ColumnModel(
            name: columnModel.Name,
            dbName: columnModel.DbName,
            ordinalPosition: columnModel.OrdinalPosition,
            columnType: columnModel.ColumnType,
            pk: columnModel.Pk,
            identity: columnModel.Identity,
            defaultValue: columnModel.DefaultValue,
            fk: filteredForeignKeys.Count > 0 ? filteredForeignKeys : null);
    }

    private static ColumnModel RemoveForeignKeys(ColumnModel columnModel)
    {
        if (columnModel.Fk == null || columnModel.Fk.Count == 0)
        {
            return columnModel;
        }

        return new ColumnModel(
            name: columnModel.Name,
            dbName: columnModel.DbName,
            ordinalPosition: columnModel.OrdinalPosition,
            columnType: columnModel.ColumnType,
            pk: columnModel.Pk,
            identity: columnModel.Identity,
            defaultValue: columnModel.DefaultValue,
            fk: null);
    }

    private readonly struct TableMapping
    {
        public TableMapping(TableModel model, SqTable table)
        {
            this.Model = model;
            this.Table = table;
        }

        public TableModel Model { get; }

        public SqTable Table { get; }
    }
}