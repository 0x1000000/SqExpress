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
        IndRelId = CreateInt32Column("indrelid");
        IndExRelId = CreateInt32Column("indexrelid");
        IndIsPrimary = CreateBooleanColumn("indisprimary");
        IndIsUnique = CreateBooleanColumn("indisunique");
        IndIsClustered = CreateBooleanColumn("indisclustered");
        IndNkeysAtts = CreateInt32Column("indnkeyatts");
        IndOption = CreateInt32Column("indoption");
    }
}