namespace SqExpress.GetStarted
{
    public class TableUser : TableBase
    {
        public readonly Int32TableColumn UserId;
        public readonly StringTableColumn FirstName;
        public readonly StringTableColumn LastName;
        //Audit Columns
        public readonly Int32TableColumn Version;
        public readonly DateTimeTableColumn ModifiedAt;

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