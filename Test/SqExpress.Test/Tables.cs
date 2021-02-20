namespace SqExpress.Test
{
    public static class Tables
    {
        public static User User(Alias alias = default) => new User(alias);

        public static Customer Customer(Alias alias = default) => new Customer(alias);

        public static CustomerOrder CustomerOrder(Alias alias = default) => new CustomerOrder(alias);
    }

    public class User : TableBase
    {
        public Int32TableColumn UserId { get; }
        public StringTableColumn FirstName { get; }
        public StringTableColumn LastName { get; }
        public StringTableColumn Email { get; }
        public Int32TableColumn Version { get; }
        public DateTimeTableColumn Created { get; }
        public DateTimeTableColumn Modified { get; }
        public DateTimeTableColumn RegDate { get; }

        public User() : this(default)
        {
        }

        public User(string databaseName, Alias alias = default) : base(databaseName, "dbo", "user", alias)
        {
        }

        public User(Alias alias) : base("dbo", "user", alias)
        {
            this.UserId = this.CreateInt32Column("UserId");
            this.FirstName = this.CreateStringColumn("FirstName", 255);
            this.LastName = this.CreateStringColumn("LastName", 255);
            this.Email = this.CreateStringColumn("Email", 255);
            this.RegDate = this.CreateDateTimeColumn("RegDate");
            this.Version = this.CreateInt32Column("Version");
            this.Created = this.CreateDateTimeColumn("Created");
            this.Modified = this.CreateDateTimeColumn("Modified");
        }
    }

    public class Organization : TableBase
    {
        public readonly Int32TableColumn OrganizationId;
        public readonly StringTableColumn OrganizationName;

        public Organization() : this(default) { }

        public Organization(Alias alias) : base("dbo", nameof(Organization), alias)
        {
            this.OrganizationId = this.CreateInt32Column(nameof(this.OrganizationId), ColumnMeta.PrimaryKey());
            this.OrganizationName = this.CreateStringColumn(nameof(this.OrganizationName), 250);
        }
    }

    public class Customer : TableBase
    {
        public Int32TableColumn CustomerId { get; }
        public NullableInt32TableColumn UserId { get; }
        public NullableInt32TableColumn OrganizationIdId { get; }

        public Customer() : this(default) { }

        public Customer(Alias alias) : base("dbo", nameof(Customer), alias)
        {
            this.CustomerId = this.CreateInt32Column(nameof(CustomerId), ColumnMeta.PrimaryKey().Identity());
            this.UserId = this.CreateNullableInt32Column(nameof(UserId), ColumnMeta.ForeignKey<User>(u => u.UserId));
            this.OrganizationIdId = this.CreateNullableInt32Column(nameof(OrganizationIdId), ColumnMeta.ForeignKey<Organization>(u => u.OrganizationId));
        }
    }

    public class CustomerOrder : TableBase
    {
        public Int32TableColumn CustomerId { get; }
        public Int32TableColumn OrderId { get; }

        public CustomerOrder(Alias alias = default) : base("dbo", "CustomerOrder", alias)
        {
            this.CustomerId = this.CreateInt32Column("CustomerId");
            this.OrderId = this.CreateInt32Column("OrderId");
        }
    }
}