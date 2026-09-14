import { aliasTable, column, defineTable, nullableColumn, sqlType, type ColumnValue } from "../../src/index.js";

const users = defineTable({ schema: "dbo", name: "Users", columns: { id: column(sqlType.int32), name: nullableColumn(sqlType.string(255)), version: column(sqlType.int64) } });
const aliased = aliasTable(users, "u");
const idAlias: "u" = aliased.columns.id.sourceAlias;
const idName: "id" = aliased.columns.id.name;
const id: ColumnValue<typeof aliased.columns.id> = 1;
const name: ColumnValue<typeof aliased.columns.name> = null;
const version: ColumnValue<typeof aliased.columns.version> = 1n;
const directId: ColumnValue<typeof aliased.id> = 1;
const collision = defineTable({ schema: "dbo", name: "Collision", columns: { columns: column(sqlType.int32), name: column(sqlType.string()) } });
const escapedColumns: ColumnValue<typeof collision.$columns> = 1;
const escapedName: ColumnValue<typeof collision.$name> = "value";
void [idAlias, idName, id, name, version, directId, escapedColumns, escapedName];
// @ts-expect-error Unknown columns are rejected.
users.columns.missing;
// @ts-expect-error Int64 values are bigint, not number.
const badVersion: ColumnValue<typeof users.columns.version> = 1;
void badVersion;
