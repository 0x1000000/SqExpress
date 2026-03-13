using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Utils;

namespace SqExpress.SyntaxTreeOperations.Internal;

internal readonly struct ExprTypeToTableColumnCtx
{
    public readonly TempTableData Table;
    public readonly ExprColumnName ColumnName;
    public readonly bool PrimaryKey;

    public ExprTypeToTableColumnCtx(TempTableData table, ExprColumnName columnName, bool primaryKey)
    {
        this.Table = table;
        this.ColumnName = columnName;
        this.PrimaryKey = primaryKey;
    }

    public ColumnMeta? ColumnMeta => this.PrimaryKey ? ColumnMeta.PrimaryKey() : null;
}

internal class ExprTypeToTableColumn : IExprTypeVisitor<TableColumn, ExprTypeToTableColumnCtx>
{
    public static readonly ExprTypeToTableColumn Instance = new();

    private ExprTypeToTableColumn()
    {
    }

    public TableColumn VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, ExprTypeToTableColumnCtx arg)
        => new NullableBooleanTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeByte(ExprTypeByte exprTypeByte, ExprTypeToTableColumnCtx arg)
        => new NullableByteTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, ExprTypeToTableColumnCtx arg)
        => new NullableByteArrayTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, exprTypeByte, arg.ColumnMeta);

    public TableColumn VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, ExprTypeToTableColumnCtx arg)
        => new NullableByteArrayTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, exprTypeFixSizeByteArray, arg.ColumnMeta);

    public TableColumn VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, ExprTypeToTableColumnCtx arg)
        => new NullableInt16TableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, ExprTypeToTableColumnCtx arg)
        => new NullableInt32TableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, ExprTypeToTableColumnCtx arg)
        => new NullableInt64TableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, ExprTypeToTableColumnCtx arg)
        => new NullableDecimalTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, exprTypeDecimal.PrecisionScale, arg.ColumnMeta);

    public TableColumn VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, ExprTypeToTableColumnCtx arg)
        => new NullableDoubleTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, ExprTypeToTableColumnCtx arg)
        => new NullableDateTimeTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, exprTypeDateTime.IsDate, arg.ColumnMeta);

    public TableColumn VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, ExprTypeToTableColumnCtx arg)
        => new NullableDateTimeOffsetTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, ExprTypeToTableColumnCtx arg)
        => new NullableGuidTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, arg.ColumnMeta);

    public TableColumn VisitExprTypeString(ExprTypeString exprTypeString, ExprTypeToTableColumnCtx arg)
        => new NullableStringTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, exprTypeString, arg.ColumnMeta);

    public TableColumn VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, ExprTypeToTableColumnCtx arg)
        => new NullableStringTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, exprTypeFixSizeString, arg.ColumnMeta);

    public TableColumn VisitExprTypeXml(ExprTypeXml exprTypeXml, ExprTypeToTableColumnCtx arg)
        => new NullableStringTableColumn(arg.Table.Alias, arg.ColumnName, arg.Table, exprTypeXml, arg.ColumnMeta);
}
