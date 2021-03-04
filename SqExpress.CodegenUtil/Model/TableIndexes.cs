using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model
{
    internal class PrimaryKeyModel
    {
        public readonly List<IndexColumnModel> Columns;

        public readonly string Name;

        public PrimaryKeyModel(List<IndexColumnModel> columns, string name)
        {
            this.Columns = columns;
            this.Name = name;
        }
    }

    internal class IndexModel
    {
        public readonly List<IndexColumnModel> Columns;

        public readonly string Name;

        public readonly bool IsUnique;

        public readonly bool IsClustered;

        public IndexModel(List<IndexColumnModel> columns, string name, bool isUnique, bool isClustered)
        {
            this.Columns = columns;
            this.Name = name;
            this.IsUnique = isUnique;
            this.IsClustered = isClustered;
        }
    }

    internal readonly struct IndexColumnModel
    {
        public IndexColumnModel(bool isDescending, ColumnRef dbName)
        {
            this.IsDescending = isDescending;
            this.DbName = dbName;
        }

        public readonly bool IsDescending;
        public readonly ColumnRef DbName;
    }
}