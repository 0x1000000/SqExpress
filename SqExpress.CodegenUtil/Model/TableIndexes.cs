using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model
{
    internal class PrimaryKey
    {
        public readonly List<IndexColumn> Columns;

        public readonly string Name;

        public PrimaryKey(List<IndexColumn> columns, string name)
        {
            this.Columns = columns;
            this.Name = name;
        }
    }

    internal class Index
    {
        public readonly List<IndexColumn> Columns;

        public readonly string Name;

        public readonly bool IsUnique;

        public readonly bool IsClustered;

        public Index(List<IndexColumn> columns, string name, bool isUnique, bool isClustered)
        {
            this.Columns = columns;
            this.Name = name;
            this.IsUnique = isUnique;
            this.IsClustered = isClustered;
        }
    }

    internal readonly struct IndexColumn
    {
        public IndexColumn(bool isDescending, ColumnRef column)
        {
            this.IsDescending = isDescending;
            this.Column = column;
        }

        public readonly bool IsDescending;
        public readonly ColumnRef Column;
    }
}