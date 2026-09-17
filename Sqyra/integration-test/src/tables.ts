import {
  column,
  defineTable,
  exprGetUtcDate,
  indexDesc,
  nullableColumn,
  sqlType,
  tableIndex,
  type InlineExportOptions,
  type TableScript,
} from "sqyra";
import type { IntegrationDialect } from "./types.js";

export interface ScriptedTable {
  readonly $script: TableScript;
}
const unicode = (dialect: IntegrationDialect): boolean =>
  dialect === "tsql" || dialect === "sqlite";
const mysqlFamily = (dialect: IntegrationDialect): boolean =>
  dialect === "mysql-oracle" || dialect === "mariadb";

export function defineIntegrationTables(dialect: IntegrationDialect) {
  const fk0 = defineTable({
    schema: "dbo",
    name: "Fk0",
    columns: { Id: column(sqlType.int32, { primaryKey: true }) },
  });
  const fk1A = defineTable({
    schema: "dbo",
    name: "Fk1A",
    columns: {
      Id: column(sqlType.int32, { primaryKey: true }),
      Parent: column(sqlType.int32, { references: fk0.Id }),
    },
  });
  const fk1B = defineTable({
    schema: "dbo",
    name: "Fk1B",
    columns: {
      Id: column(sqlType.int32, { primaryKey: true }),
      Parent: column(sqlType.int32, { references: fk0.Id }),
    },
  });
  const company = defineTable({
    schema: "dbo",
    name: "ItCompany",
    columns: {
      CompanyId: column(sqlType.int32, { primaryKey: true, identity: true }),
      ExternalId: column(sqlType.guid),
      CompanyName: column(sqlType.string(250, { unicode: unicode(dialect) })),
      Version: column(sqlType.int32),
      Created: column(sqlType.dateTime),
      Modified: column(sqlType.dateTime),
    },
    indexes: (t) => [tableIndex(t.ExternalId, { unique: true }), tableIndex(t.CompanyName)],
  });
  const user = defineTable({
    schema: "dbo",
    name: "ItUser",
    columns: {
      UserId: column(sqlType.int32, { primaryKey: true, identity: true }),
      ExternalId: column(sqlType.guid),
      FirstName: column(sqlType.string(255, { unicode: unicode(dialect) })),
      LastName: column(sqlType.string(255, { unicode: unicode(dialect) })),
      Email: column(sqlType.string(255, { unicode: unicode(dialect) })),
      RegDate: column(sqlType.dateTime),
      Version: column(sqlType.int32, { default: 0 }),
      Created: column(sqlType.dateTime, { default: exprGetUtcDate }),
      Modified: column(sqlType.dateTime, { default: exprGetUtcDate }),
    },
    indexes: (t) => [
      tableIndex(t.ExternalId, {
        unique: true,
        clustered: !mysqlFamily(dialect) && dialect !== "sqlite",
      }),
      tableIndex(t.FirstName),
      tableIndex(indexDesc(t.LastName)),
    ],
  });
  const fk2AB = defineTable({
    schema: "dbo",
    name: "Fk2AB",
    columns: {
      Id: column(sqlType.int32, { primaryKey: true }),
      Parent0: column(sqlType.int32, { references: fk0.Id }),
      ParentA: column(sqlType.int32, { references: fk1A.Id }),
      ParentB: column(sqlType.int32, { references: fk1B.Id }),
    },
    indexes: (t) => [tableIndex([t.ParentA, t.ParentB], { unique: true })],
  });
  const customer = defineTable({
    schema: "dbo",
    name: "ItCustomer",
    columns: {
      CustomerId: column(sqlType.int32, { primaryKey: true, identity: true }),
      UserId: nullableColumn(sqlType.int32, { references: user.UserId }),
      CompanyId: nullableColumn(sqlType.int32, { references: company.CompanyId }),
    },
    indexes: (t) => [
      tableIndex([t.UserId, t.CompanyId], { unique: true }),
      tableIndex([t.CompanyId, t.UserId], { unique: true }),
    ],
  });
  const fk3AB = defineTable({
    schema: "dbo",
    name: "Fk3AB",
    columns: {
      Id: column(sqlType.int32, { primaryKey: true }),
      Parent0: column(sqlType.int32, { references: fk0.Id }),
      ParentA: column(sqlType.int32, { references: [fk1A.Id, fk2AB.ParentA] }),
      ParentB: column(sqlType.int32, { references: [fk1B.Id, fk2AB.ParentB] }),
    },
  });
  const order = defineTable({
    schema: "dbo",
    name: "ItOrder",
    columns: {
      OrderId: column(sqlType.int32, { primaryKey: true, identity: true }),
      CustomerId: column(sqlType.int32, { references: customer.CustomerId }),
      DateCreated: column(sqlType.dateTime, { default: exprGetUtcDate }),
      Notes: nullableColumn(sqlType.string(100, { unicode: true })),
    },
    indexes: (t) => [tableIndex(t.CustomerId)],
  });
  const allTypes = defineAllTypes(dialect);
  const ordered = [
    fk0,
    allTypes,
    fk1A,
    fk1B,
    company,
    user,
    fk2AB,
    customer,
    fk3AB,
    order,
  ] as const;
  return Object.freeze({
    fk0,
    allTypes,
    fk1A,
    fk1B,
    company,
    user,
    fk2AB,
    customer,
    fk3AB,
    order,
    ordered,
  });
}

