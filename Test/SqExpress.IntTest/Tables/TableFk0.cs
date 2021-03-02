namespace SqExpress.IntTest.Tables
{
    public class TableFk0 : TableBase
    {
        public TableFk0() : this(SqExpress.Alias.Auto)
        {
        }

        public TableFk0(Alias alias) : base("public", "Fk0", alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
        }

        public Int32TableColumn Id { get; set; }
    }
}