using System.Data;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlExport.Internal;

internal class MySqlDbParameterValueVisitorExtractor : DbParameterValueVisitorExtractor
{
    public new static readonly MySqlDbParameterValueVisitorExtractor Instance = new MySqlDbParameterValueVisitorExtractor();

    public override DbParameterValue? VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, string? name)
    {
        var guidBytes = exprGuidLiteral.Value?.ToByteArray();
        return new DbParameterValue(ToDbValue(guidBytes), DbType.Binary, name);
    }
}
