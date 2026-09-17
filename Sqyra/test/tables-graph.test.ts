import { describe, expect, it } from "vitest";
import {
  AmbiguousJoinPathBehavior,
  column,
  createColumnRef,
  defineTable,
  deleteFrom,
  select,
  sqlType,
  tableSource,
  tablesGraph,
  toSql,
  tryTablesGraph,
  update,
  type ExprTable,
  type GraphTable,
  type GraphTableReference,
  type TablesGraphJoinOptions,
} from "../src/index.js";

function tree() {
  const root = defineTable({ schema: "dbo", name: "Root", columns: { Id: column(sqlType.int32) } });
  const child = defineTable({
    schema: "dbo",
    name: "Child",
    columns: {
      Id: column(sqlType.int32),
      RootId: column(sqlType.int32, { references: root.Id }),
    },
  });
  const childB = defineTable({
    schema: "dbo",
    name: "ChildB",
    columns: {
      Id: column(sqlType.int32),
      RootId: column(sqlType.int32, { references: root.Id }),
    },
  });
  const grandChild = defineTable({
    schema: "dbo",
    name: "GrandChild",
    columns: {
      Id: column(sqlType.int32),
      ChildId: column(sqlType.int32, { references: child.Id }),
    },
  });
  const otherRoot = defineTable({
    schema: "dbo",
    name: "OtherRoot",
    columns: { Id: column(sqlType.int32) },
  });
  const graph = tablesGraph([root, child, childB, grandChild, otherRoot]);
  return { root, child, childB, grandChild, otherRoot, graph };
}
function diamond() {
  const hub1 = defineTable({ schema: "dbo", name: "Hub1", columns: { Id: column(sqlType.int32) } });
  const hub2 = defineTable({ schema: "dbo", name: "Hub2", columns: { Id: column(sqlType.int32) } });
  const columns = {
    Id: column(sqlType.int32),
    Hub1Id: column(sqlType.int32, { references: hub1.Id }),
    Hub2Id: column(sqlType.int32, { references: hub2.Id }),
  };
  const source = defineTable({ schema: "dbo", name: "Source", columns });
  const target = defineTable({ schema: "dbo", name: "Target", columns });
  const leaf = defineTable({
    schema: "dbo",
    name: "Leaf",
    columns: {
      Id: column(sqlType.int32),
      TargetId: column(sqlType.int32, { references: target.Id }),
    },
  });
  return {
    hub1,
    hub2,
    source,
    target,
    leaf,
    graph: tablesGraph([hub1, hub2, source, target, leaf]),
  };
}
const tableNames = (tables: Iterable<GraphTable>) =>
  [...tables].map((table) => table.$metadata.name);
const fail: TablesGraphJoinOptions = { ambiguousPathBehavior: AmbiguousJoinPathBehavior.Fail };
const callback: TablesGraphJoinOptions = {
  ambiguousPathBehavior: AmbiguousJoinPathBehavior.Callback,
  ambiguousPathResolver: () => 1,
};

