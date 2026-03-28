using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlExport;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.TableDecalationAttributes;

namespace SqExpress.Test.Meta
{
    [TestFixture]
    public class TablesGraphTest
    {
        [Test]
        public void BasicTest()
        {
            // Relations:
            //      D1
            //     ↙  ↘
            //   C1    C2
            //   ↓ ↘  ↙
            //   ↓  B1
            //   ↓ ↙  ↘
            //   A1    A2

            var tA1 = SqTable.Create(null, "A1", c => c.AppendInt32Column("Id"));
            var tA2 = SqTable.Create(null, "A2", c => c.AppendInt32Column("Id"));

            var tB1 = SqTable.Create(
                schema: null,
                name: "B1",
                columnsBuilder: c => c.AppendInt32Column("Id")
                    .AppendInt32Column(
                        "Fk",
                        ColumnMeta
                            .ForeignKey(tA1.GetColumn("Id"))
                            .ForeignKey(tA2.GetColumn("Id"))
                            .ForeignKey(//Self
                                new Int32TableColumn(
                                    null,
                                    "Id",
                                    new ExprTable(new ExprTableFullName(null, new ExprTableName("B1")), null),
                                    null
                                )
                            )
                    )
            );

            var tC1 = SqTable.Create(
                schema: null,
                name: "C1",
                columnsBuilder: c => c.AppendInt32Column("Id")
                    .AppendInt32Column(
                        "Fk",
                        ColumnMeta
                            .ForeignKey(tA1.GetColumn("Id"))
                            .ForeignKey(tB1.GetColumn("Id"))
                    )
            );

            var tC2 = SqTable.Create(
                schema: null,
                name: "C2",
                columnsBuilder: c => c.AppendInt32Column("Id")
                    .AppendInt32Column(
                        "Fk",
                        ColumnMeta
                            .ForeignKey(tB1.GetColumn("Id"))
                    )
            );

            var tD1 = SqTable.Create(
                schema: null,
                name: "D1",
                columnsBuilder: c => c.AppendInt32Column("Id")
                    .AppendInt32Column(
                        "Fk",
                        ColumnMeta
                            .ForeignKey(tC1.GetColumn("Id"))
                            .ForeignKey(tC2.GetColumn("Id"))
                    )
            );

            IReadOnlyList<TableBase> allTables = [tA1, tA2, tB1, tC1, tC2, tD1, new SelfFkTable()];

            var graph = TablesGraph.Create(allTables);

            Assert.That(graph.GetReferencedBy(tA1), Is.EquivalentTo(new TableBase[] { tB1, tC1 }));
            Assert.That(graph.GetAllReferencedBy(tA1), Is.EquivalentTo(new TableBase[] { tB1, tC1, tC2, tD1 }));
            Assert.That(graph.GetReferences(tA1), Is.EquivalentTo(new TableBase[] { }));
            Assert.That(graph.GetAllReferences(tA1), Is.EquivalentTo(new TableBase[] { }));

            Assert.That(graph.GetReferencedBy(tA2), Is.EquivalentTo(new TableBase[] { tB1 }));
            Assert.That(graph.GetAllReferencedBy(tA2), Is.EquivalentTo(new TableBase[] { tB1, tC1, tC2, tD1 }));
            Assert.That(graph.GetReferences(tA2), Is.EquivalentTo(new TableBase[] { }));
            Assert.That(graph.GetAllReferences(tA2), Is.EquivalentTo(new TableBase[] { }));

            Assert.That(graph.GetReferencedBy(tB1), Is.EquivalentTo(new TableBase[] { tC1, tC2 }));
            Assert.That(graph.GetAllReferencedBy(tB1), Is.EquivalentTo(new TableBase[] { tC1, tC2, tD1 }));
            Assert.That(graph.GetReferences(tB1), Is.EquivalentTo(new TableBase[] { tA1, tA2 }));
            Assert.That(graph.GetAllReferences(tB1), Is.EquivalentTo(new TableBase[] { tA1, tA2 }));

            Assert.That(graph.GetReferencedBy(tC1), Is.EquivalentTo(new TableBase[] { tD1 }));
            Assert.That(graph.GetAllReferencedBy(tC1), Is.EquivalentTo(new TableBase[] { tD1 }));
            Assert.That(graph.GetReferences(tC1), Is.EquivalentTo(new TableBase[] { tA1, tB1 }));
            Assert.That(graph.GetAllReferences(tC1), Is.EquivalentTo(new TableBase[] { tA1, tB1, tA2 }));

            Assert.That(graph.GetReferencedBy(tC2), Is.EquivalentTo(new TableBase[] { tD1 }));
            Assert.That(graph.GetAllReferencedBy(tC2), Is.EquivalentTo(new TableBase[] { tD1 }));
            Assert.That(graph.GetReferences(tC2), Is.EquivalentTo(new TableBase[] { tB1 }));
            Assert.That(graph.GetAllReferences(tC2), Is.EquivalentTo(new TableBase[] { tB1, tA1, tA2 }));

            Assert.That(graph.GetReferencedBy(tD1), Is.EquivalentTo(new TableBase[] { }));
            Assert.That(graph.GetAllReferencedBy(tD1), Is.EquivalentTo(new TableBase[] { }));
            Assert.That(graph.GetReferences(tD1), Is.EquivalentTo(new TableBase[] { tC1, tC2 }));
            Assert.That(graph.GetAllReferences(tD1), Is.EquivalentTo(new TableBase[] { tC1, tC2, tA1, tB1, tA2 }));


            Assert.That(graph.TryToJoinTables(tD1, tA1, out var join), Is.True);
            Assert.That(join, Is.Not.Null);

            Assert.That(
                join!.ToSql(),
                Is.EqualTo(
                    "[D1] [A0] JOIN [C1] [A1] ON [A0].[Fk]=[A1].[Id] JOIN [A1] [A2] ON [A1].[Fk]=[A2].[Id]"
                )
            );

            Assert.That(graph.TryToJoinTables(tD1, tA1, [tB1], out join), Is.True);
            Assert.That(join, Is.Not.Null);

            Assert.That(
                join!.ToSql(),
                Is.EqualTo(
                    "[D1] [A0] JOIN [C1] [A1] ON [A0].[Fk]=[A1].[Id] JOIN [B1] [A2] ON [A1].[Fk]=[A2].[Id] JOIN [A1] [A3] ON [A2].[Fk]=[A3].[Id]"
                )
            );

        }

