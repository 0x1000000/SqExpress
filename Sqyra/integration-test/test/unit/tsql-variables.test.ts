import { describe, expect, it } from "vitest";
import { normalizeTsqlRows, renameTsqlVariables } from "../../src/adapters/tsql.js";

describe("T-SQL driver parameter names", () => {
  it("renames complete tokens without touching literals, quoted identifiers, or longer names", () => {
    const names = new Map([
      ["p1", "sq_000001"],
      ["p10", "sq_000010"],
    ]);
    expect(renameTsqlVariables("SELECT @p1,@p10,@p100,'@p1',[X@p10],\"@p1\"", names)).toBe(
      "SELECT @sq_000001,@sq_000010,@p100,'@p1',[X@p10],\"@p1\"",
    );
  });
  it("uses driver SQL type metadata to distinguish numeric text from exact integer and decimal values", () => {
    const rows = Object.assign(
      [{ Big: "9223372036854775807", Small: "123", Dec: "2.123456000", Txt: "123", Flag: "1" }],
      {
        columns: {
          Big: { type: Object.assign(() => {}, { declaration: "bigint" }) },
          Small: { type: Object.assign(() => {}, { declaration: "int" }) },
          Dec: { type: Object.assign(() => {}, { declaration: "decimal" }) },
          Txt: { type: Object.assign(() => {}, { declaration: "varchar" }) },
          Flag: { type: Object.assign(() => {}, { declaration: "bit" }) },
        },
      },
    );
    expect(normalizeTsqlRows(rows)[0]).toEqual({
      Big: 9223372036854775807n,
      Small: 123,
      Dec: "2.123456",
      Txt: "123",
      Flag: true,
    });
  });
  it("matches the C# XML reader's self-closing element serialization only for XML-typed columns", () => {
    const rows = Object.assign([{ Xml: "<root><Item2/></root>", Text: "<root><Item2/></root>" }], {
      columns: {
        Xml: { type: Object.assign(() => {}, { declaration: "xml" }) },
        Text: { type: Object.assign(() => {}, { declaration: "nvarchar" }) },
      },
    });
    expect(normalizeTsqlRows(rows)[0]).toEqual({
      Xml: "<root><Item2 /></root>",
      Text: "<root><Item2/></root>",
    });
  });
});
