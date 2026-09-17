import {
  column,
  cte,
  defineTable,
  insertInto,
  lit,
  nullableColumn,
  select,
  sqlType,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

interface TreeNode {
  readonly Id: number;
  readonly ParentId: number | null;
}
function buildSourceTree(): ReadonlyArray<TreeNode> {
  const nodes: TreeNode[] = [{ Id: 1, ParentId: null }];
  const child = (parent: number) => {
    const Id = nodes.length + 1;
    nodes.push({ Id, ParentId: parent });
    return Id;
  };
  const left = child(1);
  for (let branch = 0; branch < 2; branch++) {
    const next = child(left);
    child(next);
    child(next);
  }
  const right = child(1);
  for (let branch = 0; branch < 3; branch++) {
    const next = child(right);
    child(next);
  }
  return nodes;
}
function expectedClosure(nodes: ReadonlyArray<TreeNode>): Set<string> {
  const byId = new Map(nodes.map((node) => [node.Id, node]));
  const result = new Set<string>();
  for (const node of nodes) {
    let parent = node.ParentId,
      depth = 1;
    while (true) {
      result.add(`${node.Id},${parent === null ? "null" : parent},${depth}`);
      if (parent === null) break;
      const ancestor = byId.get(parent);
      if (ancestor === undefined) throw new Error(`Source tree lacks ancestor ${parent}.`);
      parent = ancestor.ParentId;
      depth++;
    }
  }
  return result;
}

export const treeClosureScenario: Scenario = {
  source: "ScTreeClosure",
  async run(context) {
    if (context.dialect === "mysql-oracle") return;
    const table = defineTable({
      schema: null,
      name: "TreeData",
      temporary: true,
      columns: {
        Id: column(sqlType.int32, { primaryKey: true }),
        ParentId: nullableColumn(sqlType.int32),
      },
    });
    const options: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(table.$script.dropIfExists().toSql(options));
    await context.database.executeScript(table.$script.create().toSql(options));
    try {
      const nodes = buildSourceTree();
      const first = nodes[0];
      if (first === undefined) throw new Error("Source tree is empty.");
      await context.execute(
        insertInto(table)
          .columns("Id", "ParentId")
          .values(first, ...nodes.slice(1)),
      );
      const current = table.as("current");
      const initial = table.as("initial");
      const closure = cte(
        "CteTreeClosure",
        {
          Id: column(sqlType.int32),
          ParentId: nullableColumn(sqlType.int32),
          Depth: column(sqlType.int32),
        },
        (self) =>
          select({ Id: initial.Id, ParentId: initial.ParentId, Depth: lit(1) })
            .from(initial)
            .unionAll(
              select({
                Id: self.Id,
                ParentId: current.ParentId,
                Depth: self.Depth.add(1),
              })
                .from(current)
                .innerJoin(self, self.ParentId.eq(current.Id)),
            ),
      );
      const rows = await context.query(
        select(closure.Id, closure.ParentId, closure.Depth).from(closure),
      );
      const expected = expectedClosure(nodes);
      if (rows.length !== expected.size)
        throw new Error(`Expected ${expected.size} closure rows, received ${rows.length}.`);
      for (const row of rows) {
        const key = `${String(row.Id)},${row.ParentId === null ? "null" : String(row.ParentId)},${String(row.Depth)}`;
        if (!expected.delete(key))
          throw new Error(`Unexpected or duplicate tree-closure row ${key}.`);
      }
      if (expected.size !== 0)
        throw new Error(`${expected.size} expected closure rows were missing.`);
    } finally {
      await context.database.executeScript(table.$script.drop().toSql(options));
    }
  },
};
