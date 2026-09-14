import { childFields, enumValues, isAstKind, nodeFields, nodeTypeKinds, visitNode, type AstKind, type AstVisitor, type Expr } from "./generated/ast.generated.js";
import { freezeNode } from "./runtime.js";

type NodeRecord = Readonly<Record<string, unknown>>;

export function visit<R, C = void>(visitor: AstVisitor<R, C>, node: Expr, context: C): R {
  return visitNode(visitor, node, context);
}

export function createVisitor<R, C = void>(fallback: (node: Expr, context: C) => R, overrides: Partial<AstVisitor<R, C>> = {}): AstVisitor<R, C> {
  const visitor = new Proxy(overrides, { get(target, property) { const handler = Reflect.get(target, property); return typeof handler === "function" ? handler : fallback; } });
  // Generated dispatch requests only AstKind keys, and the proxy supplies a
  // callable fallback for each missing key. The dispatch test covers this invariant.
  return visitor as AstVisitor<R, C>;
}

function asRecord(node: Expr): NodeRecord {
  // Generated AST interfaces are closed readonly records; this local view is used only with generated field names.
  return node as Expr & NodeRecord;
}

export function isAstNode(value: unknown, ancestors: ReadonlySet<object> = new Set()): value is Expr {
  if (typeof value !== "object" || value === null || !("kind" in value) || typeof value.kind !== "string" || !isAstKind(value.kind)) return false;
  if (ancestors.has(value)) return false;
  const nextAncestors = new Set(ancestors).add(value);
  const record = value as NodeRecord & { readonly kind: AstKind };
  for (const field of nodeFields[record.kind]) {
    if (!(field.name in record)) return false;
    const fieldValue = record[field.name];
    if (fieldValue === null) { if (!field.nullable) return false; continue; }
    if (!field.child) {
      if (field.collection) {
        if (field.expectedType === "Byte") { if (!(fieldValue instanceof Uint8Array)) return false; }
        else if (!Array.isArray(fieldValue) || !fieldValue.every((item) => isScalar(item, field.expectedType))) return false;
      } else if (!isScalar(fieldValue, field.expectedType)) return false;
      continue;
    }
    const values = field.collection ? fieldValue : [fieldValue];
    if (!Array.isArray(values)) return false;
    for (const child of values) if (!isAstNode(child, nextAncestors) || field.expectedType === null || !nodeTypeKinds[field.expectedType]?.has(child.kind)) return false;
  }
  return true;
}

export function* walk(root: Expr): IterableIterator<Expr> {
  const ancestors = new Set<object>();
  function* inner(node: Expr): IterableIterator<Expr> {
    if (ancestors.has(node)) throw new Error(`Cycle detected at ${node.kind}`);
    ancestors.add(node); yield node;
    const record = asRecord(node);
    for (const field of childFields[node.kind]) {
      const value = record[field.name];
      if (value === null) continue;
      if (field.collection) for (const child of value as ReadonlyArray<Expr>) yield* inner(child);
      else yield* inner(value as Expr);
    }
    ancestors.delete(node);
  }
  yield* inner(root);
}

export interface WalkEntry { readonly node: Expr; readonly parent: Expr | null; readonly depth: number; readonly path: ReadonlyArray<Expr>; }
export function* walkWithParent(root: Expr): IterableIterator<WalkEntry> {
  const ancestors = new Set<object>(); const path: Expr[] = [];
  function* inner(node: Expr, parent: Expr | null): IterableIterator<WalkEntry> {
    if (ancestors.has(node)) throw new Error(`Cycle detected at ${node.kind}`);
    ancestors.add(node); path.push(node);
    yield Object.freeze({ node, parent, depth: path.length - 1, path: Object.freeze([...path]) });
    const record = asRecord(node);
    for (const field of childFields[node.kind]) {
      const value = record[field.name]; if (value === null) continue;
      if (field.collection) for (const child of value as ReadonlyArray<Expr>) yield* inner(child, node);
      else yield* inner(value as Expr, node);
    }
    path.pop(); ancestors.delete(node);
  }
  yield* inner(root, null);
}

export function* descendants(root: Expr, includeSelf = false): IterableIterator<Expr> {
  let first = true;
  for (const node of walk(root)) { if (includeSelf || !first) yield node; first = false; }
}

export function find(root: Expr, predicate: (node: Expr) => boolean): Expr | null {
  for (const node of walk(root)) if (predicate(node)) return node;
  return null;
}

export function modify(root: Expr, modifier: (node: Expr) => Expr | null): Expr | null {
  const ancestors = new Set<object>();
  function inner(node: Expr): Expr | null {
    if (ancestors.has(node)) throw new Error(`Cycle detected at ${node.kind}`);
    ancestors.add(node);
    const source = asRecord(node); let changed = false; const fields: Record<string, unknown> = { ...source };
    delete fields.kind;
    for (const field of childFields[node.kind]) {
      const value = source[field.name];
      if (value === null) continue;
      if (field.collection) {
        const original = value as ReadonlyArray<Expr>; const replacement: Expr[] = []; let collectionChanged = false;
        for (const child of original) { const next = inner(child); if (next !== child) collectionChanged = true; if (next !== null) { ensureType(next, field.expectedType, node.kind, field.name); replacement.push(next); } }
        if (collectionChanged) { changed = true; fields[field.name] = replacement; }
      } else {
        const original = value as Expr; const next = inner(original);
        if (next === null && !field.nullable) throw new Error(`Cannot remove required child ${node.kind}.${field.name}`);
        if (next !== original) { changed = true; if (next !== null) ensureType(next, field.expectedType, node.kind, field.name); fields[field.name] = next; }
      }
    }
    ancestors.delete(node);
    const rebuilt = changed ? freezeNode(node.kind, fields, nodeFields[node.kind].map((field) => field.name)) : node;
    // The kind is unchanged and every generated child field was checked above, so the rebuilt record satisfies the same union member.
    return modifier(rebuilt as Expr);
  }
  return inner(root);
}

