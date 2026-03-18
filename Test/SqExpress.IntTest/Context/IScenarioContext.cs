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
        MariaDb,
        OracleMySql
    }

    [VisitorToMethod("GetExporter")]
    public readonly struct ExporterSwitcher: ISqlDialectVisitor<ISqlExporter>
    {
        public ISqlExporter CaseTSql() => TSqlExporter.Default;

        public ISqlExporter CasePgSql() => PgSqlExporter.Default;

        public ISqlExporter CaseMariaDb() => MySqlExporter.MariaDbDefault;

        public ISqlExporter CaseOracleMySql() => MySqlExporter.OracleDefault;
    }
}
