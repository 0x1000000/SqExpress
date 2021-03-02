using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model
{
    public record TableModel
    {
        public TableModel(string name, TableRef dbName, IReadOnlyList<ColumnModel> column)
        {
            this.Name = name;
            this.DbName = dbName;
            this.Column = column;
        }

        public string Name { get; }
        public TableRef DbName { get; }
        public IReadOnlyList<ColumnModel> Column { get; }
    }
}