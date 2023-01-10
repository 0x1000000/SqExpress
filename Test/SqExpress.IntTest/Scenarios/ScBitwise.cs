using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Scenarios
{
    public class ScBitwise : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var rawValue = await SqQueryBuilder.Select(SqQueryBuilder.Literal(3) | SqQueryBuilder.Literal(5) & SqQueryBuilder.Literal(2))
                .QueryScalar(context.Database);

            var value = rawValue switch
            {
                int i => i,
                ulong l => (int)l,
                _ => throw new ArgumentOutOfRangeException(rawValue?.GetType().Name ?? "null")
            };


            if (value != 3)
            {
                throw new Exception("Unexpected bitwise operator behaviour");
            }
        }
    }
}