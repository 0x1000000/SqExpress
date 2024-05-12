namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.PgSchema
{
    internal class PgAttribute : TableBase
    {
        public Int32TableColumn AttRelId { get; }

        public Int32TableColumn AttNum { get; }

        public StringTableColumn AttName { get; }


        public PgAttribute(Alias alias = default) : base(null, "pg_attribute", alias)
        {
            AttRelId = CreateInt32Column("attrelid");
            AttNum = CreateInt32Column("attnum");
            AttName = CreateStringColumn("attname", null);
        }
    }
}
