using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using System.Collections.Generic;

namespace SqExpress.SqlExport
{
    public class MySqlExporter : ISqlExporterInternal
    {
        public static readonly MySqlExporter Default = new MySqlExporter(SqlBuilderOptions.Default);

        private readonly SqlBuilderOptions _builderOptions;

        public MySqlExporter(SqlBuilderOptions builderOptions)
        {
            this._builderOptions = builderOptions;
        }

        public string ToSql(IExpr expr)
        {
            return ((ISqlExporterInternal)this).ToSql(expr, out _);
        }

        public string ToSql(IStatement statement)
        {
            var builder = new MySqlStatementBuilder(this._builderOptions, null);
            statement.Accept(builder);
            return builder.Build();
        }

        string ISqlExporterInternal.ToSql(IExpr expr, out IReadOnlyList<DbParameterValue>? parameters)
        {
            var sqlExporter = new MySqlBuilder(this._builderOptions);
            if (expr.Accept(sqlExporter, null))
            {
                parameters = sqlExporter.ParameterValues;
                return sqlExporter.ToString();
            }

            throw new SqExpressException("Could not build Sql");
        }
    }
}