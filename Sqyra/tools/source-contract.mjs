import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { spawnSync } from "node:child_process";

const sqyraRoot = resolve(import.meta.dirname, "..");
const repositoryRoot = resolve(sqyraRoot, "..");
const outputPath = resolve(sqyraRoot, "baseline/source-contract.json");
const roots = [
  "SqExpress/Syntax", "SqExpress/SyntaxTreeOperations", "SqExpress/SqlParser",
  "SqExpress/SqlExport", "SqExpress/QueryBuilders", "Test/SqExpress.Test/SqlParser",
  "Test/SqExpress.Test/Syntax", "Test/SqExpress.Test/QueryBuilder", "Test/SqExpress.Test/Export"
];

function filesBelow(path) {
  if (!existsSync(path)) return [];
  return readdirSync(path, { withFileTypes: true }).flatMap((entry) => {
    const child = resolve(path, entry.name);
    return entry.isDirectory() ? filesBelow(child) : /\.(?:cs|md)$/.test(entry.name) ? [child] : [];
  });
}
function sha256(buffer) { return createHash("sha256").update(buffer).digest("hex"); }
function git(...args) {
  const result = spawnSync("git", ["-c", `safe.directory=${repositoryRoot.replaceAll("\\", "/")}`, ...args], { cwd: repositoryRoot, encoding: "utf8" });
  if (result.status !== 0) throw new Error(result.stderr.trim() || `git ${args.join(" ")} failed`);
  return result.stdout.trim();
}
function testInventory(text) {
  const className = /\bclass\s+(\w+)/.exec(text)?.[1] ?? "UnknownClass";
  const cases = [];
  let attributes = [];
  for (const line of text.split(/\r?\n/)) {
    const attribute = /^\s*\[(Test|TestCase|TestCaseSource)\b/.exec(line)?.[1];
    if (attribute) { attributes.push(attribute); continue; }
    const method = /^\s*public\s+(?:async\s+)?[^\r\n(]+?\s+(\w+)\s*\(/.exec(line)?.[1];
    if (method && attributes.length) {
      const direct = attributes.filter((item) => item === "TestCase");
      if (direct.length) direct.forEach((_, i) => cases.push({ id: `${className}.${method}#${i + 1}`, method, source: "TestCase" }));
      else cases.push({ id: `${className}.${method}`, method, source: attributes.includes("TestCaseSource") ? "TestCaseSource" : "Test" });
      attributes = [];
      continue;
    }
    if (line.trim() && !/^\s*\[/.test(line)) attributes = [];
  }
  for (const match of text.matchAll(/\.SetName\(\s*"([^"]+)"\s*\)/g)) cases.push({ id: `${className}.${match[1]}`, method: null, source: "SetName" });
  return [...new Map(cases.map((item) => [item.id, item])).values()].sort((a, b) => a.id.localeCompare(b.id));
}

export function createContract() {
  const paths = roots.flatMap((root) => filesBelow(resolve(repositoryRoot, root))).sort((a, b) => a.localeCompare(b));
  const files = paths.map((path) => {
    const bytes = readFileSync(path); const name = relative(repositoryRoot, path).replaceAll("\\", "/");
    return { path: name, sha256: sha256(bytes), ...(name.startsWith("Test/") ? { tests: testInventory(bytes.toString("utf8")) } : {}) };
  });
  const changes = git("status", "--porcelain=v1").split(/\r?\n/).filter(Boolean)
    .filter((line) => roots.some((root) => line.slice(3).replaceAll("\\", "/").startsWith(root))).sort();
  const parserFiles = files.filter((file) => file.path.startsWith("Test/SqExpress.Test/SqlParser/"));
  return { schemaVersion: 1, sourceCommit: git("rev-parse", "HEAD"), relevantWorkingTreeChanges: changes, roots, files,
    totals: { files: files.length, tests: files.reduce((sum, file) => sum + (file.tests?.length ?? 0), 0), parserTestFiles: parserFiles.length, parserTests: parserFiles.reduce((sum, file) => sum + (file.tests?.length ?? 0), 0) } };
}
export function stableJson(value) { return `${JSON.stringify(value, null, 2)}\n`; }
export function checkContract(expected, actual) {
  if (stableJson(expected) === stableJson(actual)) return [];
  const errors = [];
  if (expected.sourceCommit !== actual.sourceCommit) errors.push(`source commit changed: ${expected.sourceCommit} -> ${actual.sourceCommit}`);
  const oldFiles = new Map(expected.files.map((file) => [file.path, file])); const newFiles = new Map(actual.files.map((file) => [file.path, file]));
  for (const path of oldFiles.keys()) if (!newFiles.has(path)) errors.push(`source file removed: ${path}`);
  for (const path of newFiles.keys()) if (!oldFiles.has(path)) errors.push(`source file added: ${path}`);
  for (const [path, file] of newFiles) { const old = oldFiles.get(path); if (old && old.sha256 !== file.sha256) errors.push(`source hash changed: ${path}`); if (old && stableJson(old.tests ?? []) !== stableJson(file.tests ?? [])) errors.push(`source tests changed: ${path}`); }
  if (stableJson(expected.relevantWorkingTreeChanges) !== stableJson(actual.relevantWorkingTreeChanges)) errors.push("relevant working-tree changes changed");
  return errors.length ? errors : ["source contract changed"];
}

const mode = process.argv[2];
if (mode === "generate") { const contract = createContract(); mkdirSync(dirname(outputPath), { recursive: true }); writeFileSync(outputPath, stableJson(contract)); console.log(`Recorded ${contract.totals.files} source files and ${contract.totals.tests} source test identities.`); }
else if (mode === "check") { if (!existsSync(outputPath)) { console.error("Source contract is missing. Run npm run baseline:generate after reviewing the source baseline."); process.exit(1); } const errors = checkContract(JSON.parse(readFileSync(outputPath, "utf8")), createContract()); if (errors.length) { console.error(`Source contract is stale:\n${errors.map((error) => `- ${error}`).join("\n")}`); process.exit(1); } console.log("Source contract matches the recorded C# baseline."); }
