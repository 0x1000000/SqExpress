import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const source = await readFile(
  resolve(root, "../Test/SqExpress.Test/SqlParser/TSqlParserBasicTest.cs"),
  "utf8",
);
const path = resolve(root, "test/fixtures/cases.json");
const cases = JSON.parse(await readFile(path, "utf8"));
const names = [
  "ExtractTableArtifacts_ForMultiJoinQuery_CollectsExpectedColumns",
  "ParseComplexQueryWithCteAndOuterApply_BuildsExpectedAstNodes",
  "ParseLargeMultiCteQuery_BuildsExpectedAstNodes",
  "ParseNestedParenthesizedSetOperations_MapsSuccessfully",
];
const prefix = "SqExpress.Test.SqlParser.TSqlParserBasicTest.";
const imported = [];
for (const name of names) {
  const start = source.indexOf(`public void ${name}()`);
  const next = source.indexOf("\n    [Test]", start + 1);
  const body = source.slice(start, next < 0 ? source.length : next);
  const ordinary = /(?:var|const string) inputSql\s*=\s*("(?:\\.|[^"\\])*")/.exec(body);
  const verbatim = /(?:var|const string) inputSql\s*=\s*@"([\s\S]*?)";/.exec(body);
  const sql =
    ordinary !== null
      ? JSON.parse(ordinary[1])
      : verbatim !== null
        ? verbatim[1].replaceAll('""', '"')
        : null;
  if (sql === null) throw new Error(`Could not extract inputSql for ${name}.`);
  imported.push({ id: `${prefix}${name}#1`, sql });
}
const byId = new Map(cases.map((item) => [item.id, item]));
for (const item of imported) byId.set(item.id, item);
await writeFile(path, `${JSON.stringify([...byId.values()], null, 2)}\n`);
console.log(`Imported ${imported.length} remaining basic parser fixtures.`);
