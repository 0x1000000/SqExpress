import type { ExprBoolean, ExprValue } from "../ast/generated/ast.generated.js";

export type SqlTypeName = "boolean" | "int16" | "int32" | "int64" | "decimal" | "double" | "string" | "guid" | "binary" | "dateTime" | "dateTimeOffset";
declare const sqlTypeValue: unique symbol;
declare const columnRefValue: unique symbol;
declare const autoAliasType: unique symbol;
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
export function isAutoAlias(alias: string): alias is AutoAlias { return automaticAliases.has(alias); }

export interface SqlType<T, N extends SqlTypeName = SqlTypeName> {
  readonly name: N;
  readonly arguments: ReadonlyArray<number>;
  readonly [sqlTypeValue]?: T;
}
export type ValueOfSqlType<T> = T extends SqlType<infer V, SqlTypeName> ? V : never;

function scalar<T, const N extends SqlTypeName>(name: N, ...args: number[]): SqlType<T, N> { return Object.freeze({ name, arguments: Object.freeze(args) }); }
export const sqlType = {
  boolean: scalar<boolean, "boolean">("boolean"), int16: scalar<number, "int16">("int16"), int32: scalar<number, "int32">("int32"),
  int64: scalar<bigint, "int64">("int64"), double: scalar<number, "double">("double"), guid: scalar<string, "guid">("guid"),
  dateTime: scalar<string, "dateTime">("dateTime"), dateTimeOffset: scalar<string, "dateTimeOffset">("dateTimeOffset"),
  string: (size?: number): SqlType<string, "string"> => scalar("string", ...(size === undefined ? [] : [size])),
  decimal: (precision: number, scale: number): SqlType<string, "decimal"> => scalar("decimal", precision, scale),
  binary: (size?: number): SqlType<Uint8Array, "binary"> => scalar("binary", ...(size === undefined ? [] : [size]))
} as const;

export interface ColumnDefinition<T extends SqlType<unknown>, Nullable extends boolean> { readonly sqlType: T; readonly nullable: Nullable; }
export function column<const T extends SqlType<unknown>>(type: T): ColumnDefinition<T, false> { return Object.freeze({ sqlType: type, nullable: false }); }
export function nullableColumn<const T extends SqlType<unknown>>(type: T): ColumnDefinition<T, true> { return Object.freeze({ sqlType: type, nullable: true }); }
export type ColumnDefinitions = Readonly<Record<string, ColumnDefinition<SqlType<unknown>, boolean>>>;

