namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.PgSchema
{
    internal class PgConstraint : TableBase
    {
        public StringTableColumn ConName { get; }

        public Int32TableColumn ConRelId { get; }

        public Int32TableColumn ConFRelId { get; }

        public StringTableColumn ConKey { get; }

        public StringTableColumn ConFKey { get; }

        public StringTableColumn ConType { get; }

        public PgConstraint(Alias alias = default) : base(null, "pg_constraint", alias)
        {
            ConName = CreateStringColumn("conname", null);
            ConRelId = CreateInt32Column("conrelid");
            ConFRelId = CreateInt32Column("confrelid");
            ConKey = CreateStringColumn("conkey", null);
            ConFKey = CreateStringColumn("confkey", null);
            ConType = CreateStringColumn("contype", null);
        }
    }
}
