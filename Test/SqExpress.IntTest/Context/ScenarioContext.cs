using System;
using SqExpress.DataAccess;
using SqExpress.SqlExport;

namespace SqExpress.IntTest.Context
{
    public class ScenarioContext : IScenarioContext
    {
        private readonly Func<ISqDatabase> _dbFactory;

        public ScenarioContext(ISqDatabase database, SqlDialect dialect, Func<ISqDatabase> dbFactory, ISqlExporter sqlExporter)
        {
            this.Database = database;
            this.Dialect = dialect;
            this._dbFactory = dbFactory;
            this.SqlExporter = sqlExporter;
        }

        public ISqDatabase Database { get; }

        public SqlDialect Dialect { get; }

        public void Write(string? line)
        {
            Console.Write(line);
        }

        public void WriteLine(string? line)
        {
            Console.WriteLine(line);
        }

        public ISqlExporter SqlExporter { get; private set;  }

        public ISqDatabase CreteConnection()
        {
            return this._dbFactory();
        }
    }
}