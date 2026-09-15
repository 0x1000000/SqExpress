import { aliasTable, column, defineTable, eq, gt, insertInto, lit, mergeInto, neq, nullableColumn, select, sqlType, update, type Query, type FluentBoolean } from "../../src/index.js";
const users = aliasTable(defineTable({ schema: "dbo", name: "Users", columns: { id: column(sqlType.int32), name: nullableColumn(sqlType.string(100)) } }), "u");
const query = select({ id: users.id, name: users.name }).from(users);
const typed: Query<{ readonly id: number; readonly name: string | null }> = query;
const variadic: Query<{ readonly id: number; readonly name: string | null }> = select(users.id, users.name).from(users);
const tuple: Query<{ readonly id: number; readonly name: string | null }> = select([users.id, users.name]).from(users);
const inner = select({ id: users.id, name: users.name }).from(users);
const nested: Query<{ readonly id: number; readonly name: string | null }> = select(inner.id, inner.name).from(inner);
const keywordProjection = select({ from: users.id }).from(users);
const projectedFrom: "from" = keywordProjection.$from.name;
const primitive: Query<readonly [number]> = select(1);
const expression: Query<readonly [unknown]> = select(lit(1));
const expressions: Query<readonly [number, string]> = select(1, "one");
const aliasedExpression: Query<{ readonly one: 1 }> = select(lit(1).as("one"));
const modifiedExpression: Query<{ readonly one: 1 }> = aliasedExpression.modify(node => node);
const aliasedName: "one" = select(lit(1).as("one")).one.name;
const allExpressions: Query<readonly [number, string]> = select(expressions.$all).from(expressions);
const allNamed: Query<{ readonly one: 1 }> = select(aliasedExpression.$all).from(aliasedExpression);
const allNamedColumn: "one" = select(aliasedExpression.$all).from(aliasedExpression).one.name;
void [typed, variadic, tuple, nested, keywordProjection, projectedFrom, primitive, expression, expressions, aliasedExpression, modifiedExpression, aliasedName, allExpressions, allNamed, allNamedColumn];
const queryKind: string = query.kind;
void queryKind;
// @ts-expect-error Completed queries are AST nodes directly, without a wrapper.
query.ast;
eq(users.id, 1);
eq(users.name, "Alice");
users.id.eq(1).and(users.name.eq("Alice"));
users.name.eq(null);
// @ts-expect-error Fluent column comparisons preserve descriptor value types.
users.id.eq("wrong");
const chained: FluentBoolean = eq(1, "1").and(neq(1, "1").or(gt(1, 2))).not();
void chained;
// @ts-expect-error Integer columns cannot be compared with strings.
eq(users.id, "wrong");
// @ts-expect-error String columns cannot be compared with numbers.
eq(users.name, 42);
// @ts-expect-error Other comparison helpers preserve descriptor value types.
neq(users.id, "wrong");
// @ts-expect-error Ordering helpers preserve descriptor value types.
gt(users.name, 42);
insertInto(users).values({ id: 1, name: null });
update(users).set({ name: "new" });
const merge = mergeInto(users, aliasTable(users, "source")).on(eq(users.id, 1));
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
const filtered = query.where(eq(users.id, 1));
// @ts-expect-error WHERE cannot be applied twice.
filtered.where;
// @ts-expect-error SELECT stages are query ASTs directly and have no finalizer.
query.done;
