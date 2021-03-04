using SqExpress;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableFk0 : TableBase
    {
        public TableFk0(): this(alias: SqExpress.Alias.Auto)
        {
        }

        public TableFk0(Alias alias): base(schema: "dbo", name: "Fk0", alias: alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
        }

        public Int32TableColumn Id { get; }
    }
}