import { bindTSqlParameters, parseTSql, type Expr } from "sqyra";
import { scalar } from "../context.js";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const parserTypedParamsScenario: Scenario = {
  source: "ScParserTypedParams",
  async run(context) {
    const tables = defineIntegrationTables(context.dialect);
    const parserOptions = { existingTables: [...tables.ordered] };
    const sampleRows = await context.query(
      bindTSqlParameters(
        parseTSql(
          "SELECT TOP (1) U.UserId,U.LastName FROM dbo.ItUser U ORDER BY U.UserId",
          parserOptions,
        ).ast,
        {},
      ),
    );
    const sample = sampleRows[0];
    if (sample === undefined)
      throw new Error("Could not find a sample user for parser typed params scenario.");
    const userId = Number(sample.UserId);
    const originalLastName = String(sample.LastName);
    const suffix = "_SQX";
    const updatedLastName = `${originalLastName.slice(0, 255 - suffix.length)}${suffix}`;
    const parsedQuery = parseTSql(
      "SELECT U.LastName FROM dbo.ItUser U WHERE U.UserId = @userId",
      parserOptions,
    ).ast;
    const parsedUpdate = parseTSql(
      "UPDATE dbo.ItUser SET LastName = @lastName WHERE UserId = @userId",
      parserOptions,
    ).ast;
    try {
      const before = scalar(await context.query(bindTSqlParameters(parsedQuery, { userId })));
      if (before !== originalLastName)
        throw new Error(
          `Expected LastName '${originalLastName}' before update but got '${String(before)}'.`,
        );
      await context.execute(
        bindTSqlParameters(parsedUpdate, { userId, lastName: updatedLastName }) as Expr,
      );
      const after = scalar(await context.query(bindTSqlParameters(parsedQuery, { userId })));
      if (after !== updatedLastName)
        throw new Error(
          `Expected LastName '${updatedLastName}' after update but got '${String(after)}'.`,
        );
    } finally {
      await context.execute(
        bindTSqlParameters(parsedUpdate, { userId, lastName: originalLastName }) as Expr,
      );
    }
  },
};
