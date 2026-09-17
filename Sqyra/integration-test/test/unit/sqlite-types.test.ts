import { describe, expect, it } from "vitest";
import { normalizeSqliteRows } from "../../src/adapters/sqlite.js";

describe("SQLite declared-type result normalization", () => {
  it("preserves canonical decimal, integer, Boolean, and text types", () => {
    const rows = [
      {
        ColDecimal: 2.123456,
        ColInt32: 17n,
        ColInt64: 9223372036854775807n,
        Flag: 1n,
        Txt: "2.123456",
      },
    ];
    const columns = [
      { name: "ColDecimal", type: "NUMERIC" },
      { name: "ColInt32", type: "INTEGER" },
      { name: "ColInt64", type: "INTEGER" },
      { name: "Flag", type: "BOOLEAN" },
      { name: "Txt", type: "TEXT" },
    ];
    expect(normalizeSqliteRows(rows, columns)[0]).toEqual({
      ColDecimal: "2.123456",
      ColInt32: 17,
      ColInt64: 9223372036854775807n,
      Flag: true,
      Txt: "2.123456",
    });
  });
});
