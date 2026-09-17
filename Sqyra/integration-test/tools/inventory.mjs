import { writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { buildInventory, projectRoot } from "./inventory-lib.mjs";
const inventory = buildInventory();
writeFileSync(
  resolve(projectRoot, "scenario-inventory.json"),
  `${JSON.stringify(inventory, null, 2)}\n`,
);
console.log(`Recorded ${inventory.scenarios.length} C# integration scenarios.`);
