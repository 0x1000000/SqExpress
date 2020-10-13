using SqExpress.SqlExport.Statement;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.SqlExport
{
    public class PgSqlExporter : ISqlExporter
    {
        public static readonly PgSqlExporter Default = new PgSqlExporter(SqlBuilderOptions.Default);

        private readonly SqlBuilderOptions _builderOptions;

        public PgSqlExporter(SqlBuilderOptions builderOptions)
        {
            this._builderOptions = builderOptions;
        }

        public string ToSql(IExpr expr)
        {
            var sqlExporter = new PgSqlBuilder(this._builderOptions);
            if (expr.Accept(sqlExporter))
            {
                return sqlExporter.ToString();
            }

            throw new SqExpressException("Could not build Sql");
        }

        public string ToSql(IStatement statement)
        {
            var builder = new PgSqlStatementBuilder(this._builderOptions);
            statement.Accept(builder);
            return builder.Build();
        }
    }
}