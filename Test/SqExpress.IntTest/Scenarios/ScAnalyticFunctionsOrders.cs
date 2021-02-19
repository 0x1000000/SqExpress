using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Value;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScAnalyticFunctionsOrders : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tTestData = new TestData();

            await context.Database.Statement(tTestData.Script.Create());

            var data = new (int Index, int Group)[]
            {
                (1,1),(2,1),(3,1),(4,1),(5,1),(6,2),(7,2),(8,2),(9,2),(10,3),(11,3),(12,3),(13,3),(14,4)
            };

            await InsertDataInto(tTestData, data)
                .MapData(s => s.Set(s.Target.Index, s.Source.Index).Set(s.Target.Group, s.Source.Group))
                .Exec(context.Database);

            await RowNumberTest(context: context, tTestData: tTestData, data: data);
            await RowNumberPartTest(context: context, tTestData: tTestData, data: data);
            await RankTest(context: context, tTestData: tTestData, data: data);
            await FirstLastPartTest(context: context, tTestData: tTestData, data: data);
            await LagLeadPartTest(context: context, tTestData: tTestData, data: data);
            await OtherTests(context: context, tTestData: tTestData, data: data);
            await context.Database.Statement(tTestData.Script.Drop());
        }

        private static async Task RowNumberTest(IScenarioContext context, TestData tTestData, (int Index, int Group)[] data)
        {
            var result = await Select(tTestData.Index,
                    tTestData.Group,
                    Cast(RowNumber().OverOrderBy(Desc(tTestData.Index)), SqlType.Int64).As("Num"))
                .From(tTestData)
                .QueryList(context.Database,
                    r => (Index: tTestData.Index.Read(r), Group: tTestData.Group.Read(r), RowNo: r.GetInt64("Num")));

            if (result.Count != data.Length)
            {
                throw new Exception("Something went wrong");
            }

            foreach (var tuple in result)
            {
                if (tuple.Index != 15 - tuple.RowNo)
                {
                    throw new Exception("Something went wrong");
                }
            }
        }

        private static async Task RowNumberPartTest(IScenarioContext context, TestData tTestData, (int Index, int Group)[] data)
        {
            var result = await Select(tTestData.Index,
                    tTestData.Group,
                    Cast(RowNumber().OverPartitionBy(tTestData.Group).OverOrderBy(tTestData.Index), SqlType.Int64).As("Num"))
                .From(tTestData)
                .QueryList(context.Database,
                    r => (Index: tTestData.Index.Read(r), Group: tTestData.Group.Read(r), RowNo: r.GetInt64("Num")));

            if (result.Count != data.Length)
            {
                throw new Exception("Something went wrong");
            }

            int[] expected = {1, 2, 3, 4, 5, 1, 2, 3, 4, 1, 2, 3, 4, 1};

            for (var index = 0; index < result.Count; index++)
            {
                if (result[index].RowNo != expected[index])
                {
                    throw new Exception("Something went wrong");
                }
            }
        }         
        
        private static async Task FirstLastPartTest(IScenarioContext context, TestData tTestData, (int Index, int Group)[] data)
        {
            var result = await Select(tTestData.Index,
                    tTestData.Group,
                    FirstValue(tTestData.Index)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .FrameClause(FrameBorder.UnboundedPreceding, null)
                        .As("First"),
                    LastValue(tTestData.Index)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .FrameClause(FrameBorder.UnboundedPreceding, FrameBorder.UnboundedFollowing)
                        .As("Last"))
                .From(tTestData)
                .QueryList(context.Database,
                    r => (Index: tTestData.Index.Read(r), Group: tTestData.Group.Read(r), First: r.GetInt32("First"), Last: r.GetInt32("Last")));

            if (result.Count != data.Length)
            {
                throw new Exception("Something went wrong");
            }

            int[] expectedFirst = {1, 1, 1, 1, 1, 6, 6, 6, 6, 10, 10, 10, 10, 14};
            int[] expectedLast =  {5, 5, 5, 5, 5, 9, 9, 9, 9, 13, 13, 13, 13, 14};

            for (var index = 0; index < result.Count; index++)
            {
                if (result[index].First != expectedFirst[index])
                {
                    throw new Exception("Something went wrong");
                }
                if (result[index].Last != expectedLast[index])
                {
                    throw new Exception("Something went wrong");
                }
            }
        }
        
        private static async Task LagLeadPartTest(IScenarioContext context, TestData tTestData, (int Index, int Group)[] data)
        {
            var clLag = CustomColumnFactory.NullableInt32("Lag");
            var clLag2 = CustomColumnFactory.NullableInt32("Lag2");
            var clLagDef = CustomColumnFactory.NullableInt32("LagDef");
            var clLead = CustomColumnFactory.NullableInt32("Lead");
            var clLead2 = CustomColumnFactory.NullableInt32("Lead2");
            var clLeadDef = CustomColumnFactory.NullableInt32("LeadDef");

            var result = await Select(tTestData.Index,
                    tTestData.Group,
                    Lag(tTestData.Index)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .As(clLag),
                    Lag(tTestData.Index, 2)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .As(clLag2),
                    Lag(tTestData.Index, 1, context.Dialect == SqlDialect.MySql ? (ExprValue?)null : 100)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .As(clLagDef),
                    Lead(tTestData.Index)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .As(clLead),
                    Lead(tTestData.Index, 2)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .As(clLead2),
                    Lead(tTestData.Index, 1, context.Dialect == SqlDialect.MySql ? (ExprValue?)null : 100)
                        .OverPartitionBy(tTestData.Group)
                        .OverOrderBy(tTestData.Index)
                        .As(clLeadDef)
                    )
                .From(tTestData)
                .QueryList(context.Database,
                    r => new {
                        Index = tTestData.Index.Read(r),
                        Group = tTestData.Group.Read(r),
                        Lag = clLag.Read(r),
                        Lag2 = clLag2.Read(r),
                        LagDef = clLagDef.Read(r),
                        Lead = clLead.Read(r),
                        Lead2 = clLead2.Read(r),
                        LeadDef = clLeadDef.Read(r)
                        });

            if (result.Count != data.Length)
            {
                throw new Exception("Something went wrong");
            }
        }        
        
        private static async Task RankTest(IScenarioContext context, TestData tTestData, (int Index, int Group)[] data)
        {
            var subQueryAlias = TableAlias();

            var result = await 
                Select(
                    subQueryAlias.Column(tTestData.Index),
                    subQueryAlias.Column(tTestData.Group),
                    Cast(Rank().OverPartitionBy(subQueryAlias.Column(tTestData.Group)).OverOrderBy(subQueryAlias.Column(tTestData.Index)), SqlType.Int64)
                        .As("Num"))
                .From(
                    Select(
                            (tTestData.Index*10).As(tTestData.Index),
                            tTestData.Group)
                        .From(tTestData)
                        .As(subQueryAlias)
                )
                .QueryList(context.Database,
                    r => (Index: tTestData.Index.Read(r), Group: tTestData.Group.Read(r), RowNo: r.GetInt64("Num")));

            if (result.Count != data.Length)
            {
                throw new Exception("Something went wrong");
            }

            int[] expected = {1, 2, 3, 4, 5, 1, 2, 3, 4, 1, 2, 3, 4, 1};

            for (var index = 0; index < result.Count; index++)
            {
                if (result[index].RowNo != expected[index])
                {
                    throw new Exception("Something went wrong");
                }
            }
        }

        private static async Task OtherTests(IScenarioContext context, TestData tTestData, (int Index, int Group)[] data)
        {
            var result = await Select(
                    tTestData.Index,
                    tTestData.Group,
                    DenseRank().OverPartitionBy(tTestData.Group).OverOrderBy(tTestData.Index),
                    Ntile(2).OverPartitionBy(tTestData.Group).OverOrderBy(tTestData.Index),
                    CumeDist().OverPartitionBy(tTestData.Group).OverOrderBy(tTestData.Index),
                    PercentRank().OverPartitionBy(tTestData.Group).OverOrderBy(tTestData.Index))
                .From(tTestData)
                .QueryList(context.Database,
                    r => (Index: tTestData.Index.Read(r), Group: tTestData.Group.Read(r)));

            if (result.Count != data.Length)
            {
                throw new Exception("Something went wrong");
            }
        }

        class TestData : TempTableBase
        {
            public readonly Int32TableColumn Index;

            public readonly Int32TableColumn Group;

            public TestData(Alias alias = default) : base(nameof(TestData),alias)
            {
                this.Index = this.CreateInt32Column(nameof(this.Index));
                this.Group = this.CreateInt32Column(nameof(this.Group));
            }
        }
    }
}