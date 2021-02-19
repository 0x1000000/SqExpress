using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScLike : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            string[] values =
            {
                "Some simple text",
                "a%b%c",
                "aabcc"
            };

            var table = new TmpStr();
            await context.Database.Statement(table.Script.Create());
            await InsertDataInto(table, values).MapData(s => s.Set(s.Target.Index, s.Index).Set(s.Target.Text, s.Source)).Exec(context.Database);

            var res = await Find("%simple%");
            AssertResult(res, 0);

            res = await Find(context.Dialect == SqlDialect.TSql ? "a[%]b[%]c" : "a\\%b\\%c");
            AssertResult(res, 1);

            res = await Find("a%b%c");
            AssertResult(res, 1, 2);

            await context.Database.Statement(table.Script.Drop());

            Task<List<int>> Find(string pattern)
            {
                return Select(table.Index)
                    .From(table)
                    .Where(Like(table.Text, pattern))
                    .OrderBy(table.Index)
                    .QueryList(context.Database, r => table.Index.Read(r));
            }

            void AssertResult(IReadOnlyList<int> result, params int[] expected)
            {
                if (result.Count != expected.Length)
                {
                    throw new Exception($"{context.Dialect}: search is incorrect: expected length: {expected.Length} but was {result.Count}");
                }

                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] != expected[i])
                    {
                        throw new Exception($"{context.Dialect}: Search is incorrect: expected value: {expected[i]} but was {result[i]}");
                    }
                }
            }
        }

        private class TmpStr : TempTableBase
        {
            public readonly Int32TableColumn Index;
            public readonly StringTableColumn Text;

            public TmpStr(Alias alias = default) : base("TmpStr", alias)
            {
                this.Index = this.CreateInt32Column("Index", ColumnMeta.PrimaryKey());
                this.Text = this.CreateStringColumn("Text", 255, true);
            }
        }
    }
}