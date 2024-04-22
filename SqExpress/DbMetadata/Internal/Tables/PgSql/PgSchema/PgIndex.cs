namespace SqExpress.DbMetadata.Internal.Tables.PgSql.PgSchema;

internal class PgIndex : TableBase
{
    public Int32TableColumn IndRelId { get; }

    public Int32TableColumn IndExRelId { get; }

    public BooleanTableColumn IndIsUnique { get; }

    public BooleanTableColumn IndIsPrimary { get; }

    public BooleanTableColumn IndIsClustered { get; }

    public Int32TableColumn IndNkeysAtts { get; }

    public Int32TableColumn IndOption { get; set; }

    public PgIndex(Alias alias = default) : base(null, "pg_index", alias)
    {
        this.IndRelId = this.CreateInt32Column("indrelid");
        this.IndExRelId = this.CreateInt32Column("indexrelid");
        this.IndIsPrimary = this.CreateBooleanColumn("indisprimary");
        this.IndIsUnique = this.CreateBooleanColumn("indisunique");
        this.IndIsClustered = this.CreateBooleanColumn("indisclustered");
        this.IndNkeysAtts = this.CreateInt32Column("indnkeyatts");
        this.IndOption = this.CreateInt32Column("indoption");
    }
}