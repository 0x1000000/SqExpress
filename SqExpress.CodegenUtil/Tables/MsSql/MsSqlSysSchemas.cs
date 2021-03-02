namespace SqExpress.CodeGenUtil.Tables.MsSql
{
    public class MsSqlSysSchemas : TableBase
    {
        public MsSqlSysSchemas(Alias alias = default) : base("sys", "schemas", alias)
        {
            this.SchemaId = this.CreateInt32Column("schema_id");
            this.Name = this.CreateStringColumn("name", 128, true);
        }

        public Int32TableColumn SchemaId { get; }

        public StringTableColumn Name { get; }
    }

    public class MsSqlSysTables : TableBase
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

    public class MsSqlSysColumns : TableBase
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

    public class MsSqlSysIndexes : TableBase
    {
        public MsSqlSysIndexes(Alias alias = default) : base("sys", "indexes", alias)
        {
            this.ObjectId = this.CreateInt32Column("object_id");
            this.IndexId = this.CreateInt32Column("index_id");
            this.Name = this.CreateStringColumn("name", 128, true);
            this.IsPrimaryKey = this.CreateBooleanColumn("is_primary_key");
        }

        public BooleanTableColumn IsPrimaryKey { get; set; }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn IndexId { get; }

        public StringTableColumn Name { get; }


    }

    public class MsSqlSysIndexColumns : TableBase
    {
        public MsSqlSysIndexColumns(Alias alias = default) : base("sys", "index_columns", alias)
        {
            this.ObjectId = this.CreateInt32Column("object_id");
            this.IndexId = this.CreateInt32Column("index_id");
            this.ColumnId = this.CreateInt32Column("column_id");
            this.KeyOrdinal = this.CreateInt32Column("key_ordinal");
        }

        public Int32TableColumn KeyOrdinal { get; set; }

        public Int32TableColumn ObjectId { get; }

        public Int32TableColumn IndexId { get; }

        public Int32TableColumn ColumnId { get; }
    }
}