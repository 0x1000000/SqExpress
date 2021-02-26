namespace SqExpress.CodeGenUtil.Tables.MsSql
{
    internal class MsSqlIsTables : TableBase
    {
        public readonly StringTableColumn TableCatalog;

        public readonly StringTableColumn TableSchema;

        public readonly StringTableColumn TableName;

        public readonly StringTableColumn TableType;

        public MsSqlIsTables(Alias alias = default)
            : base("INFORMATION_SCHEMA", "TABLES", alias)
        {
            this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", null);
            this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", null);
            this.TableName = this.CreateStringColumn("TABLE_NAME", null);
            this.TableType = this.CreateStringColumn("TABLE_TYPE", null);
        }
    }
}