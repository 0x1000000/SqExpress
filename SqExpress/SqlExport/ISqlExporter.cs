using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.SqlExport
{
    public interface ISqlExporter
    {
        string ToSql(IExpr expr);

        string ToSql(IStatement statement);
    }
}