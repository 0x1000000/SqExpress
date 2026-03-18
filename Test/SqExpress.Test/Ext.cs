using System;
using System.Collections.Generic;
using SqExpress.DbMetadata;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;

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

        public static IExpr RebindParsedTables(this IExpr expr, IReadOnlyList<SqTable> tables)
        {
            if (tables.Count < 1)
            {
                return expr;
            }

            var tablesByKey = new Dictionary<string, SqTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables)
            {
                tablesByKey[BuildTableKey(table.FullName)] = table;
            }

            return expr.SyntaxTree().Modify<ExprTable>(tableExpr =>
            {
                if (tableExpr is TableBase)
                {
                    return tableExpr;
                }

                return tablesByKey.TryGetValue(BuildTableKey(tableExpr.FullName), out var sqTable)
                    ? sqTable.With(tableExpr.Alias, tableExpr.FullName)
                    : tableExpr;
            }) as IExpr ?? expr;
        }

        private static string BuildTableKey(IExprTableFullName fullName)
        {
            var schema = fullName.SchemaName ?? string.Empty;
            return schema + "|" + fullName.TableName;
        }
    }
}
