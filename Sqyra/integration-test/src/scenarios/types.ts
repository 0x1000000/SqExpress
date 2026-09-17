import type { ScenarioContext } from "../types.js";
export interface Scenario {
  readonly source: string;
  run(context: ScenarioContext): Promise<void>;
}
