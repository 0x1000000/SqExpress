using SqExpress;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableFk3AB : TableBase
    {
        public TableFk3AB(): this(alias: SqExpress.Alias.Auto)
        {
        }

        public TableFk3AB(Alias alias): base(schema: "dbo", name: "Fk3AB", alias: alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            this.Parent0 = this.CreateInt32Column("Parent0", ColumnMeta.ForeignKey<TableFk0>(t => t.Id));
            this.ParentA = this.CreateInt32Column("ParentA", ColumnMeta.ForeignKey<TableFk1A>(t => t.Id).ForeignKey<TableFk2AB>(t => t.ParentA));
            this.ParentB = this.CreateInt32Column("ParentB", ColumnMeta.ForeignKey<TableFk1B>(t => t.Id).ForeignKey<TableFk2AB>(t => t.ParentB));
        }

        public Int32TableColumn Id { get; }

        public Int32TableColumn Parent0 { get; }

        public Int32TableColumn ParentA { get; }

        public Int32TableColumn ParentB { get; }
    }
}