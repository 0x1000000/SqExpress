import { describe, expect, it } from "vitest";
import {
  createVisitor,
  deserializeAst,
  descendants,
  descendantsOfType,
  exprBooleanAnd,
  exprBooleanEq,
  exprInt32Literal,
  exprPortableScalarFunction,
  exprStringLiteral,
  find,
  isAstNode,
  modify,
  parseTSql,
  serializeAst,
  toSql,
  visit,
  walk,
  walkWithParent,
} from "../src/index.js";

function tree() {
  const one = exprInt32Literal({ value: 1 });
  const two = exprInt32Literal({ value: 2 });
  const equality = exprBooleanEq({ left: one, right: two });
  return { one, two, equality, root: exprBooleanAnd({ left: equality, right: equality }) };
}

describe("AST operations", () => {
  it("walks pre-order and visits shared nodes by occurrence", () => {
    expect([...walk(tree().root)].map((n) => n.kind)).toEqual([
      "ExprBooleanAnd",
      "ExprBooleanEq",
      "ExprInt32Literal",
      "ExprInt32Literal",
      "ExprBooleanEq",
      "ExprInt32Literal",
      "ExprInt32Literal",
    ]);
  });
  it("returns descendants and finds the first match", () => {
    const value = tree();
    expect([...descendants(value.root)]).toHaveLength(6);
    expect(find(value.root, (node) => node.kind === "ExprInt32Literal")).toBe(value.one);
  });
  it("returns typed descendants with an optional predicate", () => {
    const value = tree();
    expect([...descendantsOfType(value.root, "ExprInt32Literal")]).toEqual([
      value.one,
      value.two,
      value.one,
      value.two,
    ]);
    expect([
      ...descendantsOfType(value.root, "ExprInt32Literal", (node) => node.value === 2),
    ]).toEqual([value.two, value.two]);
  });
  it("preserves identity for a no-op and rebuilds only changed paths", () => {
    const value = tree();
    expect(modify(value.root, (node) => node)).toBe(value.root);
    const changed = modify(value.root, (node) =>
      node === value.one ? exprInt32Literal({ value: 3 }) : node,
    );
    expect(changed).not.toBe(value.root);
    expect(changed?.kind).toBe("ExprBooleanAnd");
  });
  it("validates kinds, required fields, and child categories", () => {
    const value = tree();
    expect(isAstNode(value.root)).toBe(true);
    expect(isAstNode({ kind: "Unknown" })).toBe(false);
    expect(isAstNode({ kind: "ExprBooleanAnd", left: value.equality })).toBe(false);
    expect(isAstNode({ kind: "ExprBooleanAnd", left: value.one, right: value.equality })).toBe(
      false,
    );
    expect(isAstNode({ kind: "ExprInt32Literal", value: "1" })).toBe(false);
    expect(isAstNode({ kind: "ExprInt32Literal", value: 2147483648 })).toBe(false);
  });
  it("serializes bigint values deterministically", () => {
    expect(serializeAst(exprInt32Literal({ value: 1 }))).toBe(
      '{"kind":"ExprInt32Literal","value":1}',
    );
  });
  it("reports exact parents, depths, and root-to-node paths", () => {
    const value = tree();
    const entries = [...walkWithParent(value.root)];
    expect(
      entries.map(({ node, parent, depth }) => [node.kind, parent?.kind ?? null, depth]),
    ).toEqual([
      ["ExprBooleanAnd", null, 0],
      ["ExprBooleanEq", "ExprBooleanAnd", 1],
      ["ExprInt32Literal", "ExprBooleanEq", 2],
      ["ExprInt32Literal", "ExprBooleanEq", 2],
      ["ExprBooleanEq", "ExprBooleanAnd", 1],
      ["ExprInt32Literal", "ExprBooleanEq", 2],
      ["ExprInt32Literal", "ExprBooleanEq", 2],
    ]);
    expect(entries[2]?.path.map((node) => node.kind)).toEqual([
      "ExprBooleanAnd",
      "ExprBooleanEq",
      "ExprInt32Literal",
    ]);
  });
  it("round-trips deterministic JSON and rejects malformed serialized nodes", () => {
    const value = tree().root;
    const restored = deserializeAst(serializeAst(value));
    expect(restored).toEqual(value);
    expect(Object.isFrozen(restored)).toBe(true);
    expect(() => deserializeAst('{"kind":"ExprBooleanAnd","left":null,"right":null}')).toThrow(
      /invalid/i,
    );
  });
  it("rejects removal of required children and incompatible replacements", () => {
    const value = tree();
    expect(() => modify(value.root, (node) => (node === value.one ? null : node))).toThrow(
      /required child/,
    );
    expect(() =>
      modify(value.root, (node) => (node === value.equality ? value.one : node)),
    ).toThrow(/Invalid replacement/);
  });
  it("dispatches exhaustive visitors by concrete node kind", () => {
    const calls: string[] = [];
    const visitor = createVisitor((node) => {
      calls.push(node.kind);
      return node.kind;
    });
    expect(visit(visitor, exprInt32Literal({ value: 1 }), undefined)).toBe("ExprInt32Literal");
    expect(calls).toEqual(["ExprInt32Literal"]);
  });
  it("traverses, modifies, and serializes STRING_AGG nodes", () => {
    const ast = parseTSql("SELECT STRING_AGG('old','|') WITHIN GROUP (ORDER BY 2 DESC)").ast;
    expect([...walk(ast)].some((node) => node.kind === "ExprStringAgg")).toBe(true);
    const changed = modify(ast, (node) =>
      node.kind === "ExprStringLiteral" && node.value === "old"
        ? exprStringLiteral({ value: "new" })
        : node,
    );
    expect(changed).not.toBeNull();
    expect(toSql(deserializeAst(serializeAst(changed!)), { dialect: "tsql" })).toBe(
      "SELECT STRING_AGG('new','|') WITHIN GROUP (ORDER BY 2 DESC)",
    );
  });
  it("traverses and round-trips portable JSON AST nodes", () => {
    const ast = parseTSql(`SELECT JSON_VALUE('{"old":1}','$.old') [Value] FOR JSON PATH`).ast;
    const kinds = [...walk(ast)].map((node) => node.kind);
    expect(kinds).toContain("ExprQueryAsJson");
    expect(kinds).toContain("ExprJsonValue");
    expect(deserializeAst(serializeAst(ast))).toEqual(ast);
  });
  it("round-trips portable scalar function nodes", () => {
    const node = exprPortableScalarFunction({
      portableFunction: "Len",
      arguments: [exprStringLiteral({ value: "abc" })],
    });
    expect(toSql(deserializeAst(serializeAst(node)), { dialect: "tsql" })).toBe("LEN('abc')");
  });
});
