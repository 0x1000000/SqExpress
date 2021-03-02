namespace SqExpress.CodeGenUtil.Tables.MsSql
{
    internal class MsSqlReferentialConstraints : TableBase
    {
        public MsSqlReferentialConstraints(Alias alias = default) : base("INFORMATION_SCHEMA", "REFERENTIAL_CONSTRAINTS", alias)
        {
            this.ConstraintCatalog = this.CreateStringColumn("CONSTRAINT_CATALOG", 128, true);
            this.ConstraintSchema = this.CreateStringColumn("CONSTRAINT_SCHEMA", 128, true);
            this.ConstraintName = this.CreateStringColumn("CONSTRAINT_NAME", 128, true);

            this.UniqueConstraintCatalog = this.CreateStringColumn("UNIQUE_CONSTRAINT_CATALOG", 128, true);
            this.UniqueConstraintSchema = this.CreateStringColumn("UNIQUE_CONSTRAINT_SCHEMA", 128, true);
            this.UniqueConstraintName = this.CreateStringColumn("UNIQUE_CONSTRAINT_NAME", 128, true);

            this.MatchOption = this.CreateStringColumn("MATCH_OPTION", 11);
            this.UpdateRule = this.CreateStringColumn("UPDATE_RULE", 11);
        }

        public StringTableColumn UpdateRule { get; set; }

        public StringTableColumn MatchOption { get; set; }

        public StringTableColumn ConstraintCatalog { get; set; }

        public StringTableColumn ConstraintSchema { get; set; }

        public StringTableColumn ConstraintName { get; set; }

        public StringTableColumn UniqueConstraintCatalog { get; set; }

        public StringTableColumn UniqueConstraintSchema { get; set; }

        public StringTableColumn UniqueConstraintName { get; set; }
    }
}