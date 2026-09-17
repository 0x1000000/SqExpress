import { describe, expect, it } from "vitest";
import {
  column,
  exprAnalyticFunction,
  exprFrameClause,
  exprFunctionName,
  exprOrderBy,
  exprOrderByItem,
  exprOver,
  exprUnboundedFrameBorder,
  lit,
  select,
  sqlType,
} from "../../src/index.js";

describe("analytic projections", () => {
  it("accepts an analytic AST directly and preserves its frame clause", () => {
    const value = lit(1);
    const functionNode = exprAnalyticFunction({
      name: exprFunctionName({ builtIn: true, name: "FIRST_VALUE" }),
      arguments: [value],
      over: exprOver({
        partitions: null,
        orderBy: exprOrderBy({ orderList: [exprOrderByItem({ value, descendant: false })] }),
        frameClause: exprFrameClause({
          start: exprUnboundedFrameBorder({ frameBorderDirection: "Preceding" }),
          end: null,
        }),
      }),
    });
    expect(select({ First: functionNode }).toSql("sqlite")).toBe(
      'SELECT FIRST_VALUE(1)OVER(ORDER BY 1 ROWS UNBOUNDED PRECEDING) "First"',
    );
  });
  it("does not emit unsupported SQLite derived-table column alias lists", () => {
    const source = select({ Id: lit(1) }).as("s", { Id: column(sqlType.int32) });
    expect(select(source.Id).from(source).toSql("sqlite")).toBe(
      'SELECT "s"."Id" FROM (SELECT 1 "Id") AS "s"',
    );
  });
  it("does not emit derived-table column alias lists for MariaDB", () => {
    const source = select({ Id: lit(1) }).as("s", { Id: column(sqlType.int32) });
    expect(select(source.Id).from(source).toSql({ dialect: "mysql", mysqlFlavor: "mariadb" })).toBe(
      "SELECT `s`.`Id` FROM (SELECT 1 `Id`)`s`",
    );
  });
});
