using EnumVisitorGenerator;
using SqExpress.DataAccess;
using SqExpress.SqlExport;

namespace SqExpress.IntTest.Context
{
    public interface IScenarioContext
    {
        ISqDatabase Database { get; }

        public SqlDialect Dialect { get; }

        public ParametrizationMode ParametrizationMode { get; }

        void Write(string? line);

        void WriteLine(string? line);

        ISqlExporter SqlExporter { get; }

        ISqDatabase CreteConnection();
    }

    [VisitorGenerator]
    public enum SqlDialect
    {
        TSql,
        PgSql,
        MySql
    }

    [VisitorToMethod("GetExporter")]
    public readonly struct ExporterSwitcher: ISqlDialectVisitor<ISqlExporter>
    {
        public ISqlExporter CaseTSql() => TSqlExporter.Default;

        public ISqlExporter CasePgSql() => PgSqlExporter.Default;

        public ISqlExporter CaseMySql() => MySqlExporter.Default;
    }
}
