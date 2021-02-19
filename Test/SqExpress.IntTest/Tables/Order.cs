namespace SqExpress.IntTest.Tables
{
    public class Order : TableBase
    {
        public Int32TableColumn CustomerId { get; }
        public Int32TableColumn OrderId { get; }
        public DateTimeTableColumn DateCreated { get; }
        public NullableStringTableColumn Notes { get; }

        public Order(Alias alias = default) : base("public", "ItOrder", alias)
        {
            this.OrderId = this.CreateInt32Column(nameof(OrderId), ColumnMeta.PrimaryKey().Identity());
            this.CustomerId = this.CreateInt32Column(nameof(CustomerId), ColumnMeta.ForeignKey<Customer>(c=>c.CustomerId));
            this.DateCreated = this.CreateDateTimeColumn(nameof(DateCreated), columnMeta: ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));
            this.Notes = this.CreateNullableStringColumn(nameof(Notes), size: 100, isUnicode: true);
        }
    }
}