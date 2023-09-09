using System.Collections.Generic;

namespace SqExpress.DbMetadata.Internal.Model
{
    internal class TableModel
    {
        public TableModel(string name, TableRef dbName, List<ColumnModel> columns, List<IndexModel> indexes)
        {
            Name = name;
            DbName = dbName;
            Columns = columns;
            Indexes = indexes;
        }

        public string Name { get; }
        public TableRef DbName { get; }
        public List<ColumnModel> Columns { get; }
        public List<IndexModel> Indexes { get; }


        public TableModel WithNewName(string newName) =>
            new TableModel(newName, DbName, Columns, Indexes);
    }
}