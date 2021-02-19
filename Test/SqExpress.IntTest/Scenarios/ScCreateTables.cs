using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Scenarios
{
    public class ScCreateTables : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            IReadOnlyList<TableBase> createList = new List<TableBase>
            {
                Tables.TableList.User(),
                Tables.TableList.Company(),
                Tables.TableList.Customer(),
                Tables.TableList.Order()
            };

            var dropping = createList.Reverse().Select(i => i.Script.DropIfExist()).Combine();
            await context.Database.Statement(dropping);

            var creating = createList.Select(i => i.Script.Create()).Combine();
            await context.Database.Statement(creating);
        }
    }
}