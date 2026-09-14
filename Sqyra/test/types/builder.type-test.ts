import { aliasTable, column, defineTable, eq, gt, insertInto, mergeInto, neq, nullableColumn, select, sqlType, update, type BuiltQuery, type FluentBoolean } from "../../src/index.js";
const users = aliasTable(defineTable({ schema: "dbo", name: "Users", columns: { id: column(sqlType.int32), name: nullableColumn(sqlType.string(100)) } }), "u");
const query = select({ id: users.columns.id, name: users.columns.name }).from(users).done();
const typed: BuiltQuery<{ readonly id: number; readonly name: string | null }> = query;
void typed;
eq(users.columns.id, 1);
eq(users.columns.name, "Alice");
const chained: FluentBoolean = eq(1, "1").and(neq(1, "1").or(gt(1, 2))).not();
void chained;
// @ts-expect-error Integer columns cannot be compared with strings.
eq(users.columns.id, "wrong");
// @ts-expect-error String columns cannot be compared with numbers.
eq(users.columns.name, 42);
// @ts-expect-error Other comparison helpers preserve descriptor value types.
neq(users.id, "wrong");
// @ts-expect-error Ordering helpers preserve descriptor value types.
gt(users.$name, 42);
insertInto(users).values({ id: 1, name: null });
update(users).set({ name: "new" });
const merge = mergeInto(users, aliasTable(users, "source")).on(eq(users.columns.id, 1));
merge.whenMatchedUpdate({ name: "new" }).whenNotMatchedInsert({ id: 1, name: null }).done();
// @ts-expect-error MERGE assignments must match their target column type.
merge.whenMatchedUpdate({ id: "wrong" });
// @ts-expect-error Unknown MERGE target columns are rejected.
merge.whenNotMatchedInsert({ missing: 1 });
// @ts-expect-error INSERT requires every descriptor column.
insertInto(users).values({ id: 1 });
// @ts-expect-error Unknown INSERT columns are rejected.
insertInto(users).values({ id: 1, name: null, missing: 2 });
// @ts-expect-error UPDATE values must match their column value type.
update(users).set({ id: "wrong" });
// @ts-expect-error WHERE is unavailable after the query is finalized.
query.where;
