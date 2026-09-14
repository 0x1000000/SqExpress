import {
  exprAggregateFunction, exprAggregateOverFunction, exprAlias, exprAliasedColumn, exprAliasedColumnName, exprAliasedSelecting, exprBoolLiteral, exprBooleanEq, exprByteArrayLiteral, exprCase, exprCaseWhenThen, exprCast, exprColumn, exprColumnAlias, exprColumnName, exprDatabaseName, exprDbSchema,
  exprBooleanAnd, exprBooleanGt, exprBooleanGtEq, exprBooleanLt, exprBooleanLtEq, exprBooleanNot, exprBooleanNotEq, exprBooleanOr,
  exprColumnSetClause, exprCrossedTable, exprCteQuery, exprDefault, exprDelete, exprDeleteOutput, exprDerivedTableValues, exprDiv, exprDoubleLiteral, exprExists, exprIdentityInsert, exprInSubQuery, exprInValues, exprInsert, exprInsertOutput, exprInsertQuery, exprInsertValueRow, exprInsertValues,
  exprFunctionName, exprInt32Literal, exprInt64Literal, exprJoinedTable, exprJsonArray, exprJsonMember, exprJsonObject, exprJsonOutputColumn, exprJsonQuery, exprJsonRemove, exprJsonSet, exprJsonTableValueColumn, exprJsonValue, exprModulo, exprMul, exprNull,
  exprDerivedTableQuery, exprExprMergeNotMatchedInsert, exprExprMergeNotMatchedInsertDefault, exprMerge, exprMergeMatchedDelete, exprMergeMatchedUpdate, exprMergeOutput, exprOffsetFetch, exprOrderBy, exprOrderByItem, exprOrderByOffsetFetch, exprOutput, exprOutputAction, exprOutputColumn, exprOutputColumnDeleted, exprOutputColumnInserted, exprParameter, exprPortableScalarFunction, exprQueryAsJson, exprQueryExpression, exprQuerySpecification, exprSchemaName, exprSelect, exprSelectOffsetFetch,
  exprOver, exprScalarFunction, exprSelectingValue, exprStringAgg, exprStringConcat, exprStringLiteral, exprSub, exprSum, exprTable, exprTableAlias, exprTableFullName, exprTableFunction, exprTableValueConstructor, exprAliasedTableFunction, exprTableName, exprUnsafeValue, exprUpdate, exprValueQuery, exprValueRow, type Expr, type ExprAggregateFunction, type ExprBoolean, type ExprColumn, type ExprType,
  type ExprJsonOutputColumn, type ExprOrderByItem, type ExprQuerySpecification, type ExprValue, type IExprComplete, type IExprMergeMatched, type IExprMergeNotMatched, type IExprSelecting, type IExprTableSource, type PortableScalarFunction
} from "../ast/generated/ast.generated.js";
import type { ColumnDefinitions, ColumnRef, ColumnsOf, ColumnValue, DirectColumnsOf, TableDescriptor, ValueOfSqlType } from "../descriptors/index.js";
import { column, sqlType } from "../descriptors/index.js";

export type PrimitiveValue = string | number | bigint | boolean | Uint8Array | null;
type AnyColumn = ColumnRef<string, Exclude<PrimitiveValue, null>, boolean, string>;
export type ValueInput = ExprValue | AnyColumn | PrimitiveValue;
type InputValue<T> = T extends AnyColumn ? ColumnValue<T> : T extends PrimitiveValue ? T : unknown;
export type ProjectionRow<P extends Readonly<Record<string, ValueInput>>> = { readonly [K in keyof P]: InputValue<P[K]> };
export interface BuiltQuery<Row> { readonly ast: Expr; readonly __row?: Row; }

