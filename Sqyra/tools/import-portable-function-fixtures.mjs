import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const source = await readFile(resolve(root, "../Test/SqExpress.Test/SqlParser/TSqlParserPortableFunctionTest.cs"), "utf8");
const path = resolve(root, "test/fixtures/cases.json");
const cases = JSON.parse(await readFile(path, "utf8"));
const byId = new Map(cases.map((item) => [item.id, item]));
const method = "SqExpress.Test.SqlParser.TSqlParserPortableFunctionTest.ParseUnknownScalarFunctions_MapToExprScalarFunction";
const pattern = /\[TestCase\(@"([^"]*)",\s*"([^"]+)",\s*(\d+)\)\]/g;
let count = 0;
for (const match of source.matchAll(pattern)) {
  const sql = match[1]; const name = match[2]; const argumentCount = Number(match[3]);
  const id = `${method}(${JSON.stringify(sql)},${JSON.stringify(name)},${argumentCount})#1`;
  byId.set(id, { id, sql }); count++;
}
if (count !== 10) throw new Error(`Expected 10 unknown-function cases, found ${count}.`);
await writeFile(path, `${JSON.stringify([...byId.values()], null, 2)}\n`);
console.log(`Imported ${count} unknown scalar-function fixtures.`);
