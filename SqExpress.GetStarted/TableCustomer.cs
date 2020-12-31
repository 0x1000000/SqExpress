namespace SqExpress.GetStarted
{
    public class TableCustomer : TableBase
    {
        public Int32TableColumn CustomerId { get; }
        public NullableInt32TableColumn UserId { get; }
        public NullableInt32TableColumn CompanyId { get; }

        public TableCustomer() : this(default) { }

        public TableCustomer(Alias alias) : base("dbo", "Customer", alias)
        {
            this.CustomerId = this.CreateInt32Column(nameof(this.CustomerId), ColumnMeta.PrimaryKey().Identity());
            this.UserId = this.CreateNullableInt32Column(nameof(this.UserId), ColumnMeta.ForeignKey<TableUser>(u => u.UserId));
            this.CompanyId = this.CreateNullableInt32Column(nameof(this.CompanyId), ColumnMeta.ForeignKey<TableCompany>(u => u.CompanyId));

            //Indexes            
            this.AddUniqueIndex(this.UserId, this.CompanyId);
            this.AddUniqueIndex(this.CompanyId, this.UserId);
        }
    }
}