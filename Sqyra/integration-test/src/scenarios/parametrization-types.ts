import {
  column,
  dateTimeOffsetValue,
  dateTimeValue,
  defineTable,
  exprDateTimeLiteral,
  exprDateTimeOffsetLiteral,
  exprGuidLiteral,
  guidValue,
  insertInto,
  nullableColumn,
  select,
  sqlType,
  type InlineExportOptions,
} from "sqyra";
import type { CanonicalRow } from "../types.js";
import type { Scenario } from "./types.js";

export const parametrizationTypesScenario: Scenario = {
  source: "ScParametrizationTypes",
  async run(context) {
    const mysql = context.dialect === "mysql-oracle" || context.dialect === "mariadb";
    const options: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    const guid1 = exprGuidLiteral({ value: guidValue("11111111-1111-1111-1111-111111111111") }),
      guid2 = exprGuidLiteral({ value: guidValue("22222222-2222-2222-2222-222222222222") });
    const dt1 = exprDateTimeLiteral({ value: dateTimeValue("2024-01-02T03:04:05") }),
      dt2 = exprDateTimeLiteral({ value: dateTimeValue("2024-01-03T04:05:06") });
    if (mysql) {
      const table = defineTable({
        schema: null,
        name: "ParamTypesProbe",
        temporary: true,
        columns: commonColumns(),
      });
      await context.database.executeScript(table.$script.dropIfExists().toSql(options));
      await context.database.executeScript(table.$script.create().toSql(options));
      await context.execute(
        insertInto(table).values(
          {
            Id: 1,
            GuidValue: guid1,
            NullableGuidValue: guid2,
            DateTimeValue: dt1,
            NullableDateTimeValue: dt2,
          },
          {
            Id: 2,
            GuidValue: guid2,
            NullableGuidValue: null,
            DateTimeValue: dt2,
            NullableDateTimeValue: null,
          },
        ),
      );
      validateCommon(
        await context.query(
          select({
            Id: table.Id,
            GuidValue: table.GuidValue,
            NullableGuidValue: table.NullableGuidValue,
            DateTimeValue: table.DateTimeValue,
            NullableDateTimeValue: table.NullableDateTimeValue,
          })
            .from(table)
            .orderBy(table.Id),
        ),
      );
      await context.database.executeScript(table.$script.drop().toSql(options));
      return;
    } else {
      const table = defineTable({
        schema: null,
        name: "ParamTypesProbe",
        temporary: true,
        columns: {
          ...commonColumns(),
          DateTimeOffsetValue: column(sqlType.dateTimeOffset),
          NullableDateTimeOffsetValue: nullableColumn(sqlType.dateTimeOffset),
        },
      });
      await context.database.executeScript(table.$script.dropIfExists().toSql(options));
      await context.database.executeScript(table.$script.create().toSql(options));
      const dto1 = exprDateTimeOffsetLiteral({
          value: dateTimeOffsetValue("2024-01-02T03:04:05+03:00"),
        }),
        dto2 = exprDateTimeOffsetLiteral({
          value: dateTimeOffsetValue("2024-01-03T04:05:06-05:00"),
        });
      await context.execute(
        insertInto(table).values(
          {
            Id: 1,
            GuidValue: guid1,
            NullableGuidValue: guid2,
            DateTimeValue: dt1,
            NullableDateTimeValue: dt2,
            DateTimeOffsetValue: dto1,
            NullableDateTimeOffsetValue: dto2,
          },
          {
            Id: 2,
            GuidValue: guid2,
            NullableGuidValue: null,
            DateTimeValue: dt2,
            NullableDateTimeValue: null,
            DateTimeOffsetValue: dto2,
            NullableDateTimeOffsetValue: null,
          },
        ),
      );
      const rows = await context.query(
        select({
          Id: table.Id,
          GuidValue: table.GuidValue,
          NullableGuidValue: table.NullableGuidValue,
          DateTimeValue: table.DateTimeValue,
          NullableDateTimeValue: table.NullableDateTimeValue,
          DateTimeOffsetValue: table.DateTimeOffsetValue,
          NullableDateTimeOffsetValue: table.NullableDateTimeOffsetValue,
        })
          .from(table)
          .orderBy(table.Id),
      );
      validateCommon(rows);
      const first = rows[0],
        second = rows[1];
      if (
        new Date(String(first?.DateTimeOffsetValue)).toISOString() !== "2024-01-02T00:04:05.000Z" ||
        new Date(String(first?.NullableDateTimeOffsetValue)).toISOString() !==
          "2024-01-03T09:05:06.000Z" ||
        new Date(String(second?.DateTimeOffsetValue)).toISOString() !==
          "2024-01-03T09:05:06.000Z" ||
        second?.NullableDateTimeOffsetValue !== null
      )
        throw new Error("DateTimeOffset parameters did not preserve their UTC instants.");
      await context.database.executeScript(table.$script.drop().toSql(options));
    }
  },
};
function commonColumns() {
  return {
    Id: column(sqlType.int32, { primaryKey: true }),
    GuidValue: column(sqlType.guid),
    NullableGuidValue: nullableColumn(sqlType.guid),
    DateTimeValue: column(sqlType.dateTime),
    NullableDateTimeValue: nullableColumn(sqlType.dateTime),
  };
}
function validateCommon(rows: ReadonlyArray<CanonicalRow>): void {
  const first = rows[0],
    second = rows[1];
  if (
    first?.GuidValue !== "11111111-1111-1111-1111-111111111111" ||
    first.NullableGuidValue !== "22222222-2222-2222-2222-222222222222" ||
    !sameLocalDate(first.DateTimeValue, "2024-01-02T03:04:05") ||
    !sameLocalDate(first.NullableDateTimeValue, "2024-01-03T04:05:06")
  )
    throw new Error("First parameter type row did not round-trip.");
  if (
    second?.GuidValue !== "22222222-2222-2222-2222-222222222222" ||
    second.NullableGuidValue !== null ||
    !sameLocalDate(second.DateTimeValue, "2024-01-03T04:05:06") ||
    second.NullableDateTimeValue !== null
  )
    throw new Error("Second parameter type row did not round-trip.");
}
function sameLocalDate(actual: unknown, expected: string): boolean {
  return (
    typeof actual === "string" && actual.replace(" ", "T").replace(/(?:\.0+)?Z?$/, "") === expected
  );
}
