import { describe, expect, it } from "vitest";
import { normalizeRows, normalizeValue, parameterValues } from "../../src/normalize.js";

describe("database value normalization", () => {
  it("preserves exact scalar representations", () => {
    expect(normalizeValue(42n)).toBe(42n);
    expect(normalizeValue("1.2300")).toBe("1.2300");
    expect(normalizeValue(new Date("2024-01-02T03:04:05.678Z"))).toBe("2024-01-02T03:04:05.678Z");
    const source = new Uint8Array([1, 2]);
    const normalized = normalizeValue(source);
    source[0] = 9;
    expect(normalized).toEqual(new Uint8Array([1, 2]));
  });
  it("preserves column names and converts parameter boundary values", () => {
    expect(normalizeRows([{ Value: true, Exact: "1.00" }])).toEqual([
      { Value: true, Exact: "1.00" },
    ]);
    expect(parameterValues([{ value: 9n }, { value: new Uint8Array([255]) }])).toEqual([
      "9",
      Buffer.from([255]),
    ]);
  });
});
