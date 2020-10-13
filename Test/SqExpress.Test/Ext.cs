using SqExpress.SqlExport;
using SqExpress.Syntax;

namespace SqExpress.Test
{
    public static class Ext
    {
        public static string ToSql(this IExpr expr)
        {
            var exporter = new TSqlBuilder();
            expr.Accept(exporter);

            return exporter.ToString();
        }

        public static string ToPgSql(this IExpr expr)
        {
            var exporter = new PgSqlBuilder(SqlBuilderOptions.Default.WithSchemaMap(new []{new SchemaMap("dbo", "public") }) );
            expr.Accept(exporter);

            return exporter.ToString();
        }
    }
}