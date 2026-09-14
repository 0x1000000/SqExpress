import type { SqlDialect } from "../exporters/index.js";

export type TemporaryColumnType = "int32" | "date" | "string";
export interface TemporaryColumnDefinition {
  readonly name: string;
  readonly type: TemporaryColumnType;
  readonly nullable?: boolean;
  readonly identity?: boolean;
  readonly primaryKey?: boolean;
  readonly size?: number;
}

const quote = (value: string, dialect: SqlDialect): string => dialect === "tsql" ? "[" + value.split("]").join("]]" ) + "]" : dialect === "mysql" ? `\`${value.split("`").join("``")}\`` : `"${value.split('"').join('""')}"`;

/** Emits the source-compatible drop/create script for a temporary table.
 * General DDL and StatementSyntax remain outside the stage-one AST scope. */
export function dropAndCreateTemporaryTable(name: string, columns: ReadonlyArray<TemporaryColumnDefinition>, dialect: "tsql" | "postgresql"): string {
  if (columns.length === 0) throw new TypeError("A temporary table requires at least one column.");
  const primary = columns.filter(column => column.primaryKey); if (primary.length === 0) throw new TypeError("A temporary table requires a primary key.");
  const qName = quote(dialect === "tsql" ? `#${name}` : name, dialect);
  const definitions = columns.map(column => {
    const type = column.type === "int32" ? (dialect === "tsql" ? "int" : "int4") : column.type === "date" ? "date" : dialect === "postgresql" ? `character varying(${column.size ?? 255})` : `nvarchar(${column.size ?? "MAX"})`;
    const identity = column.identity ? dialect === "tsql" ? "  IDENTITY (1, 1)" : "  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 )" : "";
    return `${quote(column.name, dialect)} ${type}${column.nullable ? "" : " NOT NULL"}${identity}`;
  });
  definitions.push(`CONSTRAINT ${quote(`PK_${name}`, dialect)} PRIMARY KEY (${primary.map(column => quote(column.name, dialect)).join(",")})`);
  if (dialect === "tsql") { const objectName = `[#${name.replace(/'/g, "''")}]`; return `IF OBJECT_ID('tempdb..${objectName}') IS NOT NULL DROP TABLE ${qName}CREATE TABLE ${qName}(${definitions.join(",")});`; }
  return `DROP TABLE IF EXISTS ${qName};CREATE TEMP TABLE ${qName}(${definitions.join(",")});`;
}
