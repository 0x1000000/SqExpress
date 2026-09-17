import {
  exprAlias,
  exprBooleanAnd,
  exprBooleanEq,
  exprColumn,
  exprColumnName,
  exprJoinedTable,
  exprTableAlias,
  type ExprBoolean,
  type ExprTable,
  type IExprTableSource,
} from "./ast/generated/ast.generated.js";
import { tableSource } from "./builders/index.js";
import {
  getColumnOrigin,
  type ColumnDefinitions,
  type TableDescriptor,
} from "./descriptors/index.js";

export type GraphTable = TableDescriptor<string | null, string, ColumnDefinitions, string>;
export type GraphTableReference = GraphTable | ExprTable;
export const AmbiguousJoinPathBehavior = {
  DeterministicFirst: "deterministic-first",
  Fail: "fail",
  Callback: "callback",
} as const;
export type AmbiguousJoinPathBehavior =
  (typeof AmbiguousJoinPathBehavior)[keyof typeof AmbiguousJoinPathBehavior];
export interface TablesGraphJoinOptions {
  readonly ambiguousPathBehavior?: AmbiguousJoinPathBehavior;
  readonly ambiguousPathResolver?: (paths: ReadonlyArray<ReadonlyArray<GraphTable>>) => number;
}
export type TablesGraphCreateResult =
  | { readonly success: true; readonly graph: TablesGraph; readonly error: null }
  | { readonly success: false; readonly graph: null; readonly error: string };
interface ForeignKeyLink {
  readonly childColumn: string;
  readonly targetKey: string;
  readonly targetColumn: string;
}
const identity = (database: string | null, schema: string | null, name: string): string =>
  JSON.stringify([database ?? "", schema ?? "", name].map((part) => part.toUpperCase()));
function key(table: GraphTableReference): string {
  if ("$metadata" in table) {
    const metadata = table.$metadata;
    return identity(metadata.database, metadata.schema, metadata.name);
  }
  const name = table.fullName;
  return name.kind === "ExprTempTableName"
    ? identity(null, null, name.name)
    : identity(
        name.dbSchema?.database?.name ?? null,
        name.dbSchema?.schema.name ?? null,
        name.tableName.name,
      );
}
function name(table: GraphTable): string {
  const metadata = table.$metadata;
  return [metadata.database, metadata.schema, metadata.name]
    .filter((part) => part !== null && part !== "")
    .join(".");
}
const actualTable = (table: GraphTableReference): ExprTable => tableSource(table) as ExprTable;

/** Foreign-key navigation and join discovery, matching SqExpress TablesGraph. */
export class TablesGraph {
  private constructor(
    private readonly tables: ReadonlyMap<string, GraphTable>,
    private readonly outgoing: ReadonlyMap<string, ReadonlyArray<GraphTable>>,
    private readonly incoming: ReadonlyMap<string, ReadonlyArray<GraphTable>>,
    private readonly links: ReadonlyMap<string, ReadonlyArray<ForeignKeyLink>>,
  ) {
    Object.freeze(this);
  }

  static create(tables: ReadonlyArray<GraphTable>): TablesGraph {
    const result = TablesGraph.tryCreate(tables);
    if (!result.success) throw new TypeError(result.error);
    return result.graph;
  }

