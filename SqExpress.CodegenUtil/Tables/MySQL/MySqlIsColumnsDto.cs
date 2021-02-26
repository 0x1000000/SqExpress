using SqExpress.CodeGenUtil.Tables.MsSql;

namespace SqExpress.CodeGenUtil.Tables.MySQL
{
    internal record MySqlIsColumnsDto(
        string TableCatalog,
        string TableSchema,
        string TableName,
        string ColumnName,
        int OrdinalPosition,
        string? ColumnDefault,
        string IsNullable,
        string? DataType,
        long? CharacterMaximumLength,
        long? CharacterOctetLength,
        long? NumericPrecision,
        long? DatetimePrecision,
        string? CharacterSetName) : IIsColumnsDto
    {
        public static MySqlIsColumnsDto FromRecord(ISqDataRecordReader r, MySqlISColumns table) =>
            new(
                TableCatalog: table.TableCatalog.Read(r),
                TableSchema: table.TableSchema.Read(r),
                TableName: table.TableName.Read(r),
                ColumnName: table.ColumnName.Read(r),
                OrdinalPosition: table.OrdinalPosition.Read(r),
                ColumnDefault: table.ColumnDefault.Read(r),
                IsNullable: table.IsNullable.Read(r),
                DataType: table.DataType.Read(r),
                CharacterMaximumLength: table.CharacterMaximumLength.Read(r),
                CharacterOctetLength: table.CharacterOctetLength.Read(r),
                NumericPrecision: table.NumericPrecision.Read(r),
                DatetimePrecision: table.DatetimePrecision.Read(r),
                CharacterSetName: table.CharacterSetName.Read(r));
    }
}