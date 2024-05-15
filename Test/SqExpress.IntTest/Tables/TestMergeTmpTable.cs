namespace SqExpress.IntTest.Tables
{
    public class TestMergeTmpTable : TempTableBase
    {
        public TestMergeTmpTable() : base("TargetTable", SqExpress.Alias.Auto)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            this.Value = this.CreateInt32Column("Value");
            this.Version = this.CreateInt32Column("Version", ColumnMeta.DefaultValue(0));
            this.Extra = this.CreateNullableStringColumn("Extra", 255, true);
        }


        [SqModel("TestMergeData")]
        [SqModel("TestMergeDataRow")]
        public Int32TableColumn Id { get; }

        [SqModel("TestMergeData")]
        [SqModel("TestMergeDataRow")]
        public Int32TableColumn Value { get; set; }

        [SqModel("TestMergeDataRow")]
        public Int32TableColumn Version { get; set; }

        [SqModel("TestMergeDataRow")]
        public NullableStringTableColumn Extra { get; set; }
    }
}