import { describe, expect, it } from "vitest";
import { exprBooleanAnd, exprBooleanEq, exprInt32Literal, visitNode, type AstVisitor } from "../src/index.js";

describe("generated AST", () => {
  it("creates immutable, category-safe nodes", () => {
    const equality = exprBooleanEq({ left: exprInt32Literal({ value: 1 }), right: exprInt32Literal({ value: 2 }) });
    const node = exprBooleanAnd({ left: equality, right: equality });

    expect(node.kind).toBe("ExprBooleanAnd");
    expect(node.left).toBe(equality);
    expect(Object.isFrozen(node)).toBe(true);
  });

  it("dispatches visitors exhaustively", () => {
    const node = exprInt32Literal({ value: 42 });
    const visitor = new Proxy({}, { get: (_, kind: string) => (value: { kind: string }) => `${kind}:${value.kind}` }) as AstVisitor<string>;
    expect(visitNode(visitor, node, undefined)).toBe("ExprInt32Literal:ExprInt32Literal");
  });
  it("rejects missing and extra fields at runtime", () => {
    expect(() => Reflect.apply(exprInt32Literal, null, [{}])).toThrow("requires exactly these fields");
    expect(() => Reflect.apply(exprInt32Literal, null, [{ value: 1, extra: true }])).toThrow("requires exactly these fields");
  });
});
