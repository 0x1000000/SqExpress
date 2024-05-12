namespace SqExpress.DbMetadata.Internal.DbManagers.MsSql.Tables
{
    internal class MsSqlReferentialConstraints : TableBase
    {
        public MsSqlReferentialConstraints(Alias alias = default) : base("INFORMATION_SCHEMA", "REFERENTIAL_CONSTRAINTS", alias)
        {
            ConstraintCatalog = CreateStringColumn("CONSTRAINT_CATALOG", 128, true);
            ConstraintSchema = CreateStringColumn("CONSTRAINT_SCHEMA", 128, true);
            ConstraintName = CreateStringColumn("CONSTRAINT_NAME", 128, true);

            UniqueConstraintCatalog = CreateStringColumn("UNIQUE_CONSTRAINT_CATALOG", 128, true);
            UniqueConstraintSchema = CreateStringColumn("UNIQUE_CONSTRAINT_SCHEMA", 128, true);
            UniqueConstraintName = CreateStringColumn("UNIQUE_CONSTRAINT_NAME", 128, true);

            MatchOption = CreateStringColumn("MATCH_OPTION", 11);
            UpdateRule = CreateStringColumn("UPDATE_RULE", 11);
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