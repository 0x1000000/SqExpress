import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const path = resolve(import.meta.dirname, "../test/fixtures/cases.json");
const cases = JSON.parse(await readFile(path, "utf8"));
const prefix = "SqExpress.Test.SqlParser.TSqlParserParameterTest.";
const shared =
  "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId]=@id AND [u].[Name]=@name";
const imported = [
  {
    id: `${prefix}ExtractParameters("select UserId from Users u wHeRe [u].[UserId]=@id and [u].[Name]=@name")#1`,
    sql: "select UserId from Users u wHeRe [u].[UserId]=@id and [u].[Name]=@name",
  },
  {
    id: `${prefix}ExtractParameters("select UserId from Users wHeRe userId=@id and [Name]=@name")#1`,
    sql: "select UserId from Users wHeRe userId=@id and [Name]=@name",
  },
  { id: `${prefix}NamedParametersArePreservedByFormatter#1`, sql: shared },
  { id: `${prefix}NamedParametersAreMappedToSqExpr#1`, sql: shared },
  {
    id: `${prefix}ParameterMarkersInsideStringLiteralArePreservedByFormatter#1`,
    sql: "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='@id'",
  },
  { id: `${prefix}DoubleAtVariablesArePreservedByFormatter#1`, sql: "SELECT @@ROWCOUNT [Cnt]" },
];
const byId = new Map(cases.map((item) => [item.id, item]));
for (const item of imported) byId.set(item.id, item);
await writeFile(path, `${JSON.stringify([...byId.values()], null, 2)}\n`);
console.log(`Imported ${imported.length} parameter fixtures.`);
