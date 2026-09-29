using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScCascadeDelete : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var parent = new TableCascadeParent();
        var child = new TableCascadeChild();

        await child.Script.DropIfExist().Exec(context.Database);
        await parent.Script.DropIfExist().Exec(context.Database);
        try
        {
            await parent.Script.Create().Exec(context.Database);
            await child.Script.Create().Exec(context.Database);

            var discoveredChild = (await context.Database.GetTables())
                .Single(t => string.Equals(t.FullName.TableName, "CascadeChild", StringComparison.OrdinalIgnoreCase));
            var discoveredAction = discoveredChild.Columns.Single(c => string.Equals(c.ColumnName.Name, "ParentId", StringComparison.OrdinalIgnoreCase))
                .ColumnMeta?.ForeignKeys?[0].OnDelete;
            if (discoveredAction != ForeignKeyDeleteAction.Cascade)
            {
                throw new Exception("Schema discovery did not retain ON DELETE CASCADE.");
            }

            await InsertInto(parent, parent.Id).Values(1).Values(2).DoneWithValues().Exec(context.Database);
            await InsertInto(child, child.Id, child.ParentId)
                .Values(10, 1).Values(11, 1).Values(12, 2).DoneWithValues().Exec(context.Database);

            await Delete(parent).Where(parent.Id == 1).Exec(context.Database);

            var remaining = Convert.ToInt32(await Select(Count(1)).From(child).QueryScalar(context.Database));
            var survivor = await Select(Count(1)).From(child)
                .Where(child.Id == 12 & child.ParentId == 2).QueryScalar(context.Database);
            if (remaining != 1 || Convert.ToInt32(survivor) != 1)
            {
                throw new Exception("ON DELETE CASCADE did not delete only the matching child rows.");
            }
        }
        finally
        {
            await child.Script.DropIfExist().Exec(context.Database);
            await parent.Script.DropIfExist().Exec(context.Database);
        }
    }
}
