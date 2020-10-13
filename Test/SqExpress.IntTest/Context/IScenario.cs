using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqExpress.IntTest.Context
{
    public interface IScenario
    {
        Task Exec(IScenarioContext context);
    }

    public static class ScenarioExt
    {
        public static IScenario Then(this IScenario scenario1, IScenario scenario2)
        {
            var result = scenario1 is ScenarioList list ? list : new ScenarioList(scenario1);

            result.Add(scenario2);

            return result;
        }

        private class ScenarioList : IScenario
        {
            private readonly List<IScenario> _scenarios = new List<IScenario>();

            public ScenarioList(IScenario first)
            {
                this._scenarios.Add(first);
            }

            public async Task Exec(IScenarioContext context)
            {
                foreach (var scenario in this._scenarios)
                {
                    context.WriteLine($"--{scenario.GetType().Name}--");
                    context.WriteLine(null);

                    await scenario.Exec(context);
                    context.WriteLine(null);
                }
            }

            public void Add(IScenario scenario)
            {
                if (scenario is ScenarioList sl)
                {
                    this._scenarios.AddRange(sl._scenarios);
                }
                else
                {
                    this._scenarios.Add(scenario);
                }
            }
        }
    }
}