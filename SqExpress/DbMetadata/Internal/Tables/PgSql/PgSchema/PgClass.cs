namespace SqExpress.DbMetadata.Internal.Tables.PgSql.PgSchema;

internal class PgClass : TableBase
{
    public Int32TableColumn Oid { get; }

    public Int32TableColumn RelNamespace { get; }

    public Int32TableColumn RelTablespace { get; }

    public StringTableColumn RelName { get; }

    public StringTableColumn RelKind { get; }

    public PgClass(Alias alias = default) : base(null, "pg_class", alias)
    {
        this.Oid = this.CreateInt32Column("oid");
        this.RelNamespace = this.CreateInt32Column("relnamespace");
        this.RelTablespace = this.CreateInt32Column("reltablespace");
        this.RelName = this.CreateStringColumn("relname", null);
        this.RelKind = this.CreateStringColumn("relkind", null);
    }
}
