import type { ExprValue } from "../ast/generated/ast.generated.js";
import {
  getColumnOrigin,
  type ColumnDefaultValue,
  type ColumnDefinitions,
  type ColumnRef,
  type SqlType,
  type TableMetadata,
} from "../descriptors/index.js";
import {
  normalizeExportOptions,
  renderSqlText,
  type ExportOptions,
  type InlineExportOptions,
  type SqlDialect,
} from "../exporters/index.js";

export interface TableIndexColumn {
  readonly name: string;
  readonly descending: boolean;
}
export interface TableIndex {
  readonly columns: ReadonlyArray<TableIndexColumn>;
  readonly name: string | null;
  readonly unique: boolean;
  readonly clustered: boolean;
}
export interface TableIndexOptions {
  readonly name?: string;
  readonly unique?: boolean;
  readonly clustered?: boolean;
}
export interface DescendingIndexColumn {
  readonly column: ColumnRef<string, unknown, boolean, string>;
  readonly descending: true;
}
export type IndexColumnInput = ColumnRef<string, unknown, boolean, string> | DescendingIndexColumn;

export function indexDesc(
  column: ColumnRef<string, unknown, boolean, string>,
): DescendingIndexColumn {
  return Object.freeze({ column, descending: true });
}
export function tableIndex(
  columns: IndexColumnInput | ReadonlyArray<IndexColumnInput>,
  options: TableIndexOptions = {},
): TableIndex {
  const input = Array.isArray(columns) ? columns : [columns];
  if (input.length === 0) throw new TypeError("An index requires at least one column.");
  return Object.freeze({
    columns: Object.freeze(
      input.map((item) =>
        "column" in item
          ? { name: item.column.name, descending: true }
          : { name: item.name, descending: false },
      ),
    ),
    name: options.name ?? null,
    unique: options.unique ?? false,
    clustered: options.clustered ?? false,
  });
}

export interface TableScriptCommand {
  readonly kind: "create" | "drop" | "dropIfExists" | "dropAndCreate";
  toSql(options: SqlDialect | InlineExportOptions): string;
}
export interface TableScript {
  create(): TableScriptCommand;
  drop(): TableScriptCommand;
  dropIfExists(): TableScriptCommand;
  dropAndCreate(): TableScriptCommand;
}

type Metadata = TableMetadata<string | null, string, ColumnDefinitions, string>;
export function createTableScript(metadata: Metadata): TableScript {
  const command = (kind: TableScriptCommand["kind"]): TableScriptCommand =>
    Object.freeze({
      kind,
      toSql: (options: SqlDialect | InlineExportOptions) =>
        render(metadata, kind, normalizeExportOptions(options)),
    });
  return Object.freeze({
    create: () => command("create"),
    drop: () => command("drop"),
    dropIfExists: () => command("dropIfExists"),
    dropAndCreate: () => command("dropAndCreate"),
  });
}

const replace = (value: string, search: string, replacement: string): string =>
  value.split(search).join(replacement);
const quote = (value: string, dialect: SqlDialect): string =>
  dialect === "tsql"
    ? `[${replace(value, "]", "]]")}]`
    : dialect === "mysql"
      ? `\`${replace(value, "`", "``")}\``
      : `"${replace(value, '"', '""')}"`;

