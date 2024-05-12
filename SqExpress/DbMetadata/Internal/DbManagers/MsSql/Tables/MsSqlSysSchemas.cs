namespace SqExpress.DbMetadata.Internal.DbManagers.MsSql.Tables
{
    internal class MsSqlSysSchemas : TableBase
    {
        public MsSqlSysSchemas(Alias alias = default) : base("sys", "schemas", alias)
        {
            SchemaId = CreateInt32Column("schema_id");
            Name = CreateStringColumn("name", 128, true);
        }

        public Int32TableColumn SchemaId { get; }

        public StringTableColumn Name { get; }
    }

    internal class MsSqlSysTables : TableBase
    {
        public MsSqlSysTables(Alias alias = default) : base("sys", "tables", alias)
        {
            ObjectId = CreateInt32Column("object_id");
            SchemaId = CreateInt32Column("schema_id");
            Name = CreateStringColumn("name", 128, true);
        }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn SchemaId { get; }

        public StringTableColumn Name { get; }
    }

    internal class MsSqlSysColumns : TableBase
    {
        public MsSqlSysColumns(Alias alias = default) : base("sys", "columns", alias)
        {
            ObjectId = CreateInt32Column("object_id");
            ColumnId = CreateInt32Column("column_id");
            Name = CreateStringColumn("name", 128, true);
        }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn ColumnId { get; }

        public StringTableColumn Name { get; }
    }

    internal class MsSqlSysIndexes : TableBase
    {
        public MsSqlSysIndexes() : this(default) { }

        public MsSqlSysIndexes(Alias alias = default) : base("sys", "indexes", alias)
        {
            ObjectId = CreateInt32Column("object_id");
            IndexId = CreateInt32Column("index_id");
            Name = CreateStringColumn("name", 128, true);
            Type = CreateByteColumn("type");
            IsPrimaryKey = CreateBooleanColumn("is_primary_key");
            IsUnique = CreateBooleanColumn("is_unique");
        }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn IndexId { get; }

        public StringTableColumn Name { get; }

        public ByteTableColumn Type { get; }

        public BooleanTableColumn IsPrimaryKey { get; }

        public BooleanTableColumn IsUnique { get; }
    }

    internal class MsSqlSysIndexColumns : TableBase
    {
        public MsSqlSysIndexColumns(Alias alias = default) : base("sys", "index_columns", alias)
        {
            ObjectId = CreateInt32Column("object_id");
            IndexId = CreateInt32Column("index_id");
            ColumnId = CreateInt32Column("column_id");
            KeyOrdinal = CreateInt32Column("key_ordinal");
            IsDescendingKey = CreateBooleanColumn("is_descending_key");
            IsIncludedColumn = CreateBooleanColumn("is_included_column");
        }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn IndexId { get; }

        public Int32TableColumn ColumnId { get; }

        public Int32TableColumn KeyOrdinal { get; set; }

        public BooleanTableColumn IsDescendingKey { get; set; }

        public BooleanTableColumn IsIncludedColumn { get; set; }
    }
}