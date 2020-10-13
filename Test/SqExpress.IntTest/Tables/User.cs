namespace SqExpress.IntTest.Tables
{
    public class User : TableBase
    {
        public Int32TableColumn UserId { get; }
        public GuidTableColumn ExternalId { get; }
        public StringTableColumn FirstName { get; }
        public StringTableColumn LastName { get; }
        public StringTableColumn Email { get; }
        public Int32TableColumn Version { get; }
        public DateTimeTableColumn Created { get; }
        public DateTimeTableColumn Modified { get; }
        public DateTimeTableColumn RegDate { get; }

        public User() : this(default) { }

        public User(Alias alias = default) : base("public", "ItUser", alias)
        {
            this.UserId = this.CreateInt32Column("UserId", ColumnMeta.PrimaryKey().Identity());
            this.ExternalId = this.CreateGuidColumn("ExternalId");
            this.FirstName = this.CreateStringColumn("FirstName", 255);
            this.LastName = this.CreateStringColumn("LastName", 255);
            this.Email = this.CreateStringColumn("Email", 255);
            this.RegDate = this.CreateDateTimeColumn("RegDate");
            this.Version = this.CreateInt32Column("Version");
            this.Created = this.CreateDateTimeColumn("Created");
            this.Modified = this.CreateDateTimeColumn("Modified");
        }
    }
}