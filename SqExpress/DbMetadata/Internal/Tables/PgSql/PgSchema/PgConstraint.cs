namespace SqExpress.DbMetadata.Internal.Tables.PgSql.PgSchema;

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
        this.ConName = this.CreateStringColumn("conname", null);
        this.ConRelId = this.CreateInt32Column("conrelid");
        this.ConFRelId = this.CreateInt32Column("confrelid");
        this.ConKey = this.CreateStringColumn("conkey", null);
        this.ConFKey = this.CreateStringColumn("confkey", null);
        this.ConType = this.CreateStringColumn("contype", null);
    }
}
