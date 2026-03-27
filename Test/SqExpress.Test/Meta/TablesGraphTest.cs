using System.Linq;
using NUnit.Framework;
using SqExpress.SqlExport;

namespace SqExpress.Test.Meta
{
    [TestFixture]
    public class TablesGraphTest
    {
        [Test]
        public void Create_SimpleChain_TraversesCorrectly()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var grandChild = new GrandChildTable();

            var graph = TablesGraph.Create(new TableBase[] { root, child, grandChild });

            Assert.That(graph.Roots, Is.EqualTo(new TableBase[] { root }));
            Assert.That(graph.GetParent(root), Is.Null);
            Assert.That(graph.GetParent(child), Is.SameAs(root));
            Assert.That(graph.GetParent(grandChild), Is.SameAs(child));
            Assert.That(graph.GetChildren(root), Is.EqualTo(new TableBase[] { child }));
            Assert.That(graph.GetChildren(child), Is.EqualTo(new TableBase[] { grandChild }));
            Assert.That(graph.GetChildren(grandChild), Is.Empty);
            Assert.That(graph.GetAncestors(grandChild).ToArray(), Is.EqualTo(new TableBase[] { child, root }));
            Assert.That(graph.GetDescendants(root).ToArray(), Is.EqualTo(new TableBase[] { child, grandChild }));
        }

        [Test]
        public void Create_BranchingTree_TraversesChildrenInInputOrder()
        {
            var root = new RootTable();
            var childB = new ChildBTable();
            var childA = new ChildTable();

            var graph = TablesGraph.Create(new TableBase[] { root, childB, childA });

            Assert.That(graph.GetChildren(root), Is.EqualTo(new TableBase[] { childB, childA }));
            Assert.That(graph.GetDescendants(root).ToArray(), Is.EqualTo(new TableBase[] { childB, childA }));
        }

