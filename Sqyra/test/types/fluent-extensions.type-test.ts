import {
  aggregate,
  caseWhen,
  column,
  defineTable,
  exprTypeInt32,
  mergeInto,
  nullableColumn,
  select,
  sqlType,
  stringAgg,
  type AstOperations,
  type Expr,
  type Query,
} from "../../src/index.js";
const table = defineTable({
  schema: null,
  name: "Users",
  columns: { Id: column(sqlType.int32), Name: nullableColumn(sqlType.string()) },
});
const projected: Query<{ readonly id: number; readonly name: string | null }> = select(
  table.Id.as("id"),
  table.Name.as("name"),
);
const cases: Query<{ readonly label: "one" | 2 | null }> = select(
  caseWhen(table.Id.eq(1)).then("one").when(table.Id.eq(2)).then(2).else(null).as("label"),
);
const derived = select(table.Id).as("d", { Id: column(sqlType.int32) });
const id: "Id" = derived.Id.name;
const queryAst: AstOperations<typeof projected> = projected.$ast;
const found: Expr | null = projected.$ast.find(() => true);
const modified: typeof projected = projected.$ast.modify((node) => node);
const valueAst = table.Id.add(1).$ast;
aggregate("SUM", table.Id).over().partitionBy(table.Name).orderBy(table.Id.desc()).as("total");
stringAgg(table.Name, ",").orderBy(table.Id).as("names");
table.Name.cast(exprTypeInt32).gt(0);
table.Name.jsonValue("$.id").as("id");
mergeInto(table, table.as("s")).on(table.Id.eq(1)).whenMatchedDelete().toSql("tsql");
// @ts-expect-error THEN requires a preceding WHEN.
caseWhen(table.Id.eq(1)).when(table.Id.eq(2));
// @ts-expect-error CASE must have a result before ELSE.
caseWhen(table.Id.eq(1)).else(null);
// @ts-expect-error Column comparisons retain descriptor typing.
table.Id.eq("wrong");
// @ts-expect-error A derived query cannot use an unsupported alias type.
select(table.Id).as(1);
void [projected, cases, id, queryAst, found, modified, valueAst];
