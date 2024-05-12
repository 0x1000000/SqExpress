namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.PgSchema
{
    internal class PgClass : TableBase
    {
        public Int32TableColumn Oid { get; }

        public Int32TableColumn RelNamespace { get; }

        public Int32TableColumn RelTablespace { get; }

        public StringTableColumn RelName { get; }

        public StringTableColumn RelKind { get; }

        public PgClass(Alias alias = default) : base(null, "pg_class", alias)
        {
            Oid = CreateInt32Column("oid");
            RelNamespace = CreateInt32Column("relnamespace");
            RelTablespace = CreateInt32Column("reltablespace");
            RelName = CreateStringColumn("relname", null);
            RelKind = CreateStringColumn("relkind", null);
        }
    }
}
