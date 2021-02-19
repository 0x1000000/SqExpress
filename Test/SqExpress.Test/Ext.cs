using SqExpress.SqlExport;
using SqExpress.Syntax;

namespace SqExpress.Test
{
    public static class Ext
    {
        public static string ToSql(this IExpr expr)
        {
            return TSqlExporter.Default.ToSql(expr);
        }

        public static string ToPgSql(this IExpr expr)
        {
            return new PgSqlExporter(SqlBuilderOptions.Default.WithSchemaMap(new[] {new SchemaMap("dbo", "public")}))
                .ToSql(expr);
        }

        public static string ToMySql(this IExpr expr)
        {
            return new MySqlExporter(SqlBuilderOptions.Default)
                .ToSql(expr);
        }
    }
}