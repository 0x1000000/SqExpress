import { cpSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const output = resolve(process.argv[2] ?? resolve(root, "package", "sqyra"));
const source = JSON.parse(readFileSync(resolve(root, "package.json"), "utf8"));

const manifest = {
  name: source.name,
  version: source.version,
  description: source.description,
  keywords: source.keywords,
  author: source.author,
  license: source.license,
  homepage: source.homepage,
  repository: source.repository,
  bugs: source.bugs,
  type: source.type,
  main: "./dist/index.cjs",
  module: "./dist/index.js",
  types: "./dist/index.d.ts",
  exports: source.exports,
  engines: source.engines,
};

rmSync(output, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 });
mkdirSync(output, { recursive: true });
cpSync(resolve(root, "dist"), resolve(output, "dist"), { recursive: true });
cpSync(resolve(root, "Readme.md"), resolve(output, "README.md"));
cpSync(resolve(root, "LICENSE"), resolve(output, "LICENSE"));
writeFileSync(resolve(output, "package.json"), `${JSON.stringify(manifest, null, 2)}\n`);

console.log(`Prepared clean package folder: ${output}`);