function defineAllTypes(dialect: IntegrationDialect) {
  const includeByte = dialect !== "pgsql";
  const includeOffset = !mysqlFamily(dialect);
  const stringMax = mysqlFamily(dialect) ? 1000 : undefined;
  return defineTable({
    schema: "dbo",
    name: "ItAllColumnTypes",
    columns: {
      Id: column(sqlType.int32, { primaryKey: true, identity: true }),
      ColBoolean: column(sqlType.boolean),
      ColNullableBoolean: nullableColumn(sqlType.boolean),
      ...(includeByte
        ? { ColByte: column(sqlType.byte), ColNullableByte: nullableColumn(sqlType.byte) }
        : {}),
      ColInt16: column(sqlType.int16),
      ColNullableInt16: nullableColumn(sqlType.int16),
      ColInt32: column(sqlType.int32),
      ColNullableInt32: nullableColumn(sqlType.int32),
      ColInt64: column(sqlType.int64),
      ColNullableInt64: nullableColumn(sqlType.int64),
      ColDecimal: column(sqlType.decimal(10, 6)),
      ColNullableDecimal: nullableColumn(sqlType.decimal(10, 6)),
      ColDouble: column(sqlType.double),
      ColNullableDouble: nullableColumn(sqlType.double),
      ColDateTime: column(sqlType.dateTime),
      ColNullableDateTime: nullableColumn(sqlType.dateTime),
      ColGuid: column(sqlType.guid),
      ColNullableGuid: nullableColumn(sqlType.guid),
      ColStringUnicode: column(sqlType.string(stringMax, { unicode: unicode(dialect) })),
      ColNullableStringUnicode: nullableColumn(
        sqlType.string(stringMax, { unicode: unicode(dialect) }),
      ),
      ColStringMax: column(sqlType.string(stringMax, { unicode: false })),
      ColNullableStringMax: nullableColumn(sqlType.string(stringMax, { unicode: false })),
      ColString5: column(sqlType.string(5, { unicode: false })),
      ColByteArraySmall: column(sqlType.binary(mysqlFamily(dialect) ? undefined : 255)),
      ColByteArrayBig: column(sqlType.binary()),
      ColNullableByteArraySmall: nullableColumn(
        sqlType.binary(mysqlFamily(dialect) ? undefined : 255),
      ),
      ColNullableByteArrayBig: nullableColumn(sqlType.binary()),
      ColFixedSizeString: column(sqlType.string(3, { unicode: false, fixed: true })),
      ColNullableFixedSizeString: nullableColumn(
        sqlType.string(3, { unicode: unicode(dialect), fixed: true }),
      ),
      ...(dialect === "pgsql"
        ? {}
        : {
            ColFixedSizeByteArray: column(sqlType.binary(2, { fixed: true })),
            ColNullableFixedSizeByteArray: nullableColumn(sqlType.binary(2, { fixed: true })),
          }),
      ColXml: column(
        mysqlFamily(dialect)
          ? sqlType.string(undefined, { unicode: true, text: true })
          : sqlType.xml,
      ),
      ColNullableXml: nullableColumn(
        mysqlFamily(dialect)
          ? sqlType.string(undefined, { unicode: true, text: true })
          : sqlType.xml,
      ),
      ...(includeOffset
        ? {
            ColDateTimeOffset: column(sqlType.dateTimeOffset),
            ColNullableDateTimeOffset: nullableColumn(sqlType.dateTimeOffset),
          }
        : {}),
    },
  });
}

export async function recreateTables(
  tables: ReadonlyArray<ScriptedTable>,
  database: { executeScript(sql: string): Promise<void> },
  options: InlineExportOptions,
): Promise<void> {
  for (const table of [...tables].reverse())
    await database.executeScript(table.$script.dropIfExists().toSql(options));
  for (const table of tables) await database.executeScript(table.$script.create().toSql(options));
}
