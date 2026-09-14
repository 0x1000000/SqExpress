import { spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const sqyraRoot = resolve(import.meta.dirname, "..");
const root = resolve(sqyraRoot, "..");
const results = resolve(sqyraRoot, ".tmp/test-discovery");
const output = resolve(import.meta.dirname, "../baseline/csharp-discovered-tests.json");
const project = "Test/SqExpress.Test/SqExpress.Test.csproj";
const groups = {
  parser: "FullyQualifiedName~TSqlParser",
  traversal: "FullyQualifiedName~SyntaxTreeOperationsTest",
  queryBuilder: "FullyQualifiedName~QueryBuilder",
  exporter: "FullyQualifiedName~Export"
};

function decode(value) { return value.replaceAll("&quot;", '"').replaceAll("&lt;", "<").replaceAll("&gt;", ">").replaceAll("&amp;", "&"); }
function attribute(tag, name) { return new RegExp(`\\b${name}="([^"]*)"`).exec(tag)?.[1]; }
function discover(name, filter) {
  mkdirSync(results, { recursive: true }); const file = `${name}.trx`;
  const args = ["test", project, "--no-restore", "--no-build", "--filter", filter, "--logger", `trx;LogFileName=${file}`, "--results-directory", results];
  const result = spawnSync("dotnet", args, { cwd: root, encoding: "utf8", maxBuffer: 16 * 1024 * 1024 });
  if (result.status !== 0) throw new Error(result.stderr || result.stdout || `dotnet exited ${result.status}`);
  const xml = readFileSync(resolve(results, file), "utf8");
  const definitions = new Map([...xml.matchAll(/(<UnitTest\b[^>]*>)([\s\S]*?)<\/UnitTest>/g)].map((match) => {
    const id = attribute(match[1], "id");
    const method = /<TestMethod\b[^>]*\bclassName="([^"]*)"[^>]*\bname="([^"]*)"/.exec(match[2]);
    if (!id || !method) throw new Error("Malformed UnitTest entry in TRX output.");
    return [id, `${decode(method[1])}.${decode(method[2])}`];
  }));
  const occurrence = new Map();
  const tests = [...xml.matchAll(/<UnitTestResult\b[^>]*>/g)].map((match) => { const testId = attribute(match[0], "testId"); const encodedName = attribute(match[0], "testName"); if (!testId || !encodedName) throw new Error("Malformed UnitTestResult entry in TRX output."); const displayName = decode(encodedName); const rawBase = definitions.get(testId) ?? displayName; const base = rawBase.replace(/_[0-9A-F]{6,8}(?=_|$)/g, "_Case"); const count = (occurrence.get(base) ?? 0) + 1; occurrence.set(base, count); return { id: `${base}#${count}`, displayName }; }).sort((a, b) => a.id.localeCompare(b.id));
  if (tests.length === 0) throw new Error(`No tests were executed for ${filter}.`);
  return { filter, command: `dotnet ${args.join(" ")}`, tests };
}

try {
  const data = { schemaVersion: 1, groups: {} };
  for (const [name, filter] of Object.entries(groups)) data.groups[name] = discover(name, filter);
  writeFileSync(output, `${JSON.stringify(data, null, 2)}\n`);
  console.log(Object.entries(data.groups).map(([name, value]) => `${name}: ${value.tests.length}`).join("\n"));
} finally { rmSync(results, { recursive: true, force: true }); }
