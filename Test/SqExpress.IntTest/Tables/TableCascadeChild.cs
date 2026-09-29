namespace SqExpress.IntTest.Tables;

public class TableCascadeChild : TableBase
{
    public TableCascadeChild() : this(SqExpress.Alias.Auto) { }

    public TableCascadeChild(Alias alias) : base("dbo", "CascadeChild", alias)
    {
        this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
        this.ParentId = this.CreateInt32Column("ParentId",
            ColumnMeta.ForeignKey<TableCascadeParent>(p => p.Id, ForeignKeyDeleteAction.Cascade));
    }

    public Int32TableColumn Id { get; }
    public Int32TableColumn ParentId { get; }
}