        [Test]
        public void Create_DirectReferencesAndReferencedBy_AreBuiltCorrectly()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var multiParent = new MultiParentChildTable();

            var graph = TablesGraph.Create([root, child, multiParent, new OtherRootTable()]);

            Assert.That(graph.GetReferences(root), Is.Empty);
            Assert.That(graph.GetReferences(child), Is.EqualTo(new TableBase[] { root }));
            Assert.That(
                graph.GetReferences(multiParent),
                Is.EqualTo(new TableBase[] { root, new OtherRootTable() }).Using(TableFullNameComparer.Instance)
            );

            Assert.That(graph.GetReferencedBy(root), Is.EqualTo(new TableBase[] { child, multiParent }));
            Assert.That(
                graph.GetReferencedBy(new OtherRootTable()),
                Is.EqualTo(new TableBase[] { multiParent }).Using(TableFullNameComparer.Instance)
            );
        }

        [Test]
        public void GetAllReferences_And_GetAllReferencedBy_TraverseTransitively()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var childB = new ChildBTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create([root, child, childB, grandChild]);

            Assert.That(graph.GetAllReferences(grandChild).ToArray(), Is.EqualTo(new TableBase[] { child, root }));
            Assert.That(graph.GetAllReferences(root).ToArray(), Is.Empty);

