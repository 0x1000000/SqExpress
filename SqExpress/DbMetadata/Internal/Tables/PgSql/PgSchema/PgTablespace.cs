namespace SqExpress.DbMetadata.Internal.Tables.PgSql.PgSchema;

internal class PgTablespace : TableBase
{
    public Int32TableColumn Oid { get; }

    public StringTableColumn SpcName { get; }

    public PgTablespace(Alias alias = default) : base(null, "pg_tablespace", alias)
    {
        this.Oid = this.CreateInt32Column("oid");
        this.SpcName = this.CreateStringColumn("spcname", null);
    }
}
