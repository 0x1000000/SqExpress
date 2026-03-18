using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScMergeExprEdgeCases : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var tt = new TestMergeTmpTable();
        var tUser = AllTables.GetItUser(context.Dialect);

        await context.Database.Statement(tt.Script.Create());

        try
        {
            await MergeDataInto(tt, new[] { new TestMergeData(1, -1) })
                .MapDataKeys(TestMergeData.GetUpdateKeyMapping)
                .MapData(TestMergeData.GetUpdateMapping)
                .WhenNotMatchedByTargetThenInsert()
                .Done()
                .Exec(context.Database);

            var source = Select(
                    Literal(1),
                    Literal("AA").As("BB"),
                    tUser.UserId,
                    GetUtcDate())
                .From(tUser)
                .Where((Literal(1) == Literal(1)) & tUser.UserId.In(1, 2))
                .Done()
                .As(
                    TableAlias("S"),
                    "Expr1",
                    "BB",
                    tUser.UserId.ColumnName,
                    "Expr4");

            await MergeInto(tt, source)
                .On(tt.Id == source.Column(tUser.UserId.ColumnName))
                .WhenMatched()
                    .ThenUpdate()
                    .Set(tt.Value, source.Column(tUser.UserId.ColumnName))
                    .Set(tt.Extra, source.Column("BB"))
                    .Set(tt.Version, tt.Version + 1)
                .WhenNotMatchedByTarget()
                    .ThenInsert()
                    .Set(tt.Id, source.Column(tUser.UserId.ColumnName))
                    .Set(tt.Value, source.Column(tUser.UserId.ColumnName))
                    .Set(tt.Extra, source.Column("BB"))
                .Exec(context.Database);

            var rows = await Select(tt.Id, tt.Value, tt.Version, tt.Extra)
                .From(tt)
                .OrderBy(tt.Id)
                .QueryList(context.Database, r => $"{tt.Id.Read(r)},{tt.Value.Read(r)},{tt.Version.Read(r)},{tt.Extra.Read(r) ?? "NULL"}");

            var actual = string.Join(';', rows);
            const string expected = "1,1,1,AA;2,2,0,AA";
            if (actual != expected)
            {
                throw new Exception($"Incorrect merge result: {actual}");
            }
        }
        finally
        {
            await context.Database.Statement(tt.Script.Drop());
        }
    }
}