function mappedSchema(metadata: Metadata, options: ExportOptions): string | null {
  if (metadata.schema === null) return null;
  return options.schemaMap?.find((item) => item.from === metadata.schema)?.to ?? metadata.schema;
}
function tableName(metadata: Metadata, dialect: SqlDialect, options: ExportOptions): string {
  const schema =
    dialect === "mysql" || dialect === "sqlite" ? null : mappedSchema(metadata, options);
  const database = dialect === "sqlite" ? null : metadata.database;
  const parts = [database, schema, metadata.name].filter(
    (part): part is string => part !== null && part !== "",
  );
  return parts.map((part) => quote(part, dialect)).join(".");
}
function generatedName(
  prefix: string,
  metadata: Metadata,
  options: ExportOptions,
  suffix = "",
): string {
  const schema = mappedSchema(metadata, options);
  return `${prefix}_${schema ? `${schema}_` : ""}${metadata.name}${suffix}`;
}
function typeSql(type: SqlType<unknown>, dialect: SqlDialect): string {
  const { name, arguments: args } = type;
  const size = args[0];
  const map: Record<string, Record<SqlDialect, string>> = {
    boolean: { tsql: "bit", pgsql: "bool", mysql: "boolean", sqlite: "integer" },
    byte: { tsql: "tinyint", pgsql: "int2", mysql: "tinyint unsigned", sqlite: "integer" },
    int16: { tsql: "smallint", pgsql: "int2", mysql: "smallint", sqlite: "integer" },
    int32: { tsql: "int", pgsql: "int4", mysql: "int", sqlite: "integer" },
    int64: { tsql: "bigint", pgsql: "int8", mysql: "bigint", sqlite: "integer" },
    double: { tsql: "float", pgsql: "float8", mysql: "double", sqlite: "real" },
    guid: { tsql: "uniqueidentifier", pgsql: "uuid", mysql: "char(36)", sqlite: "text" },
    date: { tsql: "date", pgsql: "date", mysql: "date", sqlite: "text" },
    dateTime: { tsql: "datetime2", pgsql: "timestamp", mysql: "datetime", sqlite: "text" },
    dateTimeOffset: {
      tsql: "datetimeoffset",
      pgsql: "timestamptz",
      mysql: "datetime",
      sqlite: "text",
    },
    binary: {
      tsql: type.fixed
        ? `[binary](${size})`
        : size === undefined
          ? "[varbinary](MAX)"
          : `[varbinary](${size})`,
      pgsql: "bytea",
      mysql: type.fixed
        ? `binary(${size})`
        : size === undefined
          ? "longblob"
          : `varbinary(${size})`,
      sqlite: "blob",
    },
    string: {
      tsql: type.text
        ? type.unicode === false
          ? "[varchar](MAX)"
          : "[nvarchar](MAX)"
        : `[${type.unicode === false ? (type.fixed ? "char" : "varchar") : type.fixed ? "nchar" : "nvarchar"}](${size ?? "MAX"})`,
      pgsql:
        type.text || size === undefined
          ? "text"
          : type.fixed
            ? `character(${size})`
            : `character varying(${size})`,
      mysql: type.text ? "text" : `${type.fixed ? "char" : "varchar"}(${size ?? 255})`,
      sqlite: "text",
    },
    xml: { tsql: "xml", pgsql: "xml", mysql: "text", sqlite: "text" },
    decimal: {
      tsql: `decimal(${args[0]},${args[1]})`,
      pgsql: `numeric(${args[0]},${args[1]})`,
      mysql: `decimal(${args[0]},${args[1]})`,
      sqlite: "numeric",
    },
  };
  const result = map[name]?.[dialect];
  if (result === undefined) throw new TypeError(`Unsupported SQL type '${name}' for ${dialect}.`);
  return result;
}
function defaultSql(value: ColumnDefaultValue, options: ExportOptions): string {
  if (value !== null && typeof value === "object" && "kind" in value)
    return renderSqlText(value as ExprValue, options);
  if (value === null) return "NULL";
  if (typeof value === "boolean")
    return options.dialect === "pgsql" ? String(value) : value ? "1" : "0";
  if (typeof value === "number" || typeof value === "bigint") return String(value);
  if (typeof value === "string") return `'${replace(value, "'", "''")}'`;
  const hex = Array.from(value, (byte) => byte.toString(16).padStart(2, "0")).join("");
  return options.dialect === "pgsql" ? `'\\x${hex}'` : `0x${hex}`;
}
function render(
  metadata: Metadata,
  kind: TableScriptCommand["kind"],
  options: ExportOptions,
): string {
  const dialect = options.dialect;
  const physicalName =
    dialect === "tsql" && metadata.temporary ? `#${metadata.name}` : metadata.name;
  const physicalMetadata = metadata.temporary
    ? { ...metadata, schema: null, name: physicalName }
    : metadata;
  const physical = tableName(physicalMetadata, dialect, options);
  const temp = metadata.temporary;
  const drop =
    kind === "drop" || kind === "dropAndCreate"
      ? `DROP TABLE ${physical};`
      : kind === "dropIfExists"
        ? `DROP TABLE IF EXISTS ${physical};`
        : "";
  if (kind === "drop" || kind === "dropIfExists") return drop;
  const primary = Object.entries(metadata.definitions).filter(([, d]) => d.options.primaryKey);
  const columns = Object.entries(metadata.definitions).map(([name, definition]) => {
    const sqliteIdentityPk =
      dialect === "sqlite" && definition.options.identity && definition.options.primaryKey;
    let text = `${quote(name, dialect)} ${sqliteIdentityPk ? "INTEGER PRIMARY KEY AUTOINCREMENT" : typeSql(definition.sqlType, dialect)}`;
    if (!definition.nullable && !sqliteIdentityPk) text += " NOT NULL";
    if (definition.options.identity && !sqliteIdentityPk)
      text +=
        dialect === "tsql"
          ? "  IDENTITY (1, 1)"
          : dialect === "pgsql"
            ? "  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 )"
            : dialect === "mysql"
              ? " AUTO_INCREMENT"
              : "";
    if (definition.options.default !== undefined)
      text += ` DEFAULT (${defaultSql(definition.options.default, options)})`;
    return text;
  });
  if (
    primary.length > 0 &&
    !(dialect === "sqlite" && primary.length === 1 && primary[0]![1].options.identity)
  ) {
    const constraint =
      dialect === "mysql" || dialect === "sqlite"
        ? ""
        : `CONSTRAINT ${quote(generatedName("PK", metadata, options), dialect)} `;
    columns.push(
      `${constraint}PRIMARY KEY (${primary.map(([name]) => quote(name, dialect)).join(",")})`,
    );
  }
  const foreignGroups = new Map<
    string,
    {
      readonly origin: NonNullable<ReturnType<typeof getColumnOrigin>>;
      readonly pairs: Array<{ local: string; foreign: string }>;
    }
  >();
  for (const [local, definition] of Object.entries(metadata.definitions)) {
    const configured = definition.options.references;
    if (configured === undefined) continue;
    const references = Array.isArray(configured) ? configured : [configured];
    for (const item of references) {
      const reference = typeof item === "function" ? item() : item;
      const origin = getColumnOrigin(reference);
      if (origin === undefined)
        throw new TypeError(`Foreign key '${local}' must reference a physical table column.`);
      const key = `${origin.database ?? ""}\0${origin.schema ?? ""}\0${origin.table}`;
      const group = foreignGroups.get(key) ?? { origin, pairs: [] };
      group.pairs.push({ local, foreign: reference.name });
      foreignGroups.set(key, group);
    }
  }
  for (const group of foreignGroups.values()) {
    const schema = mappedSchema(metadata, options);
    const schemaPrefix = schema === null ? "" : `${schema}__`;
    const fkName = `FK_${schemaPrefix}${metadata.name}_to_${schemaPrefix}${group.origin.table}`;
    const foreignMetadata = {
      ...metadata,
      database: group.origin.database,
      schema: group.origin.schema,
      name: group.origin.table,
    };
    columns.push(
      `CONSTRAINT ${quote(fkName, dialect)} FOREIGN KEY (${group.pairs.map((p) => quote(p.local, dialect)).join(",")}) REFERENCES ${tableName(foreignMetadata, dialect, options)}(${group.pairs.map((p) => quote(p.foreign, dialect)).join(",")})`,
    );
  }
  const inlineIndexes =
    dialect === "tsql" || dialect === "mysql"
      ? metadata.indexes.map((item) => {
          const suffix = item.columns
            .map((c) => `_${c.name}${c.descending ? "_DESC" : ""}`)
            .join("");
          const name = quote(item.name ?? generatedName("IX", metadata, options, suffix), dialect);
          const cols = item.columns
            .map((c) => `${quote(c.name, dialect)}${c.descending ? " DESC" : ""}`)
            .join(",");
          return dialect === "tsql"
            ? `INDEX ${name}${item.unique ? " UNIQUE" : ""}${item.clustered ? " CLUSTERED" : ""}(${cols})`
            : `${item.unique ? "UNIQUE KEY" : "INDEX"} ${name}(${cols})`;
        })
      : [];
  columns.push(...inlineIndexes);
  const create = `CREATE ${temp ? (dialect === "pgsql" ? "TEMP " : dialect === "mysql" ? "TEMPORARY " : "") : ""}TABLE ${physical}(${columns.join(",")});`;
  const external =
    dialect === "pgsql" || dialect === "sqlite"
      ? metadata.indexes
          .map((item) => {
            const suffix = item.columns
              .map((c) => `_${c.name}${c.descending ? "_DESC" : ""}`)
              .join("");
            const name = item.name ?? generatedName("IX", metadata, options, suffix);
            const cols = item.columns
              .map((c) => `${quote(c.name, dialect)}${c.descending ? " DESC" : ""}`)
              .join(",");
            return `CREATE ${item.unique ? "UNIQUE " : ""}INDEX ${quote(name, dialect)} ON ${physical}(${cols});${dialect === "pgsql" && item.clustered ? `CLUSTER ${physical} USING ${quote(name, dialect)};` : ""}`;
          })
          .join("")
      : "";
  if (kind !== "dropAndCreate") return create + external;
  if (dialect === "tsql" && temp) {
    const escaped = `[#${replace(metadata.name, "'", "''")}]`;
    return `IF OBJECT_ID('tempdb..${escaped}') IS NOT NULL DROP TABLE ${physical}${create}${external}`;
  }
  return `DROP TABLE IF EXISTS ${physical};${create}${external}`;
}
