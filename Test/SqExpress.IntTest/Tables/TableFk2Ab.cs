namespace SqExpress.IntTest.Tables
{
    public class TableFk2Ab : TableBase
    {
        public TableFk2Ab() : this(SqExpress.Alias.Auto)
        {
        }

        public TableFk2Ab(Alias alias) : base("public", "Fk2AB", alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            this.Parent0 = this.CreateInt32Column("Parent0", ColumnMeta.ForeignKey<TableFk0>(t=>t.Id));
            this.ParentA = this.CreateInt32Column("ParentA", ColumnMeta.ForeignKey<TableFk1A>(t=>t.Id));
            this.ParentB = this.CreateInt32Column("ParentB", ColumnMeta.ForeignKey<TableFk1B>(t=>t.Id));

            this.AddUniqueIndex(this.ParentA, this.ParentB);
        }

        public Int32TableColumn Id { get; set; }

        public Int32TableColumn Parent0 { get; set; }

        public Int32TableColumn ParentA { get; set; }

        public Int32TableColumn ParentB { get; set; }
    }
}