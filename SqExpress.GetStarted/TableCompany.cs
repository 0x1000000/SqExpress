namespace SqExpress.GetStarted
{
    public class TableCompany : TableBase
    {
        public readonly Int32TableColumn CompanyId;
        public readonly StringTableColumn CompanyName;

        //Audit Columns
        public readonly Int32TableColumn Version;
        public readonly DateTimeTableColumn ModifiedAt;

        public TableCompany() : this(default) { }

        public TableCompany(Alias alias) : base("dbo", "Company", alias)
        {
            this.CompanyId = this.CreateInt32Column(nameof(this.CompanyId), ColumnMeta.PrimaryKey().Identity());
            this.CompanyName = this.CreateStringColumn(nameof(this.CompanyName), 250);

            this.Version = this.CreateInt32Column("Version",
                ColumnMeta.DefaultValue(0));
            this.ModifiedAt = this.CreateDateTimeColumn("ModifiedAt",
                columnMeta: ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));
        }
    }
}