import {
  dateTimeOffsetValue,
  dateTimeValue,
  decimalValue,
  deleteFrom,
  exprBoolLiteral,
  exprByteArrayLiteral,
  exprByteLiteral,
  exprCast,
  exprColumnName,
  exprDateTimeLiteral,
  exprDateTimeOffsetLiteral,
  exprDecimalLiteral,
  exprDoubleLiteral,
  exprGuidLiteral,
  exprIdentityInsert,
  exprInsert,
  exprInsertValueRow,
  exprInsertValues,
  exprInt16Literal,
  exprInt32Literal,
  exprInt64Literal,
  exprNull,
  exprStringLiteral,
  exprTypeXml,
  guidValue,
  select,
  tableSource,
  type ExprValue,
} from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { CanonicalRow, CanonicalValue } from "../types.js";
import type { Scenario } from "./types.js";

export const allColumnTypesExportImportScenario: Scenario = {
  source: "ScAllColumnTypesExportImport",
  async run(context) {
    const { allTypes } = defineIntegrationTables(context.dialect);
    const columns = Object.entries(allTypes.$metadata.definitions);
    const projected = Object.values(allTypes.$metadata.columns).filter(
      (value): value is Exclude<typeof value, undefined> => value !== undefined,
    );
    const beforeRows = await context.query(select(projected).from(allTypes).orderBy(allTypes.Id));
    const exported = exportRows(
      allTypes.$metadata.name,
      columns.map(([name]) => name),
      beforeRows,
    );
    await context.execute(deleteFrom(allTypes));
    const parsed = JSON.parse(exported) as Readonly<
      Record<string, ReadonlyArray<ReadonlyArray<string | null>>>
    >;
    const data = parsed[allTypes.$metadata.name];
    if (data === undefined) throw new Error("Exported all-type table property was missing.");
    const table = tableSource(allTypes);
    if (table.kind !== "ExprTable")
      throw new Error("All-type descriptor did not resolve to a physical table.");
    const insert = exprInsert({
      target: table.fullName,
      targetColumns: columns.map(([name]) => exprColumnName({ name })),
      source: exprInsertValues({
        items: data.map((row) =>
          exprInsertValueRow({
            items: row.map((value, index) => fromText(columns[index]![1].sqlType.name, value)),
          }),
        ),
      }),
    });
    await context.execute(
      exprIdentityInsert({ insert, identityColumns: [exprColumnName({ name: "Id" })] }),
    );
    const afterRows = await context.query(select(projected).from(allTypes).orderBy(allTypes.Id));
    if (
      exportRows(
        allTypes.$metadata.name,
        columns.map(([name]) => name),
        afterRows,
      ) !== exported
    )
      throw new Error(
        "All-column-type export/import/export changed the deterministic JSON representation.",
      );
  },
};
function exportRows(
  table: string,
  columns: ReadonlyArray<string>,
  rows: ReadonlyArray<CanonicalRow>,
): string {
  return JSON.stringify({ [table]: rows.map((row) => columns.map((name) => toText(row[name]))) });
}
function toText(value: CanonicalValue | undefined): string | null {
  if (value === null || value === undefined) return null;
  if (value instanceof Uint8Array) return Buffer.from(value).toString("base64");
  return String(value);
}
function fromText(type: string, value: string | null): ExprValue {
  if (value === null) return exprNull;
  switch (type) {
    case "boolean":
      return exprBoolLiteral({ value: value === "true" || value === "1" });
    case "byte":
      return exprByteLiteral({ value: Number(value) });
    case "int16":
      return exprInt16Literal({ value: Number(value) });
    case "int32":
      return exprInt32Literal({ value: Number(value) });
    case "int64":
      return exprInt64Literal({ value: BigInt(value) });
    case "decimal":
      return exprDecimalLiteral({ value: decimalValue(value) });
    case "double":
      return exprDoubleLiteral({ value: Number(value) });
    case "guid":
      return exprGuidLiteral({ value: guidValue(value) });
    case "binary":
      return exprByteArrayLiteral({ value: Uint8Array.from(Buffer.from(value, "base64")) });
    case "date":
    case "dateTime":
      return exprDateTimeLiteral({ value: dateTimeValue(normalizeDateTime(value)) });
    case "dateTimeOffset":
      return exprDateTimeOffsetLiteral({ value: dateTimeOffsetValue(value) });
    case "xml":
      return exprCast({ expression: exprStringLiteral({ value }), sqlType: exprTypeXml });
    default:
      return exprStringLiteral({ value });
  }
}
function normalizeDateTime(value: string): string {
  const normalized = value.replace(" ", "T");
  return /^\d{4}-\d{2}-\d{2}$/.test(normalized)
    ? `${normalized}T00:00:00`
    : normalized.replace(/Z$/, "");
}
