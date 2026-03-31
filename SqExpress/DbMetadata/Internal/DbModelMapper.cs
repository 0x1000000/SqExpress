using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal
{
    internal static class DbModelMapper
    {
        public static List<SqTable> ToSqDbTables(IReadOnlyList<TableModel> tableModels)
            => ToSqDbTables(tableModels, skipUnknownColumnTypes: false);

        public static List<SqTable> ToSqDbTables(IReadOnlyList<TableModel> tableModels, bool skipUnknownColumnTypes)
        {
            Dictionary<ColumnRef, TableColumn> refColStorage = new();

            List<SqTable> result = new();

            foreach (var tableModel in tableModels)
            {
                result.Add(ToSqDbTable(tableModel, refColStorage, skipUnknownColumnTypes));
            }

            return result;
        }

        public static SqTable ToSqDbTable(this TableModel tableModel, Dictionary<ColumnRef, TableColumn> storage)
            => ToSqDbTable(tableModel, storage, skipUnknownColumnTypes: false);

        public static SqTable ToSqDbTable(this TableModel tableModel, Dictionary<ColumnRef, TableColumn> storage, bool skipUnknownColumnTypes)
        {
            var sqDbTable = new SqTable(tableModel.DbName.Schema, tableModel.DbName.Name);

            TableColumn GetTableColumn(ColumnRef columnRef) =>
                storage.TryGetValue(columnRef, out var tableColumn)
                    ? tableColumn
                    : throw new SqExpressException("Could not create consistent foreign column references");

            foreach (var tableModelColumn in tableModel.Columns)
            {
                var columnToAdd = skipUnknownColumnTypes
                    ? SanitizeForeignKeys(tableModelColumn, storage)
                    : tableModelColumn;

                TableColumn addedColumn;
                try
                {
                    addedColumn = sqDbTable.AddColumn(columnToAdd, GetTableColumn);
                }
                catch (Exception e) when (skipUnknownColumnTypes && IsUnsupportedColumnTypeException(e))
                {
                    continue;
                }

                storage.Add(tableModelColumn.DbName, addedColumn);
            }

            sqDbTable.AddIndexes(tableModel.Indexes
                .Where(im => !skipUnknownColumnTypes || im.Columns.All(imc => storage.ContainsKey(imc.DbName)))
                .Select(im =>
                    new IndexMeta(
                        im.Columns.Select(imc => new IndexMetaColumn(GetTableColumn(imc.DbName), imc.IsDescending))
                            .ToList(), im.Name, im.IsUnique, im.IsClustered))
                .ToList());

            return sqDbTable;
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

        private static bool IsUnsupportedColumnTypeException(Exception exception)
        {
            if (exception is NotSupportedException)
            {
                return true;
            }

            return exception is SqExpressException sqExpressException &&
                   sqExpressException.Message.StartsWith("Not supported column type ", StringComparison.Ordinal);
        }
    }
}
