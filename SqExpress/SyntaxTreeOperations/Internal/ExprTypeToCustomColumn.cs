using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

namespace SqExpress.SyntaxTreeOperations.Internal;

internal class ExprTypeToCustomColumn : IExprTypeVisitor<ExprColumn, (string ColName, bool? IsNullable)>
{
    public static readonly ExprTypeToCustomColumn Instance = new();

    private ExprTypeToCustomColumn()
    {
    }

    public ExprColumn VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableBoolean(arg.ColName)
            : CustomColumnFactory.Boolean(arg.ColName);
    }

    public ExprColumn VisitExprTypeByte(ExprTypeByte exprTypeByte, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableByte(arg.ColName)
            : CustomColumnFactory.Byte(arg.ColName);
    }

    public ExprColumn VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? new NullableByteArrayCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeByte)
            : new ByteArrayCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeByte);
    }

    public ExprColumn VisitExprTypeFixSizeByteArray(
        ExprTypeFixSizeByteArray exprTypeFixSizeByteArray,
        (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? new NullableByteArrayCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeFixSizeByteArray)
            : new ByteArrayCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeFixSizeByteArray);
    }

    public ExprColumn VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableInt16(arg.ColName)
            : CustomColumnFactory.Int16(arg.ColName);
    }

    public ExprColumn VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableInt32(arg.ColName)
            : CustomColumnFactory.Int32(arg.ColName);
    }

    public ExprColumn VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableInt64(arg.ColName)
            : CustomColumnFactory.Int64(arg.ColName);
    }

    public ExprColumn VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? new NullableDecimalCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeDecimal)
            : new DecimalCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeDecimal);
    }

    public ExprColumn VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableDouble(arg.ColName)
            : CustomColumnFactory.Double(arg.ColName);
    }

    public ExprColumn VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? new NullableDateTimeCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeDateTime)
            : new DateTimeCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeDateTime);
    }

    public ExprColumn VisitExprTypeDateTimeOffset(
        ExprTypeDateTimeOffset exprTypeDateTimeOffset,
        (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableDateTimeOffset(arg.ColName)
            : CustomColumnFactory.DateTimeOffset(arg.ColName);
    }

    public ExprColumn VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? CustomColumnFactory.NullableGuid(arg.ColName)
            : CustomColumnFactory.Guid(arg.ColName);
    }

    public ExprColumn VisitExprTypeString(ExprTypeString exprTypeString, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? new NullableStringCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeString)
            : new StringCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeString);
    }

    public ExprColumn VisitExprTypeFixSizeString(
        ExprTypeFixSizeString exprTypeFixSizeString,
        (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? new NullableStringCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeFixSizeString)
            : new StringCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeFixSizeString);
    }

    public ExprColumn VisitExprTypeXml(ExprTypeXml exprTypeXml, (string ColName, bool? IsNullable) arg)
    {
        return IsNullable(arg)
            ? new NullableStringCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeXml)
            : new StringCustomColumn(new ExprColumnName(arg.ColName), null, exprTypeXml);
    }

    private static bool IsNullable((string ColName, bool? IsNullable) arg)
        => arg.IsNullable != false;
}
