namespace SqExpress.IntTest.Tables
{
    public class CustomerOrder : TableBase
    {
        public Int32TableColumn CustomerId { get; }
        public Int32TableColumn OrderId { get; }

        public CustomerOrder(Alias alias = default) : base("public", "ItCustomerOrder", alias)
        {
            this.CustomerId = this.CreateInt32Column("CustomerId", ColumnMeta.PrimaryKey().ForeignKey<Customer>(c=>c.CustomerId));
            this.OrderId = this.CreateInt32Column("OrderId", ColumnMeta.PrimaryKey());
        }
    }
}