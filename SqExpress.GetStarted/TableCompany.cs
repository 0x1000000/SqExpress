namespace SqExpress.GetStarted
{
    public class TableCompany : TableBase
    {
        [SqModel("CompanyName", PropertyName = "Id")]
        public Int32TableColumn CompanyId { get; }

        [SqModel("CompanyName", PropertyName = "Name")]
        public StringTableColumn CompanyName { get; }

        //Audit Columns
        [SqModel("AuditData")]
        public Int32TableColumn Version { get; }

        [SqModel("AuditData")]
        public DateTimeTableColumn ModifiedAt { get; }

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