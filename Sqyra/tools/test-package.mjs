import { mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { build } from "esbuild";

const root = resolve(import.meta.dirname, "..");
const temp = resolve(root, ".package-test-tmp");
const npmCache = resolve(temp, "npm-cache");
function run(command, args, cwd = root) {
  const npmCli = process.env.npm_execpath;
  const executable = command === "npm" && npmCli ? process.execPath : command;
  const actualArgs = command === "npm" && npmCli ? [npmCli, ...args] : args;
  const result = spawnSync(executable, actualArgs, { cwd, encoding: "utf8" });
  if (result.status !== 0)
    throw new Error(`${command} ${args.join(" ")} failed:\n${result.stdout}\n${result.stderr}`);
  return result.stdout;
}

try {
  rmSync(temp, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 });
  mkdirSync(temp, { recursive: true });
  const packed = JSON.parse(
    run("npm", ["pack", "--json", "--cache", npmCache, "--pack-destination", temp]),
  );
  const tarball = resolve(temp, packed[0].filename);
  writeFileSync(resolve(temp, "package.json"), '{"type":"module","private":true}\n');
  run(
    "npm",
    ["install", "--ignore-scripts", "--no-audit", "--no-fund", "--cache", npmCache, tarball],
    temp,
  );
  writeFileSync(
    resolve(temp, "esm.mjs"),
    'import {parseTSql,toSql} from "sqyra"; if(toSql(parseTSql("SELECT 1").ast,{dialect:"tsql"})!=="SELECT 1") process.exit(1);\n',
  );
  writeFileSync(
    resolve(temp, "cjs.cjs"),
    'const {parseTSql,toSql}=require("sqyra"); if(toSql(parseTSql("SELECT 1").ast,{dialect:"tsql"})!=="SELECT 1") process.exit(1);\n',
  );
  run("node", ["esm.mjs"], temp);
  run("node", ["cjs.cjs"], temp);
  writeFileSync(
    resolve(temp, "browser.js"),
    'import {parseTSql,toSql} from "sqyra"; globalThis.sqyraResult=toSql(parseTSql("SELECT 1").ast,{dialect:"sqlite"});\n',
  );
  await build({
    entryPoints: [resolve(temp, "browser.js")],
    outfile: resolve(temp, "browser.bundle.js"),
    bundle: true,
    platform: "browser",
    format: "iife",
    target: "es2020",
  });
  const bundle = readFileSync(resolve(temp, "browser.bundle.js"), "utf8");
  if (/node:|require\(["'](?:fs|path|crypto)/.test(bundle))
    throw new Error("Browser bundle contains a Node runtime dependency.");
  console.log("Packed ESM, CommonJS, and browser bundle checks passed.");
} finally {
  rmSync(temp, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 });
}
