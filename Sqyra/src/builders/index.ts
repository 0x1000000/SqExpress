import {
  exprAggregateFunction, exprAggregateOverFunction, exprAlias, exprAliasedColumn, exprAliasedColumnName, exprAliasedSelecting, exprBoolLiteral, exprBooleanEq, exprByteArrayLiteral, exprCase, exprCaseWhenThen, exprCast, exprColumn, exprColumnAlias, exprColumnName, exprDatabaseName, exprDbSchema,
  exprBooleanAnd, exprBooleanGt, exprBooleanGtEq, exprBooleanLt, exprBooleanLtEq, exprBooleanNot, exprBooleanNotEq, exprBooleanOr,
  exprColumnSetClause, exprCrossedTable, exprCteQuery, exprDefault, exprDelete, exprDeleteOutput, exprDerivedTableValues, exprDiv, exprDoubleLiteral, exprExists, exprIdentityInsert, exprInSubQuery, exprInValues, exprInsert, exprInsertOutput, exprInsertQuery, exprInsertValueRow, exprInsertValues,
  exprFunctionName, exprInt32Literal, exprInt64Literal, exprJoinedTable, exprJsonArray, exprJsonMember, exprJsonObject, exprJsonOutputColumn, exprJsonQuery, exprJsonRemove, exprJsonSet, exprJsonTableValueColumn, exprJsonValue, exprModulo, exprMul, exprNull, exprAliasGuid,
  exprAllColumns, exprDerivedTableQuery, exprExprMergeNotMatchedInsert, exprExprMergeNotMatchedInsertDefault, exprMerge, exprMergeMatchedDelete, exprMergeMatchedUpdate, exprMergeOutput, exprOffsetFetch, exprOrderBy, exprOrderByItem, exprOrderByOffsetFetch, exprOutput, exprOutputAction, exprOutputColumn, exprOutputColumnDeleted, exprOutputColumnInserted, exprParameter, exprPortableScalarFunction, exprQueryAsJson, exprQueryExpression, exprQuerySpecification, exprSchemaName, exprSelect, exprSelectOffsetFetch,
  exprOver, exprScalarFunction, exprSelectingValue, exprStringAgg, exprStringConcat, exprStringLiteral, exprSub, exprSum, exprTable, exprTableAlias, exprTableFullName, exprTableFunction, exprTableValueConstructor, exprAliasedTableFunction, exprTableName, exprUnsafeValue, exprUpdate, exprValueQuery, exprValueRow, type Expr, type ExprAggregateFunction, type ExprBoolean, type ExprColumn, type ExprType,
  type ExprAliasedSelecting, type ExprAllColumns, type ExprJsonOutputColumn, type ExprOrderByItem, type ExprQuerySpecification, type ExprValue, type IExprComplete, type IExprMergeMatched, type IExprMergeNotMatched, type IExprSelecting, type IExprTableSource, type PortableScalarFunction
} from "../ast/generated/ast.generated.js";
import type { AutoAlias, ColumnDefinitions, ColumnRef, ColumnsOf, ColumnValue, DirectColumnsOf, TableDescriptor, ValueOfSqlType } from "../descriptors/index.js";
import { column, createAutoAlias, createColumnRef, isAutoAlias, registerColumnOperatorFactory, sqlType } from "../descriptors/index.js";
import { guidValue, queryAstNode, unwrapAstNode } from "../ast/runtime.js";
import { modify as modifyAst } from "../ast/operations.js";
import { toSql as exportSql, type ExportOptions } from "../exporters/index.js";

