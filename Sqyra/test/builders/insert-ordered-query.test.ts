import { describe, expect, it } from "vitest";
import { column, defineTable, insertInto, select, sqlType } from "../../src/index.js";

describe("INSERT from an ordered SELECT", () => {
  it("retains ORDER BY when the source is a complete SELECT query", () => {
    const table = defineTable({
      schema: null,
      name: "Numbers",
      columns: { Id: column(sqlType.int32) },
    });
    const source = select({ Id: table.Id }).from(table).orderBy(table.Id);
    expect(insertInto(table).from(source, "Id").toSql("sqlite")).toBe(
      'INSERT INTO "Numbers"("Id") SELECT "Numbers"."Id" "Id" FROM "Numbers" ORDER BY "Numbers"."Id"',
    );
  });
});