function isScalar(value: unknown, type: string): boolean {
  const enumSet = enumValues[type];
  if (enumSet) return typeof value === "string" && enumSet.has(value);
  switch (type) {
    case "String": return typeof value === "string";
    case "Boolean": return typeof value === "boolean";
    case "Byte": return typeof value === "number" && Number.isInteger(value) && value >= 0 && value <= 255;
    case "Int16": return typeof value === "number" && Number.isInteger(value) && value >= -32768 && value <= 32767;
    case "Int32": return typeof value === "number" && Number.isInteger(value) && value >= -2147483648 && value <= 2147483647;
    case "Int64": return typeof value === "bigint" && value >= -9223372036854775808n && value <= 9223372036854775807n;
    case "Double": return typeof value === "number";
    case "Decimal": return isRecord(value) && typeof value.value === "string" && /^[+-]?(?:0|[1-9]\\d*)(?:\\.\\d+)?$/.test(value.value);
    case "Guid": return typeof value === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
    case "DateTime": return isRecord(value) && typeof value.value === "string" && ["unspecified", "utc", "local"].includes(String(value.kind));
    case "DateTimeOffset": return isRecord(value) && typeof value.value === "string";
    case "DecimalPrecisionScale": return isRecord(value) && Number.isInteger(value.precision) && Number.isInteger(value.scale);
    default: return false;
  }
}

function isRecord(value: unknown): value is Readonly<Record<string, unknown>> { return typeof value === "object" && value !== null; }

function ensureType(node: Expr, expected: string, parent: AstKind, field: string): void {
  if (!nodeTypeKinds[expected]?.has(node.kind)) throw new Error(`Invalid replacement ${node.kind} for ${parent}.${field}; expected ${expected}`);
}

export function serializeAst(root: Expr): string {
  return JSON.stringify(root, (_, value: unknown) => {
    if (typeof value === "bigint") return { $type: "bigint", value: value.toString() };
    if (value instanceof Uint8Array) return { $type: "binary", value: Array.from(value) };
    return value;
  });
}

export function deserializeAst(json: string): Expr {
  const revive = (value: unknown): unknown => {
    if (Array.isArray(value)) return value.map(revive);
    if (!isRecord(value)) return value;
    if (value.$type === "bigint" && typeof value.value === "string") return BigInt(value.value);
    if (value.$type === "binary" && Array.isArray(value.value) && value.value.every((item) => typeof item === "number")) return Uint8Array.from(value.value);
    if (typeof value.kind !== "string" || !isAstKind(value.kind)) return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, revive(item)]));
    const fields: Record<string, unknown> = {};
    for (const field of nodeFields[value.kind]) {
      if (!(field.name in value)) throw new TypeError(`Serialized ${value.kind} is missing ${field.name}.`);
      fields[field.name] = revive(value[field.name]);
    }
    const node = freezeNode(value.kind, fields, nodeFields[value.kind].map((field) => field.name));
    if (!isAstNode(node)) throw new TypeError(`Serialized ${value.kind} has an invalid field value.`);
    return node;
  };
  const result: unknown = revive(JSON.parse(json));
  if (!isAstNode(result)) throw new TypeError("Serialized value is not a Sqyra AST node.");
  return result;
}

/** Produces the stable property-oriented shape used by SqExpress syntax-tree
 * fixtures. Null properties are omitted because the C# exporter omits them. */
export function normalizeAstForCSharpFixture(root: Expr): Readonly<Record<string, unknown>> {
  const normalizeNode = (node: Expr): Readonly<Record<string, unknown>> => {
    const source = asRecord(node); const result: Record<string, unknown> = { $type: node.kind.startsWith("ExprExpr") ? node.kind.slice(4) : node.kind.slice(4) };
    for (const field of nodeFields[node.kind]) {
      const value = source[field.name]; if (value === null) continue;
      if (node.kind === "ExprCteQuery" && field.name === "query" && typeof value === "object" && value !== null && "kind" in value && value.kind === "ExprQuerySpecification" && "selectList" in value && Array.isArray(value.selectList) && value.selectList.length === 0) continue;
      const name = `${field.name[0]!.toUpperCase()}${field.name.slice(1)}`;
      if (field.child) result[name] = field.collection ? (value as ReadonlyArray<Expr>).map(normalizeNode) : normalizeNode(value as Expr);
      else if (typeof value === "bigint") result[name] = value.toString();
      else if (value instanceof Uint8Array) result[name] = Array.from(value);
      else if (typeof value === "object" && value !== null && "value" in value && Object.keys(value).length === 1) result[name] = (value as { readonly value: unknown }).value;
      else if (typeof value === "object" && value !== null && field.expectedType === "DecimalPrecisionScale") for (const [key, item] of Object.entries(value)) result[`${name}.${key[0]!.toUpperCase()}${key.slice(1)}`] = item;
      else if (typeof value === "object" && value !== null) result[name] = Object.fromEntries(Object.entries(value).map(([key, item]) => [`${key[0]!.toUpperCase()}${key.slice(1)}`, item]));
      else result[name] = value;
    }
    return result;
  };
  return normalizeNode(root);
}
