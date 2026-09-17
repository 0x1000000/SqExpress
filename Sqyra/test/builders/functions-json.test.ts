import { describe, expect, it } from "vitest";
import { lit, toExpr } from "../../src/index.js";
import {
  aggregate,
  call,
  callCustom,
  caseWhen,
  column,
  dateTimeValue,
  exprColumn,
  exprColumnName,
  exprDateTimeLiteral,
  exprInt32Literal,
  exprQuerySpecification,
  exprTypeBoolean,
  exprTypeDecimal,
  exprTypeInt32,
  exprTypeString,
  jsonArray,
  jsonObject,
  jsonOutput,
  jsonTable,
  jsonTableValueColumn,
  jsonValue,
  hour,
  param,
  select,
  selectJson,
  sqlType,
  stringAgg,
  tableFunction,
  toSql,
  toSqlType,
  unsafeSql,
} from "../../src/index.js";

describe("function and JSON builders", () => {
  it("builds scalar, aggregate, CASE, and STRING_AGG expressions", () => {
    const query = select({
      lower: call("LOWER", "ABC"),
      count: aggregate("COUNT", 1),
      label: caseWhen(lit(1).eq(1)).then("yes").else("no"),
      joined: stringAgg("a", "|"),
    });
    expect(query.toSql({ dialect: "tsql" })).toBe(
      "SELECT LOWER('ABC') [lower],COUNT(1) [count],CASE WHEN 1=1 THEN 'yes' ELSE 'no' END [label],STRING_AGG('a','|') [joined]",
    );
  });
  it("ports aggregate and window forms", () => {
    const id = exprColumn({ source: null, columnName: exprColumnName({ name: "UserId" }) });
    const version = exprColumn({ source: null, columnName: exprColumnName({ name: "Version" }) });
    expect(toSql(aggregate("COUNT", 1), { dialect: "tsql" })).toBe("COUNT(1)");
    expect(toSql(aggregate("COUNT", id), { dialect: "tsql" })).toBe("COUNT([UserId])");
    expect(toSql(aggregate("COUNT", id, true), { dialect: "tsql" })).toBe(
      "COUNT(DISTINCT [UserId])",
    );
    expect(toSql(aggregate("COUNT", id).over(), { dialect: "tsql" })).toBe("COUNT([UserId])OVER()");
    for (const name of ["MIN", "MAX", "SUM"] as const) {
      expect(toSql(aggregate(name, id), { dialect: "tsql" })).toBe(`${name}([UserId])`);
      expect(toSql(aggregate(name, id, true), { dialect: "tsql" })).toBe(
        `${name}(DISTINCT [UserId])`,
      );
    }
    expect(toSql(aggregate("MIN", id, true).over().orderBy(version), { dialect: "tsql" })).toBe(
      "MIN(DISTINCT [UserId])OVER(ORDER BY [Version])",
    );
    expect(
      toSql(aggregate("MIN", id, true).over().partitionBy(version).orderBy(version), {
        dialect: "tsql",
      }),
    ).toBe("MIN(DISTINCT [UserId])OVER(PARTITION BY [Version] ORDER BY [Version])");
    expect(toSql(aggregate("MIN", id, true).over().partitionBy(version), { dialect: "tsql" })).toBe(
      "MIN(DISTINCT [UserId])OVER(PARTITION BY [Version])",
    );
    expect(toSql(aggregate("MAX", id, true).over(), { dialect: "tsql" })).toBe(
      "MAX(DISTINCT [UserId])OVER()",
    );
  });
  it("ports portable ordered and unordered STRING_AGG forms", () => {
    const unordered = stringAgg("name", "'|");
    const ordered = stringAgg("name", "'|").orderBy(lit(1).asc(), lit(2).desc());
    expect(toSql(unordered, { dialect: "tsql" })).toBe("STRING_AGG('name','''|')");
    expect(toSql(unordered, { dialect: "pgsql" })).toBe("STRING_AGG('name','''|')");
    expect(toSql(unordered, { dialect: "mysql" })).toBe("GROUP_CONCAT('name' SEPARATOR '''|')");
    expect(toSql(unordered, { dialect: "sqlite" })).toBe("GROUP_CONCAT('name','''|')");
    expect(toSql(ordered, { dialect: "tsql" })).toContain("WITHIN GROUP (ORDER BY 1,2 DESC)");
    expect(toSql(ordered, { dialect: "pgsql" })).toContain("ORDER BY 1,2 DESC)");
    expect(toSql(ordered, { dialect: "mysql" })).toContain("ORDER BY 1,2 DESC SEPARATOR");
    expect(toSql(ordered, { dialect: "sqlite" })).toContain("ORDER BY 1,2 DESC");
  });
  it("unwraps literal parameters and rejects dynamic MySQL STRING_AGG separators", () => {
    expect(toSql(stringAgg("name", param("'|")), { dialect: "tsql" })).toBe(
      "STRING_AGG('name','''|')",
    );
    expect(toSql(stringAgg("name", param("'|")), { dialect: "mysql" })).toBe(
      "GROUP_CONCAT('name' SEPARATOR '''|')",
    );
    expect(() => toSql(stringAgg("name", call("LOWER", "x")), { dialect: "mysql" })).toThrow(
      /non-null string literal/,
    );
  });
  it("ports built-in and custom scalar functions", () => {
    expect(toSql(call("COUNT"), { dialect: "tsql" })).toBe("COUNT()");
    expect(toSql(call("COUNT", 1, "5"), { dialect: "tsql" })).toBe("COUNT(1,'5')");
    const date = exprDateTimeLiteral({ value: dateTimeValue("2020-10-19T00:00:00") });
    expect(toSql(call("COUNT", 1, "5", date), { dialect: "tsql" })).toBe(
      "COUNT(1,'5','2020-10-19')",
    );
    const custom = callCustom("m]yFun'c", [1, "5"], { schema: "dbo" });
    expect(toSql(custom, { dialect: "tsql" })).toBe("[dbo].[m]]yFun'c](1,'5')");
    expect(
      toSql(callCustom("m]yFun'c", [1, "5", date], { schema: "dbo" }), { dialect: "tsql" }),
    ).toBe("[dbo].[m]]yFun'c](1,'5','2020-10-19')");
    expect(
      toSql(callCustom("m]yFun'c", [], { schema: "dbo", database: "db1" }), { dialect: "tsql" }),
    ).toBe("[db1].[dbo].[m]]yFun'c]()");
  });
  it("ports SQLite schema elision for scalar functions", () => {
    expect(toSql(callCustom("MyFunc", [1], { schema: "dbo" }), { dialect: "sqlite" })).toBe(
      '"MyFunc"(1)',
    );
  });
  it("ports SQLite schema elision for table functions", () => {
    const source = tableFunction(
      "MyTableFunc",
      [1],
      "T",
      { value: column(sqlType.int32) },
      { schema: "dbo" },
    );
    const query = exprQuerySpecification({
      selectList: [exprInt32Literal({ value: 1 })],
      top: null,
      from: source.$metadata.source,
      where: null,
      groupBy: null,
      distinct: false,
    });
    expect(toSql(query, { dialect: "sqlite" })).toBe('SELECT 1 FROM "MyTableFunc"(1) "T"');
    expect(source.value.sourceAlias).toBe("T");
  });
  it("ports explicit-size Unicode VARCHAR to MySQL utf8mb4", () => {
    expect(
      toSqlType(exprTypeString({ size: 20000, isUnicode: true, isText: false }), {
        dialect: "mysql",
      }),
    ).toBe("varchar(20000) character set utf8mb4");
  });
  it("ports Oracle MySQL HOUR date-time literal formatting", () => {
    const date = exprDateTimeLiteral({ value: dateTimeValue("2020-02-03T04:05:06.000") });
    expect(toSql(hour(date), { dialect: "mysql", mysqlFlavor: "oracle" })).toBe(
      "HOUR('2020-02-03 04:05:06.000')",
    );
  });
  it("ports explicit unsafe values", () => {
    expect(toSql(unsafeSql("'Wh' + 'at ever'"), { dialect: "tsql" })).toBe("'Wh' + 'at ever'");
    expect(select({ value: unsafeSql("'Wh' + 'at ever'") }).toSql({ dialect: "tsql" })).toBe(
      "SELECT 'Wh' + 'at ever' [value]",
    );
  });
  it("ports mixed-result CASE expressions", () => {
    const name = exprColumn({ source: null, columnName: exprColumnName({ name: "FirstName" }) });
    const value = caseWhen(toExpr(name).eq("John"))
      .then("J")
      .when(toExpr(name).eq("Bob"))
      .then(false)
      .else(5);
    expect(toSql(value, { dialect: "tsql" })).toBe(
      "CASE WHEN [FirstName]='John' THEN 'J' WHEN [FirstName]='Bob' THEN 0 ELSE 5 END",
    );
  });
  it("builds portable JSON operations for every dialect", () => {
    const query = select({
      value: lit('{"a":1}').jsonValue("$.a", exprTypeInt32),
      fragment: lit("[1]").jsonQuery(),
      changed: lit("{}").jsonSet("$.a", 1),
      removed: lit('{"a":1}').jsonRemove("$.a"),
      array: jsonArray(1, null),
      object: jsonObject({ a: 1, b: null }),
    });
    expect(query.toSql({ dialect: "tsql" })).toContain("OPENJSON");
    expect(query.toSql({ dialect: "pgsql" })).toContain("jsonb_set");
    expect(query.toSql({ dialect: "mysql" })).toContain("JSON_SET");
    expect(query.toSql({ dialect: "sqlite" })).toContain("json_set");
  });
  it("rejects unsupported JSON paths and root removal", () => {
    for (const path of ["", "store", "$..name", "$.items[-1]", "$.items[*]", "$.items[0:2]"])
      expect(() => lit("{}").jsonValue(path)).toThrow(/Invalid portable JSON path/);
    expect(() => lit("{}").jsonRemove("$")).toThrow(/root/);
    expect(() => jsonObject(["a", 1], ["a", 2])).toThrow(/unique/);
  });
  it("rejects an omitted JSON value path", () => {
    expect(() => Reflect.apply(jsonValue, undefined, ["{}"])).toThrow(/Invalid portable JSON path/);
  });
  it("rejects an omitted JSON table-column path", () => {
    expect(() =>
      Reflect.apply(jsonTableValueColumn, undefined, ["Id", undefined, exprTypeInt32]),
    ).toThrow(/Invalid portable JSON path/);
  });
  it("builds a typed JSON table source", () => {
    const item = jsonTable('{"items":[{"id":2}]}', "$.items")
      .value("Id", "$.id", exprTypeInt32)
      .query("Meta", "$.meta")
      .ordinal("Ordinal")
      .as("j");
    expect(
      select(item.Id, item.Meta, item.Ordinal).from(item).toSql({ dialect: "pgsql" }),
    ).toContain("jsonb_array_elements");
    expect(item.Id.nullable).toBe(true);
  });
  it("uses the parent object for typed T-SQL Boolean extraction", () => {
    const sql = toSql(lit('{"item":{"active":true}}').jsonValue("$.item.active", exprTypeBoolean), {
      dialect: "tsql",
    });
    expect(sql).toContain("OPENJSON('{\"item\":{\"active\":true}}','$.item')");
    expect(sql).toContain("[key]='active' AND [type]=3");
  });
  it("preserves fractional precision for unspecified JSON decimals", () => {
    const value = lit('{"price":12.5}').jsonValue(
      "$.price",
      exprTypeDecimal({ precisionScale: null }),
    );
    expect(toSql(value, { dialect: "tsql" })).toContain("TRY_CONVERT(decimal(38,18),[value])");
    expect(toSql(value, { dialect: "mysql" })).toContain("AS decimal(38,18)");
  });
  it("keeps JSON output columns terminal", () => {
    expect(() => toSql(jsonOutput(1, "$.nested.value"), { dialect: "tsql" })).toThrow(
      /only be exported as part/,
    );
  });
  it("accepts JSON output columns in SELECT and rejects duplicate paths during export", () => {
    const query = selectJson(jsonOutput(1, "$.a"), jsonOutput(1, "$.a")).forJson();
    expect(() => query.toSql({ dialect: "tsql" })).toThrow(/paths must be unique/);
  });
  it("stores and exports portable FOR JSON options", () => {
    const query = select({ optional: null }).forJson({
      withoutArrayWrapper: true,
      includeNullValues: false,
    });
    expect(query).toMatchObject({
      kind: "ExprQueryAsJson",
      withoutArrayWrapper: true,
      includeNullValues: false,
    });
    const tsql = query.toSql({ dialect: "tsql" });
    expect(tsql).toContain("JSON_QUERY(J1.Json,'$[0]')");
    expect(tsql).not.toContain("INCLUDE_NULL_VALUES");
    expect(query.toSql({ dialect: "pgsql" })).toContain("jsonb_strip_nulls");
    expect(query.toSql({ dialect: "mysql" })).toContain("JSON_MERGE_PATCH");
    expect(query.toSql({ dialect: "sqlite" })).toContain("json_patch");
  });
  it("keeps nested JSON fragments structured in SQLite FOR JSON output", () => {
    const query = selectJson(
      jsonOutput(lit('{"active":true}').jsonQuery(), "$.metadata"),
    ).forJson();
    expect(query.toSql({ dialect: "sqlite" })).toContain('json(J0."metadata")');
  });
  it("keeps nested JSON fragments structured in MySQL-family FOR JSON output", () => {
    const query = selectJson(
      jsonOutput(lit('{"active":true}').jsonQuery(), "$.metadata"),
    ).forJson();
    expect(query.toSql({ dialect: "mysql", mysqlFlavor: "mariadb" })).toContain(
      "JSON_QUERY(J0.`metadata`,'$')",
    );
  });
  it("preserves MariaDB JSON fragments across derived-table boundaries", () =>
    expect(toSql(lit("{}").jsonQuery(), { dialect: "mysql", mysqlFlavor: "mariadb" })).toContain(
      "AS CHAR(65535)",
    ));
  it("projects direct columns in portable FOR JSON queries", () => {
    const source = tableFunction("items", [], "i", { id: column(sqlType.int32) });
    expect(select(source.id).from(source).forJson().toSql({ dialect: "sqlite" })).toContain(
      'J0."id"',
    );
  });
});
