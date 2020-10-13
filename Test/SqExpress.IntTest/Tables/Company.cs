namespace SqExpress.IntTest.Tables
{
    public class Company : TableBase
    {
        public readonly Int32TableColumn CompanyId;
        public readonly GuidTableColumn ExternalId;
        public readonly StringTableColumn CompanyName;
        public readonly Int32TableColumn Version;
        public readonly DateTimeTableColumn Created;
        public readonly DateTimeTableColumn Modified;

        public Company() : this(default){}

        public Company(Alias alias) : base("public", "ItCompany", alias)
        {
            this.CompanyId = this.CreateInt32Column(nameof(this.CompanyId), ColumnMeta.PrimaryKey().Identity());
            this.ExternalId = this.CreateGuidColumn(nameof(this.ExternalId));
            this.CompanyName = this.CreateStringColumn(nameof(this.CompanyName), 250);
            this.Version = this.CreateInt32Column("Version");
            this.Created = this.CreateDateTimeColumn("Created");
            this.Modified = this.CreateDateTimeColumn("Modified");
        }
    }
}