export type PrimitiveValue = string | number | bigint | boolean | Uint8Array | null;
type AnyColumn = ColumnRef<string, Exclude<PrimitiveValue, null>, boolean, string>;
export type ValueInput = ExprValue | AnyColumn | PrimitiveValue;
declare const aliasedExpressionName: unique symbol;
declare const aliasedExpressionValue: unique symbol;
export type AliasedExpression<Alias extends string, Value> = ExprAliasedSelecting & { readonly [aliasedExpressionName]?: Alias; readonly [aliasedExpressionValue]?: Value };
export type FluentValue<Value> = ExprValue & { as<const Alias extends string>(alias: Alias): AliasedExpression<Alias, Value> };
declare const allColumnsRow: unique symbol;
const allProjectionMetadata = Symbol("Sqyra.allProjectionMetadata");
const selectSource = Symbol("Sqyra.selectSource");
export type AllColumns<Row> = ExprAllColumns & { readonly [allColumnsRow]?: Row };
type SelectingInput = ValueInput | AliasedExpression<string, unknown> | AllColumns<unknown>;
type InputValue<T> = T extends AllColumns<infer R> ? R : T extends AliasedExpression<string, infer V> ? V : T extends AnyColumn ? ColumnValue<T> : T extends PrimitiveValue ? T : unknown;
export type ProjectionRow<P extends Readonly<Record<string, ValueInput>>> = { readonly [K in keyof P]: InputValue<P[K]> };
export type ColumnProjectionRow<C extends ReadonlyArray<AnyColumn>> = { readonly [K in C[number] as K["name"]]: ColumnValue<K> };
export type ExpressionProjectionRow<V extends ReadonlyArray<SelectingInput>> = V extends readonly [AllColumns<infer R>] ? R : V extends ReadonlyArray<AliasedExpression<string, unknown>> ? { readonly [E in V[number] as E extends AliasedExpression<infer A, unknown> ? A : never]: InputValue<E> } : { readonly [K in keyof V]: InputValue<V[K]> };
declare const expressionValueType: unique symbol;
type QueryNode = Extract<Expr, { readonly kind: "ExprQuerySpecification" | "ExprQueryExpression" | "ExprSelect" | "ExprSelectOffsetFetch" | "ExprQueryAsJson" }>;
function isQueryNode(node: Expr): node is QueryNode {
  return node.kind === "ExprQuerySpecification" || node.kind === "ExprQueryExpression" || node.kind === "ExprSelect" || node.kind === "ExprSelectOffsetFetch" || node.kind === "ExprQueryAsJson";
}
declare const queryRowType: unique symbol;
export type QueryExportOptions = Omit<ExportOptions, "dialect"> & { readonly dialect?: ExportOptions["dialect"] };
/** A query AST carrying its inferred result row only at compile time. */
export type Query<Row> = QueryNode & {
  readonly [queryRowType]?: Row;
  readonly [selectSource]: () => IExprTableSource;
  readonly $all: AllColumns<Row>;
  toSql(options?: QueryExportOptions): string;
  modify(modifier: (node: Expr) => Expr | null): Query<Row>;
};

function typedQuery<Row, Q extends QueryNode>(query: Q): Q & Query<Row> {
  const render = (options: QueryExportOptions = {}): string => exportSql(query, { ...options, dialect: options.dialect ?? "tsql" });
  const modify = (modifier: (node: Expr) => Expr | null): Query<Row> => {
    const changed = modifyAst(query, modifier);
    if (changed === null || !isQueryNode(changed)) throw new TypeError("Query.modify must retain a query root.");
    return typedQuery<Row, QueryNode>(changed);
  };
  const alias = createAutoAlias();
  const source = () => exprDerivedTableQuery({ query: asSubQuery(query), alias: exprTableAlias({ alias: exprAliasGuid({ id: guidValue(alias) }) }), columns: null });
  const all = (): AllColumns<Row> => exprAllColumns({ source: exprTableAlias({ alias: exprAliasGuid({ id: guidValue(alias) }) }) }) as AllColumns<Row>;
  // The proxy exposes SQL rendering without adding fields to the immutable AST.
  return new Proxy(query, {
    get: (target, property, receiver) => property === selectSource ? source : property === "$all" ? all() : property === "toSql" ? render : property === "modify" ? modify : Reflect.get(target, property, receiver),
    has: (target, property) => property === selectSource || property === "$all" || property === "toSql" || property === "modify" || Reflect.has(target, property),
  }) as Q & Query<Row>;
}