  static tryCreate(tables: ReadonlyArray<GraphTable> | null): TablesGraphCreateResult {
    const fail = (error: string): TablesGraphCreateResult => ({
      success: false,
      graph: null,
      error,
    });
    if (tables == null) return fail("Table list cannot be null.");
    const canonical = new Map<string, GraphTable>();
    for (const table of tables) {
      if (table == null) throw new TypeError("Table list cannot contain null.");
      if (canonical.has(key(table)))
        return fail(`Duplicate table '${name(table)}' in graph input.`);
      canonical.set(key(table), table);
    }
    const outgoing = new Map<string, GraphTable[]>();
    const incoming = new Map<string, GraphTable[]>();
    const links = new Map<string, ForeignKeyLink[]>();
    const append = (map: Map<string, GraphTable[]>, tableKey: string, table: GraphTable): void => {
      const items = map.get(tableKey) ?? [];
      items.push(table);
      map.set(tableKey, items);
    };
    for (const table of tables) {
      const sourceKey = key(table);
      const seen = new Set<string>();
      const tableLinks: ForeignKeyLink[] = [];
      for (const [columnName, definition] of Object.entries(table.$metadata.definitions)) {
        const references = definition.options.references;
        if (references === undefined) continue;
        for (const input of Array.isArray(references) ? references : [references]) {
          const reference = typeof input === "function" ? input() : input;
          const origin = getColumnOrigin(reference);
          if (origin === undefined) continue;
          const targetKey = identity(origin.database, origin.schema, origin.table);
          const target = canonical.get(targetKey);
          if (target === undefined)
            return fail(
              `Foreign key on '${name(table)}.${columnName}' references '${[origin.database, origin.schema, origin.table].filter((part) => part !== null).join(".")}' which is not included in the graph.`,
            );
          tableLinks.push({ childColumn: columnName, targetKey, targetColumn: reference.name });
          if (!seen.has(targetKey)) {
            seen.add(targetKey);
            append(outgoing, sourceKey, target);
            append(incoming, targetKey, table);
          }
        }
      }
      links.set(sourceKey, Object.freeze(tableLinks) as ForeignKeyLink[]);
    }
    const indegree = new Map([...canonical.keys()].map((tableKey) => [tableKey, 0]));
    for (const [sourceKey, targets] of outgoing)
      for (const target of targets)
        if (sourceKey !== key(target)) indegree.set(key(target), indegree.get(key(target))! + 1);
    const queue = [...canonical.keys()].filter((tableKey) => indegree.get(tableKey) === 0);
    for (let index = 0; index < queue.length; index++) {
      const sourceKey = queue[index]!;
      for (const target of outgoing.get(sourceKey) ?? []) {
        const targetKey = key(target);
        if (targetKey === sourceKey) continue;
        indegree.set(targetKey, indegree.get(targetKey)! - 1);
        if (indegree.get(targetKey) === 0) queue.push(targetKey);
      }
    }
    if (queue.length !== canonical.size) return fail("Cycle detected in tables graph.");
    for (const items of [...outgoing.values(), ...incoming.values()]) Object.freeze(items);
    return {
      success: true,
      graph: new TablesGraph(canonical, outgoing, incoming, links),
      error: null,
    };
  }

  contains(table: GraphTableReference | null): boolean {
    return this.tryGetTable(table) !== null;
  }
  tryGetTable(table: GraphTableReference | null): GraphTable | null {
    return table == null ? null : (this.tables.get(key(table)) ?? null);
  }
  references(
    table: GraphTableReference | null,
    candidate: GraphTableReference | null,
    includeSelfRef = false,
  ): boolean {
    return (
      table != null &&
      candidate != null &&
      this.contains(table) &&
      this.getReferences(table, includeSelfRef).some((target) => key(target) === key(candidate))
    );
  }
  getReferences(table: GraphTableReference, includeSelfRef = false): ReadonlyArray<GraphTable> {
    return this.adjacent(table, this.outgoing, includeSelfRef);
  }
  getReferencedBy(table: GraphTableReference, includeSelfRef = false): ReadonlyArray<GraphTable> {
    return this.adjacent(table, this.incoming, includeSelfRef);
  }
  getAllReferences(
    table: GraphTableReference,
    includeSelfRef = false,
  ): IterableIterator<GraphTable> {
    return this.traverse(table, this.outgoing, includeSelfRef);
  }
  getAllReferencedBy(
    table: GraphTableReference,
    includeSelfRef = false,
  ): IterableIterator<GraphTable> {
    return this.traverse(table, this.incoming, includeSelfRef);
  }

