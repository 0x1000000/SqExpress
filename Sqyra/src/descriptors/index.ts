import type { ExprBoolean, ExprValue } from "../ast/generated/ast.generated.js";
import type { AliasedExpression, FluentValue, ValueInput } from "../builders/index.js";

import { createTableScript, type TableIndex, type TableScript } from "../schema/index.js";

export type SqlTypeName =
  | "boolean"
  | "byte"
  | "int16"
  | "int32"
  | "int64"
  | "decimal"
  | "double"
  | "string"
  | "guid"
  | "binary"
  | "date"
  | "dateTime"
  | "dateTimeOffset"
  | "xml";
declare const sqlTypeValue: unique symbol;
declare const columnRefValue: unique symbol;
declare const autoAliasType: unique symbol;
export interface ColumnOrigin {
  readonly database: string | null;
  readonly schema: string | null;
  readonly table: string;
}
const columnOrigins = new WeakMap<object, ColumnOrigin>();
const columnSqlTypes = new WeakMap<object, SqlType<unknown>>();
/** @internal Resolves physical ownership for schema metadata without exposing implementation fields on columns. */
export function getColumnOrigin(
  column: ColumnRef<string, unknown, boolean, string>,
): ColumnOrigin | undefined {
  return columnOrigins.get(column);
}
/** @internal Resolves descriptor typing for contextual literal conversion. */
export function getColumnSqlType(
  column: ColumnRef<string, unknown, boolean, string>,
): SqlType<unknown> | undefined {
  return columnSqlTypes.get(column);
}
export type AutoAlias = string & { readonly [autoAliasType]: true };
let nextAutoAlias = 1n;
const automaticAliases = new Set<string>();
/** @internal Creates an identity that an exporter names within one SQL scope. */
export function createAutoAlias(): AutoAlias {
  const hex = (nextAutoAlias++).toString(16).padStart(32, "0");
  const id = `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-8${hex.slice(17, 20)}-${hex.slice(20)}`;
  automaticAliases.add(id);
  return id as AutoAlias;
}
/** @internal Identifies aliases that must be named by the SQL export scope. */
export function isAutoAlias(alias: string): alias is AutoAlias {
  return automaticAliases.has(alias);
}

export interface SqlType<T, N extends SqlTypeName = SqlTypeName> {
  readonly name: N;
  readonly arguments: ReadonlyArray<number>;
  readonly unicode?: boolean;
  readonly fixed?: boolean;
  readonly text?: boolean;
  readonly [sqlTypeValue]?: T;
}
export type ValueOfSqlType<T> = T extends SqlType<infer V, SqlTypeName> ? V : never;

function scalar<T, const N extends SqlTypeName>(
  name: N,
  args: ReadonlyArray<number> = [],
  options: { readonly unicode?: boolean; readonly fixed?: boolean; readonly text?: boolean } = {},
): SqlType<T, N> {
  return Object.freeze({ name, arguments: Object.freeze([...args]), ...options });
}
export const sqlType = {
  boolean: scalar<boolean, "boolean">("boolean"),
  byte: scalar<number, "byte">("byte"),
  int16: scalar<number, "int16">("int16"),
  int32: scalar<number, "int32">("int32"),
  int64: scalar<bigint, "int64">("int64"),
  double: scalar<number, "double">("double"),
  guid: scalar<string, "guid">("guid"),
  xml: scalar<string, "xml">("xml"),
  date: scalar<string, "date">("date"),
  dateTime: scalar<string, "dateTime">("dateTime"),
  dateTimeOffset: scalar<string, "dateTimeOffset">("dateTimeOffset"),
  string: (
    size?: number,
    options: { readonly unicode?: boolean; readonly fixed?: boolean; readonly text?: boolean } = {},
  ): SqlType<string, "string"> => scalar("string", size === undefined ? [] : [size], options),
  decimal: (precision: number, scale: number): SqlType<string, "decimal"> =>
    scalar("decimal", [precision, scale]),
  binary: (
    size?: number,
    options: { readonly fixed?: boolean } = {},
  ): SqlType<Uint8Array, "binary"> => scalar("binary", size === undefined ? [] : [size], options),
} as const;