export function columnExpression<C extends ColumnRef<string, unknown, boolean, string>>(column: C): ExprColumn & { readonly [expressionValueType]?: ColumnValue<C> } {
  const alias = isAutoAlias(column.sourceAlias) ? exprAliasGuid({ id: guidValue(column.sourceAlias) }) : exprAlias({ name: column.sourceAlias });
  return exprColumn({ source: exprTableAlias({ alias }), columnName: exprColumnName({ name: column.name }) });
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
registerColumnOperatorFactory((operator, left, right) => {
  // ColumnRef's public methods restrict right at compile time; this runtime
  // dispatcher forwards that already-validated operand to the matching builder.
  const leftAlias = isAutoAlias(left.sourceAlias) ? exprAliasGuid({ id: guidValue(left.sourceAlias) }) : exprAlias({ name: left.sourceAlias });
  const leftExpression = exprColumn({ source: exprTableAlias({ alias: leftAlias }), columnName: exprColumnName({ name: left.name }) });
  const operand = toExpr(right as ValueInput);
  switch (operator) {
    case "eq": return fluentBoolean(exprBooleanEq({ left: leftExpression, right: operand }));
    case "neq": return fluentBoolean(exprBooleanNotEq({ left: leftExpression, right: operand }));
    case "gt": return fluentBoolean(exprBooleanGt({ left: leftExpression, right: operand }));
    case "gte": return fluentBoolean(exprBooleanGtEq({ left: leftExpression, right: operand }));
    case "lt": return fluentBoolean(exprBooleanLt({ left: leftExpression, right: operand }));
    case "lte": return fluentBoolean(exprBooleanLtEq({ left: leftExpression, right: operand }));
  }
});
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
export function inQuery(test: ValueInput, query: QueryNode): ExprBoolean { return exprInSubQuery({ testExpression: toExpr(test), subQuery: asSubQuery(query) }); }
export function exists(query: QueryNode): ExprBoolean { return exprExists({ subQuery: asSubQuery(query) }); }
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
export function scalarSubquery(query: QueryNode): ExprValue { return exprValueQuery({ query: asSubQuery(query) }); }
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
export function selectJson(first: ExprJsonOutputColumn, ...rest: ReadonlyArray<ExprJsonOutputColumn>): Query<string> { return typedQuery<string, ExprQuerySpecification>(exprQuerySpecification({ selectList: [first, ...rest], top: null, from: null, where: null, groupBy: null, distinct: false })); }
export function forJson(query: QueryNode, options: { readonly withoutArrayWrapper?: boolean; readonly includeNullValues?: boolean } = {}): Query<string> { return typedQuery<string, Extract<Expr, { readonly kind: "ExprQueryAsJson" }>>(exprQueryAsJson({ query: asSubQuery(query), withoutArrayWrapper: options.withoutArrayWrapper ?? false, includeNullValues: options.includeNullValues ?? true })); }

export function lit<const V extends PrimitiveValue>(value: V): FluentValue<V> {
  const node = toExpr(value);
  return new Proxy(node, { get: (target, property, receiver) => property === "as" ? <const A extends string>(alias: A): AliasedExpression<A, V> => exprAliasedSelecting({ value: node, alias: exprColumnAlias({ name: alias }) }) as AliasedExpression<A, V> : Reflect.get(target, property, receiver) }) as FluentValue<V>;
}
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
export type DerivedTableDescriptor<A extends string, D extends ColumnDefinitions> = TableDescriptor<null, A, D, A> & { readonly $metadata: TableDescriptor<null, A, D, A>["$metadata"] & { readonly source: IExprTableSource } };
type VirtualTableDescriptor<N extends string, A extends string, D extends ColumnDefinitions> = TableDescriptor<null, N, D, A> & { readonly $metadata: TableDescriptor<null, N, D, A>["$metadata"] & { readonly source: IExprTableSource } };
interface SelectQuerySource<Row> { readonly [selectSource]: () => IExprTableSource; readonly $all: AllColumns<Row>; }
type TableSourceInput = AnyTable | DerivedTableDescriptor<string, ColumnDefinitions> | SelectQuerySource<unknown>;
type SelectStageKey = "$all" | "toSql" | "modify" | "kind" | "selectList" | "top" | "from" | "where" | "groupBy" | "distinct" | "selectQuery" | "orderBy" | "offsetFetch" | "query" | "innerJoin" | "leftJoin" | "rightJoin" | "fullJoin" | "crossJoin" | "groupByList" | "orderByList";
export type SelectProjectionColumns<Row> = Row extends ReadonlyArray<unknown> ? {} : { readonly [K in keyof Row & string as K extends SelectStageKey ? `$${K}` : K]: ColumnRef<K, Exclude<Row[K], null>, null extends Row[K] ? true : false, AutoAlias> };
interface SelectStartMethods<Row> { from(table: TableSourceInput): SelectFrom<Row>; }
export type SelectStart<Row> = Query<Row> & SelectStartMethods<Row> & SelectProjectionColumns<Row> & SelectQuerySource<Row>;
interface SelectFromMethods<Row> {
  innerJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>; leftJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>;
  rightJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>; fullJoin(table: TableSourceInput, on: ExprBoolean): SelectFrom<Row>;
  crossJoin(table: TableSourceInput): SelectFrom<Row>; where(predicate: ExprBoolean | null): SelectFiltered<Row>;
  groupBy(...values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; groupByList(values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; orderBy(...items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; orderByList(items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>;
}
export type SelectFrom<Row> = Query<Row> & SelectFromMethods<Row> & SelectProjectionColumns<Row> & SelectQuerySource<Row>;
interface SelectFilteredMethods<Row> { groupBy(...values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; groupByList(values: ReadonlyArray<ValueInput>): SelectGrouped<Row>; orderBy(...items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; orderByList(items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; }
export type SelectFiltered<Row> = Query<Row> & SelectFilteredMethods<Row> & SelectProjectionColumns<Row> & SelectQuerySource<Row>;
interface SelectGroupedMethods<Row> { orderBy(...items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; orderByList(items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row>; }
export type SelectGrouped<Row> = Query<Row> & SelectGroupedMethods<Row> & SelectProjectionColumns<Row> & SelectQuerySource<Row>;
interface SelectOrderedMethods<Row> { offsetFetch(offset: ValueInput, fetch?: ValueInput): SelectFinal<Row>; }
export type SelectOrdered<Row> = Query<Row> & SelectOrderedMethods<Row> & SelectProjectionColumns<Row> & SelectQuerySource<Row>;
export type SelectFinal<Row> = Query<Row> & SelectProjectionColumns<Row> & SelectQuerySource<Row>;

export interface SelectOptions { readonly distinct?: boolean; readonly top?: ValueInput; }
function isColumn(value: unknown): value is AnyColumn { return typeof value === "object" && value !== null && "sourceAlias" in value; }
function isValueArray(value: Readonly<Record<string, ValueInput>> | readonly [SelectingInput, ...SelectingInput[]] | SelectingInput): value is readonly [SelectingInput, ...SelectingInput[]] { return Array.isArray(value); }
function isAliasedExpression(value: SelectingInput): value is AliasedExpression<string, unknown> { return typeof value === "object" && value !== null && "kind" in value && value.kind === "ExprAliasedSelecting"; }
function isSelectOptions(value: unknown): value is SelectOptions { return typeof value === "object" && value !== null && !Array.isArray(value) && !(value instanceof Uint8Array) && !("kind" in value) && !("sourceAlias" in value); }
function selectingExpression(value: SelectingInput): IExprSelecting { return isAliasedExpression(value) || (typeof value === "object" && value !== null && "kind" in value && value.kind === "ExprAllColumns") ? value : toExpr(value); }
function projectedSelections(values: ReadonlyArray<SelectingInput>): ReadonlyArray<{ readonly name: string; readonly nullable: boolean }> {
  const first = values[0];
  if (values.length === 1 && typeof first === "object" && first !== null && "kind" in first && first.kind === "ExprAllColumns") return (first as ExprAllColumns & { readonly [allProjectionMetadata]?: ReadonlyArray<{ readonly name: string; readonly nullable: boolean }> })[allProjectionMetadata] ?? [];
  if (values.every(isColumn)) return values.map(item => ({ name: item.name, nullable: item.nullable }));
  if (values.every(isAliasedExpression)) return values.map(item => ({ name: item.alias.name, nullable: item.value.kind === "ExprNull" }));
  return [];
}
export function select<const C extends readonly [AnyColumn, ...AnyColumn[]]>(columns: C, options?: SelectOptions): SelectStart<ColumnProjectionRow<C>>;
export function select<const C extends readonly [AnyColumn, ...AnyColumn[]]>(...columns: C): SelectStart<ColumnProjectionRow<C>>;
export function select<const V extends readonly [SelectingInput, ...SelectingInput[]]>(values: V, options?: SelectOptions): SelectStart<ExpressionProjectionRow<V>>;
export function select<const V extends readonly [SelectingInput, ...SelectingInput[]]>(...values: V): SelectStart<ExpressionProjectionRow<V>>;
export function select<const V extends SelectingInput>(value: V, options?: SelectOptions): SelectStart<ExpressionProjectionRow<readonly [V]>>;
export function select<const P extends Readonly<Record<string, ValueInput>>>(projection: P, options?: SelectOptions): SelectStart<ProjectionRow<P>>;
export function select(first: Readonly<Record<string, ValueInput>> | readonly [SelectingInput, ...SelectingInput[]] | SelectingInput, second?: SelectOptions | SelectingInput, ...rest: ReadonlyArray<SelectingInput>): SelectStart<never> {
  let list: IExprSelecting[];
  let projected: ReadonlyArray<{ readonly name: string; readonly nullable: boolean }>;
  let options: SelectOptions;
  if (isValueArray(first)) {
    if (first.length === 0) throw new TypeError("SELECT requires at least one column.");
    list = first.map(selectingExpression);
    projected = projectedSelections(first);
    if (second !== undefined && !isSelectOptions(second)) throw new TypeError("Array SELECT accepts options only after the expression tuple.");
    options = second ?? {};
  } else if (isColumn(first) || typeof first !== "object" || first === null || "kind" in first || first instanceof Uint8Array) {
    const secondIsOptions = second !== undefined && isSelectOptions(second);
    const values = [first, ...(second === undefined || secondIsOptions ? [] : [second]), ...rest] as ReadonlyArray<SelectingInput>;
    list = values.map(selectingExpression);
    projected = projectedSelections(values);
    options = secondIsOptions ? second : {};
  } else {
    const entries = Object.entries(first);
    list = entries.map(([alias, input]) => exprAliasedSelecting({ value: toExpr(input), alias: exprColumnAlias({ name: alias }) }));
    projected = entries.map(([name, input]) => ({ name, nullable: isColumn(input) ? input.nullable : input === null }));
    if (second !== undefined && !isSelectOptions(second)) throw new TypeError("Object SELECT requires an options object as its second argument.");
    options = second ?? {};
  }
  type Row = Readonly<Record<string, unknown>>;
  const derivedAlias = createAutoAlias();
  const projectedColumns: Record<string, ColumnRef<string, unknown, boolean, AutoAlias>> = {};
  for (const item of projected) {
    const property = new Set<SelectStageKey>(["$all", "toSql", "modify", "kind", "selectList", "top", "from", "where", "groupBy", "distinct", "selectQuery", "orderBy", "offsetFetch", "query", "innerJoin", "leftJoin", "rightJoin", "fullJoin", "crossJoin", "groupByList", "orderByList"]).has(item.name as SelectStageKey) ? `$${item.name}` : item.name;
    if (property in projectedColumns) continue;
    projectedColumns[property] = createColumnRef(item.name, derivedAlias, item.nullable);
  }
  const finish = (from: IExprTableSource | null, where: ExprBoolean | null, groupBy: ReadonlyArray<ExprValue> | null, order: ReadonlyArray<ExprOrderByItem> | null, page: { offset: ExprValue; fetch: ExprValue | null } | null): Query<Row> => {
    const query = exprQuerySpecification({ selectList: list, top: options.top === undefined ? null : toExpr(options.top), from, where, groupBy, distinct: options.distinct ?? false });
    return typedQuery<Row, QueryNode>(order === null ? query : page === null ? exprSelect({ selectQuery: query, orderBy: exprOrderBy({ orderList: order }) }) : exprSelectOffsetFetch({ selectQuery: query, orderBy: exprOrderByOffsetFetch({ orderList: order, offsetFetch: exprOffsetFetch(page) }) }));
  };
  const groups = (values: ReadonlyArray<ValueInput>): ReadonlyArray<ExprValue> => { if (values.length === 0) throw new TypeError("GROUP BY requires at least one expression."); return values.map(toExpr); };
  const stage = <T extends object>(methods: T, build: () => Query<Row>): Query<Row> & T & SelectProjectionColumns<Row> & SelectQuerySource<Row> => {
    const query = build();
    const source = () => exprDerivedTableQuery({ query: asSubQuery(query), alias: exprTableAlias({ alias: exprAliasGuid({ id: guidValue(derivedAlias) }) }), columns: null });
    const all = (): AllColumns<Row> => {
      const wildcard = exprAllColumns({ source: exprTableAlias({ alias: exprAliasGuid({ id: guidValue(derivedAlias) }) }) });
      return new Proxy(wildcard, { get: (target, property, receiver) => property === allProjectionMetadata ? projected : Reflect.get(target, property, receiver) }) as AllColumns<Row>;
    };
    // Use a configurable facade so escaped projected SQL names can coexist with
    // AST fields and fluent methods. Every mutation trap rejects writes.
    const facade = { ...query };
    return new Proxy(facade, {
      get: (target, property, receiver) => property === queryAstNode ? query : property === selectSource ? source : property === "$all" ? all() : property === "toSql" ? query.toSql : property === "modify" ? query.modify : property === "toJSON" ? () => query : property in methods ? Reflect.get(methods, property) : property in projectedColumns ? projectedColumns[property as string] : Reflect.get(target, property, receiver),
      has: (target, property) => property === queryAstNode || property === selectSource || property === "$all" || property === "toSql" || property === "modify" || property in methods || property in projectedColumns || Reflect.has(target, property),
      set: () => false,
      defineProperty: () => false,
      deleteProperty: () => false,
    }) as Query<Row> & T & SelectProjectionColumns<Row> & SelectQuerySource<Row>;
  };
  const ordered = (from: IExprTableSource, where: ExprBoolean | null, groupBy: ReadonlyArray<ExprValue> | null, items: ReadonlyArray<ValueInput | ExprOrderByItem>): SelectOrdered<Row> => { if (items.length === 0) throw new TypeError("ORDER BY requires at least one expression."); const order = items.map((item) => typeof item === "object" && item !== null && "kind" in item && item.kind === "ExprOrderByItem" ? item : asc(item as ValueInput)); const current = () => finish(from, where, groupBy, order, null); return stage({ offsetFetch: (offset: ValueInput, fetch?: ValueInput) => { const page = () => finish(from, where, groupBy, order, { offset: toExpr(offset), fetch: fetch === undefined ? null : toExpr(fetch) }); return stage({}, page); } }, current); };
  const grouped = (from: IExprTableSource, where: ExprBoolean | null, values: ReadonlyArray<ValueInput>): SelectGrouped<Row> => { const group = groups(values); const current = () => finish(from, where, group, null, null); return stage({ orderBy: (...items: ReadonlyArray<ValueInput | ExprOrderByItem>) => ordered(from, where, group, items), orderByList: (items: ReadonlyArray<ValueInput | ExprOrderByItem>) => ordered(from, where, group, items) }, current); };
  const afterFilter = (from: IExprTableSource, where: ExprBoolean | null): SelectFiltered<Row> => { const current = () => finish(from, where, null, null, null); return stage({ groupBy: (...values: ReadonlyArray<ValueInput>) => grouped(from, where, values), groupByList: (values: ReadonlyArray<ValueInput>) => grouped(from, where, values), orderBy: (...items: ReadonlyArray<ValueInput | ExprOrderByItem>) => ordered(from, where, null, items), orderByList: (items: ReadonlyArray<ValueInput | ExprOrderByItem>) => ordered(from, where, null, items) }, current); };
  const fromStage = (from: IExprTableSource): SelectFrom<Row> => stage({
    where: (predicate) => afterFilter(from, predicate),
    groupBy: (...values) => grouped(from, null, values), groupByList: (values) => grouped(from, null, values), orderBy: (...items) => ordered(from, null, null, items), orderByList: (items) => ordered(from, null, null, items),
    innerJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Inner" })),
    leftJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Left" })),
    rightJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Right" })),
    fullJoin: (table, on) => fromStage(exprJoinedTable({ left: from, right: tableSourceExpression(table), searchCondition: on, joinType: "Full" })),
    crossJoin: (table) => fromStage(exprCrossedTable({ left: from, right: tableSourceExpression(table) })),
  }, () => finish(from, null, null, null, null));
  const root = () => finish(null, null, null, null, null);
  return stage({ from: (table: TableSourceInput) => fromStage(tableSourceExpression(table)) }, root) as SelectStart<never>;
}

function tableExpression<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(table: TableDescriptor<S, N, D, A>) {
  const metadata = table.$metadata;
  const alias = metadata.alias === "" ? null : exprTableAlias({ alias: isAutoAlias(metadata.alias) ? exprAliasGuid({ id: guidValue(metadata.alias) }) : exprAlias({ name: metadata.alias }) });
  return exprTable({ fullName: exprTableFullName({ dbSchema: metadata.schema === null ? null : exprDbSchema({ database: metadata.database === null ? null : exprDatabaseName({ name: metadata.database }), schema: exprSchemaName({ name: metadata.schema }) }), tableName: exprTableName({ name: metadata.name }) }), alias });
}
function tableSourceExpression(table: TableSourceInput): IExprTableSource { return selectSource in table ? table[selectSource]() : "source" in table.$metadata ? table.$metadata.source : tableExpression(table); }
export function tableSource(table: TableSourceInput): IExprTableSource { return tableSourceExpression(table); }

export function derivedTable<const A extends string>(query: QueryNode, alias: A): DerivedTableDescriptor<A, ColumnDefinitions>;
export function derivedTable<const A extends string, const D extends ColumnDefinitions>(query: QueryNode, alias: A, definitions: D): DerivedTableDescriptor<A, D>;
export function derivedTable<const A extends string, const D extends ColumnDefinitions>(query: QueryNode, alias: A, definitions?: D): DerivedTableDescriptor<A, D> | DerivedTableDescriptor<A, ColumnDefinitions> {
  return definitions === undefined ? inferredDerivedTable(query, alias) : typedDerivedTable(query, alias, definitions);
}

function typedDerivedTable<const A extends string, const D extends ColumnDefinitions>(query: QueryNode, alias: A, definitions: D): DerivedTableDescriptor<A, D> {
  query = unwrapAstNode(query);
  if (!(query.kind === "ExprQuerySpecification" || query.kind === "ExprQueryExpression" || query.kind === "ExprSelectOffsetFetch")) throw new TypeError("Derived table source must be a subquery AST.");
  const selected = query.kind === "ExprQuerySpecification" ? query.selectList.length : query.kind === "ExprSelectOffsetFetch" && query.selectQuery.kind === "ExprQuerySpecification" ? query.selectQuery.selectList.length : null;
  if (selected !== null && selected !== Object.keys(definitions).length) throw new TypeError("Number of declared columns does not match the number of selected columns in the derived table subquery.");
  const refs: Record<string, ColumnRef<string, unknown, boolean, A>> = {};
  for (const [name, definition] of Object.entries(definitions)) refs[name] = createColumnRef(name, alias, definition.nullable);
  const source = exprDerivedTableQuery({ query, alias: exprTableAlias({ alias: exprAlias({ name: alias }) }), columns: Object.keys(definitions).map((name) => exprColumnName({ name })) });
  return virtualTable(alias, alias, definitions, refs, source);
}
function inferredDerivedTable<const A extends string>(query: QueryNode, alias: A): DerivedTableDescriptor<A, ColumnDefinitions> {
  const subQuery = asSubQuery(query); const selection = subQuery.kind === "ExprQuerySpecification" ? subQuery.selectList : subQuery.kind === "ExprSelectOffsetFetch" && subQuery.selectQuery.kind === "ExprQuerySpecification" ? subQuery.selectQuery.selectList : null;
  if (selection === null) throw new TypeError("Dynamic derived tables require a query specification with discoverable projection names.");
  const names = selection.map((item, index) => item.kind === "ExprAliasedSelecting" ? item.alias.name : item.kind === "ExprAliasedColumn" ? item.alias?.name ?? item.column.columnName.name : item.kind === "ExprColumn" ? item.columnName.name : `Expr${index + 1}`);
  const definitions: Record<string, ColumnDefinitions[string]> = {}; for (const name of names) { if (name in definitions) throw new TypeError(`Duplicate derived column '${name}'.`); definitions[name] = column(sqlType.string()); }
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
  for (const [columnName, definition] of Object.entries(definitions)) refs[columnName] = createColumnRef(columnName, name, definition.nullable);
  return virtualTable(name, name, definitions, refs, derivedSource);
}

function virtualTable<N extends string, A extends string, D extends ColumnDefinitions>(name: N, alias: A, definitions: D, refs: Record<string, ColumnRef<string, unknown, boolean, A>>, derivedSource: IExprTableSource): VirtualTableDescriptor<N, A, D> {
  const reserved = new Set(["$metadata", "as"]); const direct: Record<string, ColumnRef<string, unknown, boolean, A>> = {};
  for (const [columnName, ref] of Object.entries(refs)) { const property = reserved.has(columnName) ? `$${columnName}` : columnName; if (property !== columnName && property in definitions) throw new TypeError(`Column '${columnName}' escapes to '${property}', which is also a column name.`); direct[property] = ref; }
  // The loop creates exactly one reference for every key in D and applies the
  // same reserved-name transformation represented by DirectColumnsOf.
  const columns = Object.freeze(refs) as ColumnsOf<D, A>;
  const directColumns = direct as DirectColumnsOf<D, A>;
  const as = <const NextAlias extends string>(nextAlias: NextAlias): TableDescriptor<null, N, D, NextAlias> => {
    const nextRefs: Record<string, ColumnRef<string, unknown, boolean, NextAlias>> = {};
    for (const [columnName, definition] of Object.entries(definitions)) nextRefs[columnName] = createColumnRef(columnName, nextAlias, definition.nullable);
    return virtualTable(name, nextAlias, definitions, nextRefs, aliasDerivedSource(derivedSource, nextAlias));
  };
  function instantiate(): TableDescriptor<null, N, D, AutoAlias>;
  function instantiate<const NextAlias extends string>(nextAlias: NextAlias): TableDescriptor<null, N, D, NextAlias>;
  function instantiate(nextAlias?: string): TableDescriptor<null, N, D, string> {
    const resolvedAlias = nextAlias ?? createAutoAlias();
    const nextRefs: Record<string, ColumnRef<string, unknown, boolean, typeof resolvedAlias>> = {};
    for (const [columnName, definition] of Object.entries(definitions)) nextRefs[columnName] = createColumnRef(columnName, resolvedAlias, definition.nullable);
    return virtualTable(name, resolvedAlias, definitions, nextRefs, aliasDerivedSource(derivedSource, resolvedAlias));
  }
  // The callable is populated with the complete descriptor surface immediately below.
  const descriptor = instantiate as unknown as VirtualTableDescriptor<N, A, D>;
  const metadata = Object.freeze({ database: null, schema: null, name, alias, definitions, columns, isDynamic: false, source: derivedSource });
  Object.defineProperties(descriptor, {
    $metadata: { enumerable: false, value: metadata }, as: { enumerable: false, value: as },
  });
  for (const [property, reference] of Object.entries(directColumns)) Object.defineProperty(descriptor, property, { enumerable: true, value: reference });
  Object.freeze(descriptor);
  return descriptor;
}

function aliasDerivedSource(source: IExprTableSource, alias: string): IExprTableSource {
  const tableAlias = exprTableAlias({ alias: exprAlias({ name: alias }) });
  switch (source.kind) {
    case "ExprDerivedTableQuery": return exprDerivedTableQuery({ query: source.query, alias: tableAlias, columns: source.columns });
    case "ExprDerivedTableValues": return exprDerivedTableValues({ values: source.values, alias: tableAlias, columns: source.columns });
    case "ExprAliasedTableFunction": return exprAliasedTableFunction({ function: source.function, alias: tableAlias });
    case "ExprCteQuery": return exprCteQuery({ name: source.name, query: source.query, alias: tableAlias });
    default: throw new TypeError(`Table source '${source.kind}' cannot be aliased.`);
  }
}

export function cte<const N extends string, const D extends ColumnDefinitions>(name: N, definitions: D, build: (self: DerivedTableDescriptor<N, D>) => QueryNode): DerivedTableDescriptor<N, D> {
  const empty = exprQuerySpecification({ selectList: [], top: null, from: null, where: null, groupBy: null, distinct: false });
  const self = virtualDescriptor(name, definitions, exprCteQuery({ name, query: empty, alias: null }));
  const query = asSubQuery(build(self));
  return virtualDescriptor(name, definitions, exprCteQuery({ name, query, alias: null }));
}

function asSubQuery(query: QueryNode): Extract<Expr, { readonly kind: "ExprQuerySpecification" | "ExprQueryExpression" | "ExprSelectOffsetFetch" }> {
  query = unwrapAstNode(query);
  if (query.kind === "ExprQuerySpecification" || query.kind === "ExprQueryExpression" || query.kind === "ExprSelectOffsetFetch") return query;
  throw new TypeError("Set operation requires query operands.");
}
function setOperation<L, R>(left: Query<L>, right: Query<R>, type: "Union" | "UnionAll" | "Except" | "Intersect"): Query<L | R> { return typedQuery<L | R, Extract<Expr, { readonly kind: "ExprQueryExpression" }>>(exprQueryExpression({ left: asSubQuery(left), right: asSubQuery(right), queryExpressionType: type })); }
export const union = <L, R>(left: Query<L>, right: Query<R>) => setOperation(left, right, "Union");
export const unionAll = <L, R>(left: Query<L>, right: Query<R>) => setOperation(left, right, "UnionAll");
export const except = <L, R>(left: Query<L>, right: Query<R>) => setOperation(left, right, "Except");
export const intersect = <L, R>(left: Query<L>, right: Query<R>) => setOperation(left, right, "Intersect");
export function orderQuery<Row>(query: Query<Row>, items: ReadonlyArray<ValueInput | ExprOrderByItem>, pagination?: { readonly offset: ValueInput; readonly fetch?: ValueInput }): Query<Row> {
  if (items.length === 0) throw new TypeError("ORDER BY requires at least one expression.");
  const orderList = items.map(item => typeof item === "object" && item !== null && "kind" in item && item.kind === "ExprOrderByItem" ? item : asc(item as ValueInput));
  return typedQuery<Row, QueryNode>(pagination === undefined ? exprSelect({ selectQuery: asSubQuery(query), orderBy: exprOrderBy({ orderList }) }) : exprSelectOffsetFetch({ selectQuery: asSubQuery(query), orderBy: exprOrderByOffsetFetch({ orderList, offsetFetch: exprOffsetFetch({ offset: toExpr(pagination.offset), fetch: pagination.fetch === undefined ? null : toExpr(pagination.fetch) }) }) }));
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
  return { from: (query: QueryNode, first: keyof D & string, ...rest: ReadonlyArray<keyof D & string>): InsertStatement => {
    const names = [first, ...rest];
    return finish(exprInsert({ target: tableExpression(table).fullName, targetColumns: names.map((name) => exprColumnName({ name })), source: exprInsertQuery({ query: asSubQuery(query) }) }));
  }, values: (first: InsertRow<D>, ...rest: ReadonlyArray<InsertRow<D>>): InsertStatement => {
    const names = Object.keys(table.$metadata.definitions); const rows = [first, ...rest];
    for (const row of rows) { const actual = Object.keys(row); if (actual.length !== names.length || names.some((name) => !Object.prototype.hasOwnProperty.call(row, name))) throw new TypeError(`Insert row must define exactly: ${names.join(", ")}`); }
    return finish(exprInsert({ target: tableExpression(table).fullName, targetColumns: names.map((name) => exprColumnName({ name })), source: exprInsertValues({ items: rows.map((row) => exprInsertValueRow({ items: names.map((name) => toExpr(row[name] as ValueInput)) })) }) }));
  } };
}

export function update<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(table: TableDescriptor<S, N, D, A>) {
  const target = tableExpression(table);
  return { set(values: UpdateSet<D>) {
    // Object.entries loses the mapped value type, while UpdateSet<D> ensures it
    // is a supported assigning value for the corresponding descriptor column.
    const clauses = Object.entries(values).map(([name, value]) => exprColumnSetClause({ column: columnExpression(table.$metadata.columns[name]!), value: value === exprDefault ? exprDefault : toExpr(value as ValueInput) }));
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
      if (!(name in target.$metadata.definitions)) throw new TypeError(`Unknown MERGE target column '${name}'.`);
      return exprColumnSetClause({ column: columnExpression(target.$metadata.columns[name]!), value: toExpr(value as ValueInput) });
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
