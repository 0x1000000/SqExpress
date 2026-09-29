namespace SqExpress.IntTest.Tables;

public class TableCascadeParent : TableBase
{
    public TableCascadeParent() : this(SqExpress.Alias.Auto) { }

    public TableCascadeParent(Alias alias) : base("dbo", "CascadeParent", alias)
    {
        this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
    }

    public Int32TableColumn Id { get; }
}
