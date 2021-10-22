using SqExpress;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableItOrder : TableBase
    {
        public TableItOrder(): this(alias: SqExpress.Alias.Auto)
        {
        }

        public TableItOrder(Alias alias): base(schema: "dbo", name: "ItOrder", alias: alias)
        {
            this.OrderId = this.CreateInt32Column("OrderId", ColumnMeta.PrimaryKey().Identity());
            this.CustomerId = this.CreateInt32Column("CustomerId", ColumnMeta.ForeignKey<TableItCustomer>(t => t.CustomerId));
            this.DateCreated = this.CreateDateTimeColumn("DateCreated", false, ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));
            this.Notes = this.CreateNullableStringColumn(name: "Notes", size: 100, isUnicode: true, isText: false, columnMeta: null);
        }

        [SqModel("OrderDateCreated")]
        public Int32TableColumn OrderId { get; }

        public Int32TableColumn CustomerId { get; }

        [SqModel("OrderDateCreated")]
        public DateTimeTableColumn DateCreated { get; }

        public NullableStringTableColumn Notes { get; }
    }
}