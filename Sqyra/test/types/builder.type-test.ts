import {
  aliasTable,
  column,
  defineTable,
  exprTypeInt32,
  insertInto,
  jsonTable,
  lit,
  mergeInto,
  nullableColumn,
  select,
  sqlType,
  update,
  type CompiledSql,
  type Query,
  type FluentBoolean,
} from "../../src/index.js";
const users = aliasTable(
  defineTable({
    schema: "dbo",
    name: "Users",
    columns: { id: column(sqlType.int32), name: nullableColumn(sqlType.string(100)) },
  }),
  "u",
);
const query = select({ id: users.id, name: users.name }).from(users);
const typed: Query<{ readonly id: number; readonly name: string | null }> = query;
const variadic: Query<{ readonly id: number; readonly name: string | null }> = select(
  users.id,
  users.name,
).from(users);
const tuple: Query<{ readonly id: number; readonly name: string | null }> = select([
  users.id,
  users.name,
]).from(users);
const inner = select({ id: users.id, name: users.name }).from(users);
const nested: Query<{ readonly id: number; readonly name: string | null }> = select(
  inner.id,
  inner.name,
).from(inner);
const keywordProjection = select({ from: users.id }).from(users);
const projectedFrom: "from" = keywordProjection.$from.name;
const primitive: Query<readonly [number]> = select(1);
const expression: Query<readonly [unknown]> = select(lit(1));
const expressions: Query<readonly [number, string]> = select(1, "one");
const scalarQuery: Query<readonly [number]> = select(select(1));
const namedScalarQuery: Query<{ readonly value: number }> = select({ value: select(1) });
const aliasedExpression: Query<{ readonly one: 1 }> = select(lit(1).as("one"));
const modifiedExpression: Query<{ readonly one: 1 }> = aliasedExpression.$ast.modify(
  (node) => node,
);
const intLiterals = query.$ast.descendantsOfType("ExprInt32Literal", (node) => node.value !== null);
const firstIntValue: number | null | undefined = intLiterals.next().value?.value;
// @ts-expect-error AST operations are available only through the $ast namespace.
query.modify;
const aliasedName: "one" = select(lit(1).as("one")).one.name;
const allExpressions: Query<readonly [number, string]> = select(expressions.$all).from(expressions);
const allNamed: Query<{ readonly one: 1 }> = select(aliasedExpression.$all).from(aliasedExpression);
const allNamedColumn: "one" = select(aliasedExpression.$all).from(aliasedExpression).one.name;
void [
  typed,
  variadic,
  tuple,
  nested,
  keywordProjection,
  projectedFrom,
  primitive,
  expression,
  expressions,
  scalarQuery,
  namedScalarQuery,
  aliasedExpression,
  modifiedExpression,
  firstIntValue,
  aliasedName,
  allExpressions,
  allNamed,
  allNamedColumn,
];
const queryKind: string = query.kind;
const shorthandSql: string = query.toSql("pgsql");
const booleanCompiled: CompiledSql = query.toSql({ dialect: "tsql", parameterize: true });
const disabledCompiled: CompiledSql = query.toSql({ dialect: "tsql", parameterize: false });
const noneCompiled: CompiledSql = query.toSql({ dialect: "tsql", parameterize: "none" });
const strictCompiled: CompiledSql = query.toSql({
  dialect: "tsql",
  parameterize: "throw-on-limit",
});
const fallbackCompiled: CompiledSql = query.toSql({
  dialect: "tsql",
  parameterize: "literal-fallback",
});
void [booleanCompiled, disabledCompiled, noneCompiled, strictCompiled, fallbackCompiled];
query.toSql({ dialect: "tsql", formatting: "spacious" });
query.toSql({ dialect: "tsql", formatting: "compact" });
// @ts-expect-error Unknown formatting presets are rejected.
query.toSql({ dialect: "tsql", formatting: "pretty" });
// @ts-expect-error The options-object form requires a dialect.
query.toSql({ parameterize: true });
// @ts-expect-error Unknown parameterization strategies are rejected.
query.toSql({ dialect: "tsql", parameterize: "fallback" });
// @ts-expect-error A dialect or complete export-options object is required.
query.toSql();
// @ts-expect-error Only supported dialect names are accepted.
query.toSql("oracle");
void queryKind;
void shorthandSql;
// @ts-expect-error Completed queries are AST nodes directly, without a wrapper.
query.ast;
users.id.eq(1);
users.name.eq("Alice");
users.id.eq(1).and(users.name.eq("Alice"));
users.name.eq(null);
// @ts-expect-error Fluent column comparisons preserve descriptor value types.
users.id.eq("wrong");
const chained: FluentBoolean = lit(1)
  .eq("1")
  .and(lit(1).neq("1").or(lit(1).gt(2)))
  .not();
void chained;
const jsonItems = jsonTable("[]", "$").value("Id", "$", exprTypeInt32).as("items");
const jsonId: number | null = null as typeof jsonItems.Id extends { readonly nullable: true }
  ? number | null
  : never;
void jsonId;
// @ts-expect-error Integer columns cannot be compared with strings.
users.id.eq("wrong");
// @ts-expect-error String columns cannot be compared with numbers.
users.name.eq(42);
// @ts-expect-error Other comparison helpers preserve descriptor value types.
users.id.neq("wrong");
// @ts-expect-error Ordering helpers preserve descriptor value types.
users.name.gt(42);
insertInto(users).values({ id: 1, name: null });
insertInto(users).columns("name").values({ name: "partial" });
// @ts-expect-error Selected INSERT columns remain value typed.
insertInto(users).columns("id").values({ id: "wrong" });
// @ts-expect-error A partial INSERT row must contain every selected column.
insertInto(users).columns("id", "name").values({ id: 1 });
update(users).set({ name: "new" });
const merge = mergeInto(users, aliasTable(users, "source")).on(users.id.eq(1));
merge.whenMatchedUpdate({ name: "new" }).whenNotMatchedInsert({ id: 1, name: null });
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
const filtered = query.where(users.id.eq(1));
// @ts-expect-error WHERE cannot be applied twice.
filtered.where;
// @ts-expect-error SELECT stages are query ASTs directly and have no finalizer.
query.done;
