import {
  aliasTable,
  column,
  defineTable,
  nullableColumn,
  sqlType,
  type ColumnValue,
} from "../../src/index.js";

const users = defineTable({
  schema: "dbo",
  name: "Users",
  columns: {
    id: column(sqlType.int32),
    name: nullableColumn(sqlType.string(255)),
    version: column(sqlType.int64),
  },
});
const aliased = users.as("u");
const calledAlias = users("called");
const calledAliasName: "called" = calledAlias.$metadata.alias;
const automatic = users();
const idAlias: "u" = aliased.id.sourceAlias;
const idName: "id" = aliased.id.name;
const id: ColumnValue<typeof aliased.id> = 1;
const name: ColumnValue<typeof aliased.name> = null;
const version: ColumnValue<typeof aliased.version> = 1n;
const directId: ColumnValue<typeof aliased.id> = 1;
const collision = defineTable({
  schema: "dbo",
  name: "Collision",
  columns: { columns: column(sqlType.int32), name: column(sqlType.string()) },
});
const escapedColumns: ColumnValue<typeof collision.columns> = 1;
const escapedName: ColumnValue<typeof collision.name> = "value";
const asCollision = defineTable({
  schema: "dbo",
  name: "AsCollision",
  columns: { as: column(sqlType.int32) },
});
const escapedAs: ColumnValue<typeof asCollision.$as> = 1;
void [
  idAlias,
  idName,
  id,
  name,
  version,
  directId,
  escapedColumns,
  escapedName,
  escapedAs,
  aliasTable,
  calledAliasName,
  automatic,
];
const tableName: "Users" = users.$metadata.name;
const schemaName: "dbo" = users.$metadata.schema;
void [tableName, schemaName];
// @ts-expect-error Metadata is available only under $metadata.
users.schema;
// @ts-expect-error Unknown columns are rejected.
users.missing;
// @ts-expect-error Int64 values are bigint, not number.
const badVersion: ColumnValue<typeof users.version> = 1;
void badVersion;
