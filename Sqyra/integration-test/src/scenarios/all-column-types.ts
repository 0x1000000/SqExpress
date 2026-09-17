import {
  dateTimeOffsetValue,
  dateTimeValue,
  exprDateTimeLiteral,
  exprDateTimeOffsetLiteral,
  insertInto,
  select,
  type InlineExportOptions,
} from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { CanonicalRow, CanonicalValue } from "../types.js";
import type { Scenario } from "./types.js";

export const allColumnTypesScenario: Scenario = {
  source: "ScAllColumnTypes",
  async run(context) {
    const { allTypes } = defineIntegrationTables(context.dialect);
    const mysql = context.dialect === "mysql-oracle" || context.dialect === "mariadb";
    const pg = context.dialect === "pgsql";
    const options: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
      ...(context.database.schemaMap === undefined
        ? {}
        : { schemaMap: context.database.schemaMap }),
    };
    await context.database.executeScript(allTypes.$script.dropIfExists().toSql(options));
    await context.database.executeScript(allTypes.$script.create().toSql(options));
    const bytes = (shift: number, size: number) =>
      Uint8Array.from({ length: size }, (_, index) => (index + shift) & 255);
    const dto = (text: string) => exprDateTimeOffsetLiteral({ value: dateTimeOffsetValue(text) });
    const dt = (text: string) => exprDateTimeLiteral({ value: dateTimeValue(`${text}T00:00:00`) });
    const row1 = {
      Id: 1,
      ColBoolean: true,
      ColNullableBoolean: true,
      ...(pg ? {} : { ColByte: 255, ColNullableByte: 255 }),
      ColInt16: 32767,
      ColNullableInt16: 32767,
      ColInt32: 2147483647,
      ColNullableInt32: 2147483647,
      ColInt64: 9223372036854775807n,
      ColNullableInt64: 9223372036854775807n,
      ColDecimal: "2.123456",
      ColNullableDecimal: "2.123456",
      ColDouble: 2.123456,
      ColNullableDouble: 2.123456,
      ColDateTime: dt("2020-10-13"),
      ColNullableDateTime: dt("2020-10-13"),
      ColGuid: "e580d8df-78ed-4add-ac20-4c32bc8d94fc",
      ColNullableGuid: "e580d8df-78ed-4add-ac20-4c32bc8d94fc",
      ColStringUnicode: "абсдеф",
      ColNullableStringUnicode: "абсдеф",
      ColStringMax: "abcdef",
      ColNullableStringMax: "abcdef",
      ColString5: "abcd",
      ColByteArraySmall: bytes(3, 255),
      ColByteArrayBig: bytes(29, 65535 * 2),
      ColNullableByteArraySmall: bytes(17, 255),
      ColNullableByteArrayBig: bytes(17, 65535 * 2),
      ColFixedSizeString: "123",
      ColNullableFixedSizeString: "321",
      ...(pg
        ? {}
        : { ColFixedSizeByteArray: bytes(255, 2), ColNullableFixedSizeByteArray: bytes(255, 2) }),
      ColXml: "<root><Item2 /></root>",
      ColNullableXml: "<root><Item /></root>",
      ...(mysql
        ? {}
        : {
            ColDateTimeOffset: dto("2022-07-10T18:10:45+03:00"),
            ColNullableDateTimeOffset: dto("2022-07-10T18:10:46+03:00"),
          }),
    };
    const row2 = {
      Id: 2,
      ColBoolean: false,
      ColNullableBoolean: null,
      ...(pg ? {} : { ColByte: 0, ColNullableByte: null }),
      ColInt16: -32768,
      ColNullableInt16: null,
      ColInt32: -2147483648,
      ColNullableInt32: null,
      ColInt64: -9223372036854775808n,
      ColNullableInt64: null,
      ColDecimal: "-2.123456",
      ColNullableDecimal: null,
      ColDouble: -2.123456,
      ColNullableDouble: null,
      ColDateTime: dt("2020-10-14"),
      ColNullableDateTime: null,
      ColGuid: "0cff587d-2a78-4891-83f6-5ee291221dfc",
      ColNullableGuid: null,
      ColStringUnicode: "",
      ColNullableStringUnicode: null,
      ColStringMax: "",
      ColNullableStringMax: null,
      ColString5: "",
      ColByteArraySmall: bytes(7, 13),
      ColByteArrayBig: bytes(13, 17),
      ColNullableByteArraySmall: null,
      ColNullableByteArrayBig: null,
      ColFixedSizeString: "abc",
      ColNullableFixedSizeString: null,
      ...(pg ? {} : { ColFixedSizeByteArray: bytes(128, 2), ColNullableFixedSizeByteArray: null }),
      ColXml: "<root><Item3 /></root>",
      ColNullableXml: null,
      ...(mysql
        ? {}
        : { ColDateTimeOffset: dto("2022-07-10T18:10:45+03:00"), ColNullableDateTimeOffset: null }),
    };
    await context.execute(insertInto(allTypes).values(row1, row2).identity("Id"));
    const projected = Object.values(allTypes.$metadata.columns).filter(
      (value): value is Exclude<typeof value, undefined> => value !== undefined,
    );
    const rows = await context.query(select(projected).from(allTypes).orderBy(allTypes.Id));
    if (rows.length !== 2) throw new Error(`Expected two all-type rows, received ${rows.length}.`);
    assertRow(rows[0]!, row1);
    assertRow(rows[1]!, row2);
  },
};
function assertRow(actual: CanonicalRow, expected: Readonly<Record<string, unknown>>): void {
  for (const [name, value] of Object.entries(expected)) {
    const received = actual[name];
    if (!equalValue(received, value))
      throw new Error(
        `${name}: round-trip mismatch; expected ${print(value)}, received ${print(received)}.`,
      );
  }
}
function equalValue(actual: CanonicalValue | undefined, expected: unknown): boolean {
  if (expected === null) return actual === null;
  if (expected instanceof Uint8Array)
    return (
      actual instanceof Uint8Array &&
      expected.length === actual.length &&
      expected.every((value, index) => actual[index] === value)
    );
  if (
    typeof expected === "object" &&
    expected !== null &&
    "kind" in expected &&
    "value" in expected
  ) {
    const wrapped = (expected as { readonly value: { readonly value: string } }).value;
    return sameTemporal(String(actual), wrapped.value);
  }
  if (typeof expected === "bigint") return BigInt(String(actual)) === expected;
  if (typeof expected === "number") return Number(actual) === expected;
  if (typeof expected === "boolean") return actual === expected;
  if (typeof expected === "string" && /^[0-9a-f]{8}-/i.test(expected))
    return String(actual).toLowerCase() === expected.toLowerCase();
  return actual === expected;
}
function sameTemporal(actual: string, expected: string): boolean {
  if (expected.includes("+"))
    return new Date(actual).toISOString() === new Date(expected).toISOString();
  const normalized = actual.replace(" ", "T");
  return (
    normalized === expected ||
    normalized.replace(/(?:\.0+)?Z?$/, "") === expected ||
    (expected.endsWith("T00:00:00") && normalized === expected.slice(0, 10))
  );
}
function print(value: unknown): string {
  return value instanceof Uint8Array ? `[${value.length} bytes]` : String(value);
}
