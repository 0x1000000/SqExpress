namespace SqExpress.IntTest.Tables;

public class TableFkCycleB : TableBase
{
    public TableFkCycleB() : this(SqExpress.Alias.Auto)
    {
    }

    public TableFkCycleB(Alias alias) : base("dbo", "FkCycleB", alias)
    {
        this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
        this.FkCycleAId = this.CreateNullableInt32Column("FkCycleAId",
            ColumnMeta.ForeignKey<TableFkCycleA>(t => t.Id));
    }

    public Int32TableColumn Id { get; }

    public NullableInt32TableColumn FkCycleAId { get; }
}
