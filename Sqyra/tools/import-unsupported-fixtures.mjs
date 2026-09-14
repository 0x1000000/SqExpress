import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const source = await readFile(resolve(root, "../Test/SqExpress.Test/SqlParser/TSqlParserUnsupportedFeatureTest.cs"), "utf8");
const path = resolve(root, "test/fixtures/cases.json");
const cases = JSON.parse(await readFile(path, "utf8"));
const prefix = "SqExpress.Test.SqlParser.TSqlParserUnsupportedFeatureTest.";
const imported = [];
const pattern = /new TestCaseData\(\s*("(?:\\.|[^"\\])*")\s*,\s*("(?:\\.|[^"\\])*")\s*\)\s*\.SetName\("([^"]+)"\)/g;
for (const match of source.matchAll(pattern)) imported.push({ id: `${prefix}${match[3]}#1`, sql: JSON.parse(match[1]), expectedErrorExact: JSON.parse(match[2]) });
if (imported.length !== 6) throw new Error(`Expected 6 unsupported cases, found ${imported.length}.`);
const byId = new Map(cases.map((item) => [item.id, item]));
for (const item of imported) byId.set(item.id, item);
await writeFile(path, `${JSON.stringify([...byId.values()], null, 2)}\n`);
console.log(`Imported ${imported.length} unsupported-feature fixtures.`);
