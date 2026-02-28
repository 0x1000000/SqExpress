using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using System.Collections.Generic;
using SqExpress.SqlExport.Internal;

namespace SqExpress.SqlExport
{
    public interface ISqlExporter
    {
        string ToSql(IExpr expr);

        string ToSql(IStatement statement);
    }

    internal interface ISqlExporterInternal : ISqlExporter
    {
        internal string ToSql(IExpr expr, out IReadOnlyList<DbParameterValue>? parameters);
    }
}