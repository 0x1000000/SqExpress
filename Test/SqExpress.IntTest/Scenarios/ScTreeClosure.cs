using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;

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

            await SqQueryBuilder.InsertDataInto(targetTable, nodes)
                .MapData(m => m.Set(m.Target.Id, m.Source.Id).Set(m.Target.ParentId, m.Source.ParentId))
                .Exec(context.Database);

            var treeClosure = new CteTreeClosure();

            var result = await SqQueryBuilder.Select(treeClosure.Id, treeClosure.ParentId, treeClosure.Depth)
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
                this.Id = this.CreateInt32Column(nameof(this.Id));
                this.ParentId = this.CreateNullableInt32Column(nameof(this.ParentId));
                this.Depth = this.CreateInt32Column(nameof(this.Depth));
            }

            public Int32CustomColumn Id { get; }

            public NullableInt32CustomColumn ParentId { get; }

            public Int32CustomColumn Depth { get; }

            public override IExprSubQuery CreateQuery()
            {
                var initial = new TreeData();
                var current = new TreeData();

                var previous = new CteTreeClosure();

                return SqQueryBuilder.Select(initial.Id, initial.ParentId, SqQueryBuilder.Literal(1).As(this.Depth))
                    .From(initial)
                    .UnionAll(SqQueryBuilder.Select(
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
}