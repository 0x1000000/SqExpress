import {
  column,
  defineTable,
  eq,
  gt,
  insertInto,
  lit,
  select,
  sqlType,
  update,
  type CompiledSql,
  type Query,
} from "../../src/index.js";
const table = defineTable({ schema: null, name: "Users", columns: { Id: column(sqlType.int32) } });
const json: Query<string> = select(1).forJson();
const combined: Query<{ readonly value: 1 | 2 }> = select({ value: lit(1) }).unionAll(
  select({ value: lit(2) }),
);
const scalar = select(1).scalarSubquery().as("value");
const text: Query<{ readonly text: string }> = select(lit("a").concat("b").as("text"));
const compiled: CompiledSql = update(table)
  .set({ Id: 1 })
  .toSql({ dialect: "tsql", parameterize: true });
insertInto(table).values({ Id: 1 }).output(table.Id).toSql("tsql");
select(1).union(select(2)).orderBy(lit(1).asc()).offsetFetch(0, 10);
// @ts-expect-error FROM is not available after JSON conversion.
select(1).forJson().from(table);
// @ts-expect-error WHERE remains unavailable before FROM.
select(1).where(lit(1).eq(1));
// @ts-expect-error INSERT must be supplied with rows or a query before export.
insertInto(table).toSql("tsql");
// @ts-expect-error Required UPDATE assignments remain descriptor-typed.
update(table).set({ Id: "wrong" });
void [json, combined, scalar, text, compiled];
// @ts-expect-error Standalone comparison compatibility retains descriptor checks.
eq(table.Id, "wrong");
// @ts-expect-error Standalone ordering comparison compatibility retains descriptor checks.
gt(table.Id, "wrong");
