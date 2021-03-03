using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model
{
    internal class TableModel
    {
        public TableModel(string name, TableRef dbName, List<ColumnModel> columns, List<Index> indexes)
        {
            this.Name = name;
            this.DbName = dbName;
            this.Columns = columns;
            this.Indexes = indexes;
        }

        public string Name { get; }
        public TableRef DbName { get; }
        public List<ColumnModel> Columns { get; }
        public List<Index> Indexes { get; }


        public TableModel WithNewName(string newName) =>
            new TableModel(newName, this.DbName, this.Columns, this.Indexes);
    }
}