using System;
using SqExpress.DataAccess;

namespace SqExpress.IntTest.Context
{
    public class ScenarioContext : IScenarioContext
    {
        public ScenarioContext(ISqDatabase database, bool isPostgresSql)
        {
            this.Database = database;
            this.IsPostgresSql = isPostgresSql;
        }

        public ISqDatabase Database { get; }

        public bool IsPostgresSql { get; }

        public void WriteLine(string? line)
        {
            Console.WriteLine(line);
        }
    }
}