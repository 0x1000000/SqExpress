namespace SqExpress.IntTest.Tables
{
    public class TableFk3Ab : TableBase
    {
        public TableFk3Ab() : this(SqExpress.Alias.Auto)
        {
        }

        public TableFk3Ab(Alias alias) : base("public", "Fk3AB", alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            this.Parent0 = this.CreateInt32Column("Parent0", ColumnMeta.ForeignKey<TableFk0>(t => t.Id));
            this.ParentA = this.CreateInt32Column("ParentA", ColumnMeta.ForeignKey<TableFk2Ab>(t=>t.ParentA).ForeignKey<TableFk1A>(t=>t.Id));
            this.ParentB = this.CreateInt32Column("ParentB", ColumnMeta.ForeignKey<TableFk2Ab>(t => t.ParentB).ForeignKey<TableFk1B>(t => t.Id));
        }

        public Int32TableColumn Id { get; set; }

        public Int32TableColumn Parent0 { get; set; }

        public Int32TableColumn ParentA { get; set; }

        public Int32TableColumn ParentB { get; set; }
    }
}