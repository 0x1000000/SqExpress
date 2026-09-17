import { describe, expect, it } from "vitest";
import {
  identifierValue,
  isKeyword,
  tokenize,
  tryTokenize,
} from "../../src/parser/internal/lexer.js";

describe("T-SQL lexer port", () => {
  it("tokenizes identifiers, escaped brackets, unicode strings, numbers, and punctuation", () => {
    const tokens = tokenize("SELECT [a]]b], N'it''s', 12.50 FROM #t;");
    expect(tokens.map((item) => item.type)).toEqual([
      "identifier",
      "bracketIdentifier",
      "comma",
      "stringLiteral",
      "comma",
      "numberLiteral",
      "identifier",
      "identifier",
      "semicolon",
      "endOfFile",
    ]);
    expect(identifierValue(tokens[1]!)).toBe("a]b");
    expect(isKeyword(tokens[0]!, "select")).toBe(true);
  });
  it("skips line and block comments while preserving offsets", () => {
    const tokens = tokenize("-- x\nSELECT /* y */ 1");
    expect(tokens.map((item) => item.text)).toEqual(["SELECT", "1", ""]);
    expect(tokens[0]!.start).toBe(5);
  });
  it.each([
    ["/*", "Syntax error: unterminated block comment."],
    ["[abc", "Syntax error: unterminated bracket identifier."],
    ["'abc", "Syntax error: unterminated string literal."],
  ])("rejects malformed token %s", (sql, error) =>
    expect(tryTokenize(sql)).toEqual({ success: false, error }),
  );
});
