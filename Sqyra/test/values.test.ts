import { describe, expect, it } from "vitest";
import { dateTimeOffsetValue, dateTimeValue, decimalValue, guidValue, int64Value } from "../src/index.js";

describe("exact AST values", () => {
  it("preserves decimal text", () => expect(decimalValue("123.4500").value).toBe("123.4500"));
  it("checks signed Int64 boundaries", () => { expect(int64Value(9223372036854775807n)).toBe(9223372036854775807n); expect(() => int64Value(9223372036854775808n)).toThrow(RangeError); });
  it("validates GUIDs", () => expect(guidValue("123e4567-e89b-12d3-a456-426614174000")).toBe("123e4567-e89b-12d3-a456-426614174000"));
  it("does not perform local-time conversion", () => { expect(dateTimeValue("2026-09-12T20:10:11.1234567", "unspecified").value).toBe("2026-09-12T20:10:11.1234567"); expect(dateTimeOffsetValue("2026-09-12T20:10:11-04:00").value).toContain("-04:00"); });
  it.each(["1.", "NaN", "01.2"])("rejects invalid decimal %s", (value) => expect(() => decimalValue(value)).toThrow(RangeError));
});
