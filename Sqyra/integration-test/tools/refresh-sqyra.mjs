import { execFileSync } from "node:child_process";
import { rmSync } from "node:fs";
import { resolve } from "node:path";
import { projectRoot } from "./inventory-lib.mjs";
rmSync(resolve(projectRoot, "node_modules", "sqyra"), { recursive: true, force: true });
execFileSync("npm", ["install", "--ignore-scripts"], {
  cwd: projectRoot,
  stdio: "inherit",
  shell: process.platform === "win32",
  env: { ...process.env, npm_config_cache: resolve(projectRoot, ".npm-cache") },
});
