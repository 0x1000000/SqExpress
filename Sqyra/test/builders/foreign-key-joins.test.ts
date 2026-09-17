import { describe, expect, it } from "vitest";
import {
  column,
  createColumnRef,
  defineTable,
  deleteFrom,
  nullableColumn,
  select,
  sqlType,
  update,
  values,
} from "../../src/index.js";

const rolesDefinition = defineTable({
  schema: "dbo",
  name: "Roles",
  columns: { Id: column(sqlType.int32), Name: column(sqlType.string(100)) },
});
const usersDefinition = defineTable({
  schema: "dbo",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    RoleId: nullableColumn(sqlType.int32, { references: rolesDefinition.Id }),
  },
});

describe("foreign key join inference", () => {
  for (const method of ["innerJoin", "leftJoin", "rightJoin", "fullJoin"] as const)
    for (const dialect of ["tsql", "pgsql", "mysql", "sqlite"] as const)
      it(`${method} matches an explicit predicate in ${dialect}`, () => {
        const users = usersDefinition("u");
        const roles = rolesDefinition("r");
        const base = select(users.Id, roles.Name).from(users);
        expect(base[method](roles).toSql(dialect)).toBe(
          base[method](roles, users.RoleId.eq(roles.Id)).toSql(dialect),
        );
        expect(base.toSql(dialect)).not.toContain("JOIN");
      });

  it("finds a relationship declared on the right table", () => {
    const roles = rolesDefinition("r");
    const users = usersDefinition("u");
    const base = select(users.Id).from(roles);
    expect(base.innerJoin(users).toSql("pgsql")).toBe(
      base.innerJoin(users, users.RoleId.eq(roles.Id)).toSql("pgsql"),
    );
  });

  it("preserves automatically aliased references and works with unaliased definitions", () => {
    const users = usersDefinition();
    const roles = rolesDefinition();
    expect(select(users.Id).from(users).innerJoin(roles).toSql("pgsql")).toBe(
      'SELECT "A0"."Id" FROM "dbo"."Users" "A0" JOIN "dbo"."Roles" "A1" ON "A0"."RoleId"="A1"."Id"',
    );
    expect(
      select(usersDefinition.Id).from(usersDefinition).innerJoin(rolesDefinition).toSql("tsql"),
    ).toContain("ON [Users].[RoleId]=[Roles].[Id]");
  });

  it("finds an FK on a previously joined table, including across a cross join", () => {
    const memberships = defineTable({
      schema: "dbo",
      name: "Memberships",
      columns: { UserId: column(sqlType.int32, { references: usersDefinition.Id }) },
    })("m");
    const unrelated = defineTable({
      schema: "dbo",
      name: "Other",
      columns: { Id: column(sqlType.int32) },
    });
    const users = usersDefinition("u");
    const roles = rolesDefinition("r");
    const base = select(users.Id).from(memberships).innerJoin(users).crossJoin(unrelated);
    expect(base.leftJoin(roles).toSql("pgsql")).toBe(
      base.leftJoin(roles, users.RoleId.eq(roles.Id)).toSql("pgsql"),
    );
  });

  it("resolves lazy and repeated FK references, retargeting to current aliases", () => {
    const users = defineTable({
      schema: "dbo",
      name: "LazyUsers",
      columns: {
        RoleId: column(sqlType.int32, {
          references: [() => rolesDefinition.Id, rolesDefinition.Id],
        }),
      },
    })("u");
    expect(
      select(users.RoleId).from(users).innerJoin(rolesDefinition("r")).toSql("tsql"),
    ).toContain("ON [u].[RoleId]=[r].[Id]");
  });

  it("supports a self-referencing FK from the left occurrence to the right", () => {
    const nodesDefinition = defineTable({
      schema: "dbo",
      name: "Nodes",
      columns: {
        Id: column(sqlType.int32),
        ParentId: nullableColumn(sqlType.int32, {
          references: () =>
            createColumnRef("Id", "Nodes", false, {
              database: null,
              schema: "dbo",
              table: "Nodes",
            }),
        }),
      },
    });
    const child = nodesDefinition();
    const parent = nodesDefinition();
    const base = select(child.Id).from(child);
    expect(base.leftJoin(parent).toSql("pgsql")).toBe(
      base.leftJoin(parent, child.ParentId.eq(parent.Id)).toSql("pgsql"),
    );
    expect(() => select(1).from(nodesDefinition).innerJoin(nodesDefinition)).toThrow(
      "distinct table references",
    );
  });

  it("does not match same-named tables in another schema or database", () => {
    for (const definition of [{ database: "OtherDb", schema: "dbo" }, { schema: "other" }]) {
      const roles = defineTable({
        ...definition,
        name: "Roles",
        columns: { Id: column(sqlType.int32) },
      });
      expect(() => select(1).from(usersDefinition).innerJoin(roles)).toThrow("No foreign key");
    }
  });

  it("requires an explicit predicate when there are several FK relationships", () => {
    const users = defineTable({
      schema: "dbo",
      name: "AmbiguousUsers",
      columns: {
        PrimaryRoleId: column(sqlType.int32, { references: rolesDefinition.Id }),
        SecondaryRoleId: column(sqlType.int32, { references: rolesDefinition.Id }),
      },
    });
    const base = select(1).from(users);
    expect(() => base.innerJoin(rolesDefinition)).toThrow("Ambiguous foreign key");
    expect(
      base.innerJoin(rolesDefinition, users.PrimaryRoleId.eq(rolesDefinition.Id)).toSql("tsql"),
    ).toContain("ON [AmbiguousUsers].[PrimaryRoleId]=[Roles].[Id]");
  });

  it("rejects ambiguity between several occurrences in the existing source", () => {
    const users = usersDefinition("u");
    const otherUsers = usersDefinition("other");
    expect(() => select(1).from(users).crossJoin(otherUsers).innerJoin(rolesDefinition)).toThrow(
      "Ambiguous foreign key",
    );
  });

  it("keeps derived and query sources explicit and does not infer through subquery boundaries", () => {
    const data = values([[1]], "data", { Id: column(sqlType.int32) });
    expect(() => select(1).from(usersDefinition).innerJoin(data)).toThrow("physical table");
    const projected = select(usersDefinition.RoleId).from(usersDefinition);
    expect(() => select(1).from(projected).innerJoin(rolesDefinition)).toThrow("No foreign key");
    expect(
      select(1).from(usersDefinition).innerJoin(data, usersDefinition.Id.eq(data.Id)).toSql("tsql"),
    ).toContain("JOIN (VALUES");
  });

  it("rejects references to missing target columns", () => {
    const roles = defineTable({
      schema: "dbo",
      name: "Roles",
      columns: { Other: column(sqlType.int32) },
    });
    expect(() => select(1).from(usersDefinition).innerJoin(roles)).toThrow(
      "unknown column 'Roles.Id'",
    );
  });

  for (const method of ["innerJoin", "leftJoin", "rightJoin", "fullJoin"] as const)
    it(`supports ${method} in executable UPDATE and DELETE stages`, () => {
      const users = usersDefinition("u");
      const roles = rolesDefinition("r");
      const predicate = users.RoleId.eq(roles.Id);
      const rename = update(users).set({ RoleId: 1 }).from(users);
      const remove = deleteFrom(users).from(users);
      expect(rename[method](roles).toSql("tsql")).toBe(
        rename[method](roles, predicate).toSql("tsql"),
      );
      expect(remove[method](roles).toSql("tsql")).toBe(
        remove[method](roles, predicate).toSql("tsql"),
      );
    });
});
