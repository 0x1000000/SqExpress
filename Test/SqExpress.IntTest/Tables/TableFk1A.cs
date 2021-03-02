namespace SqExpress.IntTest.Tables
{
    public class TableFk1A : TableBase
    {
        public TableFk1A() : this(SqExpress.Alias.Auto)
        {
        }

        public TableFk1A(Alias alias) : base("public", "Fk1A", alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            this.Parent = this.CreateInt32Column("Parent", ColumnMeta.ForeignKey<TableFk0>(t=>t.Id));
        }

        public Int32TableColumn Id { get; set; }

        public Int32TableColumn Parent { get; set; }
    }
}