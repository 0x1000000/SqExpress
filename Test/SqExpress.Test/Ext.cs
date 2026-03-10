using SqExpress.SqlExport;
using SqExpress.Syntax;

namespace SqExpress.Test
{
    public static class Ext
    {
        public static string ToSql(this IExpr expr) => expr.ToSql(TSqlExporter.Default);

        public static string ToPgSql(this IExpr expr)
        {
            var pgSqlExporter = new PgSqlExporter(SqlBuilderOptions.Default.WithSchemaMap(new []{new SchemaMap("dbo", "public")}));
            return expr.ToSql(pgSqlExporter);
        }

        public static string ToMySql(this IExpr expr) => expr.ToSql(MySqlExporter.MariaDbDefault);

        public static string ToMariaDb(this IExpr expr) => expr.ToSql(MySqlExporter.MariaDbDefault);

        public static string ToOracleSql(this IExpr expr) => expr.ToSql(MySqlExporter.OracleDefault);
    }
}
