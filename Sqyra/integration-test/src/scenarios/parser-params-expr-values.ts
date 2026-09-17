import {
  bindTSqlParameters,
  dateTimeOffsetValue,
  dateTimeValue,
  decimalValue,
  exprBoolLiteral,
  exprByteArrayLiteral,
  exprDateTimeLiteral,
  exprDateTimeOffsetLiteral,
  exprDecimalLiteral,
  exprGuidLiteral,
  exprNull,
  guidValue,
  parseTSql,
} from "sqyra";
import type { CanonicalRow } from "../types.js";
import type { Scenario } from "./types.js";

export const parserParamsExprValuesScenario: Scenario = {
  source: "ScParserParamsExprValues",
  async run(context) {
    const mysql = context.dialect === "mysql-oracle" || context.dialect === "mariadb";
    const aliases = mysql
      ? "@pBool [BoolV],@pByte [ByteV],@pInt16 [Int16V],@pInt32 [Int32V],@pInt64 [Int64V],@pDecimal [DecimalV],@pDouble [DoubleV],@pString [StringV],@pGuid [GuidV],@pDateTime [DateTimeV],@pBytes [BytesV],@pNull [NullV]"
      : "@pBool [BoolV],@pByte [ByteV],@pInt16 [Int16V],@pInt32 [Int32V],@pInt64 [Int64V],@pDecimal [DecimalV],@pDouble [DoubleV],@pString [StringV],@pGuid [GuidV],@pDateTime [DateTimeV],@pDateTimeOffset [DateTimeOffsetV],@pBytes [BytesV],@pNull [NullV]";
    const values = {
      pBool: exprBoolLiteral({ value: true }),
      pByte: 7,
      pInt16: 1234,
      pInt32: 567890,
      pInt64: 1234567890123n,
      pDecimal: exprDecimalLiteral({ value: decimalValue("12345.678") }),
      pDouble: 12345.25,
      pString: "Hello Ж",
      pGuid: exprGuidLiteral({ value: guidValue("11111111-2222-3333-4444-555555555555") }),
      pDateTime: exprDateTimeLiteral({ value: dateTimeValue("2024-11-12T13:14:15") }),
      pBytes: exprByteArrayLiteral({ value: Uint8Array.from([0, 1, 2, 3, 255]) }),
      pNull: exprNull,
      ...(!mysql
        ? {
            pDateTimeOffset: exprDateTimeOffsetLiteral({
              value: dateTimeOffsetValue("2024-11-12T13:14:15+03:00"),
            }),
          }
        : {}),
    };
    const rows = await context.query(
      bindTSqlParameters(parseTSql(`SELECT ${aliases}`).ast, values),
    );
    assertRow(rows[0], mysql);
  },
};

function assertRow(row: CanonicalRow | undefined, mysql: boolean): void {
  if (row === undefined) throw new Error("Parser expression parameter query returned no row.");
  if (
    row.BoolV !== true ||
    Number(row.ByteV) !== 7 ||
    Number(row.Int16V) !== 1234 ||
    Number(row.Int32V) !== 567890
  )
    throw new Error("Parser Boolean or small integer expression values did not round-trip.");
  if (
    BigInt(String(row.Int64V)) !== 1234567890123n ||
    String(row.DecimalV) !== "12345.678" ||
    Number(row.DoubleV) !== 12345.25
  )
    throw new Error(
      `Parser numeric expression values did not round-trip exactly: ${String(row.Int64V)}, ${String(row.DecimalV)}, ${String(row.DoubleV)}.`,
    );
  if (
    row.StringV !== "Hello Ж" ||
    String(row.GuidV).toLowerCase() !== "11111111-2222-3333-4444-555555555555"
  )
    throw new Error("Parser string or Guid expression values did not round-trip.");
  if (
    String(row.DateTimeV)
      .replace(" ", "T")
      .replace(/(?:\.0+)?Z?$/, "") !== "2024-11-12T13:14:15"
  )
    throw new Error("Parser DateTime expression value did not round-trip.");
  if (!(row.BytesV instanceof Uint8Array) || !sameBytes(row.BytesV, [0, 1, 2, 3, 255]))
    throw new Error("Parser binary expression value did not round-trip.");
  if (row.NullV !== null) throw new Error("Parser null expression value did not round-trip.");
  if (!mysql && new Date(String(row.DateTimeOffsetV)).toISOString() !== "2024-11-12T10:14:15.000Z")
    throw new Error("Parser DateTimeOffset expression value did not preserve its UTC instant.");
}
function sameBytes(value: Uint8Array, expected: ReadonlyArray<number>): boolean {
  return value.length === expected.length && expected.every((item, index) => value[index] === item);
}
