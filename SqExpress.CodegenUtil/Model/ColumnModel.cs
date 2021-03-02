using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model
{
    public class ColumnModel
    {
        public ColumnModel(string name, ColumnRef dbName, ColumnType columnType, int? pkIndex, bool identity, string? defaultValue, List<ColumnRef>? fk)
        {
            this.Name = name;
            this.DbName = dbName;
            this.ColumnType = columnType;
            this.PkIndex = pkIndex;
            this.Identity = identity;
            this.DefaultValue = defaultValue;
            this.Fk = fk;
        }

        public string Name { get; }
        public ColumnRef DbName { get; }
        public ColumnType ColumnType { get; }
        public int? PkIndex { get; }
        public bool Identity { get; }
        public string? DefaultValue { get; }
        public List<ColumnRef>? Fk { get; }
    }
}