            Assert.That(
                graph.GetAllReferencedBy(root).ToArray(),
                Is.EqualTo(new TableBase[] { child, grandChild, childB })
            );
            Assert.That(graph.GetAllReferencedBy(grandChild).ToArray(), Is.Empty);
        }

        [Test]
        public void References_UsesFullNameAndReturnsFalseForUnknownTables()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create([root, child, grandChild]);

            Assert.That(graph.References(child, root), Is.True);
            Assert.That(graph.References(new ChildTable(), new RootTable()), Is.True);
            Assert.That(graph.References(grandChild, child), Is.True);
            Assert.That(graph.References(root, child), Is.False);
            Assert.That(graph.References(new UnknownTable(), root), Is.False);
            Assert.That(graph.References(child, new UnknownTable()), Is.False);
        }

        [Test]
        public void Methods_WithSameFullNameDifferentInstance_ResolveCanonicalTable()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var graph = TablesGraph.Create([root, child]);

            Assert.That(graph.GetReferences(new ChildTable()).Single(), Is.SameAs(root));
            Assert.That(graph.GetReferencedBy(new RootTable()).Single(), Is.SameAs(child));
        }

        [Test]
        public void Methods_WithUnknownTable_ThrowArgumentException()
        {
            var graph = TablesGraph.Create([new RootTable()]);
            var unknown = new UnknownTable();

            Assert.That(() => graph.GetReferences(unknown), Throws.ArgumentException);
            Assert.That(() => graph.GetAllReferences(unknown).ToArray(), Throws.ArgumentException);
            Assert.That(() => graph.GetReferencedBy(unknown), Throws.ArgumentException);
            Assert.That(() => graph.GetAllReferencedBy(unknown).ToArray(), Throws.ArgumentException);
        }

        [Test]
        public void TryToJoinTables_DirectReference_BuildsJoinableTableSource()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var graph = TablesGraph.Create([root, child]);

            var ok = graph.TryToJoinTables(child, root, out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(
                join!.ToSql(),
                Is.EqualTo("[dbo].[Child] [A0] JOIN [dbo].[Root] [A1] ON [A0].[RootId]=[A1].[Id]")
            );
            Assert.That(
                SqQueryBuilder.Select(child.Id, root.Id).From(join!).ToSql(TSqlExporter.Default),
                Is.EqualTo(
                    "SELECT [A0].[Id],[A1].[Id] FROM [dbo].[Child] [A0] JOIN [dbo].[Root] [A1] ON [A0].[RootId]=[A1].[Id]"
                )
            );
        }

        [Test]
        public void TryToJoinTables_WithIntermediateTable_BuildsShortestPath()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create([root, child, grandChild]);

            var ok = graph.TryToJoinTables(root, grandChild, out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(
                join!.ToSql(),
                Is.EqualTo(
                    "[dbo].[Root] [A0] JOIN [dbo].[Child] [A1] ON [A1].[RootId]=[A0].[Id] JOIN [dbo].[GrandChild] [A2] ON [A2].[ChildId]=[A1].[Id]"
                )
            );
        }

        [Test]
        public void TryToJoinTables_WithRequiredIntermediateTable_UsesSpecifiedCheckpoint()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var shortcut = new ShortcutToChildAndRootTable();
            var graph = TablesGraph.Create([root, child, shortcut]);

            var ok = graph.TryToJoinTables(
                child,
                shortcut,
                new TableBase[] { root },
                out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(
                join!.ToSql(),
                Is.EqualTo(
                    "[dbo].[Child] [A0] JOIN [dbo].[Root] [A1] ON [A0].[RootId]=[A1].[Id] JOIN [dbo].[ShortcutToChildAndRoot] [A2] ON [A2].[RootId]=[A1].[Id]"
                )
            );
        }

        [Test]
        public void TryToJoinTables_WithRequiredIntermediateTables_UsesOrderedCheckpoints()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var grandChild = new GrandChildTable();
            var graph = TablesGraph.Create([root, child, grandChild]);

            var ok = graph.TryToJoinTables(
                root,
                grandChild,
                new TableBase[] { child },
                out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(
                join!.ToSql(),
                Is.EqualTo(
                    "[dbo].[Root] [A0] JOIN [dbo].[Child] [A1] ON [A1].[RootId]=[A0].[Id] JOIN [dbo].[GrandChild] [A2] ON [A2].[ChildId]=[A1].[Id]"
                )
            );
        }

        [Test]
        public void TryToJoinTables_WithMultipleRoutes_UsesShortestPath()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var shortcut = new ShortcutToChildAndRootTable();
            var graph = TablesGraph.Create([root, child, shortcut]);

            var ok = graph.TryToJoinTables(child, shortcut, out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(
                join!.ToSql(),
                Is.EqualTo("[dbo].[Child] [A0] JOIN [dbo].[ShortcutToChildAndRoot] [A1] ON [A1].[ChildId]=[A0].[Id]")
            );
        }

        [Test]
        public void TryToJoinTables_WithEqualShortestPaths_UsesDiscoveryOrder()
        {
            var hub1 = new Hub1Table();
            var hub2 = new Hub2Table();
            var source = new MultiRouteSourceTable();
            var target = new MultiRouteTargetTable();
            var graph = TablesGraph.Create([hub1, hub2, source, target]);

            var ok = graph.TryToJoinTables(source, target, out var join);

            Assert.That(ok, Is.True);
            Assert.That(join, Is.Not.Null);
            Assert.That(
                join!.ToSql(),
                Is.EqualTo(
                    "[dbo].[MultiRouteSource] [A0] JOIN [dbo].[Hub1] [A1] ON [A0].[Hub1Id]=[A1].[Id] JOIN [dbo].[MultiRouteTarget] [A2] ON [A2].[Hub1Id]=[A1].[Id]"
                )
            );
        }

        [Test]
        public void TryToJoinTables_UsesFullNameAndReturnsFalseForUnknownOrDisconnectedTables()
        {
            var root = new RootTable();
            var child = new ChildTable();
            var otherRoot = new OtherRootTable();
            var graph = TablesGraph.Create([root, child, otherRoot]);

            Assert.That(graph.TryToJoinTables(new ChildTable(), new RootTable(), out var sameNameJoin), Is.True);
            Assert.That(sameNameJoin, Is.Not.Null);
            Assert.That(graph.TryToJoinTables(root, otherRoot, out var disconnectedJoin), Is.False);
            Assert.That(disconnectedJoin, Is.Null);
            Assert.That(graph.TryToJoinTables(new UnknownTable(), root, out var unknownJoin), Is.False);
            Assert.That(unknownJoin, Is.Null);
            Assert.That(
                graph.TryToJoinTables(child, root, new TableBase[] { new UnknownTable() }, out var unknownIntermediateJoin),
                Is.False
            );
            Assert.That(unknownIntermediateJoin, Is.Null);
        }

        [Test]
        public void Contains_UsesFullNameNotReference()
        {
            var root = new RootTable();
            var graph = TablesGraph.Create([root]);

            Assert.That(graph.Contains(root), Is.True);
            Assert.That(graph.Contains(new RootTable()), Is.True);
            Assert.That(graph.Contains(new UnknownTable()), Is.False);
        }

        [Test]
        public void TryCreate_DuplicateInputTableFullName_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate([new RootTable(), new RootTable()], out var graph, out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("Duplicate table"));
        }

        [Test]
        public void TryCreate_SelfReference_ReturnsTrue()
        {
            var selfFkTable = new SelfFkTable();
            var ok = TablesGraph.TryCreate([selfFkTable], out var graph, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(graph, Is.Not.Null);
            Assert.That(graph!.GetReferences(selfFkTable), Is.Empty);
            Assert.That(graph.GetReferencedBy(selfFkTable), Is.Empty);
            Assert.That(graph.GetAllReferences(selfFkTable), Is.EquivalentTo(Array.Empty<TableBase>()));
            Assert.That(graph.GetAllReferencedBy(selfFkTable), Is.EquivalentTo(Array.Empty<TableBase>()));
            Assert.That(graph.References(selfFkTable, selfFkTable), Is.False);

            Assert.That(
                graph.GetReferences(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(
                graph.GetReferencedBy(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(
                graph.GetAllReferences(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(
                graph.GetAllReferencedBy(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(graph.References(selfFkTable, selfFkTable, includeSelfRef: true), Is.True);
            Assert.That(graph.TryToJoinTables(selfFkTable, selfFkTable, out var selfJoin), Is.False);
            Assert.That(selfJoin, Is.Null);
        }

        [Test]
        public void TryCreate_AttributeSelfReference_ReturnsTrue()
        {
            var selfFkTable = new TableSelfFk();
            var ok = TablesGraph.TryCreate([selfFkTable], out var graph, out var error);

            Assert.That(ok, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(graph, Is.Not.Null);
            Assert.That(graph!.GetReferences(selfFkTable), Is.Empty);
            Assert.That(graph.GetReferencedBy(selfFkTable), Is.Empty);
            Assert.That(graph.GetAllReferences(selfFkTable), Is.EquivalentTo(Array.Empty<TableBase>()));
            Assert.That(graph.GetAllReferencedBy(selfFkTable), Is.EquivalentTo(Array.Empty<TableBase>()));
            Assert.That(graph.References(selfFkTable, selfFkTable), Is.False);

            Assert.That(
                graph.GetReferences(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(
                graph.GetReferencedBy(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(
                graph.GetAllReferences(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(
                graph.GetAllReferencedBy(selfFkTable, includeSelfRef: true),
                Is.EquivalentTo(new TableBase[] { selfFkTable })
            );
            Assert.That(graph.References(selfFkTable, selfFkTable, includeSelfRef: true), Is.True);
            Assert.That(graph.TryToJoinTables(selfFkTable, selfFkTable, out var selfJoin), Is.False);
            Assert.That(selfJoin, Is.Null);
        }

        [Test]
        public void TryCreate_Cycle_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate([new CrossFkTable1(), new CrossFkTable2()], out var graph, out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("Cycle detected"));
        }

        [Test]
        public void TryCreate_ForeignKeyOutsideGraph_ReturnsFalse()
        {
            var ok = TablesGraph.TryCreate([new ChildTable()], out var graph, out var error);

            Assert.That(ok, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(error, Does.Contain("is not included in the graph"));
        }

        [Test]
        public void Create_InvalidGraph_ThrowsSqExpressException()
        {
            Assert.That(
                () => TablesGraph.Create([new CrossFkTable1(), new CrossFkTable2()]),
                Throws.TypeOf<SqExpressException>()
            );
        }

        private sealed class TableFullNameComparer : IEqualityComparer<TableBase>
        {
            public static readonly TableFullNameComparer Instance = new TableFullNameComparer();

            public bool Equals(TableBase? x, TableBase? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return false;
                }

                return x.FullName.ToSql() == y.FullName.ToSql();
            }

            public int GetHashCode(TableBase obj) => obj.FullName.ToSql().GetHashCode();
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

        private class ShortcutToChildAndRootTable : TableBase
        {
            public ShortcutToChildAndRootTable() : base("dbo", "ShortcutToChildAndRoot")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.ChildId = this.CreateInt32Column("ChildId", ColumnMeta.ForeignKey<ChildTable>(t => t.Id));
                this.RootId = this.CreateInt32Column("RootId", ColumnMeta.ForeignKey<RootTable>(t => t.Id));
            }

            public Int32TableColumn Id { get; }
            public Int32TableColumn ChildId { get; }
            public Int32TableColumn RootId { get; }
        }

        private class MultiParentChildTable : TableBase
        {
            public MultiParentChildTable() : base("dbo", "MultiParentChild")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.RootId = this.CreateInt32Column("RootId", ColumnMeta.ForeignKey<RootTable>(t => t.Id));
                this.OtherRootId = this.CreateInt32Column(
                    "OtherRootId",
                    ColumnMeta.ForeignKey<OtherRootTable>(t => t.Id)
                );
            }

            public Int32TableColumn Id { get; }
            public Int32TableColumn RootId { get; }
            public Int32TableColumn OtherRootId { get; }
        }

        private class Hub1Table : TableBase
        {
            public Hub1Table() : base("dbo", "Hub1")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            }

            public Int32TableColumn Id { get; }
        }

        private class Hub2Table : TableBase
        {
            public Hub2Table() : base("dbo", "Hub2")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            }

            public Int32TableColumn Id { get; }
        }

        private class MultiRouteSourceTable : TableBase
        {
            public MultiRouteSourceTable() : base("dbo", "MultiRouteSource")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.Hub1Id = this.CreateInt32Column("Hub1Id", ColumnMeta.ForeignKey<Hub1Table>(t => t.Id));
                this.Hub2Id = this.CreateInt32Column("Hub2Id", ColumnMeta.ForeignKey<Hub2Table>(t => t.Id));
            }

            public Int32TableColumn Id { get; }
            public Int32TableColumn Hub1Id { get; }
            public Int32TableColumn Hub2Id { get; }
        }

        private class MultiRouteTargetTable : TableBase
        {
            public MultiRouteTargetTable() : base("dbo", "MultiRouteTarget")
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.Hub1Id = this.CreateInt32Column("Hub1Id", ColumnMeta.ForeignKey<Hub1Table>(t => t.Id));
                this.Hub2Id = this.CreateInt32Column("Hub2Id", ColumnMeta.ForeignKey<Hub2Table>(t => t.Id));
            }

            public Int32TableColumn Id { get; }
            public Int32TableColumn Hub1Id { get; }
            public Int32TableColumn Hub2Id { get; }
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

    [TableDescriptor("SelfFk")]
    [Int32Column("Id")]
    [Int32Column("Fk", FkTable = "SelfFk", FkColumn = "Id")]
    public partial class TableSelfFk;
}
