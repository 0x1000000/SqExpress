import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const sourcePath = resolve(root, "../Test/SqExpress.Test/SqlParser/TSqlParserInvalidSyntaxAndInjectionTest.cs");
const casesPath = resolve(root, "test/fixtures/cases.json");
const source = await readFile(sourcePath, "utf8");
const cases = JSON.parse(await readFile(casesPath, "utf8"));
const prefix = "SqExpress.Test.SqlParser.TSqlParserInvalidSyntaxAndInjectionTest.";

// These TestCaseData entries use ordinary C# string literals. JSON decoding has
// the same escape rules for every escaped character present in this source file.
const pattern = /new TestCaseData\(\s*("(?:\\.|[^"\\])*")\s*,\s*("(?:\\.|[^"\\])*")\s*\)\s*\.SetName\("([^"]+)"\)/g;
const imported = [];
for (const match of source.matchAll(pattern)) {
  imported.push({ id: `${prefix}${match[3]}#1`, sql: JSON.parse(match[1]), expectedErrorExact: JSON.parse(match[2]) });
}
if (imported.length < 70) throw new Error(`Expected at least 70 invalid-syntax cases, found ${imported.length}.`);

// Import the individually declared regression tests as well. Their bodies are
// deliberately simple and keep the SQL in a local C# string literal.
const methodPattern = /\[Test\]\s*public void (\w+)\(\)\s*\{([\s\S]*?)\n\s*\}/g;
for (const match of source.matchAll(methodPattern)) {
  const sql = /var sql = ("(?:\\.|[^"\\])*");/.exec(match[2]);
  if (sql === null) continue;
  imported.push({ id: `${prefix}${match[1]}#1`, sql: JSON.parse(sql[1]) });
}

// NUnit disambiguates this named TestCaseData entry from a method with the
// same display name by assigning the discovered identity suffix #2.
const duplicateCrossJoin = imported.find((item) => item.id === `${prefix}CrossJoinWithOn#1`);
if (duplicateCrossJoin !== undefined) imported.push({ ...duplicateCrossJoin, id: `${prefix}CrossJoinWithOn#2` });

const byId = new Map(cases.map((item) => [item.id, item]));
for (const item of imported) byId.set(item.id, item);
await writeFile(casesPath, `${JSON.stringify([...byId.values()], null, 2)}\n`);
console.log(`Imported ${imported.length} named invalid-syntax fixtures.`);
