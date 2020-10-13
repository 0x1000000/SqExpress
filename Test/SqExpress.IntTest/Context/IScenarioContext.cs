using SqExpress.DataAccess;

namespace SqExpress.IntTest.Context
{
    public interface IScenarioContext
    {
        ISqDatabase Database { get; }

        public bool IsPostgresSql { get; }

        void WriteLine(string? line);
    }
}