export type ColumnDefaultValue = ExprValue | boolean | number | bigint | string | Uint8Array | null;
export interface ColumnOptions {
  readonly primaryKey?: boolean;
  readonly identity?: boolean;
  readonly default?: ColumnDefaultValue;
  readonly references?: ForeignKeyInput | ReadonlyArray<ForeignKeyInput>;
}
export type ForeignKeyInput =
  ColumnRef<string, unknown, boolean, string> | (() => ColumnRef<string, unknown, boolean, string>);
export interface ColumnDefinition<T extends SqlType<unknown>, Nullable extends boolean> {
  readonly sqlType: T;
  readonly nullable: Nullable;
  readonly options: Readonly<ColumnOptions>;
}
function freezeColumnOptions(options: ColumnOptions): Readonly<ColumnOptions> {
  return Object.freeze({
    ...options,
    ...(options.default instanceof Uint8Array ? { default: new Uint8Array(options.default) } : {}),
  });
}
export function column<const T extends SqlType<unknown>>(
  type: T,
  options: ColumnOptions = {},
): ColumnDefinition<T, false> {
  return Object.freeze({ sqlType: type, nullable: false, options: freezeColumnOptions(options) });
}
export function nullableColumn<const T extends SqlType<unknown>>(
  type: T,
  options: ColumnOptions = {},
): ColumnDefinition<T, true> {
  return Object.freeze({ sqlType: type, nullable: true, options: freezeColumnOptions(options) });
}
export type ColumnDefinitions = Readonly<
  Record<string, ColumnDefinition<SqlType<unknown>, boolean>>
>;

export type ColumnPredicate = ExprBoolean & {
  and(right: ExprBoolean): ColumnPredicate;
  or(right: ExprBoolean): ColumnPredicate;
  not(): ColumnPredicate;
};
type ColumnOperand<Value, Nullable extends boolean> =
  | Value
  | (Nullable extends true ? null : never)
  | ExprValue
  | ColumnRef<string, Exclude<Value, null>, boolean, string>;
export interface ColumnRef<
  Name extends string,
  Value,
  Nullable extends boolean,
  Alias extends string,
