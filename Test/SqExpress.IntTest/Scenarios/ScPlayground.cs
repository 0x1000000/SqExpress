using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Scenarios;

public class ScPlayground :  IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        await Task.Delay(0);
    }
}
