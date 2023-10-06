using System.Collections.Generic;

namespace SqExpress
{
    public class IndexMeta
    {
        public IndexMeta(IReadOnlyList<IndexMetaColumn> columns, string? name, bool unique, bool clustered)
        {
            this.Columns = columns;
            this.Name = name;
            this.Unique = unique;
            this.Clustered = clustered;
        }

        public IReadOnlyList<IndexMetaColumn> Columns { get; }
        
        public string? Name { get; }
        
        public bool Unique { get; }
        
        public bool Clustered { get; }
    }

    public class IndexMetaColumn
    {
        public readonly TableColumn Column;

        public readonly bool Descending;

        internal IndexMetaColumn(TableColumn column, bool descending)
        {
            this.Column = column;
            this.Descending = descending;
        }

        public static IndexMetaColumn Asc(TableColumn column)
            => new IndexMetaColumn(column, false);

        public static IndexMetaColumn Desc(TableColumn column)
            => new IndexMetaColumn(column, true);

        public static implicit operator IndexMetaColumn(TableColumn column) 
            => new IndexMetaColumn(column, false);
    }
}