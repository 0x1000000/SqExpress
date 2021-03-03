namespace SqExpress.CodeGenUtil.Tables.MsSql
{
    internal class MsSqlSysSchemas : TableBase
    {
        public MsSqlSysSchemas(Alias alias = default) : base("sys", "schemas", alias)
        {
            this.SchemaId = this.CreateInt32Column("schema_id");
            this.Name = this.CreateStringColumn("name", 128, true);
        }

        public Int32TableColumn SchemaId { get; }

        public StringTableColumn Name { get; }
    }

    internal class MsSqlSysTables : TableBase
    {
        public MsSqlSysTables(Alias alias = default) : base("sys", "tables", alias)
        {
            this.ObjectId = this.CreateInt32Column("object_id");
            this.SchemaId = this.CreateInt32Column("schema_id");
            this.Name = this.CreateStringColumn("name", 128, true);
        }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn SchemaId { get; }

        public StringTableColumn Name { get; }
    }

    internal class MsSqlSysColumns : TableBase
    {
        public MsSqlSysColumns(Alias alias = default) : base("sys", "columns", alias)
        {
            this.ObjectId = this.CreateInt32Column("object_id");
            this.ColumnId = this.CreateInt32Column("column_id");
            this.Name = this.CreateStringColumn("name", 128, true);
        }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn ColumnId { get; }

        public StringTableColumn Name { get; }
    }

    internal class MsSqlSysIndexes : TableBase
    {
        public MsSqlSysIndexes(Alias alias = default) : base("sys", "indexes", alias)
        {
            this.ObjectId = this.CreateInt32Column("object_id");
            this.IndexId = this.CreateInt32Column("index_id");
            this.Name = this.CreateStringColumn("name", 128, true);
            this.Type = this.CreateByteColumn("type");
            this.IsPrimaryKey = this.CreateBooleanColumn("is_primary_key");
            this.IsUnique = this.CreateBooleanColumn("is_unique");
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
            this.ObjectId = this.CreateInt32Column("object_id");
            this.IndexId = this.CreateInt32Column("index_id");
            this.ColumnId = this.CreateInt32Column("column_id");
            this.KeyOrdinal = this.CreateInt32Column("key_ordinal");
            this.IsDescendingKey = this.CreateBooleanColumn("is_descending_key");
            this.IsIncludedColumn = this.CreateBooleanColumn("is_included_column");
        }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn IndexId { get; }

        public Int32TableColumn ColumnId { get; }

        public Int32TableColumn KeyOrdinal { get; set; }

        public BooleanTableColumn IsDescendingKey { get; set; }

        public BooleanTableColumn IsIncludedColumn { get; set; }
    }
}