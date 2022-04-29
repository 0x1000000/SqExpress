using System;
using System.Threading;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;

namespace SqExpress.IntTest.Scenarios
{
    public class ScCancellation : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            if (context.Dialect != SqlDialect.MySql)
            {
                return;
            }

            var source = new CancellationTokenSource(100/*ms*/);

            var tQ = SqQueryBuilder
                .Select(SqQueryBuilder.ScalarFunctionCustom("", "SLEEP", 1000/*s*/))
                .QueryScalar(context.Database, source.Token);

            Exception? cancelException = null;
            try
            {
                await tQ;
            }
            catch (Exception e)
            {
                cancelException = e;
            }

            if (!(cancelException?.InnerException is OperationCanceledException))
            {
                throw new Exception($"{nameof(OperationCanceledException)} was expected");
            }
        }
    }
}