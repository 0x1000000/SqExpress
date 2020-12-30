namespace SqExpress.IntTest.Tables
{
    public class Customer : TableBase
    {
        public Int32TableColumn CustomerId { get; }
        public NullableInt32TableColumn UserId { get; }
        public NullableInt32TableColumn CompanyId { get; }

        public Customer() :  this(default){}

        public Customer(Alias alias) : base("public", "ItCustomer", alias)
        {
            //Columns
            this.CustomerId = this.CreateInt32Column(nameof(this.CustomerId), ColumnMeta.PrimaryKey().Identity());
            this.UserId = this.CreateNullableInt32Column(nameof(this.UserId), ColumnMeta.ForeignKey<User>(u=>u.UserId));
            this.CompanyId = this.CreateNullableInt32Column(nameof(this.CompanyId), ColumnMeta.ForeignKey<Company>(u=>u.CompanyId));

            //Indexes
            this.AddUniqueIndex(this.UserId, this.CompanyId);
            this.AddUniqueIndex(this.CompanyId, this.UserId);
        }
    }
}