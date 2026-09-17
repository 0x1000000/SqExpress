import { describe, expect, it } from "vitest";
import {
  column,
  cte,
  defineTable,
  deleteFrom,
  lit,
  select,
  sqlType,
  update,
} from "../../src/index.js";

describe("SQLite joined UPDATE", () => {
  it("retains every inner-join source and predicate", () => {
    const target = defineTable({
      schema: null,
      name: "Target",
      columns: { Id: column(sqlType.int32), Value: column(sqlType.int32) },
    });
    const one = cte("One", { Id: column(sqlType.int32) }, () =>
      select({ Id: lit(1) }).unionAll(select({ Id: lit(2) })),
    );
    const two = cte("Two", { Id: column(sqlType.int32), Value: column(sqlType.int32) }, () =>
      select({ Id: lit(1), Value: lit(10) }),
    );
    const statement = update(target)
      .set({ Value: two.Value })
      .from(target)
      .innerJoin(one, one.Id.eq(target.Id))
      .innerJoin(two, two.Id.eq(one.Id));
    const sql = statement.toSql("sqlite");
    expect(sql).toContain('FROM "One","Two"');
    expect(sql).toContain('"One"."Id"="Target"."Id" AND "Two"."Id"="One"."Id"');
  });
  it("retains every PostgreSQL joined DELETE source and predicate", () => {
    const target = defineTable({
      schema: null,
      name: "Target",
      columns: { Id: column(sqlType.int32) },
    });
    const one = cte("One", { Id: column(sqlType.int32) }, () => select({ Id: lit(1) }));
    const two = cte("Two", { Id: column(sqlType.int32) }, () => select({ Id: lit(1) }));
    const sql = deleteFrom(target)
      .from(target)
      .innerJoin(one, one.Id.eq(target.Id))
      .innerJoin(two, two.Id.eq(one.Id))
      .toSql("pgsql");
    expect(sql).toContain('DELETE FROM "Target" USING "One","Two"');
    expect(sql).toContain('"One"."Id"="Target"."Id" AND "Two"."Id"="One"."Id"');
  });
});
