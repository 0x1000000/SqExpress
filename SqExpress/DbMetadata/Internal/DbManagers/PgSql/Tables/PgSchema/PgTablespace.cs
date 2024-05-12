namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.PgSchema
{
    internal class PgTablespace : TableBase
    {
        public Int32TableColumn Oid { get; }

        public StringTableColumn SpcName { get; }

        public PgTablespace(Alias alias = default) : base(null, "pg_tablespace", alias)
        {
            Oid = CreateInt32Column("oid");
            SpcName = CreateStringColumn("spcname", null);
        }
    }
}
