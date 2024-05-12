namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.PgSchema
{
    internal class PgNamespace : TableBase
    {
        public Int32TableColumn Oid { get; }

        public StringTableColumn NspName { get; }

        public PgNamespace(Alias alias = default) : base(null, "pg_namespace", alias)
        {
            Oid = CreateInt32Column("oid");
            NspName = CreateStringColumn("nspname", null);
        }
    }
}