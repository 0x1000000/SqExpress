using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;

namespace SqExpress.IntTest.Scenarios;

public class ScCreateTables : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        IReadOnlyList<TableBase> createList = AllTables.BuildAllTableList(context.Dialect);
        var regularTables = createList.Where(i => !ForeignKeyTables.IsCycleTable(i)).ToArray();

        await ForeignKeyTables.DropCycleTables(context);

        var dropping = regularTables.Reverse().Select(i => i.Script.DropIfExist()).Combine();
        await context.Database.Statement(dropping);

        var creating = regularTables.Select(i => i.Script.Create()).Combine();
        await context.Database.Statement(creating);

        await ForeignKeyTables.CreateCycleTables(context);
    }
}