export type ColumnPredicate = ExprBoolean & {
  and(right: ExprBoolean): ColumnPredicate;
  or(right: ExprBoolean): ColumnPredicate;
  not(): ColumnPredicate;
};
type ColumnOperand<Value, Nullable extends boolean> = Value | (Nullable extends true ? null : never) | ExprValue | ColumnRef<string, Exclude<Value, null>, boolean, string>;
export interface ColumnRef<Name extends string, Value, Nullable extends boolean, Alias extends string> {
  readonly name: Name; readonly sourceAlias: Alias; readonly nullable: Nullable; readonly [columnRefValue]?: Value;
  eq(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  neq(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  gt(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  gte(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  lt(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
  lte(right: ColumnOperand<Value, Nullable>): ColumnPredicate;
}

type ColumnOperatorName = "eq" | "neq" | "gt" | "gte" | "lt" | "lte";
type RuntimeColumn = ColumnRef<string, unknown, boolean, string>;
type ColumnOperatorFactory = (operator: ColumnOperatorName, left: RuntimeColumn, right: unknown) => ColumnPredicate;
let columnOperatorFactory: ColumnOperatorFactory | null = null;
/** @internal Connects descriptor methods to the expression builder without a module cycle. */
export function registerColumnOperatorFactory(factory: ColumnOperatorFactory): void { columnOperatorFactory = factory; }
/** @internal Creates the immutable runtime representation shared by physical and derived columns. */
export function createColumnRef<Name extends string, Value, Nullable extends boolean, Alias extends string>(name: Name, sourceAlias: Alias, nullable: Nullable): ColumnRef<Name, Value, Nullable, Alias> {
  let reference: ColumnRef<Name, Value, Nullable, Alias>;
  const invoke = (operator: ColumnOperatorName, right: unknown): ColumnPredicate => {
    if (columnOperatorFactory === null) throw new Error("Sqyra column operators are not initialized. Import descriptors through the 'sqyra' package entry point.");
    return columnOperatorFactory(operator, reference, right);
  };
  reference = {
    name, sourceAlias, nullable,
    eq: right => invoke("eq", right), neq: right => invoke("neq", right),
    gt: right => invoke("gt", right), gte: right => invoke("gte", right),
    lt: right => invoke("lt", right), lte: right => invoke("lte", right),
  };
  for (const operator of ["eq", "neq", "gt", "gte", "lt", "lte"] as const) Object.defineProperty(reference, operator, { enumerable: false });
  return Object.freeze(reference);
}
export type ColumnsOf<D extends ColumnDefinitions, A extends string> = { readonly [K in keyof D]: K extends string ? ColumnRef<K, ValueOfSqlType<D[K]["sqlType"]>, D[K]["nullable"], A> : never };
export type TableMemberKey = "$metadata" | "as";
export type DirectColumnsOf<D extends ColumnDefinitions, A extends string> = string extends keyof D ? {} : {
  readonly [K in keyof D as K extends string ? K extends TableMemberKey ? `$${K}` : K : never]:
    K extends string ? ColumnRef<K, ValueOfSqlType<D[K]["sqlType"]>, D[K]["nullable"], A> : never
};
type TableQualifier<Name extends string, Alias extends string> = Alias extends "" ? Name : Alias;
export interface TableMetadata<Schema extends string | null, Name extends string, D extends ColumnDefinitions, Alias extends string> {
  readonly database: string | null;
  readonly schema: Schema;
  readonly name: Name;
  readonly alias: Alias;
  readonly definitions: D;
  readonly columns: ColumnsOf<D, TableQualifier<Name, Alias>>;
  readonly isDynamic: boolean;
}
interface TableDescriptorMetadata<Schema extends string | null, Name extends string, D extends ColumnDefinitions, Alias extends string> {
  readonly $metadata: TableMetadata<Schema, Name, D, Alias>;
  (): TableDescriptor<Schema, Name, D, AutoAlias>;
  <const A extends string>(alias: A): TableDescriptor<Schema, Name, D, A>;
  as<const A extends string>(alias: A): TableDescriptor<Schema, Name, D, A>;
}
export type TableDescriptor<Schema extends string | null, Name extends string, D extends ColumnDefinitions, Alias extends string = Name> =
  TableDescriptorMetadata<Schema, Name, D, Alias> & DirectColumnsOf<D, TableQualifier<Name, Alias>>;

export function defineTable<const S extends string | null, const N extends string, const D extends ColumnDefinitions>(definition: { readonly database?: string; readonly schema: S; readonly name: N; readonly columns: D }): TableDescriptor<S, N, D, ""> {
  return makeTable(definition.database ?? null, definition.schema, definition.name, "", definition.columns);
}
export function aliasTable<S extends string | null, N extends string, D extends ColumnDefinitions, const A extends string>(table: TableDescriptor<S, N, D, string>, alias: A): TableDescriptor<S, N, D, A> {
  return table.as(alias);
}
function makeTable<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(database: string | null, schema: S, name: N, alias: A, definitions: D, dynamic = false): TableDescriptor<S, N, D, A> {
  const qualifier: TableQualifier<N, A> = (alias === "" ? name : alias) as TableQualifier<N, A>;
  const refs: Record<string, ColumnRef<string, unknown, boolean, TableQualifier<N, A>>> = {};
  for (const [columnName, definition] of Object.entries(definitions)) refs[columnName] = createColumnRef(columnName, qualifier, definition.nullable);
  const direct: Record<string, ColumnRef<string, unknown, boolean, TableQualifier<N, A>>> = {};
  for (const [columnName, ref] of Object.entries(refs)) {
    const property = memberKeys.has(columnName as TableMemberKey) ? `$${columnName}` : columnName;
    if (property !== columnName && property in definitions) throw new TypeError(`Column '${columnName}' escapes to '${property}', which is also a column name.`);
    direct[property] = ref;
  }
  const descriptor = ((nextAlias?: string) => makeTable(database, schema, name, nextAlias === undefined ? createAutoAlias() : nextAlias, definitions, dynamic)) as TableDescriptor<S, N, D, A>;
  const metadata = Object.freeze({ database, schema, name, alias, definitions, columns: Object.freeze(refs), isDynamic: dynamic });
  Object.defineProperties(descriptor, {
    $metadata: { enumerable: false, value: metadata },
    as: { enumerable: false, value: <const NextAlias extends string>(nextAlias: NextAlias) => makeTable(database, schema, name, nextAlias, definitions, dynamic) },
  });
  for (const [property, reference] of Object.entries(direct)) Object.defineProperty(descriptor, property, { enumerable: true, value: reference });
  return Object.freeze(descriptor);
}

const memberKeys = new Set<TableMemberKey>(["$metadata", "as"]);
export type DynamicTableDescriptor = TableDescriptor<string | null, string, ColumnDefinitions, string> & { readonly $metadata: TableMetadata<string | null, string, ColumnDefinitions, string> & { readonly isDynamic: true } };
export function defineDynamicTable(schema: string | null, name: string, definitions: ColumnDefinitions): DynamicTableDescriptor {
  return makeTable(null, schema, name, "", definitions, true) as DynamicTableDescriptor;
}
export type ColumnValue<C> = C extends ColumnRef<string, infer V, infer N, string> ? N extends true ? V | null : V : never;
