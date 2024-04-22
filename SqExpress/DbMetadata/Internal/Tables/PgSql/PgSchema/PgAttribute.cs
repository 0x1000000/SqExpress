namespace SqExpress.DbMetadata.Internal.Tables.PgSql.PgSchema;

internal class PgAttribute : TableBase
{
    public Int32TableColumn AttRelId { get; }

    public Int32TableColumn AttNum { get; }

    public StringTableColumn AttName { get; }


    public PgAttribute(Alias alias = default) : base(null, "pg_attribute", alias)
    {
        this.AttRelId = this.CreateInt32Column("attrelid");
        this.AttNum = this.CreateInt32Column("attnum");
        this.AttName = this.CreateStringColumn("attname", null);
    }
}