> {
  readonly name: Name;
  readonly sourceAlias: Alias;
  readonly nullable: Nullable;
  readonly [columnRefValue]?: Value;
  eq(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  neq(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  gt(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  gte(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  lt(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  lte(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  as<const A extends string>(
    alias: A,
  ): AliasedExpression<A, Value | (Nullable extends true ? null : never)>;
  add(right: ValueInput): FluentValue<unknown>;
  subtract(right: ValueInput): FluentValue<unknown>;
  multiply(right: ValueInput): FluentValue<unknown>;
  divide(right: ValueInput): FluentValue<unknown>;
  modulo(right: ValueInput): FluentValue<unknown>;
  concat(right: ValueInput): FluentValue<string>;
  inList(first: ValueInput, ...rest: ReadonlyArray<ValueInput>): ColumnPredicate;
  inQuery(query: Parameters<FluentValue<unknown>["inQuery"]>[0]): ColumnPredicate;
  like(pattern: ValueInput): ColumnPredicate;
  asc(): ReturnType<FluentValue<unknown>["asc"]>;
  desc(): ReturnType<FluentValue<unknown>["desc"]>;
  cast(type: Parameters<FluentValue<unknown>["cast"]>[0]): FluentValue<unknown>;
  jsonValue(
    path: string,
    type?: Parameters<FluentValue<unknown>["jsonValue"]>[1],
  ): FluentValue<unknown>;
  jsonQuery(path?: string): FluentValue<unknown>;
  jsonSet(path: string, value: ValueInput): FluentValue<unknown>;
  jsonRemove(path: string): FluentValue<unknown>;
}

type ColumnOperatorName = "eq" | "neq" | "gt" | "gte" | "lt" | "lte";
type RuntimeColumn = ColumnRef<string, unknown, boolean, string>;
type ColumnOperatorFactory = (
  operator: ColumnOperatorName,
  left: RuntimeColumn,
  right: unknown,
) => ColumnPredicate;
let columnOperatorFactory: ColumnOperatorFactory | null = null;
let columnExpressionFactory: ((column: RuntimeColumn) => FluentValue<unknown>) | null = null;
/** @internal Connects fluent scalar column operations without a runtime module cycle. */
export function registerColumnExpressionFactory(
  factory: (column: RuntimeColumn) => FluentValue<unknown>,
): void {
  columnExpressionFactory = factory;
}
/** @internal Connects descriptor methods to the expression builder without a module cycle. */
export function registerColumnOperatorFactory(factory: ColumnOperatorFactory): void {
  columnOperatorFactory = factory;
}
/** @internal Creates the immutable runtime representation shared by physical and derived columns. */
export function createColumnRef<
  Name extends string,
  Value,
  Nullable extends boolean,
  Alias extends string,
>(
  name: Name,
  sourceAlias: Alias,
  nullable: Nullable,
  origin?: ColumnOrigin,
  sqlType?: SqlType<unknown>,
): ColumnRef<Name, Value, Nullable, Alias> {
  let reference: ColumnRef<Name, Value, Nullable, Alias>;
  const invoke = (operator: ColumnOperatorName, right: unknown): ColumnPredicate => {
    if (columnOperatorFactory === null)
      throw new Error(
        "Sqyra column operators are not initialized. Import descriptors through the 'sqyra' package entry point.",
      );
    return columnOperatorFactory(operator, reference, right);
  };
  reference = {
    name,
    sourceAlias,
    nullable,
    eq: (right) => invoke("eq", right),
    neq: (right) => invoke("neq", right),
    gt: (right) => invoke("gt", right),
    gte: (right) => invoke("gte", right),
    lt: (right) => invoke("lt", right),
    lte: (right) => invoke("lte", right),
  } as ColumnRef<Name, Value, Nullable, Alias>;
  for (const method of [
    "as",
    "add",
    "subtract",
    "multiply",
    "divide",
    "modulo",
    "concat",
    "inList",
    "inQuery",
    "like",
    "asc",
    "desc",
    "cast",
    "jsonValue",
    "jsonQuery",
    "jsonSet",
    "jsonRemove",
  ] as const) {
    Object.defineProperty(reference, method, {
      enumerable: false,
      value: (...args: unknown[]) => {
        if (columnExpressionFactory === null)
          throw new Error(
            "Sqyra column operators are not initialized. Import the sqyra package entry point.",
          );
        const value = columnExpressionFactory(reference);
        return Reflect.apply(value[method], value, args);
      },
    });
  }
  for (const operator of ["eq", "neq", "gt", "gte", "lt", "lte"] as const)
    Object.defineProperty(reference, operator, { enumerable: false });
  if (origin !== undefined) columnOrigins.set(reference, Object.freeze(origin));
  if (sqlType !== undefined) columnSqlTypes.set(reference, sqlType);
  return Object.freeze(reference);
}
export type ColumnsOf<D extends ColumnDefinitions, A extends string> = {
  readonly [K in keyof D]: K extends string
    ? ColumnRef<K, ValueOfSqlType<NonNullable<D[K]>["sqlType"]>, NonNullable<D[K]>["nullable"], A>
    : never;
};
export type TableMemberKey = "$metadata" | "$script" | "as";
export type DirectColumnsOf<D extends ColumnDefinitions, A extends string> = string extends keyof D
  ? {}
  : {
      readonly [
        K in keyof D as K extends string ? (K extends TableMemberKey ? `$${K}` : K) : never
      ]: K extends string
        ? ColumnRef<
            K,
            ValueOfSqlType<NonNullable<D[K]>["sqlType"]>,
            NonNullable<D[K]>["nullable"],
            A
          >
        : never;
    };
type TableQualifier<Name extends string, Alias extends string> = Alias extends "" ? Name : Alias;
export interface TableMetadata<
  Schema extends string | null,
  Name extends string,
  D extends ColumnDefinitions,
  Alias extends string,
> {
  readonly database: string | null;
  readonly schema: Schema;
  readonly name: Name;
  readonly alias: Alias;
  readonly definitions: D;
  readonly columns: ColumnsOf<D, TableQualifier<Name, Alias>>;
  readonly indexes: ReadonlyArray<TableIndex>;
  readonly temporary: boolean;
  readonly isDynamic: boolean;
}
interface TableDescriptorMetadata<
  Schema extends string | null,
  Name extends string,
  D extends ColumnDefinitions,
  Alias extends string,
> {
  readonly $metadata: TableMetadata<Schema, Name, D, Alias>;
  readonly $script: TableScript;
  (): TableDescriptor<Schema, Name, D, AutoAlias>;
  <const A extends string>(alias: A): TableDescriptor<Schema, Name, D, A>;
  as<const A extends string>(alias: A): TableDescriptor<Schema, Name, D, A>;
}
export type TableDescriptor<
  Schema extends string | null,
  Name extends string,
  D extends ColumnDefinitions,
  Alias extends string = Name,
> = TableDescriptorMetadata<Schema, Name, D, Alias> &
  DirectColumnsOf<D, TableQualifier<Name, Alias>>;

export type TableIndexFactory<D extends ColumnDefinitions> = (
  table: TableDescriptor<string | null, string, D, "">,
) => ReadonlyArray<TableIndex>;
interface TableBlueprint<D extends ColumnDefinitions> {
  readonly definitions: D;
  readonly temporary: boolean;
  indexes: ReadonlyArray<TableIndex>;
}
export function defineTable<
  const S extends string | null,
  const N extends string,
  const D extends ColumnDefinitions,
>(definition: {
  readonly database?: string;
  readonly schema: S;
  readonly name: N;
  readonly temporary?: boolean;
  readonly columns: D;
  readonly indexes?: TableIndexFactory<D>;
}): TableDescriptor<S, N, D, ""> {
  const blueprint: TableBlueprint<D> = {
    definitions: definition.columns,
    temporary: definition.temporary ?? false,
    indexes: Object.freeze([]),
  };
  const table = makeTable(
    definition.database ?? null,
    definition.schema,
    definition.name,
    "",
    blueprint,
  );
  if (definition.indexes !== undefined) {
    const indexes = [...definition.indexes(table as TableDescriptor<string | null, string, D, "">)];
    for (const item of indexes)
      for (const indexed of item.columns)
        if (!(indexed.name in definition.columns))
          throw new TypeError(`Index references unknown column '${indexed.name}'.`);
    blueprint.indexes = Object.freeze(indexes);
  }
  return table;
}
export function aliasTable<
  S extends string | null,
  N extends string,
  D extends ColumnDefinitions,
  const A extends string,
>(table: TableDescriptor<S, N, D, string>, alias: A): TableDescriptor<S, N, D, A> {
  return table.as(alias);
}
function makeTable<
  S extends string | null,
  N extends string,
  D extends ColumnDefinitions,
  A extends string,
>(
  database: string | null,
  schema: S,
  name: N,
  alias: A,
  blueprint: TableBlueprint<D>,
  dynamic = false,
): TableDescriptor<S, N, D, A> {
  const definitions = blueprint.definitions;
  const qualifier: TableQualifier<N, A> = (alias === "" ? name : alias) as TableQualifier<N, A>;
  const refs: Record<string, ColumnRef<string, unknown, boolean, TableQualifier<N, A>>> = {};
  for (const [columnName, definition] of Object.entries(definitions))
    refs[columnName] = createColumnRef(
      columnName,
      qualifier,
      definition.nullable,
      { database, schema, table: name },
      definition.sqlType,
    );
  const direct: Record<string, ColumnRef<string, unknown, boolean, TableQualifier<N, A>>> = {};
  for (const [columnName, ref] of Object.entries(refs)) {
    const property = memberKeys.has(columnName as TableMemberKey) ? `$${columnName}` : columnName;
    if (property !== columnName && property in definitions)
      throw new TypeError(
        `Column '${columnName}' escapes to '${property}', which is also a column name.`,
      );
    direct[property] = ref;
  }
  const descriptor = ((nextAlias?: string) =>
    makeTable(
      database,
      schema,
      name,
      nextAlias === undefined ? createAutoAlias() : nextAlias,
      blueprint,
      dynamic,
    )) as TableDescriptor<S, N, D, A>;
  const metadata = Object.freeze({
    database,
    schema,
    name,
    alias,
    definitions,
    columns: Object.freeze(refs),
    get indexes() {
      return blueprint.indexes;
    },
    temporary: blueprint.temporary,
    isDynamic: dynamic,
  });
  Object.defineProperties(descriptor, {
    $metadata: { enumerable: false, value: metadata },
    $script: { enumerable: false, get: () => createTableScript(metadata) },
    as: {
      enumerable: false,
      value: <const NextAlias extends string>(nextAlias: NextAlias) =>
        makeTable(database, schema, name, nextAlias, blueprint, dynamic),
    },
  });
  for (const [property, reference] of Object.entries(direct))
    Object.defineProperty(descriptor, property, { enumerable: true, value: reference });
  return Object.freeze(descriptor);
}

const memberKeys = new Set<TableMemberKey>(["$metadata", "$script", "as"]);
export type ColumnValue<C> =
  C extends ColumnRef<string, infer V, infer N, string> ? (N extends true ? V | null : V) : never;