  tryToJoinTables(
    tables: ReadonlyArray<GraphTableReference>,
    options?: TablesGraphJoinOptions,
  ): IExprTableSource | null;
  tryToJoinTables(
    first: GraphTableReference,
    last: GraphTableReference,
    options?: TablesGraphJoinOptions,
  ): IExprTableSource | null;
  tryToJoinTables(
    first: GraphTableReference,
    last: GraphTableReference,
    intermediateTables: ReadonlyArray<GraphTableReference> | null,
    options?: TablesGraphJoinOptions,
  ): IExprTableSource | null;
  tryToJoinTables(
    first: GraphTableReference | ReadonlyArray<GraphTableReference>,
    lastOrOptions?: GraphTableReference | TablesGraphJoinOptions,
    checkpointsOrOptions?: ReadonlyArray<GraphTableReference> | TablesGraphJoinOptions | null,
    options: TablesGraphJoinOptions = {},
  ): IExprTableSource | null {
    if (Array.isArray(first) || first == null) {
      const settings = this.validateOptions(lastOrOptions as TablesGraphJoinOptions | undefined);
      return this.joinMany(first as ReadonlyArray<GraphTableReference> | null, settings);
    }
    const last = lastOrOptions as GraphTableReference;
    const checkpoints = Array.isArray(checkpointsOrOptions) ? checkpointsOrOptions : [];
    const settings = this.validateOptions(
      Array.isArray(checkpointsOrOptions) || checkpointsOrOptions == null
        ? options
        : (checkpointsOrOptions as TablesGraphJoinOptions),
    );
    const source = first as GraphTableReference;
    if (!this.contains(source) || !this.contains(last) || key(source) === key(last)) return null;
    let previous = source;
    let from = actualTable(source) as IExprTableSource;
    let previousActual = from as ExprTable;
    for (const endpoint of [...checkpoints, last]) {
      if (!this.contains(endpoint)) return null;
      const path = this.selectPath(
        this.shortestPaths([key(previous)], new Set([key(endpoint)]), settings),
        settings,
      );
      if (path === null) return null;
      for (let index = 1; index < path.length; index++) {
        const current = path[index]!;
        const currentActual = actualTable(index === path.length - 1 ? endpoint : current());
        from = this.join(
          from,
          previousActual,
          this.tables.get(key(previous))!,
          currentActual,
          current,
        );
        previous = current;
        previousActual = currentActual;
      }
      previous = endpoint;
      previousActual = actualTable(endpoint);
    }
    return from;
  }

  toJoinTables(
    tables: ReadonlyArray<GraphTableReference>,
    options?: TablesGraphJoinOptions,
  ): IExprTableSource;
  toJoinTables(
    first: GraphTableReference,
    last: GraphTableReference,
    options?: TablesGraphJoinOptions,
  ): IExprTableSource;
  toJoinTables(
    first: GraphTableReference,
    last: GraphTableReference,
    intermediateTables: ReadonlyArray<GraphTableReference> | null,
    options?: TablesGraphJoinOptions,
  ): IExprTableSource;
  toJoinTables(
    first: GraphTableReference | ReadonlyArray<GraphTableReference>,
    lastOrOptions?: GraphTableReference | TablesGraphJoinOptions,
    checkpointsOrOptions?: ReadonlyArray<GraphTableReference> | TablesGraphJoinOptions | null,
    options?: TablesGraphJoinOptions,
  ): IExprTableSource {
    const source = Array.isArray(first)
      ? this.tryToJoinTables(first, lastOrOptions as TablesGraphJoinOptions | undefined)
      : this.tryToJoinTables(
          first as GraphTableReference,
          lastOrOptions as GraphTableReference,
          checkpointsOrOptions as ReadonlyArray<GraphTableReference> | null,
          options,
        );
    if (source === null) throw new TypeError("No join path found for the requested tables.");
    return source;
  }

