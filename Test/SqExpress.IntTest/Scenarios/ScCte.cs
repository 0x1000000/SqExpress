using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScTreeClosure : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var targetTable = new TreeData();

            await context.Database.Statement(targetTable.Script.Create());

            var nodes = NodeModel.Root(r =>
                {
                    r.AddChild(n =>
                    {
                        n.AddChild(n2 =>
                        {
                            n2.AddChild();
                            n2.AddChild();
                        });

                        n.AddChild(n2 =>
                        {
                            n2.AddChild();
                            n2.AddChild();
                        });
                    });
                    r.AddChild(n =>
                    {
                        n.AddChild(n2 =>
                        {
                            n2.AddChild();
                        });

                        n.AddChild(n2 =>
                        {
                            n2.AddChild();
                        });

                        n.AddChild(n2 =>
                        {
                            n2.AddChild();
                        });
                    });
                })
                .Trace;

            await InsertDataInto(targetTable, nodes)
                .MapData(m => m.Set(m.Target.Id, m.Source.Id).Set(m.Target.ParentId, m.Source.ParentId))
                .Exec(context.Database);

            var treeClosure = new CteTreeClosure();

            var result = await Select(treeClosure.Id, treeClosure.ParentId, treeClosure.Depth)
                .From(treeClosure)
                .QueryList(context.Database,
                    r => (
                        Id: treeClosure.Id.Read(r),
                        ParentId: treeClosure.ParentId.Read(r),
                        Depth: treeClosure.Depth.Read(r)));

            var expectedTreeClosure = BuildTreeClosure(nodes);

            if (expectedTreeClosure.Count != result.Count)
            {
                throw new Exception("Incorrect result length");
            }

            foreach (var r in result)
            {
                if (!expectedTreeClosure.Remove(r))
                {
                    throw new Exception($"Result {r} was not expected");
                }
            }

            if (expectedTreeClosure.Count > 0)
            {
                throw new Exception($"Some results are missed");
            }

            await context.Database.Statement(targetTable.Script.Drop());
        }

        private static HashSet<(int Id, int? ParentId, int Depth)> BuildTreeClosure(IReadOnlyList<NodeModel> nodes)
        {
            var result = new HashSet<(int Id, int? ParentId, int Depth)>();

            foreach (var node in nodes)
            {
                result.Add((node.Id, node.ParentId, 1));
            }

            var initial = result.ToList();

            var previousChunk = result.ToList();

            while (previousChunk.Count > 0)
            {
                var newChunk = new List<(int Id, int? ParentId, int Depth)>();

                foreach (var previous in previousChunk)
                {
                    foreach (var current in initial.Where(i => i.Id == previous.ParentId))
                    {
                        newChunk.Add((previous.Id, current.ParentId, previous.Depth + 1));
                    }
                }

                foreach (var i in newChunk)
                {
                    result.Add(i);
                }

                previousChunk = newChunk;
            }

            return result;
        }

        class CteTreeClosure : CteBase
        {
            public CteTreeClosure(Alias alias = default) : base(nameof(CteTreeClosure), alias)
            {
                this.Id = this.CreateInt32Column(nameof(Id));
                this.ParentId = this.CreateNullableInt32Column(nameof(ParentId));
                this.Depth = this.CreateInt32Column(nameof(Depth));
            }

            public Int32CustomColumn Id { get; }

            public NullableInt32CustomColumn ParentId { get; }

            public Int32CustomColumn Depth { get; }

            public override IExprSubQuery CreateQuery()
            {
                var initial = new TreeData();
                var current = new TreeData();

                var previous = new CteTreeClosure();

                return Select(initial.Id, initial.ParentId, Literal(1).As(this.Depth))
                    .From(initial)
                    .UnionAll(Select(
                            previous.Id,
                            current.ParentId,
                            (previous.Depth + 1).As(this.Depth))
                        .From(current)
                        .InnerJoin(previous, on: previous.ParentId == current.Id))
                    .Done();
            }
        }

        class NodeModel
        {
            private NodeModel(int id, int? parentId, List<NodeModel> trace)
            {
                this.Id = id;
                this.ParentId = parentId;
                this.Trace = trace;
            }

            public int Id { get; }

            public int? ParentId { get; }

            public List<NodeModel> Trace { get; }

            public static NodeModel Root(Action<NodeModel> next)
            {
                var result = new NodeModel(1, null , new List<NodeModel>());
                result.Trace.Add(result);
                next.Invoke(result);
                return result;
            }

            public NodeModel AddChild(Action<NodeModel>? next = null)
            {
                var result = new NodeModel(this.Trace.Count + 1, this.Id, this.Trace);
                this.Trace.Add(result);
                next?.Invoke(result);
                return result;
            }

        }

        class TreeData : TempTableBase
        {
            public TreeData(Alias alias = default) : base(nameof(TreeData), alias)
            {
                this.Id = this.CreateInt32Column(nameof(this.Id), ColumnMeta.PrimaryKey());
                this.ParentId = this.CreateNullableInt32Column(nameof(this.ParentId));
            }

            public Int32TableColumn Id { get; }

            public NullableInt32TableColumn ParentId { get; }
        }
    }

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