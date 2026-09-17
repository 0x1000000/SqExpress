import { describe, expect, it } from "vitest";
import {
  abs,
  ceiling,
  dataLen,
  day,
  floor,
  hour,
  indexOf,
  left,
  len,
  lower,
  lTrim,
  minute,
  month,
  nullIf,
  repeat,
  replace,
  right,
  round,
  rTrim,
  second,
  substring,
  trim,
  upper,
  year,
} from "../../src/index.js";

describe("first-class portable functions", () => {
  it.each([
    ["Abs", abs(-1)],
    ["Ceiling", ceiling(1.2)],
    ["DataLen", dataLen("text")],
    ["Day", day("2020-01-02")],
    ["Floor", floor(1.8)],
    ["Hour", hour("2020-01-02T03:04:05")],
    ["IndexOf", indexOf("x", "text")],
    ["Left", left("text", 2)],
    ["Len", len("text")],
    ["Lower", lower("TEXT")],
    ["LTrim", lTrim(" text")],
    ["Minute", minute("2020-01-02T03:04:05")],
    ["Month", month("2020-01-02")],
    ["NullIf", nullIf(1, 0)],
    ["Repeat", repeat("x", 2)],
    ["Replace", replace("text", "t", "T")],
    ["Right", right("text", 2)],
    ["Round", round(1.25, 1)],
    ["RTrim", rTrim("text ")],
    ["Second", second("2020-01-02T03:04:05")],
    ["Substring", substring("text", 1, 2)],
    ["Trim", trim(" text ")],
    ["Upper", upper("text")],
    ["Year", year("2020-01-02")],
  ] as const)("builds %s", (name, expression) => {
    expect(expression.kind).toBe("ExprPortableScalarFunction");
    if (expression.kind === "ExprPortableScalarFunction")
      expect(expression.portableFunction).toBe(name);
  });

  it("supports ROUND with and without precision", () => {
    const withoutPrecision = round(1.25);
    const withPrecision = round(1.25, 1);
    expect(withoutPrecision.kind).toBe("ExprPortableScalarFunction");
    expect(withPrecision.kind).toBe("ExprPortableScalarFunction");
    if (
      withoutPrecision.kind === "ExprPortableScalarFunction" &&
      withPrecision.kind === "ExprPortableScalarFunction"
    ) {
      expect(withoutPrecision.arguments).toHaveLength(1);
      expect(withPrecision.arguments).toHaveLength(2);
    }
  });
});
