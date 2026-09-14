import { describe, expect, it } from "vitest";
import { aliasTable, column, defineTable, nullableColumn, sqlType } from "../src/index.js";

describe("table descriptors", () => {
  it("exposes columns directly and dollar-escapes metadata collisions", () => {
    const table = defineTable({ schema: "dbo", name: "Things", columns: { id: column(sqlType.int32), columns: column(sqlType.string()), name: column(sqlType.string()) } });
    expect(table.id).toBe(table.columns.id);
    expect(table.$columns).toBe(table.columns.columns);
    expect(table.$name).toBe(table.columns.name);
    expect(table.name).toBe("Things");
  });
  const users = defineTable({ schema: "dbo", name: "Users", columns: { id: column(sqlType.int32), name: nullableColumn(sqlType.string(255)) } });
  it("preserves physical names, types, and nullability", () => {
    expect(users.columns.id).toMatchObject({ name: "id", sourceAlias: "Users", nullable: false });
    expect(users.definitions.name.sqlType.arguments).toEqual([255]);
  });
  it("creates independent aliases", () => {
    const left = aliasTable(users, "u1"); const right = aliasTable(users, "u2");
    expect(left.columns.id.sourceAlias).toBe("u1"); expect(right.columns.id.sourceAlias).toBe("u2"); expect(users.columns.id.sourceAlias).toBe("Users");
  });
});
