using System;
using SqExpress.DataAccess;
using SqExpress.SqlExport;

namespace SqExpress.IntTest.Context
{
    public interface IScenarioContext
    {
        ISqDatabase Database { get; }

        public SqlDialect Dialect { get; }

        void Write(string? line);

        void WriteLine(string? line);
    }

    public enum SqlDialect
    {
        TSql,
        PgSql,
        MySql
    }

    public static class SqlDialectExtension
    {
        public static ISqlExporter GetExporter(this SqlDialect dialect)
        {
            switch (dialect)
            {
                case SqlDialect.TSql:
                    return TSqlExporter.Default;
                case SqlDialect.PgSql:
                    return PgSqlExporter.Default;
                case SqlDialect.MySql:
                    return MySqlExporter.Default;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dialect), dialect, null);
            }
        }
    }
}