import { describe, expect, it } from "vitest";
import { aliasTable, column, defineTable, nullableColumn, sqlType } from "../src/index.js";

describe("table descriptors", () => {
  it("exposes columns directly and centralizes descriptor metadata", () => {
    const table = defineTable({
      schema: "dbo",
      name: "Things",
      columns: {
        id: column(sqlType.int32),
        columns: column(sqlType.string()),
        name: column(sqlType.string()),
      },
    });
    expect(table.id).toBe(table.id);
    expect(table.columns.name).toBe("columns");
    expect(table.name.name).toBe("name");
    expect(table.$metadata.name).toBe("Things");
    expect(table.$metadata.columns.id).toBe(table.id);
    expect("schema" in table).toBe(false);
    expect("columns" in table.$metadata).toBe(true);
  });
  it("escapes physical names that collide with descriptor members", () => {
    const table = defineTable({
      schema: "dbo",
      name: "Reserved",
      columns: { as: column(sqlType.int32), $metadata: column(sqlType.int32) },
    });
    expect(table.$as.name).toBe("as");
    expect(table.$$metadata.name).toBe("$metadata");
    expect(table.as("r").$metadata.alias).toBe("r");
  });
  const users = defineTable({
    schema: "dbo",
    name: "Users",
    columns: { id: column(sqlType.int32), name: nullableColumn(sqlType.string(255)) },
  });
  it("preserves physical names, types, and nullability", () => {
    expect(users.id).toMatchObject({ name: "id", sourceAlias: "Users", nullable: false });
    expect(users.$metadata.definitions.name.sqlType.arguments).toEqual([255]);
  });
  it("creates independent aliases", () => {
    const left = users.as("u1");
    const right = aliasTable(users, "u2");
    expect(left.id.sourceAlias).toBe("u1");
    expect(right.id.sourceAlias).toBe("u2");
    expect(users.id.sourceAlias).toBe("Users");
    expect(left.id).toBe(left.id);
    expect(Object.keys(users)).not.toContain("as");
    expect(Object.keys(left.id)).not.toContain("eq");
  });
  it("acts as an immutable table-reference factory", () => {
    const automatic = users();
    const explicit = users("U");
    const manager = users("Manager");
    expect(users.$metadata.alias).toBe("");
    expect(users.id.sourceAlias).toBe("Users");
    expect(automatic.$metadata.alias).not.toBe("");
    expect(explicit.id.sourceAlias).toBe("U");
    expect(manager.id.sourceAlias).toBe("Manager");
    expect(users.$metadata.alias).toBe("");
  });
});
