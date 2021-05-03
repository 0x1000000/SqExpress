using System;
using SqExpress.DataAccess;

namespace SqExpress.IntTest.Context
{
    public class ScenarioContext : IScenarioContext
    {
        private readonly Func<ISqDatabase> _dbFactory;

        public ScenarioContext(ISqDatabase database, SqlDialect dialect, Func<ISqDatabase> dbFactory)
        {
            this.Database = database;
            this.Dialect = dialect;
            this._dbFactory = dbFactory;
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

        public ISqDatabase CreteConnection()
        {
            return this._dbFactory();
        }
    }
}