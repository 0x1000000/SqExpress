using System.Collections.Generic;
using System.Linq;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal
{
    internal static class DbModelMapper
    {
        public static List<SqTable> ToSqDbTables(IReadOnlyList<TableModel> tableModels)
        {

            Dictionary<ColumnRef, TableColumn> refColStorage = new();

            List<SqTable> result = new ();

            foreach (var tableModel in tableModels)
            {
                result.Add(ToSqDbTable(tableModel, refColStorage));
            }

            return result;
        }

        public static SqTable ToSqDbTable(this TableModel tableModel, Dictionary<ColumnRef, TableColumn> storage)
        {
            var sqDbTable = new SqTable(tableModel.DbName.Schema, tableModel.DbName.Name);

            TableColumn GetTableColumn(ColumnRef columnRef) =>
                storage.TryGetValue(columnRef, out var tableColumn)
                    ? tableColumn
                    : throw new SqExpressException("Could not create consistent foreign column references");

            foreach (var tableModelColumn in tableModel.Columns)
            {
                var addedColumn = sqDbTable.AddColumn(tableModelColumn, GetTableColumn);
                storage.Add(tableModelColumn.DbName, addedColumn);
            }

            sqDbTable.AddIndexes(tableModel.Indexes.Select(im =>
                    new IndexMeta(
                        im.Columns.Select(imc => new IndexMetaColumn(GetTableColumn(imc.DbName), imc.IsDescending))
                            .ToList(), im.Name, im.IsUnique, im.IsClustered))
                .ToList());

            return sqDbTable;
        }
    }
}