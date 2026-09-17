import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";
import {
  column,
  defineTable,
  normalizeAstForCSharpFixture,
  sqlType,
  toSql,
  tryParseTSql,
  type ColumnDefinitions,
  type ParseTSqlOptions,
  type SqlDialect,
} from "../../src/index.js";

interface FixtureExport {
  readonly success: boolean;
  readonly sql: string | null;
}
interface TableInput {
  readonly schema: string | null;
  readonly name: string;
  readonly columns: ReadonlyArray<{
    readonly name: string;
    readonly type: "int32" | "string" | "boolean" | "decimal" | "dateTime";
  }>;
}
interface Fixture {
  readonly id: string;
  readonly sql: string;
  readonly defaultSchema: string | null;
  readonly existingTables?: ReadonlyArray<TableInput> | null;
  readonly compareSemantic?: boolean;
  readonly success: boolean;
  readonly ast: Readonly<Record<string, unknown>> | null;
  readonly tables: ReadonlyArray<Readonly<Record<string, unknown>>> | null;
  readonly exports: Readonly<
    Record<"tsql" | "postgresql" | "mysql" | "sqlite", FixtureExport>
  > | null;
  readonly error: string | null;
  readonly expectedErrorPart: string | null;
  readonly expectedErrorExact: string | null;
}
const fixtures = JSON.parse(
  readFileSync(new URL("../fixtures/csharp-reference.json", import.meta.url), "utf8"),
) as ReadonlyArray<Fixture>;

describe("C# reference fixtures", () => {
  for (const fixture of fixtures)
    it(fixture.id, () => {
      const makeTables = (inputs: ReadonlyArray<TableInput>) =>
        inputs.map((table) => {
          const definitions: Record<string, ReturnType<typeof column>> = {};
          for (const item of table.columns)
            definitions[item.name] = column(
              item.type === "string"
                ? sqlType.string(255)
                : item.type === "boolean"
                  ? sqlType.boolean
                  : item.type === "decimal"
                    ? sqlType.decimal(38, 18)
                    : item.type === "dateTime"
                      ? sqlType.dateTime
                      : sqlType.int32,
            );
          return defineTable({
            schema: table.schema,
            name: table.name,
            columns: definitions as ColumnDefinitions,
          });
        });
      const options: ParseTSqlOptions =
        fixture.existingTables == null
          ? { defaultSchema: fixture.defaultSchema }
          : {
              defaultSchema: fixture.defaultSchema,
              existingTables: makeTables(fixture.existingTables),
            };
      const result = tryParseTSql(fixture.sql, options);
      expect(result.success, result.success ? undefined : result.error.message).toBe(
        fixture.success,
      );
      if (!fixture.success) {
        const message = result.success ? null : result.error.message;
        if (fixture.expectedErrorExact !== null) expect(message).toBe(fixture.expectedErrorExact);
        else if (fixture.expectedErrorPart !== null)
          expect(message).toContain(fixture.expectedErrorPart);
        else expect(message).not.toBe("");
        return;
      }
      if (!result.success || fixture.compareSemantic === false || fixture.exports === null) return;
      expect(normalizeAstForCSharpFixture(result.ast), `${fixture.id}/ast`).toEqual(fixture.ast);
      expect(
        result.tables.map(({ database, schema, name, columnArtifacts }) => ({
          database,
          schema,
          name,
          columns: columnArtifacts,
        })),
        `${fixture.id}/tables`,
      ).toEqual(fixture.tables);
      for (const dialect of [
        "tsql",
        "pgsql",
        "mysql",
        "sqlite",
      ] as const satisfies ReadonlyArray<SqlDialect>) {
        const expected = fixture.exports[dialect === "pgsql" ? "postgresql" : dialect];
        if (expected.success)
          expect(toSql(result.ast, { dialect }), `${fixture.id}/${dialect}`).toBe(expected.sql);
      }
    });
});
