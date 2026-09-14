export type SqlTypeName = "boolean" | "int16" | "int32" | "int64" | "decimal" | "double" | "string" | "guid" | "binary" | "dateTime" | "dateTimeOffset";

export interface SqlType<T, N extends SqlTypeName = SqlTypeName> {
  readonly name: N;
  readonly arguments: ReadonlyArray<number>;
  readonly __value?: T;
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

export interface ColumnRef<Name extends string, Value, Nullable extends boolean, Alias extends string> {
  readonly name: Name; readonly sourceAlias: Alias; readonly nullable: Nullable; readonly __value?: Value;
}
export type ColumnsOf<D extends ColumnDefinitions, A extends string> = { readonly [K in keyof D]: K extends string ? ColumnRef<K, ValueOfSqlType<D[K]["sqlType"]>, D[K]["nullable"], A> : never };
export type TableMetadataKey = "database" | "schema" | "name" | "alias" | "definitions" | "columns" | "dynamic" | "derivedSource";
export type DirectColumnsOf<D extends ColumnDefinitions, A extends string> = string extends keyof D ? {} : {
  readonly [K in keyof D as K extends string ? K extends TableMetadataKey ? `$${K}` : K : never]:
    K extends string ? ColumnRef<K, ValueOfSqlType<D[K]["sqlType"]>, D[K]["nullable"], A> : never
};
interface TableDescriptorMetadata<Schema extends string | null, Name extends string, D extends ColumnDefinitions, Alias extends string> {
  readonly database: string | null; readonly schema: Schema; readonly name: Name; readonly alias: Alias; readonly definitions: D; readonly columns: ColumnsOf<D, Alias>;
}
export type TableDescriptor<Schema extends string | null, Name extends string, D extends ColumnDefinitions, Alias extends string = Name> =
  TableDescriptorMetadata<Schema, Name, D, Alias> & DirectColumnsOf<D, Alias>;

export function defineTable<const S extends string | null, const N extends string, const D extends ColumnDefinitions>(definition: { readonly database?: string; readonly schema: S; readonly name: N; readonly columns: D }): TableDescriptor<S, N, D, N> {
  return makeTable(definition.database ?? null, definition.schema, definition.name, definition.name, definition.columns);
}
export function aliasTable<S extends string | null, N extends string, D extends ColumnDefinitions, const A extends string>(table: TableDescriptor<S, N, D, string>, alias: A): TableDescriptor<S, N, D, A> {
  return makeTable(table.database, table.schema, table.name, alias, table.definitions);
}
function makeTable<S extends string | null, N extends string, D extends ColumnDefinitions, A extends string>(database: string | null, schema: S, name: N, alias: A, definitions: D): TableDescriptor<S, N, D, A> {
  const refs: Record<string, ColumnRef<string, unknown, boolean, A>> = {};
  for (const [columnName, definition] of Object.entries(definitions)) refs[columnName] = Object.freeze({ name: columnName, sourceAlias: alias, nullable: definition.nullable });
  const direct: Record<string, ColumnRef<string, unknown, boolean, A>> = {};
  for (const [columnName, ref] of Object.entries(refs)) {
    const property = metadataKeys.has(columnName as TableMetadataKey) ? `$${columnName}` : columnName;
    if (property !== columnName && property in definitions) throw new TypeError(`Column '${columnName}' escapes to '${property}', which is also a column name.`);
    direct[property] = ref;
  }
  return Object.freeze({ database, schema, name, alias, definitions, columns: Object.freeze(refs), ...direct }) as TableDescriptor<S, N, D, A>;
}

const metadataKeys = new Set<TableMetadataKey>(["database", "schema", "name", "alias", "definitions", "columns", "dynamic", "derivedSource"]);
export type DynamicTableDescriptor = TableDescriptor<string | null, string, ColumnDefinitions, string> & { readonly dynamic: true };
export function defineDynamicTable(schema: string | null, name: string, definitions: ColumnDefinitions): DynamicTableDescriptor {
  return Object.freeze({ ...makeTable(null, schema, name, name, definitions), dynamic: true });
}
export type ColumnValue<C> = C extends ColumnRef<string, infer V, infer N, string> ? N extends true ? V | null : V : never;
