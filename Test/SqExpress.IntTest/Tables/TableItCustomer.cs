using SqExpress;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableItCustomer : TableBase
    {
        public TableItCustomer(): this(alias: SqExpress.Alias.Auto)
        {
        }

        public TableItCustomer(Alias alias): base(schema: "dbo", name: "ItCustomer", alias: alias)
        {
            this.CustomerId = this.CreateInt32Column("CustomerId", ColumnMeta.PrimaryKey().Identity());
            this.UserId = this.CreateNullableInt32Column("UserId", ColumnMeta.ForeignKey<TableItUser>(t => t.UserId));
            this.CompanyId = this.CreateNullableInt32Column("CompanyId", ColumnMeta.ForeignKey<TableItCompany>(t => t.CompanyId));
            this.AddUniqueIndex(this.UserId, this.CompanyId);
            this.AddUniqueIndex(this.CompanyId, this.UserId);
        }

        public Int32TableColumn CustomerId { get; }

        public NullableInt32TableColumn UserId { get; }

        public NullableInt32TableColumn CompanyId { get; }
    }
}