namespace SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema;

internal class MySqlReferentialConstraints : TableBase
{
    public MySqlReferentialConstraints(Alias alias = default)
        : base("INFORMATION_SCHEMA", string.Empty, "REFERENTIAL_CONSTRAINTS", alias)
    {
        ConstraintSchema = CreateStringColumn("CONSTRAINT_SCHEMA", 64, true);
        ConstraintName = CreateStringColumn("CONSTRAINT_NAME", 64, true);
        DeleteRule = CreateNullableStringColumn("DELETE_RULE", 64, true);
    }

    public StringTableColumn ConstraintSchema { get; }
    public StringTableColumn ConstraintName { get; }
    public NullableStringTableColumn DeleteRule { get; }
}
