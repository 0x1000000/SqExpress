namespace SqExpress.CodeGenUtil.Tables
{
    internal interface IIsColumnsDto
    {
        string TableCatalog { get; }
        string TableSchema { get; }
        string TableName { get; }
        string ColumnName { get; }
        int OrdinalPosition { get; }
        string? ColumnDefault { get; }
        string IsNullable { get; }
        string? DataType { get; }
        string? CharacterSetName { get; }
    }
}