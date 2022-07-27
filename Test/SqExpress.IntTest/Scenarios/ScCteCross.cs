using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Scenarios
{
    public class ScCteCross : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var cte12 = new Cte12();
            var cte34 = new Cte34();


            var result = await SqQueryBuilder.Select(cte12.Val, cte34.Val34)
                .From(cte12)
                .CrossJoin(cte34)
                .OrderBy(cte12.Val, cte34.Val34)
                .QueryList(context.Database, r => (V1: cte12.Val.Read(r), V2: cte34.Val34.Read(r)));

            (int V1, int V2)[] expected = { (1, 3), (1, 4), (2, 3), (2, 4) };

            if (result.Count != 4)
            {
                throw new Exception("Incorrect result length");
            }

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] != expected[i])
                {
                    throw new Exception($"Incorrect value: expected {expected[i]} but was {result[i]}");
                }
            }
        }


        class Cte12 : CteBase
        {
            public Cte12(Alias alias = default) : base(nameof(Cte12), alias)
            {
                this.Val = this.CreateInt32Column(nameof(this.Val));
            }

            public Int32CustomColumn Val { get; set; }

            public override IExprSubQuery CreateQuery()
            {
                return SqQueryBuilder.Select(SqQueryBuilder.Literal(1).As(this.Val)).UnionAll(SqQueryBuilder.Select(SqQueryBuilder.Literal(2).As(this.Val))).Done();
            }
        }

        class Cte34 : CteBase
        {
            public Cte34(Alias alias = default) : base(nameof(Cte34), alias)
            {
                this.Val34 = this.CreateInt32Column(nameof(this.Val34));
            }

            public Int32CustomColumn Val34 { get; set; }

            public override IExprSubQuery CreateQuery()
            {
                return SqQueryBuilder.Select(SqQueryBuilder.Literal(3).As(this.Val34)).UnionAll(SqQueryBuilder.Select(SqQueryBuilder.Literal(4).As(this.Val34))).Done();
            }
        }
    }
}