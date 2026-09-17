import { mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { spawnSync } from "node:child_process";

const root = resolve(import.meta.dirname, "..");
const temp = resolve(root, ".docs-test-tmp");
const cache = resolve(temp, "npm-cache");
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
  run("npm", ["run", "build"]);
  const packed = JSON.parse(
    run("npm", ["pack", "--json", "--cache", cache, "--pack-destination", temp]),
  );
  writeFileSync(resolve(temp, "package.json"), '{"type":"module","private":true}\n');
  run(
    "npm",
    [
      "install",
      "--ignore-scripts",
      "--no-audit",
      "--no-fund",
      "--cache",
      cache,
      resolve(temp, packed[0].filename),
    ],
    temp,
  );
  const readme = readFileSync(resolve(root, "Readme.md"), "utf8");
  const examples = [
    ...readme.matchAll(/```ts\s+([\s\S]*?)```(?:\s+PostgreSQL output:\s+```sql\s+([\s\S]*?)```)?/g),
  ].map((match) => ({ source: match[1], expectedSql: match[2]?.trim() }));
  if (examples.length === 0) throw new Error("README contains no TypeScript examples.");
  for (const [index, { source }] of examples.entries())
    writeFileSync(resolve(temp, `example-${index + 1}.ts`), source);
  writeFileSync(
    resolve(temp, "tsconfig.json"),
    JSON.stringify(
      {
        compilerOptions: {
          target: "ES2020",
          module: "NodeNext",
          moduleResolution: "NodeNext",
          strict: true,
          exactOptionalPropertyTypes: true,
          skipLibCheck: true,
          outDir: "compiled",
        },
        include: ["example-*.ts"],
      },
      null,
      2,
    ),
  );
  run(
    process.execPath,
    [resolve(root, "node_modules/typescript/bin/tsc"), "-p", "tsconfig.json"],
    temp,
  );
  for (const [index, { expectedSql }] of examples.entries()) {
    const output = run(
      process.execPath,
      [resolve(temp, "compiled", `example-${index + 1}.js`)],
      temp,
    )
      .replaceAll("\r\n", "\n")
      .trim();
    if (expectedSql !== undefined && output !== expectedSql.replaceAll("\r\n", "\n"))
      throw new Error(
        `README example ${index + 1} does not match its displayed PostgreSQL SQL.\n${output}`,
      );
  }
  console.log(
    `Compiled and executed ${examples.length} README examples against the packed package.`,
  );
} finally {
  rmSync(temp, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 });
}
