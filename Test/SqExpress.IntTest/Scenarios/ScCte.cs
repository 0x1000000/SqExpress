using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScCte : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var targetTable = new TargetTable();

            await context.Database.Statement(targetTable.Script.Create());

            var cte = new RefSimpleRecursiveWithOriginalCte(10);

            await IdentityInsertInto(targetTable, targetTable.Val1, targetTable.Val2)
                .From(Select(cte.OriginalNum, cte.Num).From(cte))
                .Exec(context.Database);

            var result = await Select(targetTable.Val1, targetTable.Val2)
                .From(targetTable)
                .OrderBy(targetTable.Val1, targetTable.Val2)
                .QueryList(context.Database,
                    r => (Val1: targetTable.Val1.Read(r), Val2: targetTable.Val2.Read(r)));

            for (int i = 0; i < 9; i++)
            {
                if (result[i].Val1 != i+1 || result[i].Val2 != (i+1) * 10)
                {
                    throw new Exception($"Expected ({i+1},{(i+1) * 10}) but was ({result[i].Val1}, {result[i].Val2})");
                }
            }

            //Update
            var cte100 = new RefSimpleRecursiveWithOriginalCte(100);
            var cteSimple2 = new SimpleRecursiveCte("2");

            await Update(targetTable)
                .Set(targetTable.Val2, cte100.Num)
                .From(targetTable)
                .InnerJoin(cteSimple2, on: cteSimple2.Num == targetTable.Val1)
                .InnerJoin(cte100, on: cte100.OriginalNum == cteSimple2.Num)
                .All()
                .Exec(context.Database);

            result = await Select(targetTable.Val1, targetTable.Val2)
                .From(targetTable)
                .OrderBy(targetTable.Val1, targetTable.Val2)
                .QueryList(context.Database,
                    r => (Val1: targetTable.Val1.Read(r), Val2: targetTable.Val2.Read(r)));

            for (int i = 0; i < 9; i++)
            {
                if (result[i].Val1 != i + 1 || result[i].Val2 != (i + 1) * 100)
                {
                    throw new Exception(
                        $"Expected ({i + 1},{(i + 1) * 100}) but was ({result[i].Val1}, {result[i].Val2})");
                }
            }

            //Delete

            var subQuery = TableAlias();
            await Delete(targetTable)
                .From(targetTable)
                .InnerJoin(cteSimple2, on: cteSimple2.Num == targetTable.Val1)
                .InnerJoin(
                    (Select(cte100.Num).From(cte100).Where(cte100.Num < 50)).As(subQuery)
                    , on: cte100.Num.WithSource(subQuery) == cteSimple2.Num)
                .All()
                .Exec(context.Database);

            result = await Select(targetTable.Val1, targetTable.Val2)
                .From(targetTable)
                .OrderBy(targetTable.Val1, targetTable.Val2)
                .QueryList(context.Database,
                    r => (Val1: targetTable.Val1.Read(r), Val2: targetTable.Val2.Read(r)));

            for (int i = 0; i < 5; i++)
            {
                if (result[i].Val1 != i + 1 || result[i].Val2 != (i + 1) * 100)
                {
                    throw new Exception(
                        $"Expected ({i + 1},{(i + 1) * 100}) but was ({result[i].Val1}, {result[i].Val2})");
                }
            }

            if (context.Dialect != SqlDialect.MySql)
            {
                //Delete All Output
                result = await Delete(targetTable)
                    .From(targetTable)
                    .InnerJoin(cteSimple2, on: cteSimple2.Num == targetTable.Val1)
                    .All()
                    .Output(targetTable.Val1, targetTable.Val2)
                    .QueryList(context.Database,
                        r => (Val1: targetTable.Val1.Read(r), Val2: targetTable.Val2.Read(r)));

                for (int i = 0; i < 5; i++)
                {
                    if (result[i].Val1 != i + 1 || result[i].Val2 != (i + 1) * 100)
                    {
                        throw new Exception(
                            $"Expected ({i + 1},{(i + 1) * 100}) but was ({result[i].Val1}, {result[i].Val2})");
                    }
                }
            }

            await context.Database.Statement(targetTable.Script.Drop());
        }

        class TargetTable : TempTableBase
        {
            public TargetTable(Alias alias = default) : base("TargetTable", alias)
            {
                this.Val1 = this.CreateInt32Column(nameof(Val1), ColumnMeta.PrimaryKey().Identity());
                this.Val2 = this.CreateInt32Column(nameof(Val2));
            }

            public Int32TableColumn Val1 { get;}

            public Int32TableColumn Val2 { get;}
        }

        class SimpleRecursiveCte : CteBase
        {
            private readonly string _nameSuffix;

            public SimpleRecursiveCte(string nameSuffix = "", Alias alias = default) : base(nameof(SimpleRecursiveCte) + nameSuffix, alias)
            {
                this._nameSuffix = nameSuffix;
                this.Num = this.CreateInt32Column("Num");
            }

            public Int32CustomColumn Num { get; }

            public override IExprSubQuery CreateQuery()
            {
                var next = new SimpleRecursiveCte(this._nameSuffix);

                return Select(Literal(1).As(Num))
                    .UnionAll(
                        Select(next.Num + 1)
                            .From(next)
                            .Where(next.Num < 10))
                    .Done();
            }
        }

        class RefSimpleRecursiveWithOriginalCte : CteBase
        {
            private readonly int _mul;

            public RefSimpleRecursiveWithOriginalCte(int mul, Alias alias = default) : base(nameof(RefSimpleRecursiveWithOriginalCte), alias)
            {
                this._mul = mul;
                this.Num = this.CreateInt32Column("Num");
                this.OriginalNum = this.CreateInt32Column("OriginalNum");
            }

            public Int32CustomColumn Num { get; }

            public Int32CustomColumn OriginalNum { get; }

            public override IExprSubQuery CreateQuery()
            {
                var simpleRecursiveCte = new SimpleRecursiveCte();

                return Select(
                        simpleRecursiveCte.Num.As(this.OriginalNum),
                        (simpleRecursiveCte.Num * this._mul).As(Num))
                    .From(simpleRecursiveCte)
                    .Where(simpleRecursiveCte.Num < 10)
                    .Done();
            }
        }


    }
}