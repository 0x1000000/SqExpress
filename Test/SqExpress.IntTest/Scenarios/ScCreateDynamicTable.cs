using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DbMetadata;
using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Scenarios;

public class ScCreateDynamicTable : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var tbl = SqTable.Create(
            "dbo",
            "DynamicTable",
            b => b
                .AppendInt32Column("Id", ColumnMeta.PrimaryKey().Identity())
                .AppendStringColumn("Value", 255, true)
                .AppendBooleanColumn("IsActive", ColumnMeta.DefaultValue(true)),
            i => i
                .AppendIndex(i.Asc("Id"), i.Desc("Value"))
                .AppendIndex(i.Asc("Value"))
        );

        await tbl.Script.DropIfExist().Exec(context.Database);
        await tbl.Script.Create().Exec(context.Database);

        var tables = await context.Database.GetTables();

        var expectedTbl = tables.FirstOrDefault(t => t.FullName.LowerInvariantTableName == "dynamictable");

        try
        {
            if (ReferenceEquals(expectedTbl, null))
            {
                throw new Exception("Could not find table");
            }

            var diff = tbl.CompareWith(expectedTbl);
            if (diff != null)
            {
                throw new Exception("Tables are different");
            }
        }
        finally
        {
            await tbl.Script.Drop().Exec(context.Database);
        }
    }
}
