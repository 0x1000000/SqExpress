using System;
using SqExpress.DataAccess;

namespace SqExpress.IntTest.Context
{
    public class ScenarioContext : IScenarioContext
    {
        public ScenarioContext(ISqDatabase database, SqlDialect dialect)
        {
            this.Database = database;
            this.Dialect = dialect;
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
    }
}