  private resolve(table: GraphTableReference): GraphTable {
    const canonical = this.tryGetTable(table);
    if (canonical === null) throw new TypeError("Table does not belong to this graph.");
    return canonical;
  }
  private adjacent(
    table: GraphTableReference,
    map: ReadonlyMap<string, ReadonlyArray<GraphTable>>,
    includeSelfRef: boolean,
  ): ReadonlyArray<GraphTable> {
    const tableKey = key(this.resolve(table));
    const items = map.get(tableKey) ?? [];
    return includeSelfRef ? items : Object.freeze(items.filter((item) => key(item) !== tableKey));
  }
  private *traverse(
    table: GraphTableReference,
    map: ReadonlyMap<string, ReadonlyArray<GraphTable>>,
    includeSelfRef: boolean,
  ): IterableIterator<GraphTable> {
    const visited = new Set<string>();
    const stack = [...this.adjacent(table, map, includeSelfRef)].reverse();
    while (stack.length > 0) {
      const current = stack.pop()!;
      if (visited.has(key(current))) continue;
      visited.add(key(current));
      yield current;
      stack.push(...[...this.adjacent(current, map, includeSelfRef)].reverse());
    }
  }
  private validateOptions(
    options: TablesGraphJoinOptions = {},
  ): Required<Pick<TablesGraphJoinOptions, "ambiguousPathBehavior">> & TablesGraphJoinOptions {
    if (options === null) throw new TypeError("Join options cannot be null.");
    const behavior = options.ambiguousPathBehavior ?? AmbiguousJoinPathBehavior.DeterministicFirst;
    if (!Object.values(AmbiguousJoinPathBehavior).includes(behavior))
      throw new TypeError("Invalid ambiguous join path behavior.");
    if (
      behavior === AmbiguousJoinPathBehavior.Callback &&
      typeof options.ambiguousPathResolver !== "function"
    )
      throw new TypeError("An ambiguous join path resolver is required for Callback behavior.");
    return { ...options, ambiguousPathBehavior: behavior };
  }
  private shortestPaths(
    sources: ReadonlyArray<string>,
    targets: ReadonlySet<string>,
    options: TablesGraphJoinOptions,
  ): ReadonlyArray<ReadonlyArray<GraphTable>> {
    const queue = [...new Set(sources)];
    const distance = new Map(queue.map((source) => [source, 0]));
    const previous = new Map(queue.map((source) => [source, [] as string[]]));
    const reached: string[] = [];
    let targetDistance: number | undefined;
    for (let index = 0; index < queue.length; index++) {
      const current = queue[index]!;
      const currentDistance = distance.get(current)!;
      if (targetDistance !== undefined && currentDistance > targetDistance) break;
      if (targets.has(current)) {
        targetDistance ??= currentDistance;
        reached.push(current);
        continue;
      }
      const canonical = this.tables.get(current)!;
      const neighbors = [...this.getReferences(canonical), ...this.getReferencedBy(canonical)];
      for (const neighborKey of new Set(neighbors.map(key))) {
        const nextDistance = currentDistance + 1;
        if (!distance.has(neighborKey)) {
          distance.set(neighborKey, nextDistance);
          previous.set(neighborKey, [current]);
          queue.push(neighborKey);
        } else if (distance.get(neighborKey) === nextDistance)
          previous.get(neighborKey)!.push(current);
      }
    }
    const maxPaths =
      options.ambiguousPathBehavior === AmbiguousJoinPathBehavior.Callback
        ? Infinity
        : options.ambiguousPathBehavior === AmbiguousJoinPathBehavior.Fail
          ? 2
          : 1;
    const paths: GraphTable[][] = [];
    const reconstruct = (current: string, reversed: GraphTable[]): void => {
      reversed.push(this.tables.get(current)!);
      const parents = previous.get(current)!;
      if (parents.length === 0) paths.push([...reversed].reverse());
      else
        for (const parent of parents) {
          if (paths.length >= maxPaths) break;
          reconstruct(parent, reversed);
        }
      reversed.pop();
    };
    for (const target of reached) {
      reconstruct(target, []);
      if (paths.length >= maxPaths) break;
    }
    return paths.map((path) => Object.freeze(path));
  }
  private selectPath(
    paths: ReadonlyArray<ReadonlyArray<GraphTable>>,
    options: TablesGraphJoinOptions,
  ): ReadonlyArray<GraphTable> | null {
    if (paths.length === 0) return null;
    if (
      paths.length === 1 ||
      options.ambiguousPathBehavior === AmbiguousJoinPathBehavior.DeterministicFirst
    )
      return paths[0]!;
    if (options.ambiguousPathBehavior === AmbiguousJoinPathBehavior.Fail) return null;
    const index = options.ambiguousPathResolver!(Object.freeze([...paths]));
    if (!Number.isInteger(index) || index < 0 || index >= paths.length)
      throw new TypeError("The ambiguous join path resolver returned an invalid candidate index.");
    return paths[index]!;
  }
  private join(
    from: IExprTableSource,
    left: ExprTable,
    leftCanonical: GraphTable,
    right: ExprTable,
    rightCanonical: GraphTable,
  ): IExprTableSource {
    const conditions = (
      child: GraphTable,
      parent: GraphTable,
      actualChild: ExprTable,
      actualParent: ExprTable,
    ): ExprBoolean | null => {
      let result: ExprBoolean | null = null;
      const retarget = (columnName: string, actual: ExprTable) =>
        exprColumn({
          source:
            actual.alias ??
            exprTableAlias({
              alias: exprAlias({
                name:
                  actual.fullName.kind === "ExprTempTableName"
                    ? actual.fullName.name
                    : actual.fullName.tableName.name,
              }),
            }),
          columnName: exprColumnName({ name: columnName }),
        });
      for (const link of this.links.get(key(child)) ?? []) {
        if (link.targetKey !== key(parent)) continue;
        const targetColumn = Object.keys(parent.$metadata.columns).find(
          (columnName) => columnName.toUpperCase() === link.targetColumn.toUpperCase(),
        );
        if (targetColumn === undefined)
          throw new TypeError(
            `Referenced column '${name(parent)}.${link.targetColumn}' was not found.`,
          );
        const condition = exprBooleanEq({
          left: retarget(link.childColumn, actualChild),
          right: retarget(targetColumn, actualParent),
        });
        result = result === null ? condition : exprBooleanAnd({ left: result, right: condition });
      }
      return result;
    };
    const condition =
      conditions(leftCanonical, rightCanonical, left, right) ??
      conditions(rightCanonical, leftCanonical, right, left);
    if (condition === null) throw new TypeError("No foreign key join condition was found.");
    return exprJoinedTable({ left: from, right, searchCondition: condition, joinType: "Inner" });
  }
  private joinMany(
    tables: ReadonlyArray<GraphTableReference> | null,
    options: TablesGraphJoinOptions,
  ): IExprTableSource | null {
    if (tables == null || tables.length === 0) return null;
    const actual = new Map<string, ExprTable>();
    const requested: string[] = [];
    for (const table of tables) {
      if (!this.contains(table) || actual.has(key(table))) return null;
      const tableKey = key(table);
      actual.set(tableKey, actualTable(table));
      requested.push(tableKey);
    }
    const tree = [requested[0]!];
    const pending = new Set(requested.slice(1));
    let from: IExprTableSource = actual.get(tree[0]!)!;
    while (pending.size > 0) {
      const path = this.selectPath(this.shortestPaths(tree, pending, options), options);
      if (path === null) return null;
      for (let index = 1; index < path.length; index++) {
        const parent = path[index - 1]!;
        const current = path[index]!;
        const currentKey = key(current);
        const right = actual.get(currentKey) ?? actualTable(current());
        actual.set(currentKey, right);
        from = this.join(from, actual.get(key(parent))!, parent, right, current);
        if (!tree.includes(currentKey)) tree.push(currentKey);
        pending.delete(currentKey);
      }
    }
    return from;
  }
}

/** Builds and validates a foreign-key graph from table definitions. */
export function tablesGraph(tables: ReadonlyArray<GraphTable>): TablesGraph {
  return TablesGraph.create(tables);
}

/** Attempts to build a foreign-key graph without throwing for validation failures. */
export function tryTablesGraph(tables: ReadonlyArray<GraphTable> | null): TablesGraphCreateResult {
  return TablesGraph.tryCreate(tables);
}