export function columnExpression<C extends ColumnRef<string, unknown, boolean, string>>(column: C): ExprColumn & { readonly __value?: ColumnValue<C> } {
  return exprColumn({ source: exprTableAlias({ alias: exprAlias({ name: column.sourceAlias }) }), columnName: exprColumnName({ name: column.name }) });
}
type CompatibleColumnValue<C extends AnyColumn> = ColumnValue<C> | ExprValue | ColumnRef<string, ColumnValue<C>, boolean, string>;
export type FluentBoolean = ExprBoolean & {
  and(right: ExprBoolean): FluentBoolean;
  or(right: ExprBoolean): FluentBoolean;
  not(): FluentBoolean;
};
function fluentBoolean(node: ExprBoolean): FluentBoolean {
  const result = { ...node } as ExprBoolean & Partial<FluentBoolean>;
  Object.defineProperties(result, {
    and: { enumerable: false, value: (right: ExprBoolean) => fluentBoolean(exprBooleanAnd({ left: result as ExprBoolean, right })) },
    or: { enumerable: false, value: (right: ExprBoolean) => fluentBoolean(exprBooleanOr({ left: result as ExprBoolean, right })) },
    not: { enumerable: false, value: () => fluentBoolean(exprBooleanNot({ expr: result as ExprBoolean })) }
  });
  return Object.freeze(result) as FluentBoolean;
}
export function eq<C extends AnyColumn>(left: C, right: ColumnRef<string, Exclude<ColumnValue<C>, null>, boolean, string>): FluentBoolean;
export function eq<C extends AnyColumn>(left: C, right: CompatibleColumnValue<C>): FluentBoolean;
export function eq<C extends AnyColumn>(left: CompatibleColumnValue<C>, right: C): FluentBoolean;
export function eq(left: PrimitiveValue | ExprValue, right: PrimitiveValue | ExprValue): FluentBoolean;
export function eq(left: unknown, right: unknown): FluentBoolean {
  // Every public overload restricts both arguments to ValueInput. The broad
  // implementation signature is required because ColumnValue<C> is conditional.
  return fluentBoolean(exprBooleanEq({ left: toExpr(left as ValueInput), right: toExpr(right as ValueInput) }));
}
export function notEq<C extends AnyColumn>(left: C, right: CompatibleColumnValue<C>): FluentBoolean;
export function notEq<C extends AnyColumn>(left: CompatibleColumnValue<C>, right: C): FluentBoolean;
export function notEq(left: PrimitiveValue | ExprValue, right: PrimitiveValue | ExprValue): FluentBoolean;
export function notEq(left: unknown, right: unknown): FluentBoolean { return fluentBoolean(exprBooleanNotEq({ left: toExpr(left as ValueInput), right: toExpr(right as ValueInput) })); }
export const neq = notEq;
export function gt<C extends AnyColumn>(left: C, right: CompatibleColumnValue<C>): FluentBoolean;
export function gt<C extends AnyColumn>(left: CompatibleColumnValue<C>, right: C): FluentBoolean;
export function gt(left: PrimitiveValue | ExprValue, right: PrimitiveValue | ExprValue): FluentBoolean;
export function gt(left: unknown, right: unknown): FluentBoolean { return fluentBoolean(exprBooleanGt({ left: toExpr(left as ValueInput), right: toExpr(right as ValueInput) })); }
export function gte<C extends AnyColumn>(left: C, right: CompatibleColumnValue<C>): FluentBoolean;
export function gte<C extends AnyColumn>(left: CompatibleColumnValue<C>, right: C): FluentBoolean;
export function gte(left: PrimitiveValue | ExprValue, right: PrimitiveValue | ExprValue): FluentBoolean;
export function gte(left: unknown, right: unknown): FluentBoolean { return fluentBoolean(exprBooleanGtEq({ left: toExpr(left as ValueInput), right: toExpr(right as ValueInput) })); }
export function lt<C extends AnyColumn>(left: C, right: CompatibleColumnValue<C>): FluentBoolean;
export function lt<C extends AnyColumn>(left: CompatibleColumnValue<C>, right: C): FluentBoolean;
export function lt(left: PrimitiveValue | ExprValue, right: PrimitiveValue | ExprValue): FluentBoolean;
export function lt(left: unknown, right: unknown): FluentBoolean { return fluentBoolean(exprBooleanLt({ left: toExpr(left as ValueInput), right: toExpr(right as ValueInput) })); }
export function lte<C extends AnyColumn>(left: C, right: CompatibleColumnValue<C>): FluentBoolean;
export function lte<C extends AnyColumn>(left: CompatibleColumnValue<C>, right: C): FluentBoolean;
export function lte(left: PrimitiveValue | ExprValue, right: PrimitiveValue | ExprValue): FluentBoolean;
export function lte(left: unknown, right: unknown): FluentBoolean { return fluentBoolean(exprBooleanLtEq({ left: toExpr(left as ValueInput), right: toExpr(right as ValueInput) })); }
export function and(left: ExprBoolean, right: ExprBoolean, ...rest: ReadonlyArray<ExprBoolean>): FluentBoolean { return fluentBoolean(rest.reduce((result, item) => exprBooleanAnd({ left: result, right: item }), exprBooleanAnd({ left, right }))); }
export function or(left: ExprBoolean, right: ExprBoolean, ...rest: ReadonlyArray<ExprBoolean>): FluentBoolean { return fluentBoolean(rest.reduce((result, item) => exprBooleanOr({ left: result, right: item }), exprBooleanOr({ left, right }))); }
export function not(value: ExprBoolean): FluentBoolean { return fluentBoolean(exprBooleanNot({ expr: value })); }
export function joinAnd(values: ReadonlyArray<ExprBoolean>): FluentBoolean { if (values.length === 0) throw new TypeError("joinAnd requires at least one predicate."); return values.slice(1).reduce<FluentBoolean>((left, right) => fluentBoolean(exprBooleanAnd({ left, right })), fluentBoolean(values[0]!)); }
export function joinOr(values: ReadonlyArray<ExprBoolean>): FluentBoolean { if (values.length === 0) throw new TypeError("joinOr requires at least one predicate."); return values.slice(1).reduce<FluentBoolean>((left, right) => fluentBoolean(exprBooleanOr({ left, right })), fluentBoolean(values[0]!)); }
export function add(left: ValueInput, right: ValueInput): ExprValue { return exprSum({ left: toExpr(left), right: toExpr(right) }); }
export function subtract(left: ValueInput, right: ValueInput): ExprValue { return exprSub({ left: toExpr(left), right: toExpr(right) }); }
export function multiply(left: ValueInput, right: ValueInput): ExprValue { return exprMul({ left: toExpr(left), right: toExpr(right) }); }
export function divide(left: ValueInput, right: ValueInput): ExprValue { return exprDiv({ left: toExpr(left), right: toExpr(right) }); }
export function modulo(left: ValueInput, right: ValueInput): ExprValue { return exprModulo({ left: toExpr(left), right: toExpr(right) }); }
export function concat(left: ValueInput, right: ValueInput): ExprValue { return exprStringConcat({ left: toExpr(left), right: toExpr(right) }); }
export const defaultValue = exprDefault;
export function inList(test: ValueInput, first: ValueInput, ...rest: ReadonlyArray<ValueInput>): ExprBoolean { return exprInValues({ testExpression: toExpr(test), items: [toExpr(first), ...rest.map(toExpr)] }); }
export function inQuery(test: ValueInput, query: BuiltQuery<unknown>): ExprBoolean { return exprInSubQuery({ testExpression: toExpr(test), subQuery: asSubQuery(query) }); }
export function exists(query: BuiltQuery<unknown>): ExprBoolean { return exprExists({ subQuery: asSubQuery(query) }); }
export function asc(value: ValueInput): ExprOrderByItem { return exprOrderByItem({ value: toExpr(value), descendant: false }); }
export function desc(value: ValueInput): ExprOrderByItem { return exprOrderByItem({ value: toExpr(value), descendant: true }); }
export function isValidBuiltInFunctionName(name: string): boolean { return /^(?:@@?)?[\p{L}][\p{L}\p{N}_]*$/u.test(name); }
export function call(name: string, ...args: ReadonlyArray<ValueInput>): ExprValue { if (!isValidBuiltInFunctionName(name)) throw new TypeError(`Invalid built-in SQL function name '${name}'.`); return exprScalarFunction({ schema: null, name: exprFunctionName({ name, builtIn: true }), arguments: args.length === 0 ? null : args.map(toExpr) }); }
export function callCustom(name: string, args: ReadonlyArray<ValueInput> = [], options: { readonly schema?: string; readonly database?: string } = {}): ExprValue {
  if (name.length === 0) throw new TypeError("Custom SQL function name cannot be empty.");
  const schema = options.schema === undefined ? null : exprDbSchema({ database: options.database === undefined ? null : exprDatabaseName({ name: options.database }), schema: exprSchemaName({ name: options.schema }) });
  return exprScalarFunction({ schema, name: exprFunctionName({ name, builtIn: false }), arguments: args.length === 0 ? null : args.map(toExpr) });
}
export function portable(name: PortableScalarFunction, ...args: ReadonlyArray<ValueInput>): ExprValue { return exprPortableScalarFunction({ portableFunction: name, arguments: args.length === 0 ? null : args.map(toExpr) }); }
export function scalarSubquery(query: BuiltQuery<unknown>): ExprValue { return exprValueQuery({ query: asSubQuery(query) }); }
export function unsafeSql(sql: string): ExprValue { return exprUnsafeValue({ unsafeValue: sql }); }
export type AggregateValue = ExprValue & {
  over(options?: { readonly partitionBy?: ReadonlyArray<ValueInput>; readonly orderBy?: ReadonlyArray<ValueInput | ExprOrderByItem> }): ExprValue;
};
export function aggregate(name: "COUNT" | "SUM" | "AVG" | "MIN" | "MAX", value: ValueInput, distinct = false): AggregateValue {
  const aggregateNode = exprAggregateFunction({ name: exprFunctionName({ name, builtIn: true }), expression: toExpr(value), isDistinct: distinct });
  const result = { ...exprSelectingValue({ selecting: aggregateNode }) } as ExprValue & Partial<AggregateValue>;
  Object.defineProperty(result, "over", { enumerable: false, value: (options: { readonly partitionBy?: ReadonlyArray<ValueInput>; readonly orderBy?: ReadonlyArray<ValueInput | ExprOrderByItem> } = {}) => exprSelectingValue({ selecting: exprAggregateOverFunction({ function: aggregateNode, over: exprOver({ partitions: options.partitionBy?.map(toExpr) ?? null, orderBy: options.orderBy === undefined ? null : exprOrderBy({ orderList: options.orderBy.map((item) => typeof item === "object" && item !== null && "kind" in item && item.kind === "ExprOrderByItem" ? item : asc(item as ValueInput)) }), frameClause: null }) }) }) });
  return Object.freeze(result) as AggregateValue;
}
export function stringAgg(value: ValueInput, separator: ValueInput, ...orderBy: ReadonlyArray<ValueInput | ExprOrderByItem>): ExprValue { return exprSelectingValue({ selecting: exprStringAgg({ expression: toExpr(value), separator: toExpr(separator), orderBy: orderBy.length === 0 ? null : exprOrderBy({ orderList: orderBy.map((item) => typeof item === "object" && item !== null && "kind" in item && item.kind === "ExprOrderByItem" ? item : asc(item as ValueInput)) }) }) }); }
export function cast(value: ValueInput, sqlType: ExprType): ExprValue { return exprCast({ expression: toExpr(value), sqlType }); }
export function caseWhen(first: readonly [ExprBoolean, ValueInput], ...rest: ReadonlyArray<readonly [ExprBoolean, ValueInput]>) { return { else(defaultValue: ValueInput): ExprValue { return exprCase({ cases: [first, ...rest].map(([condition, value]) => exprCaseWhenThen({ condition, value: toExpr(value) })), defaultValue: toExpr(defaultValue) }); } }; }
const validateJsonPath = (path: string): string => { if (!/^\$(?:(?:\.(?:[\p{L}_][\p{L}\p{N}_]*|"(?:[^"\\]|\\["\\/bfnrt])+")|\[\d+\]))*$/u.test(path)) throw new TypeError(`Invalid portable JSON path '${path}'.`); return path; };
export function jsonValue(document: ValueInput, path: string, returningType: ExprType | null = null): ExprValue { return exprJsonValue({ document: toExpr(document), path: validateJsonPath(path), returningType }); }
export function jsonQuery(document: ValueInput, path: string = "$"): ExprValue { return exprJsonQuery({ document: toExpr(document), path: validateJsonPath(path) }); }
export function jsonSet(document: ValueInput, path: string, value: ValueInput): ExprValue { return exprJsonSet({ document: toExpr(document), path: validateJsonPath(path), value: toExpr(value) }); }
export function jsonRemove(document: ValueInput, path: string): ExprValue { if (path === "$") throw new TypeError("Removing the JSON root is not supported."); return exprJsonRemove({ document: toExpr(document), path: validateJsonPath(path) }); }
export function jsonArray(...items: ReadonlyArray<ValueInput>): ExprValue { return exprJsonArray({ items: items.map(toExpr) }); }
export function jsonObject(members: Readonly<Record<string, ValueInput>>): ExprValue;
export function jsonObject(...members: ReadonlyArray<readonly [string, ValueInput]>): ExprValue;
export function jsonObject(first: Readonly<Record<string, ValueInput>> | readonly [string, ValueInput], ...rest: ReadonlyArray<readonly [string, ValueInput]>): ExprValue {
  const entries: ReadonlyArray<readonly [string, ValueInput]> = Array.isArray(first) ? [first as readonly [string, ValueInput], ...rest] : Object.entries(first) as ReadonlyArray<readonly [string, ValueInput]>;
  const names = entries.map(([name]) => name); if (new Set(names).size !== names.length) throw new TypeError("JSON object keys must be unique.");
  return exprJsonObject({ members: entries.map(([name, value]) => exprJsonMember({ name, value: toExpr(value) })) });
}
export function jsonOutput(value: ValueInput, path: string): ExprJsonOutputColumn { return exprJsonOutputColumn({ value: toExpr(value), jsonPath: validateJsonPath(path) }); }
export function jsonTableValueColumn(name: string, path: string, sqlType: ExprType) { return exprJsonTableValueColumn({ name: exprColumnName({ name }), path: validateJsonPath(path), sqlType }); }
export function selectJson(first: ExprJsonOutputColumn, ...rest: ReadonlyArray<ExprJsonOutputColumn>): BuiltQuery<string> { return { ast: exprQuerySpecification({ selectList: [first, ...rest], top: null, from: null, where: null, groupBy: null, distinct: false }) }; }
export function forJson(query: BuiltQuery<unknown>, options: { readonly withoutArrayWrapper?: boolean; readonly includeNullValues?: boolean } = {}): BuiltQuery<string> { return { ast: exprQueryAsJson({ query: asSubQuery(query), withoutArrayWrapper: options.withoutArrayWrapper ?? false, includeNullValues: options.includeNullValues ?? true }) }; }