describe("SqExpress table graph port", () => {
  it("BasicTest: retains multi-target and self FKs and discovers paths with checkpoints", () => {
    const a1 = defineTable({ schema: null, name: "A1", columns: { A1Id: column(sqlType.int32) } });
    const a2 = defineTable({ schema: null, name: "A2", columns: { A2Id: column(sqlType.int32) } });
    const b1 = defineTable({
      schema: null,
      name: "B1",
      columns: {
        B1Id: column(sqlType.int32),
        Fk: column(sqlType.int32, {
          references: [
            a1.A1Id,
            a2.A2Id,
            () =>
              createColumnRef("B1Id", "B1", false, { database: null, schema: null, table: "B1" }),
          ],
        }),
      },
    });
    const c1 = defineTable({
      schema: null,
      name: "C1",
      columns: {
        C1Id: column(sqlType.int32),
        Fk: column(sqlType.int32, { references: [a1.A1Id, b1.B1Id] }),
      },
    });
    const c2 = defineTable({
      schema: null,
      name: "C2",
      columns: { C2Id: column(sqlType.int32), Fk: column(sqlType.int32, { references: b1.B1Id }) },
    });
    const d1 = defineTable({
      schema: null,
      name: "D1",
      columns: {
        D1Id: column(sqlType.int32),
        Fk: column(sqlType.int32, { references: [c1.C1Id, c2.C2Id] }),
      },
    });
    const graph = tablesGraph([a1, a2, b1, c1, c2, d1]);
    expect(tableNames(graph.getReferences(b1))).toEqual(["A1", "A2"]);
    expect(tableNames(graph.getAllReferences(d1))).toEqual(["C1", "A1", "B1", "A2", "C2"]);
    expect(tableNames(graph.getAllReferencedBy(a1))).toEqual(["B1", "C1", "D1", "C2"]);
    expect(toSql(graph.toJoinTables(d1(), a1()), "tsql")).toBe(
      "[D1] [A0] JOIN [C1] [A1] ON [A0].[Fk]=[A1].[C1Id] JOIN [A1] [A2] ON [A1].[Fk]=[A2].[A1Id]",
    );
    expect(toSql(graph.toJoinTables(d1(), a1(), [b1()]), "tsql")).toBe(
      "[D1] [A0] JOIN [C1] [A1] ON [A0].[Fk]=[A1].[C1Id] JOIN [B1] [A2] ON [A1].[Fk]=[A2].[B1Id] JOIN [A1] [A3] ON [A2].[Fk]=[A3].[A1Id]",
    );
  });

  it("Create_DirectReferencesAndReferencedBy_AreBuiltCorrectly", () => {
    const { graph, root, child, childB, grandChild } = tree();
    expect(graph.getReferences(root)).toEqual([]);
    expect(graph.getReferences(child)).toEqual([root]);
    expect(graph.getReferencedBy(root)).toEqual([child, childB]);
    expect(graph.getReferencedBy(child)).toEqual([grandChild]);
    expect(Object.isFrozen(graph.getReferences(child, true))).toBe(true);
  });

  it("GetAllReferences_And_GetAllReferencedBy_TraverseTransitively", () => {
    const { graph, root, child, childB, grandChild } = tree();
    expect([...graph.getAllReferences(grandChild)]).toEqual([child, root]);
    expect([...graph.getAllReferencedBy(root)]).toEqual([child, grandChild, childB]);
    expect([...graph.getAllReferences(root)]).toEqual([]);
  });

  it("References_UsesFullNameAndReturnsFalseForUnknownTables", () => {
    const { graph, child, root } = tree();
    const unknown = defineTable({ schema: "other", name: "Root", columns: {} });
    expect(graph.references(child(), root("r"))).toBe(true);
    expect(graph.references(root, child)).toBe(false);
    expect(graph.references(unknown, root)).toBe(false);
    expect(graph.references(child, unknown)).toBe(false);
    expect(graph.references(null, root)).toBe(false);
  });

  it("Methods_WithSameFullNameDifferentInstance_ResolveCanonicalTable", () => {
    const { graph, root, child } = tree();
    expect(graph.getReferences(tree().child)).toEqual([root]);
    expect(graph.getReferencedBy(tree().root)[0]).toBe(child);
  });

  it("Methods_WithUnknownTable_ThrowArgumentException", () => {
    const { graph } = tree();
    const unknown = defineTable({ schema: null, name: "Unknown", columns: {} });
    expect(() => graph.getReferences(unknown)).toThrow("does not belong");
    expect(() => [...graph.getAllReferences(unknown)]).toThrow("does not belong");
    expect(() => graph.getReferencedBy(unknown)).toThrow("does not belong");
    expect(() => [...graph.getAllReferencedBy(unknown)]).toThrow("does not belong");
  });

  it("TryToJoinTables_DirectReference_BuildsJoinableTableSource", () => {
    const { graph, child, root } = tree();
    const c = child(),
      r = root();
    const from = graph.toJoinTables(c, r);
    expect(select(c.Id, r.Id).from(from).toSql("tsql")).toBe(
      "SELECT [A0].[Id],[A1].[Id] FROM [dbo].[Child] [A0] JOIN [dbo].[Root] [A1] ON [A0].[RootId]=[A1].[Id]",
    );
  });

  it("TryToJoinTables_UsesPassedEndpointObjectsInJoin", () => {
    const { graph, child, root } = tree();
    const c = tableSource(child("ch")) as ExprTable;
    const r = tableSource(root("rt")) as ExprTable;
    const from = graph.toJoinTables(c, r);
    expect(from.kind).toBe("ExprJoinedTable");
    if (from.kind === "ExprJoinedTable") {
      expect(from.left).toBe(c);
      expect(from.right).toBe(r);
    }
    expect(toSql(from, "tsql")).toBe(
      "[dbo].[Child] [ch] JOIN [dbo].[Root] [rt] ON [ch].[RootId]=[rt].[Id]",
    );
  });

  it("TryToJoinTables_WithIntermediateTable_BuildsShortestPath", () => {
    const { graph, root, grandChild } = tree();
    expect(toSql(graph.toJoinTables(root(), grandChild()), "tsql")).toBe(
      "[dbo].[Root] [A0] JOIN [dbo].[Child] [A1] ON [A1].[RootId]=[A0].[Id] JOIN [dbo].[GrandChild] [A2] ON [A2].[ChildId]=[A1].[Id]",
    );
  });

  it("TryToJoinTables_WithRequiredIntermediateTable_UsesSpecifiedCheckpoint", () => {
    const { root, child } = tree();
    const shortcut = defineTable({
      schema: "dbo",
      name: "Shortcut",
      columns: {
        Id: column(sqlType.int32),
        ChildId: column(sqlType.int32, { references: child.Id }),
        RootId: column(sqlType.int32, { references: root.Id }),
      },
    });
    const graph = tablesGraph([root, child, shortcut]);
    expect(toSql(graph.toJoinTables(child("c"), shortcut("s"), [root("r")]), "tsql")).toBe(
      "[dbo].[Child] [c] JOIN [dbo].[Root] [r] ON [c].[RootId]=[r].[Id] JOIN [dbo].[Shortcut] [s] ON [s].[RootId]=[r].[Id]",
    );
    expect(toSql(graph.toJoinTables(child("c"), shortcut("s")), "tsql")).toBe(
      "[dbo].[Child] [c] JOIN [dbo].[Shortcut] [s] ON [s].[ChildId]=[c].[Id]",
    );
  });

  it("TryToJoinTables_WithRequiredIntermediateTables_UsesOrderedCheckpoints", () => {
    const { graph, root, child, grandChild } = tree();
    expect(
      toSql(graph.toJoinTables(root(), grandChild(), [child("checkpoint")]), "tsql"),
    ).toContain("JOIN [dbo].[Child] [checkpoint]");
  });

  it("TryToJoinTables_WithEqualShortestPaths_UsesDiscoveryOrder", () => {
    const { graph, source, target } = diamond();
    const sql = toSql(graph.toJoinTables(source(), target()), "tsql");
    expect(sql).toContain("[dbo].[Hub1]");
    expect(sql).not.toContain("[dbo].[Hub2]");
  });

  it("TryToJoinTables_WithAmbiguousPathOptions_FailsOrUsesCallbackSelection", () => {
    const { graph, source, target, hub1, hub2 } = diamond();
    expect(graph.tryToJoinTables(source, target, fail)).toBe(null);
    let paths: ReadonlyArray<ReadonlyArray<GraphTable>> = [];
    const from = graph.toJoinTables(source("s"), target("t"), {
      ...callback,
      ambiguousPathResolver: (candidates) => {
        paths = candidates;
        return 1;
      },
    });
    expect(paths).toEqual([
      [source, hub1, target],
      [source, hub2, target],
    ]);
    expect(toSql(from, "tsql")).toContain("[dbo].[Hub2]");
  });

  it("TryToJoinTables_CallbackConfiguration_IsValidated", () => {
    const { graph, source, target } = diamond();
    expect(() =>
      graph.tryToJoinTables(source, target, {
        ambiguousPathBehavior: AmbiguousJoinPathBehavior.Callback,
      }),
    ).toThrow("resolver is required");
    for (const index of [-1, 2, 0.5, NaN])
      expect(() =>
        graph.tryToJoinTables(source, target, { ...callback, ambiguousPathResolver: () => index }),
      ).toThrow("invalid candidate index");
    expect(() =>
      graph.tryToJoinTables(source, target, {
        ambiguousPathBehavior: "unknown",
      } as unknown as TablesGraphJoinOptions),
    ).toThrow("Invalid ambiguous");
  });

  it("TryToJoinTables_WithIntermediate_AppliesAmbiguityOptionsPerSegment", () => {
    const { graph, source, target, leaf } = diamond();
    expect(graph.tryToJoinTables(source, leaf, [target], fail)).toBe(null);
    expect(toSql(graph.toJoinTables(source(), leaf(), [target("t")], callback), "tsql")).toContain(
      "[dbo].[Hub2]",
    );
  });

  it("TryToJoinTables_MultipleTables_BuildsGreedyTreeAndPreservesRequestedAliases", () => {
    const { graph, root, childB, grandChild } = tree();
    expect(toSql(graph.toJoinTables([root("r"), childB("b"), grandChild("g")]), "tsql")).toBe(
      "[dbo].[Root] [r] JOIN [dbo].[ChildB] [b] ON [b].[RootId]=[r].[Id] JOIN [dbo].[Child] [A0] ON [A0].[RootId]=[r].[Id] JOIN [dbo].[GrandChild] [g] ON [g].[ChildId]=[A0].[Id]",
    );
  });

  it("TryToJoinTables_MultipleTables_HandlesSingletonAndInvalidInputs", () => {
    const { graph, root, otherRoot } = tree();
    const r = tableSource(root()) as ExprTable;
    expect(graph.tryToJoinTables([r])).toBe(r);
    expect(graph.tryToJoinTables([])).toBe(null);
    expect(graph.tryToJoinTables(null as unknown as GraphTableReference[])).toBe(null);
    expect(graph.tryToJoinTables([root, null as unknown as GraphTableReference])).toBe(null);
    expect(graph.tryToJoinTables([root, root()])).toBe(null);
    expect(graph.tryToJoinTables([root, otherRoot])).toBe(null);
  });

  it("TryToJoinTables_MultipleTables_AppliesAmbiguityPolicy", () => {
    const { graph, source, target } = diamond();
    expect(graph.tryToJoinTables([source, target], fail)).toBe(null);
    expect(toSql(graph.toJoinTables([source(), target()], callback), "tsql")).toContain(
      "[dbo].[Hub2]",
    );
  });

  it("TryToJoinTables_UsesFullNameAndReturnsFalseForUnknownOrDisconnectedTables", () => {
    const { graph, root, child, otherRoot } = tree();
    const unknown = defineTable({ schema: null, name: "Unknown", columns: {} });
    expect(graph.tryToJoinTables(root, otherRoot)).toBe(null);
    expect(graph.tryToJoinTables(root, root())).toBe(null);
    expect(graph.tryToJoinTables(unknown, root)).toBe(null);
    expect(graph.tryToJoinTables(root, child, [unknown])).toBe(null);
    expect(() => graph.toJoinTables(root, otherRoot)).toThrow("No join path");
  });

  it("Contains_UsesFullNameNotReference", () => {
    const { graph } = tree();
    expect(graph.contains(tree().root())).toBe(true);
    expect(graph.contains(defineTable({ schema: "DBO", name: "root", columns: {} }))).toBe(true);
    expect(
      graph.contains(defineTable({ database: "other", schema: "dbo", name: "Root", columns: {} })),
    ).toBe(false);
    expect(graph.contains(null)).toBe(false);
  });

  it("TryGetTable_ReturnsCanonicalInstance", () => {
    const { graph, root } = tree();
    expect(graph.tryGetTable(tree().root())).toBe(root);
    expect(graph.tryGetTable(null)).toBe(null);
  });

  it("TryCreate_DuplicateInputTableFullName_ReturnsFalse", () => {
    const { root } = tree();
    expect(tryTablesGraph([root, root()])).toMatchObject({
      success: false,
      graph: null,
      error: expect.stringContaining("Duplicate table"),
    });
    expect(tryTablesGraph(null).success).toBe(false);
    expect(() => tryTablesGraph([null as unknown as GraphTable])).toThrow("cannot contain null");
    expect(tablesGraph([]).contains(root)).toBe(false);
  });

  it("TryCreate_SelfReference_ReturnsTrue", () => {
    const self = defineTable({
      schema: "dbo",
      name: "Self",
      columns: {
        Id: column(sqlType.int32),
        RefId: column(sqlType.int32, {
          references: () =>
            createColumnRef("Id", "Self", false, { database: null, schema: "dbo", table: "Self" }),
        }),
      },
    });
    const graph = tablesGraph([self]);
    expect(graph.getReferences(self)).toEqual([]);
    expect(graph.getReferencedBy(self)).toEqual([]);
    expect([...graph.getAllReferences(self)]).toEqual([]);
    expect([...graph.getAllReferencedBy(self)]).toEqual([]);
    expect(graph.getReferences(self, true)).toEqual([self]);
    expect(graph.getReferencedBy(self, true)).toEqual([self]);
    expect([...graph.getAllReferences(self, true)]).toEqual([self]);
    expect([...graph.getAllReferencedBy(self, true)]).toEqual([self]);
    expect(graph.references(self, self)).toBe(false);
    expect(graph.references(self, self, true)).toBe(true);
    expect(graph.tryToJoinTables(self(), self())).toBe(null);
  });

  it("TryCreate_Cycle_ReturnsFalse and Create_InvalidGraph_Throws", () => {
    const a = defineTable({
      schema: null,
      name: "A",
      columns: {
        Id: column(sqlType.int32),
        RefId: column(sqlType.int32, {
          references: () =>
            createColumnRef("Id", "B", false, { database: null, schema: null, table: "B" }),
        }),
      },
    });
    const b = defineTable({
      schema: null,
      name: "B",
      columns: { Id: column(sqlType.int32), RefId: column(sqlType.int32, { references: a.Id }) },
    });
    expect(tryTablesGraph([a, b])).toMatchObject({
      success: false,
      error: "Cycle detected in tables graph.",
    });
    expect(() => tablesGraph([a, b])).toThrow("Cycle detected");
  });

  it("TryCreate_ForeignKeyOutsideGraph_ReturnsFalse", () => {
    const { child } = tree();
    expect(tryTablesGraph([child])).toMatchObject({
      success: false,
      error: expect.stringContaining("is not included in the graph"),
    });
  });

  it("combines all FK column pairs on an edge and reports missing canonical target columns", () => {
    const { root } = tree();
    const child = defineTable({
      schema: "dbo",
      name: "Multi",
      columns: {
        A: column(sqlType.int32, { references: root.Id }),
        B: column(sqlType.int32, { references: root.Id }),
      },
    });
    const graph = tablesGraph([root, child]);
    expect(graph.getReferences(child)).toEqual([root]);
    expect(toSql(graph.toJoinTables(child("c"), root("r")), "tsql")).toContain(
      "ON [c].[A]=[r].[Id] AND [c].[B]=[r].[Id]",
    );
    const missing = defineTable({ schema: "dbo", name: "Root", columns: {} });
    expect(() => tablesGraph([child, missing]).toJoinTables(child, missing)).toThrow(
      "Referenced column",
    );
  });

  for (const dialect of ["tsql", "pgsql", "mysql", "sqlite"] as const)
    it(`exports connector joins in ${dialect} and supports DML sources`, () => {
      const { graph, root, child, grandChild } = tree();
      const r = root("r"),
        g = grandChild("g"),
        c = child("c");
      const source = graph.toJoinTables(r, g);
      expect(select(r.Id, g.Id).from(source).toSql(dialect)).toContain("JOIN");
      const direct = graph.toJoinTables(c, r);
      expect(update(c).set({ RootId: 1 }).from(direct).toSql(dialect)).toBe(
        update(c).set({ RootId: 1 }).from(c).innerJoin(r, c.RootId.eq(r.Id)).toSql(dialect),
      );
      if (dialect === "sqlite")
        expect(() => deleteFrom(c).from(direct).toSql(dialect)).toThrow(
          "SQLite exporter does not support DELETE with source tables",
        );
      else
        expect(deleteFrom(c).from(direct).toSql(dialect)).toBe(
          deleteFrom(c).from(c).innerJoin(r, c.RootId.eq(r.Id)).toSql(dialect),
        );
    });
});
