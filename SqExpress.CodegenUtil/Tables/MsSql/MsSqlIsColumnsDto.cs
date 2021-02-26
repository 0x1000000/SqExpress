using SqExpress.CodeGenUtil.Tables.MySQL;

namespace SqExpress.CodeGenUtil.Tables.MsSql
{
    internal record MsSqlIsColumnsDto(
        string TableCatalog,
        string TableSchema,
        string TableName,
        string ColumnName,
        int OrdinalPosition,
        string? ColumnDefault,
        string IsNullable,
        string? DataType,
        int? CharacterMaximumLength,
        int? CharacterOctetLength,
        byte? NumericPrecision,
        short? DatetimePrecision,
        string? CharacterSetName) : IIsColumnsDto
    {

        public static MsSqlIsColumnsDto FromRecord(ISqDataRecordReader r, MsSqlIsColumns table) =>
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