namespace SqExpress.GetStarted
{
    public class TableUser : TableBase
    {
        [SqModel("UserName", PropertyName = "Id")]
        public Int32TableColumn UserId { get; }

        [SqModel("UserName")]
        public StringTableColumn FirstName { get; }

        [SqModel("UserName")]
        public StringTableColumn LastName { get; }

        //Audit Columns
        [SqModel("AuditData")]
        public Int32TableColumn Version { get; }

        [SqModel("AuditData")]
        public DateTimeTableColumn ModifiedAt { get; }

        public TableUser(): this(default){}

        public TableUser(Alias alias) : base("dbo", "User", alias)
        {
            this.UserId = this.CreateInt32Column("UserId", ColumnMeta.PrimaryKey().Identity());
            this.FirstName = this.CreateStringColumn("FirstName", size: 255, isUnicode: true);
            this.LastName = this.CreateStringColumn("LastName", size: 255, isUnicode: true);

            this.Version = this.CreateInt32Column("Version",
                ColumnMeta.DefaultValue(0));
            this.ModifiedAt = this.CreateDateTimeColumn("ModifiedAt",
                columnMeta: ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));
        }
    }
}