        [Test]
        public void GetAncestors_WithSameFullNameDifferentInstance_ResolvesCanonicalTables()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child, grandChild });

            Assert.That(graph.GetAncestors(new GrandChildTable()).ToArray(), Is.EqualTo(new TableBase[] { child, root }));
            Assert.That(graph.GetAncestors(root).ToArray(), Is.Empty);
        }

        [Test]
        public void GetDescendants_WithSameFullNameDifferentInstance_ResolvesCanonicalTables()
        {
            var root = new RootTable();
            var childB = new ChildBTable();
            var childA = new ChildTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create(new TableBase[] { root, childB, childA, grandChild });

            Assert.That(graph.GetDescendants(new RootTable()).ToArray(), Is.EqualTo(new TableBase[] { childB, childA, grandChild }));
            Assert.That(graph.GetDescendants(grandChild).ToArray(), Is.Empty);
        }

        [Test]
        public void FindCommonAncestor_ReturnsNearestCommonAncestor()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var childB = new ChildBTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child, childB, grandChild });

            Assert.That(graph.FindCommonAncestor(grandChild, child), Is.SameAs(child));
            Assert.That(graph.FindCommonAncestor(grandChild, childB), Is.SameAs(root));
            Assert.That(graph.FindCommonAncestor(child, child), Is.SameAs(child));
        }

        [Test]
        public void FindCommonAncestor_UsesFullNameAndReturnsNullForUnknownOrUnrelatedTables()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var otherRoot = new OtherRootTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child, otherRoot });

            Assert.That(graph.FindCommonAncestor(new ChildTable(), new RootTable()), Is.SameAs(root));
            Assert.That(graph.FindCommonAncestor(root, otherRoot), Is.Null);
            Assert.That(graph.FindCommonAncestor(new UnknownTable(), root), Is.Null);
            Assert.That(graph.FindCommonAncestor(child, new UnknownTable()), Is.Null);
        }

        [Test]
        public void TryToJoinTables_AncestorAndDescendant_BuildsJoinQuery()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child, grandChild });

            var ok = graph.TryToJoinTables(child, grandChild, out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(join!.ToSql(), Is.EqualTo("[dbo].[Child] [A0] JOIN [dbo].[GrandChild] [A1] ON [A1].[ChildId]=[A0].[Id]"));

            Assert.That(
                SqQueryBuilder.Select(child.Id, grandChild.ChildId).From(join!).ToSql(TSqlExporter.Default),
                Is.EqualTo(
                    "SELECT [A0].[Id],[A1].[ChildId] FROM [dbo].[Child] [A0] JOIN [dbo].[GrandChild] [A1] ON [A1].[ChildId]=[A0].[Id]"
                )
            );
        }

        [Test]
        public void TryToJoinTables_SiblingBranches_BuildsJoinThroughCommonAncestor()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var childB = new ChildBTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child, childB });

            var ok = graph.TryToJoinTables(child, childB, out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(join!.ToSql(), Is.EqualTo("[dbo].[Root] [A0] JOIN [dbo].[Child] [A1] ON [A1].[RootId]=[A0].[Id] JOIN [dbo].[ChildB] [A2] ON [A2].[RootId]=[A0].[Id]"));
        }

        [Test]
        public void TryToJoinTables_UsesFullNameAndReturnsFalseForUnknownOrUnrelatedTables()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var otherRoot = new OtherRootTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child, otherRoot });

            Assert.That(graph.TryToJoinTables(new ChildTable(), new RootTable(), out var sameNameJoin), Is.True);
            Assert.That(sameNameJoin, Is.Not.Null);
            Assert.That(graph.TryToJoinTables(root, otherRoot, out var unrelatedJoin), Is.False);
            Assert.That(unrelatedJoin, Is.Null);
            Assert.That(graph.TryToJoinTables(new UnknownTable(), root, out var unknownJoin), Is.False);
            Assert.That(unknownJoin, Is.Null);
        }

        [Test]
        public void Create_Forest_AllowsMultipleRoots()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var otherRoot = new OtherRootTable();

            var graph = TablesGraph.Create(new TableBase[] { root, child, otherRoot });

            Assert.That(graph.Roots, Is.EqualTo(new TableBase[] { root, otherRoot }));
        }

        [Test]
        public void Create_MultipleForeignKeysToSameParent_UsesSingleParentEdge()
        {
            var root = new RootTable();
            var child = new MultiKeySameParentChildTable();

            var graph = TablesGraph.Create(new TableBase[] { root, child });

            Assert.That(graph.GetParent(child), Is.SameAs(root));
            Assert.That(graph.GetChildren(root), Is.EqualTo(new TableBase[] { child }));
        }

        [Test]
        public void Contains_UsesFullNameNotReference()
        {
            var root = new RootTable();
            var graph = TablesGraph.Create(new TableBase[] { root });

            Assert.That(graph.Contains(root), Is.True);
            Assert.That(graph.Contains(new RootTable()), Is.True);
            Assert.That(graph.Contains(new UnknownTable()), Is.False);
        }

        [Test]
        public void Methods_WithSameFullNameDifferentInstance_ResolveCanonicalTable()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child });

            var resolvedParent = graph.GetParent(new ChildTable());
            var resolvedChildren = graph.GetChildren(new RootTable());

            Assert.That(resolvedParent, Is.SameAs(root));
            Assert.That(resolvedChildren.Single(), Is.SameAs(child));
        }

        [Test]
        public void IsParent_UsesFullNameAndReturnsFalseForUnknownTables()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create(new TableBase[] { root, child, grandChild });

            Assert.That(graph.IsParent(child, root), Is.True);
            Assert.That(graph.IsParent(new ChildTable(), new RootTable()), Is.True);
            Assert.That(graph.IsParent(grandChild, child), Is.True);
            Assert.That(graph.IsParent(root, child), Is.False);
            Assert.That(graph.IsParent(new UnknownTable(), root), Is.False);
            Assert.That(graph.IsParent(child, new UnknownTable()), Is.False);
            Assert.That(graph.IsParent(root, root), Is.False);
        }

        [Test]
        public void Methods_WithUnknownTable_ThrowArgumentException()
        {
            var graph = TablesGraph.Create(new TableBase[] { new RootTable() });
            var unknown = new UnknownTable();

            Assert.That(() => graph.GetParent(unknown), Throws.ArgumentException);
            Assert.That(() => graph.GetChildren(unknown), Throws.ArgumentException);
            Assert.That(() => graph.GetAncestors(unknown).ToArray(), Throws.ArgumentException);
            Assert.That(() => graph.GetDescendants(unknown).ToArray(), Throws.ArgumentException);
        }

        [Test]
        public void TryCreate_DuplicateInputTableFullName_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate(new TableBase[] { new RootTable(), new RootTable() }, out var graph, out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("Duplicate table"));
        }

        [Test]
        public void TryCreate_SelfReference_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate(new TableBase[] { new SelfFkTable() }, out var graph, out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("cannot reference itself"));
        }

        [Test]
        public void TryCreate_Cycle_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate(new TableBase[] { new CrossFkTable1(), new CrossFkTable2() }, out var graph, out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("Cycle detected"));
        }

        [Test]
        public void TryCreate_MultipleDistinctParents_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate(
                new TableBase[] { new RootTable(), new OtherRootTable(), new MultiParentChildTable() },
                out var graph,
                out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("more than one distinct parent"));
        }

        [Test]
        public void TryCreate_ForeignKeyOutsideGraph_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate(new TableBase[] { new ChildTable() }, out var graph, out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("is not included in the graph"));
        }

        [Test]
        public void Create_InvalidGraph_ThrowsSqExpressException()
        {
            Assert.That(
                () => TablesGraph.Create(new TableBase[] { new CrossFkTable1(), new CrossFkTable2() }),
                Throws.TypeOf<SqExpressException>());
        }

        private class RootTable : TableBase
        {
            public RootTable() : base("dbo", "Root")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            }

            public Int32TableColumn Id { get; }
        }

        private class OtherRootTable : TableBase
        {
            public OtherRootTable() : base("dbo", "OtherRoot")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            }

            public Int32TableColumn Id { get; }
        }

        private class ChildTable : TableBase
        {
            public ChildTable() : base("dbo", "Child")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.RootId = this.CreateInt32Column("RootId", ColumnMeta.ForeignKey<RootTable>(t => t.Id));
            }

            public Int32TableColumn Id { get; }

            public Int32TableColumn RootId { get; }
        }

        private class ChildBTable : TableBase
        {
            public ChildBTable() : base("dbo", "ChildB")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.RootId = this.CreateInt32Column("RootId", ColumnMeta.ForeignKey<RootTable>(t => t.Id));
            }

            public Int32TableColumn Id { get; }

            public Int32TableColumn RootId { get; }
        }

        private class GrandChildTable : TableBase
        {
            public GrandChildTable() : base("dbo", "GrandChild")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.ChildId = this.CreateInt32Column("ChildId", ColumnMeta.ForeignKey<ChildTable>(t => t.Id));
            }

            public Int32TableColumn Id { get; }

            public Int32TableColumn ChildId { get; }
        }

        private class MultiKeySameParentChildTable : TableBase
        {
            public MultiKeySameParentChildTable() : base("dbo", "MultiKeySameParentChild")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.RootId1 = this.CreateInt32Column("RootId1", ColumnMeta.ForeignKey<RootTable>(t => t.Id));
                this.RootId2 = this.CreateInt32Column("RootId2", ColumnMeta.ForeignKey<RootTable>(t => t.Id));
            }

            public Int32TableColumn Id { get; }

            public Int32TableColumn RootId1 { get; }

            public Int32TableColumn RootId2 { get; }
        }

        private class MultiParentChildTable : TableBase
        {
            public MultiParentChildTable() : base("dbo", "MultiParentChild")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.RootId = this.CreateInt32Column("RootId", ColumnMeta.ForeignKey<RootTable>(t => t.Id));
                this.OtherRootId = this.CreateInt32Column("OtherRootId", ColumnMeta.ForeignKey<OtherRootTable>(t => t.Id));
            }

            public Int32TableColumn Id { get; }

            public Int32TableColumn RootId { get; }

            public Int32TableColumn OtherRootId { get; }
        }

        private class UnknownTable : TableBase
        {
            public UnknownTable() : base("dbo", "Unknown")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            }

            public Int32TableColumn Id { get; }
        }

        private class SelfFkTable : TableBase
        {
            public SelfFkTable() : base("dbo", "SelfFkTable")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.RefId = this.CreateInt32Column("RefId", ColumnMeta.ForeignKey<SelfFkTable>(t => t.Id));
            }

            public Int32TableColumn Id { get; }

            public Int32TableColumn RefId { get; }
        }

        private class CrossFkTable1 : TableBase
        {
            public CrossFkTable1() : base("dbo", "CrossFkTable1")
            {
                this.Id1 = this.CreateInt32Column("Id1", ColumnMeta.PrimaryKey());
                this.RefId = this.CreateInt32Column("RefId", ColumnMeta.ForeignKey<CrossFkTable2>(t => t.Id2));
            }

            public Int32TableColumn Id1 { get; }

            public Int32TableColumn RefId { get; }
        }

        private class CrossFkTable2 : TableBase
        {
            public CrossFkTable2() : base("dbo", "CrossFkTable2")
            {
                this.Id2 = this.CreateInt32Column("Id2", ColumnMeta.PrimaryKey());
                this.RefId = this.CreateInt32Column("RefId", ColumnMeta.ForeignKey<CrossFkTable1>(t => t.Id1));
            }

            public Int32TableColumn Id2 { get; }

            public Int32TableColumn RefId { get; }
        }
    }
}
