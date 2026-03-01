using System;
using System.Data;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlExport.Internal;

internal class PgDbParameterValueVisitorExtractor : DbParameterValueVisitorExtractor
{
    public new static readonly PgDbParameterValueVisitorExtractor Instance = new PgDbParameterValueVisitorExtractor();

    public override DbParameterValue? VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, string? name)
    {
        var dateTime = dateTimeLiteral.Value;
        if (dateTime.HasValue && dateTime.Value.Kind != DateTimeKind.Unspecified)
        {
            dateTime = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Unspecified);
        }

        return new DbParameterValue(ToDbValue(dateTime), DbType.DateTime2, name);
    }

    public override DbParameterValue? VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, string? name)
    {
        var dateTimeOffset = dateTimeLiteral.Value;
        if (dateTimeOffset.HasValue)
        {
            dateTimeOffset = dateTimeOffset.Value.ToUniversalTime();
        }
        return new DbParameterValue(ToDbValue(dateTimeOffset), DbType.DateTimeOffset, name);
    }
}
