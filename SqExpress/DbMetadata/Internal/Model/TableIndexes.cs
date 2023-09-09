using System.Collections.Generic;

namespace SqExpress.DbMetadata.Internal.Model
{
    internal class PrimaryKeyModel
    {
        public readonly List<IndexColumnModel> Columns;

        public readonly string Name;

        public PrimaryKeyModel(List<IndexColumnModel> columns, string name)
        {
            Columns = columns;
            Name = name;
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
            Columns = columns;
            Name = name;
            IsUnique = isUnique;
            IsClustered = isClustered;
        }
    }

    internal readonly struct IndexColumnModel
    {
        public IndexColumnModel(bool isDescending, ColumnRef dbName)
        {
            IsDescending = isDescending;
            DbName = dbName;
        }

        public readonly bool IsDescending;
        public readonly ColumnRef DbName;
    }
}