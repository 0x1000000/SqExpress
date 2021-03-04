using SqExpress;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableFk1B : TableBase
    {
        public TableFk1B(): this(alias: SqExpress.Alias.Auto)
        {
        }

        public TableFk1B(Alias alias): base(schema: "dbo", name: "Fk1B", alias: alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            this.Parent = this.CreateInt32Column("Parent", ColumnMeta.ForeignKey<TableFk0>(t => t.Id));
        }

        public Int32TableColumn Id { get; }

        public Int32TableColumn Parent { get; }
    }
}