export function lit(value: PrimitiveValue): ExprValue { return toExpr(value); }
export function param(value: PrimitiveValue, name: string | null = null): ExprValue { return exprParameter({ replacedValue: toExpr(value), tagName: name }); }
export function toExpr(value: ValueInput): ExprValue {
  if (typeof value === "object" && value !== null && "kind" in value) return value;
  if (typeof value === "object" && value !== null && "sourceAlias" in value) return columnExpression(value);
  if (value instanceof Uint8Array) return exprByteArrayLiteral({ value: Uint8Array.from(value) });
  if (value === null) return exprNull;
  if (typeof value === "string") return exprStringLiteral({ value });
  if (typeof value === "boolean") return exprBoolLiteral({ value });
  if (typeof value === "bigint") return exprInt64Literal({ value });
  if (!Number.isFinite(value)) return exprDoubleLiteral({ value });
  if (Number.isInteger(value) && value >= -2147483648 && value <= 2147483647) return exprInt32Literal({ value });
  return exprDoubleLiteral({ value });
}
/** @deprecated Use {@link toExpr}. */
export const valueExpression = toExpr;

type AnyTable = TableDescriptor<string | null, string, ColumnDefinitions, string>;
export type DerivedTableDescriptor<A extends string, D extends ColumnDefinitions> = TableDescriptor<null, A, D, A> & { readonly derivedSource: IExprTableSource };
type TableSourceInput = AnyTable | DerivedTableDescriptor<string, ColumnDefinitions>;
export interface SelectStart<Row> { from(table: TableSourceInput): SelectFrom<Row>; done(): BuiltQuery<Row>; }
export interface SelectFrom<Row> {
  innerJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>; leftJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>;
  rightJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>; fullJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>;
  crossJoin(table: TableSourceInput): SelectFrom<Row>; where(predicate: ExprBoolean | null): SelectFiltered<Row>;
  groupBy(...values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; groupByList(values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; orderBy(...items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; orderByList(items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; done(): BuiltQuery<Row>;
}
export interface SelectFiltered<Row> { groupBy(...values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; groupByList(values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; orderBy(...items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; orderByList(items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; done(): BuiltQuery<Row>; }
export interface SelectGrouped<Row> { orderBy(...items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; orderByList(items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; done(): BuiltQuery<Row>; }
export interface SelectOrdered<Row> { offsetFetch(offset: ValueInput, fetch?: ValueInput): SelectFinal<Row>; done(): BuiltQuery<Row>; }
export interface SelectFinal<Row> { done(): BuiltQuery<Row>; }

export function select<const P extends Readonly<Record<string, ValueInput>>>(projection: P, options: { readonly distinct?: boolean; readonly top?: ValueInput } = {}): SelectStart<ProjectionRow<P>> {
  const list: IExprSelecting[] = Object.entries(projection).map(([alias, input]) => exprAliasedSelecting({ value: toExpr(input), alias: exprColumnAlias({ name: alias }) }));
  type Row = ProjectionRow<P>;
  const finish = (from: IExprTableSource | null, where: ExprBoolean | null, groupBy: ReadonlyArray<ExprValue> | null, order: ReadonlyArray<ExprOrderByItem> | null, page: { offset: ExprValue; fetch: ExprValue | null } | null): BuiltQuery<Row> => {
    const query = exprQuerySpecification({ selectList: list, top: options.top === undefined ? null : toExpr(options.top), from, where, groupBy, distinct: options.distinct ?? false });
    return { ast: order === null ? query : page === null ? exprSelect({ selectQuery: query, orderBy: exprOrderBy({ orderList: order }) }) : exprSelectOffsetFetch({ selectQuery: query, orderBy: exprOrderByOffsetFetch({ orderList: order, offsetFetch: exprOffsetFetch(page) }) }) };
  };
  const groups = (values: ReadonlyArray<ValueInput>): ReadonlyArray<ExprValue> => { if (values.length === 0) throw new TypeError("GROUP BY requires at least one expression."); return values.map(toExpr); };
  const ordered = (from: IExprTableSource, where: ExprBoolean | null, groupBy: ReadonlyArray<ExprValue> | null, items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row> => { if (items.length === 0) throw new TypeError("ORDER BY requires at least one expression."); const order = items.map((item) => typeof item === "object" && item !== null && "kind" in item && item.kind === "ExprOrderByItem" ? item : asc(item as ValueInput)); return { done: () => finish(from, where, groupBy, order, null), offsetFetch: (offset, fetch) => ({ done: () => finish(from, where, groupBy, order, { offset: toExpr(offset), fetch: fetch === undefined ? null : toExpr(fetch) }) }) }; };
  const grouped = (from: IExprTableSource, where: ExprBoolean | null, values: ReadonlyArray<ValueInput>): SelectGrouped<Row> => { const group = groups(values); return { done: () => finish(from, where, group, null, null), orderBy: (...items) => ordered(from, where, group, items), orderByList: (items) => ordered(from, where, group, items) }; };
  const afterFilter = (from: IExprTableSource, where: ExprBoolean | null): SelectFiltered<Row> => ({ done: () => finish(from, where, null, null, null), groupBy: (...values) => grouped(from, where, values), groupByList: (values) => grouped(from, where, values), orderBy: (...items) => ordered(from, where, null, items), orderByList: (items) => ordered(from, where, null, items) });
  const fromStage = (from: IExprTableSource): SelectFrom<Row> => ({
    done: () => finish(from, null, null, null, null), where: (predicate) => afterFilter(from, predicate),
    groupBy: (...values) => grouped(from, null, values), groupByList: (values) => grouped(from, null, values), orderBy: (...items) => ordered(from, null, null, items), orderByList: (items) => ordered(from, null, null, items),
    innerJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Inner" })),
    leftJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Left" })),
    rightJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Right" })),
    fullJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Full" })),
    crossJoin: (table) => fromStage(exprCrossedTable({ left: from, right: tableSourceExpression(table) })),
  });
  return { done: () => finish(null, null, null, null, null), from: (table) => fromStage(tableSourceExpression(table)) };
}

function tableExpression<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(table: TableDescriptor<S, N, D, A>) {
  return exprTable({ fullName: exprTableFullName({ dbSchema: table.schema === null ? null : exprDbSchema({ database: table.database === null ? null : exprDatabaseName({ name: table.database }), schema: exprSchemaName({ name: table.schema }) }), tableName: exprTableName({ name: table.name }) }), alias: String(table.alias) === String(table.name) ? null : exprTableAlias({ alias: exprAlias({ name: table.alias }) }) });
}
function tableSourceExpression(table: TableSourceInput): IExprTableSource { return "derivedSource" in table ? table.derivedSource : tableExpression(table); }
export function tableSource(table: TableSourceInput): IExprTableSource { return tableSourceExpression(table); }

export function derivedTable<const A extends string, const D extends ColumnDefinitions>(query: BuiltQuery<unknown>, alias: A, definitions: D): DerivedTableDescriptor<A, D> {
  if (!query.ast || !(query.ast.kind === "ExprQuerySpecification" || query.ast.kind === "ExprQueryExpression" || query.ast.kind === "ExprSelectOffsetFetch")) throw new TypeError("Derived table source must be a subquery AST.");
  const selected = query.ast.kind === "ExprQuerySpecification" ? query.ast.selectList.length : query.ast.kind === "ExprSelectOffsetFetch" && query.ast.selectQuery.kind === "ExprQuerySpecification" ? query.ast.selectQuery.selectList.length : null;
  if (selected !== null && selected !== Object.keys(definitions).length) throw new TypeError("Number of declared columns does not match the number of selected columns in the derived table subquery.");
  const refs: Record<string, ColumnRef<string, unknown, boolean, A>> = {};
  for (const [name, definition] of Object.entries(definitions)) refs[name] = Object.freeze({ name, sourceAlias: alias, nullable: definition.nullable });
  const source = exprDerivedTableQuery({ query: query.ast, alias: exprTableAlias({ alias: exprAlias({ name: alias }) }), columns: Object.keys(definitions).map((name) => exprColumnName({ name })) });
  return virtualTable(alias, definitions, refs, source);
}
export function dynamicDerivedTable<const A extends string>(query: BuiltQuery<unknown>, alias: A): DerivedTableDescriptor<A, ColumnDefinitions> {
  const subQuery = asSubQuery(query); const selection = subQuery.kind === "ExprQuerySpecification" ? subQuery.selectList : subQuery.kind === "ExprSelectOffsetFetch" && subQuery.selectQuery.kind === "ExprQuerySpecification" ? subQuery.selectQuery.selectList : null;
  if (selection === null) throw new TypeError("Dynamic derived tables require a query specification with discoverable projection names.");
  const names = selection.map((item, index) => item.kind === "ExprAliasedSelecting" ? item.alias.name : item.kind === "ExprAliasedColumn" ? item.alias?.name ?? item.column.columnName.name : item.kind === "ExprColumn" ? item.columnName.name : `Expr${index + 1}`);
  const definitions: Record<string, ReturnType<typeof column>> = {}; for (const name of names) { if (name in definitions) throw new TypeError(`Duplicate derived column '${name}'.`); definitions[name] = column(sqlType.string()); }
  const source = exprDerivedTableQuery({ query: subQuery, alias: exprTableAlias({ alias: exprAlias({ name: alias }) }), columns: null });
  return virtualDescriptor(alias, Object.freeze(definitions), source);
}

export function tableFunction<const A extends string, const D extends ColumnDefinitions>(
  name: string,
  args: ReadonlyArray<ValueInput>,
  alias: A,
  definitions: D,
  options: { readonly schema?: string; readonly database?: string } = {},
): DerivedTableDescriptor<A, D> {
  const schema = options.schema === undefined ? null : exprDbSchema({
    database: options.database === undefined ? null : exprDatabaseName({ name: options.database }),
    schema: exprSchemaName({ name: options.schema }),
  });
  const fn = exprTableFunction({ schema, name: exprFunctionName({ name, builtIn: false }), arguments: args.length === 0 ? null : args.map(toExpr) });
  return virtualDescriptor(alias, definitions, exprAliasedTableFunction({ function: fn, alias: exprTableAlias({ alias: exprAlias({ name: alias }) }) }));
}

export function values<const A extends string, const D extends ColumnDefinitions>(
  rows: ReadonlyArray<ReadonlyArray<ValueInput>>,
  alias: A,
  definitions: D,
): DerivedTableDescriptor<A, D> {
  const names = Object.keys(definitions);
  if (rows.length === 0) throw new TypeError("VALUES requires at least one row.");
  for (const [index, row] of rows.entries()) if (row.length !== names.length) throw new TypeError(`VALUES row ${index + 1} has ${row.length} values; expected ${names.length}.`);
  const source = exprDerivedTableValues({
    values: exprTableValueConstructor({ items: rows.map(row => exprValueRow({ items: row.map(toExpr) })) }),
    alias: exprTableAlias({ alias: exprAlias({ name: alias }) }),
    columns: names.map(name => exprColumnName({ name })),
  });
  return virtualDescriptor(alias, definitions, source);
}

function virtualDescriptor<const A extends string, const D extends ColumnDefinitions>(name: A, definitions: D, derivedSource: IExprTableSource): DerivedTableDescriptor<A, D> {
  const refs: Record<string, ColumnRef<string, unknown, boolean, A>> = {};
  for (const [columnName, definition] of Object.entries(definitions)) refs[columnName] = Object.freeze({ name: columnName, sourceAlias: name, nullable: definition.nullable });
  return virtualTable(name, definitions, refs, derivedSource);
}

function virtualTable<A extends string, D extends ColumnDefinitions>(name: A, definitions: D, refs: Record<string, ColumnRef<string, unknown, boolean, A>>, derivedSource: IExprTableSource): DerivedTableDescriptor<A, D> {
  const reserved = new Set(["database", "schema", "name", "alias", "definitions", "columns", "dynamic", "derivedSource"]); const direct: Record<string, ColumnRef<string, unknown, boolean, A>> = {};
  for (const [columnName, ref] of Object.entries(refs)) { const property = reserved.has(columnName) ? `$${columnName}` : columnName; if (property !== columnName && property in definitions) throw new TypeError(`Column '${columnName}' escapes to '${property}', which is also a column name.`); direct[property] = ref; }
  // The loop creates exactly one reference for every key in D and applies the
  // same reserved-name transformation represented by DirectColumnsOf.
  const columns = Object.freeze(refs) as ColumnsOf<D, A>;
  const directColumns = direct as DirectColumnsOf<D, A>;
  const descriptor: TableDescriptor<null, A, D, A> = Object.assign({ database: null, schema: null, name, alias: name, definitions, columns }, directColumns);
  return Object.freeze(Object.assign(descriptor, { derivedSource })) as DerivedTableDescriptor<A, D>;
}

export function cte<const N extends string, const D extends ColumnDefinitions>(name: N, definitions: D, build: (self: DerivedTableDescriptor<N, D>) => BuiltQuery<unknown>): DerivedTableDescriptor<N, D> {
  const empty = exprQuerySpecification({ selectList: [], top: null, from: null, where: null, groupBy: null, distinct: false });
  const self = virtualDescriptor(name, definitions, exprCteQuery({ name, query: empty, alias: null }));
  const query = asSubQuery(build(self));
  return virtualDescriptor(name, definitions, exprCteQuery({ name, query, alias: null }));
}

function asSubQuery(query: BuiltQuery<unknown>): Extract<Expr, { readonly kind: "ExprQuerySpecification" | "ExprQueryExpression" | "ExprSelectOffsetFetch" }> {
  if (query.ast.kind === "ExprQuerySpecification" || query.ast.kind === "ExprQueryExpression" || query.ast.kind === "ExprSelectOffsetFetch") return query.ast;
  throw new TypeError("Set operation requires query operands.");
}
function setOperation<L, R>(left: BuiltQuery<L>, right: BuiltQuery<R>, type: "Union" | "UnionAll" | "Except" | "Intersect"): BuiltQuery<L | R> { return { ast: exprQueryExpression({ left: asSubQuery(left), right: asSubQuery(right), queryExpressionType: type }) }; }
export const union = <L, R>(left: BuiltQuery<L>, right: BuiltQuery<R>) => setOperation(left, right, "Union");
export const unionAll = <L, R>(left: BuiltQuery<L>, right: BuiltQuery<R>) => setOperation(left, right, "UnionAll");
export const except = <L, R>(left: BuiltQuery<L>, right: BuiltQuery<R>) => setOperation(left, right, "Except");
export const intersect = <L, R>(left: BuiltQuery<L>, right: BuiltQuery<R>) => setOperation(left, right, "Intersect");
export function orderQuery<Row>(query: BuiltQuery<Row>, items: ReadonlyArray<ValueInput | ExprOrderByItem>, pagination?: { readonly offset: ValueInput; readonly fetch?: ValueInput }): BuiltQuery<Row> {
  if (items.length === 0) throw new TypeError("ORDER BY requires at least one expression.");
  const orderList = items.map(item => typeof item === "object" && item !== null && "kind" in item && item.kind === "ExprOrderByItem" ? item : asc(item as ValueInput));
  return { ast: pagination === undefined ? exprSelect({ selectQuery: asSubQuery(query), orderBy: exprOrderBy({ orderList }) }) : exprSelectOffsetFetch({ selectQuery: asSubQuery(query), orderBy: exprOrderByOffsetFetch({ orderList, offsetFetch: exprOffsetFetch({ offset: toExpr(pagination.offset), fetch: pagination.fetch === undefined ? null : toExpr(pagination.fetch) }) }) }) };
}

type AssignableValue<T> = T | ExprValue | ColumnRef<string, Exclude<T, null>, boolean, string>;
export type InsertRow<D extends ColumnDefinitions> = { readonly [K in keyof D]: AssignableValue<ValueOfSqlType<D[K]["sqlType"]> | (D[K]["nullable"] extends true ? null : never)> };
export type UpdateSet<D extends ColumnDefinitions> = { readonly [K in keyof D]?: AssignableValue<ValueOfSqlType<D[K]["sqlType"]> | (D[K]["nullable"] extends true ? null : never)> | typeof exprDefault };
export interface BuiltStatement { readonly ast: IExprComplete; }
export interface DeleteStatement extends BuiltStatement { output(first: AnyColumn, ...rest: ReadonlyArray<AnyColumn>): BuiltStatement; }
export interface InsertStatement extends BuiltStatement {
  output(first: AnyColumn, ...rest: ReadonlyArray<AnyColumn>): BuiltStatement;
  identity(first: string, ...rest: ReadonlyArray<string>): BuiltStatement;
}

export function insertInto<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(table: TableDescriptor<S, N, D, A>) {
  const finish = (insert: Extract<Expr, { readonly kind: "ExprInsert" }>): InsertStatement => ({
    ast: insert,
    output: (first, ...rest) => ({ ast: exprInsertOutput({ insert, outputColumns: [first, ...rest].map(item => exprAliasedColumnName({ column: exprColumnName({ name: item.name }), alias: null })) }) }),
    identity: (first, ...rest) => ({ ast: exprIdentityInsert({ insert, identityColumns: [first, ...rest].map(name => exprColumnName({ name })) }) }),
  });
  return { from: (query: BuiltQuery<unknown>, first: keyof D & string, ...rest: ReadonlyArray<keyof D & string>): InsertStatement => {
    const names = [first, ...rest];
    return finish(exprInsert({ target: tableExpression(table).fullName, targetColumns: names.map((name) => exprColumnName({ name })), source: exprInsertQuery({ query: asSubQuery(query) }) }));
  }, values: (first: InsertRow<D>, ...rest: ReadonlyArray<InsertRow<D>>): InsertStatement => {
    const names = Object.keys(table.definitions); const rows = [first, ...rest];
    for (const row of rows) { const actual = Object.keys(row); if (actual.length !== names.length || names.some((name) => !Object.prototype.hasOwnProperty.call(row, name))) throw new TypeError(`Insert row must define exactly: ${names.join(", ")}`); }
    return finish(exprInsert({ target: tableExpression(table).fullName, targetColumns: names.map((name) => exprColumnName({ name })), source: exprInsertValues({ items: rows.map((row) => exprInsertValueRow({ items: names.map((name) => toExpr(row[name] as ValueInput)) })) }) }));
  } };
}

export function update<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(table: TableDescriptor<S, N, D, A>) {
  const target = tableExpression(table);
  return { set(values: UpdateSet<D>) {
    // Object.entries loses the mapped value type, while UpdateSet<D> ensures it
    // is a supported assigning value for the corresponding descriptor column.
    const clauses = Object.entries(values).map(([name, value]) => exprColumnSetClause({ column: columnExpression(table.columns[name]!), value: value === exprDefault ? exprDefault : toExpr(value as ValueInput) }));
    if (clauses.length === 0) throw new TypeError("UPDATE requires at least one SET assignment.");
    const finish = (source: IExprTableSource | null, filter: ExprBoolean | null): BuiltStatement => ({ ast: exprUpdate({ target, setClause: clauses, source, filter }) });
    const fromStage = (source: IExprTableSource) => ({
      done: () => finish(source, null), where: (filter: ExprBoolean | null) => finish(source, filter),
      innerJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Inner" })),
      leftJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Left" })),
      rightJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Right" })),
      fullJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Full" })),
      crossJoin: (right: TableSourceInput) => fromStage(exprCrossedTable({ left: source, right: tableSourceExpression(right) })),
    });
    return { done: () => finish(null, null), where: (filter: ExprBoolean | null) => finish(null, filter), from: (source: TableSourceInput) => fromStage(tableSourceExpression(source)) };
  } };
}

export function deleteFrom<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(table: TableDescriptor<S, N, D, A>) {
  const target = tableExpression(table);
  const finish = (source: IExprTableSource | null, filter: ExprBoolean | null): DeleteStatement => {
    const deletion = exprDelete({ target, source, filter });
    return { ast: deletion, output: (first, ...rest) => ({ ast: exprDeleteOutput({ delete: deletion, outputColumns: [first, ...rest].map(column => exprAliasedColumn({ column: columnExpression(column), alias: null })) }) }) };
  };
  const fromStage = (source: IExprTableSource) => ({
    done: () => finish(source, null), where: (filter: ExprBoolean | null) => finish(source, filter),
    innerJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Inner" })),
    leftJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Left" })),
    rightJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Right" })),
    fullJoin: (right: TableSourceInput, on: ExprBoolean) => fromStage(exprJoinedTable({ left: source, right: tableSourceExpression(right), searchCondition: on, joinType: "Full" })),
    crossJoin: (right: TableSourceInput) => fromStage(exprCrossedTable({ left: source, right: tableSourceExpression(right) })),
  });
  return { done: () => finish(null, null), where: (filter: ExprBoolean | null) => finish(null, filter), from: (source: TableSourceInput) => fromStage(tableSourceExpression(source)) };
}

/** Values assigned by MERGE. Keys remain tied to the target descriptor. */
export type MergeSet<D extends ColumnDefinitions> = UpdateSet<D>;
type OutputColumnInput = AnyColumn | readonly [AnyColumn, string];
export type MergeOutputItem = { readonly kind: "column" | "inserted" | "deleted"; readonly value: OutputColumnInput } | { readonly kind: "action"; readonly alias?: string | null };
export interface MergeOutputSpec { readonly items?: ReadonlyArray<MergeOutputItem>; readonly columns?: ReadonlyArray<OutputColumnInput>; readonly inserted?: ReadonlyArray<OutputColumnInput>; readonly deleted?: ReadonlyArray<OutputColumnInput>; readonly action?: string | null; }
export interface MergeStatement extends BuiltStatement { output(spec: MergeOutputSpec): BuiltStatement; }
export interface MergeBuilder<D extends ColumnDefinitions> {
  whenMatchedUpdate(values: MergeSet<D>, and?: ExprBoolean): MergeBuilder<D>;
  whenMatchedDelete(and?: ExprBoolean): MergeBuilder<D>;
  whenNotMatchedInsert(values: MergeSet<D>, and?: ExprBoolean): MergeBuilder<D>;
  whenNotMatchedInsertDefault(and?: ExprBoolean): MergeBuilder<D>;
  whenNotMatchedBySourceUpdate(values: MergeSet<D>, and?: ExprBoolean): MergeBuilder<D>;
  whenNotMatchedBySourceDelete(and?: ExprBoolean): MergeBuilder<D>;
  done(): MergeStatement;
}

export function mergeInto<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(target: TableDescriptor<S, N, D, A>, source: TableSourceInput) {
  return { on(condition: ExprBoolean): MergeBuilder<D> {
    let matched: IExprMergeMatched | null = null;
    let notMatched: IExprMergeNotMatched | null = null;
    let notMatchedBySource: IExprMergeMatched | null = null;
    const assignments = (values: MergeSet<D>) => Object.entries(values).map(([name, value]) => {
      if (!(name in target.definitions)) throw new TypeError(`Unknown MERGE target column '${name}'.`);
      return exprColumnSetClause({ column: columnExpression(target.columns[name]!), value: toExpr(value as ValueInput) });
    });
    const api: MergeBuilder<D> = {
      whenMatchedUpdate(values, and = undefined) { const set = assignments(values); if (set.length === 0) throw new TypeError("MERGE UPDATE requires at least one assignment."); matched = exprMergeMatchedUpdate({ and: and ?? null, set }); return api; },
      whenMatchedDelete(and = undefined) { matched = exprMergeMatchedDelete({ and: and ?? null }); return api; },
      whenNotMatchedInsert(values, and = undefined) { const set = assignments(values); if (set.length === 0) throw new TypeError("MERGE INSERT requires at least one assignment."); notMatched = exprExprMergeNotMatchedInsert({ and: and ?? null, columns: set.map((item) => item.column.columnName), values: set.map((item) => item.value) }); return api; },
      whenNotMatchedInsertDefault(and = undefined) { notMatched = exprExprMergeNotMatchedInsertDefault({ and: and ?? null }); return api; },
      whenNotMatchedBySourceUpdate(values, and = undefined) { const set = assignments(values); if (set.length === 0) throw new TypeError("MERGE UPDATE requires at least one assignment."); notMatchedBySource = exprMergeMatchedUpdate({ and: and ?? null, set }); return api; },
      whenNotMatchedBySourceDelete(and = undefined) { notMatchedBySource = exprMergeMatchedDelete({ and: and ?? null }); return api; },
      done() { if (matched === null && notMatched === null && notMatchedBySource === null) throw new TypeError("MERGE requires at least one action."); const fields = { targetTable: tableExpression(target), source: tableSourceExpression(source), on: condition, whenMatched: matched, whenNotMatchedByTarget: notMatched, whenNotMatchedBySource: notMatchedBySource }; const merge = exprMerge(fields); const pair = (input: OutputColumnInput): readonly [AnyColumn, string | null] => "name" in input ? [input, null] : input; const aliasedName = (input: OutputColumnInput) => { const [item, alias] = pair(input); return exprAliasedColumnName({ column: exprColumnName({ name: item.name }), alias: alias === null ? null : exprColumnAlias({ name: alias }) }); }; const outputItem = (item: MergeOutputItem) => item.kind === "action" ? exprOutputAction({ alias: item.alias == null ? null : exprColumnAlias({ name: item.alias }) }) : item.kind === "inserted" ? exprOutputColumnInserted({ columnName: aliasedName(item.value) }) : item.kind === "deleted" ? exprOutputColumnDeleted({ columnName: aliasedName(item.value) }) : (() => { const [column, alias] = pair(item.value); return exprOutputColumn({ column: exprAliasedColumn({ column: columnExpression(column), alias: alias === null ? null : exprColumnAlias({ name: alias }) }) }); })(); return { ast: merge, output(spec) { const fallback: MergeOutputItem[] = [...(spec.columns ?? []).map(value => ({ kind: "column" as const, value })), ...(spec.inserted ?? []).map(value => ({ kind: "inserted" as const, value })), ...(spec.deleted ?? []).map(value => ({ kind: "deleted" as const, value })), ...(spec.action === undefined ? [] : [{ kind: "action" as const, alias: spec.action }])]; const columns = (spec.items ?? fallback).map(outputItem); if (columns.length === 0) throw new TypeError("MERGE OUTPUT requires at least one output column."); return { ast: exprMergeOutput({ ...fields, output: exprOutput({ columns }) }) }; } }; }
    };
    return api;
  } };
}
