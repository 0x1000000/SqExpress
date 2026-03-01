using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.SqlExport;
using SqExpress.Syntax.Names;

namespace SqExpress.IntTest.Scenarios;

public class ScPlayground :  IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var t = new MyTable();

        await t.Script.DropIfExist().Exec(context.Database);
        await t.Script.Create().Exec(context.Database);


        Console.WriteLine(t.Script.Create().ToSql(PgSqlExporter.Default));


        var now = DateTime.Now;
        var nowO = DateTimeOffset.Now;

        await SqQueryBuilder
            .InsertInto(t, t.DTCol, t.DTOffset)
            .Values(now, nowO)
            .DoneWithValues()
            .Exec(context.Database);

        await foreach (var r in SqQueryBuilder.Select(t.DTCol, t.DTOffset).From(t).Query(context.Database))
        {
            Console.WriteLine(t.DTCol.Read(r));
            Console.WriteLine(t.DTOffset.Read(r));
        }
    }

    private class MyTable: TableBase
    {
        public MyTable(Alias alias = default) : base("dbo", "MyTable", alias)
        {
            this.DTCol = this.CreateDateTimeColumn("DTCol");
            this.DTOffset = this.CreateDateTimeOffsetColumn("DTOffset");
        }

        public DateTimeOffsetTableColumn DTOffset { get; }

        public DateTimeTableColumn DTCol { get;  }
    }
}
