namespace SqExpress.IntTest.Tables;

public class TableFkCycleA : TableBase
{
    public TableFkCycleA() : this(SqExpress.Alias.Auto)
    {
    }

    public TableFkCycleA(Alias alias) : base("dbo", "FkCycleA", alias)
    {
        this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
        this.FkCycleBId = this.CreateNullableInt32Column("FkCycleBId",
            ColumnMeta.ForeignKey<TableFkCycleB>(t => t.Id));
    }

    public Int32TableColumn Id { get; }

    public NullableInt32TableColumn FkCycleBId { get; }
}
