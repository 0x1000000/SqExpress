namespace SqExpress.DbMetadata.Internal.Tables.PgSql.PgSchema;

internal class PgNamespace : TableBase
{
    public Int32TableColumn Oid { get; }

    public StringTableColumn NspName { get; }

    public PgNamespace(Alias alias = default) : base(null, "pg_namespace", alias)
    {
        this.Oid = this.CreateInt32Column("oid");
        this.NspName = this.CreateStringColumn("nspname", null);
    }
}