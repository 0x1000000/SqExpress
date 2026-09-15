import { describe, expect, it } from "vitest";
import { aggregate, asc, call, callCustom, caseWhen, column, dateTimeValue, desc, eq, exprColumn, exprColumnName, exprDateTimeLiteral, exprInt32Literal, exprQuerySpecification, exprTypeBoolean, exprTypeDecimal, exprTypeInt32, exprTypeString, forJson, jsonArray, jsonObject, jsonOutput, jsonQuery, jsonRemove, jsonSet, jsonTableValueColumn, jsonValue, param, portable, select, selectJson, sqlType, stringAgg, tableFunction, toSql, toSqlType, unsafeSql } from "../../src/index.js";

describe("function and JSON builders", () => {
  it("builds scalar, aggregate, CASE, and STRING_AGG expressions", () => {
    const query = select({ lower: call("LOWER", "ABC"), count: aggregate("COUNT", 1), label: caseWhen([eq(1, 1), "yes"]).else("no"), joined: stringAgg("a", "|") });
    expect(toSql(query, { dialect: "tsql" })).toBe("SELECT LOWER('ABC') [lower],COUNT(1) [count],CASE WHEN 1=1 THEN 'yes' ELSE 'no' END [label],STRING_AGG('a','|') [joined]");
  });
  it("ports aggregate and window forms", () => {
    const id = exprColumn({ source: null, columnName: exprColumnName({ name: "UserId" }) });
    const version = exprColumn({ source: null, columnName: exprColumnName({ name: "Version" }) });
    expect(toSql(aggregate("COUNT", 1), { dialect: "tsql" })).toBe("COUNT(1)");
    expect(toSql(aggregate("COUNT", id), { dialect: "tsql" })).toBe("COUNT([UserId])");
    expect(toSql(aggregate("COUNT", id, true), { dialect: "tsql" })).toBe("COUNT(DISTINCT [UserId])");
    expect(toSql(aggregate("COUNT", id).over(), { dialect: "tsql" })).toBe("COUNT([UserId])OVER()");
    for (const name of ["MIN", "MAX", "SUM"] as const) {
      expect(toSql(aggregate(name, id), { dialect: "tsql" })).toBe(`${name}([UserId])`);
      expect(toSql(aggregate(name, id, true), { dialect: "tsql" })).toBe(`${name}(DISTINCT [UserId])`);
    }
    expect(toSql(aggregate("MIN", id, true).over({ orderBy: [version] }), { dialect: "tsql" })).toBe("MIN(DISTINCT [UserId])OVER(ORDER BY [Version])");
    expect(toSql(aggregate("MIN", id, true).over({ partitionBy: [version], orderBy: [version] }), { dialect: "tsql" })).toBe("MIN(DISTINCT [UserId])OVER(PARTITION BY [Version] ORDER BY [Version])");
    expect(toSql(aggregate("MIN", id, true).over({ partitionBy: [version] }), { dialect: "tsql" })).toBe("MIN(DISTINCT [UserId])OVER(PARTITION BY [Version])");
    expect(toSql(aggregate("MAX", id, true).over(), { dialect: "tsql" })).toBe("MAX(DISTINCT [UserId])OVER()");
  });
  it("ports portable ordered and unordered STRING_AGG forms", () => {
    const unordered = stringAgg("name", "'|"); const ordered = stringAgg("name", "'|", asc(1), desc(2));
    expect(toSql(unordered, { dialect: "tsql" })).toBe("STRING_AGG('name','''|')");
    expect(toSql(unordered, { dialect: "postgresql" })).toBe("STRING_AGG('name','''|')");
    expect(toSql(unordered, { dialect: "mysql" })).toBe("GROUP_CONCAT('name' SEPARATOR '''|')");
    expect(toSql(unordered, { dialect: "sqlite" })).toBe("GROUP_CONCAT('name','''|')");
    expect(toSql(ordered, { dialect: "tsql" })).toContain("WITHIN GROUP (ORDER BY 1,2 DESC)");
    expect(toSql(ordered, { dialect: "postgresql" })).toContain("ORDER BY 1,2 DESC)");
    expect(toSql(ordered, { dialect: "mysql" })).toContain("ORDER BY 1,2 DESC SEPARATOR");
    expect(toSql(ordered, { dialect: "sqlite" })).toContain("ORDER BY 1,2 DESC");
  });
  it("unwraps literal parameters and rejects dynamic MySQL STRING_AGG separators", () => {
    expect(toSql(stringAgg("name", param("'|")), { dialect: "tsql" })).toBe("STRING_AGG('name','''|')");
    expect(toSql(stringAgg("name", param("'|")), { dialect: "mysql" })).toBe("GROUP_CONCAT('name' SEPARATOR '''|')");
    expect(() => toSql(stringAgg("name", call("LOWER", "x")), { dialect: "mysql" })).toThrow(/non-null string literal/);
  });
  it("ports built-in and custom scalar functions", () => {
    expect(toSql(call("COUNT"), { dialect: "tsql" })).toBe("COUNT()");
    expect(toSql(call("COUNT", 1, "5"), { dialect: "tsql" })).toBe("COUNT(1,'5')");
    const date = exprDateTimeLiteral({ value: dateTimeValue("2020-10-19T00:00:00") });
    expect(toSql(call("COUNT", 1, "5", date), { dialect: "tsql" })).toBe("COUNT(1,'5','2020-10-19')");
    const custom = callCustom("m]yFun'c", [1, "5"], { schema: "dbo" });
    expect(toSql(custom, { dialect: "tsql" })).toBe("[dbo].[m]]yFun'c](1,'5')");
    expect(toSql(callCustom("m]yFun'c", [1, "5", date], { schema: "dbo" }), { dialect: "tsql" })).toBe("[dbo].[m]]yFun'c](1,'5','2020-10-19')");
    expect(toSql(callCustom("m]yFun'c", [], { schema: "dbo", database: "db1" }), { dialect: "tsql" })).toBe("[db1].[dbo].[m]]yFun'c]()");
  });
  it("ports SQLite schema elision for scalar functions", () => {
    expect(toSql(callCustom("MyFunc", [1], { schema: "dbo" }), { dialect: "sqlite" })).toBe('"MyFunc"(1)');
  });
  it("ports SQLite schema elision for table functions", () => {
    const source = tableFunction("MyTableFunc", [1], "T", { value: column(sqlType.int32) }, { schema: "dbo" });
    const query = exprQuerySpecification({ selectList: [exprInt32Literal({ value: 1 })], top: null, from: source.$metadata.source, where: null, groupBy: null, distinct: false });
    expect(toSql(query, { dialect: "sqlite" })).toBe('SELECT 1 FROM "MyTableFunc"(1) "T"');
    expect(source.value.sourceAlias).toBe("T");
  });
  it("ports explicit-size Unicode VARCHAR to MySQL utf8mb4", () => {
    expect(toSqlType(exprTypeString({ size: 20000, isUnicode: true, isText: false }), { dialect: "mysql" })).toBe("varchar(20000) character set utf8mb4");
  });
  it("ports Oracle MySQL HOUR date-time literal formatting", () => {
    const date = exprDateTimeLiteral({ value: dateTimeValue("2020-02-03T04:05:06.000") });
    expect(toSql(portable("Hour", date), { dialect: "mysql", mysqlFlavor: "oracle" })).toBe("HOUR('2020-02-03 04:05:06.000')");
  });
  it("ports explicit unsafe values", () => {
    expect(toSql(unsafeSql("'Wh' + 'at ever'"), { dialect: "tsql" })).toBe("'Wh' + 'at ever'");
    expect(toSql(select({ value: unsafeSql("'Wh' + 'at ever'") }), { dialect: "tsql" })).toBe("SELECT 'Wh' + 'at ever' [value]");
  });
  it("ports mixed-result CASE expressions", () => {
    const name = exprColumn({ source: null, columnName: exprColumnName({ name: "FirstName" }) });
    const value = caseWhen([eq(name, "John"), "J"], [eq(name, "Bob"), false]).else(5);
    expect(toSql(value, { dialect: "tsql" })).toBe("CASE WHEN [FirstName]='John' THEN 'J' WHEN [FirstName]='Bob' THEN CAST(0 AS bit) ELSE 5 END");
  });
  it("builds portable JSON operations for every dialect", () => {
    const query = select({ value: jsonValue('{"a":1}', "$.a", exprTypeInt32), fragment: jsonQuery("[1]"), changed: jsonSet("{}", "$.a", 1), removed: jsonRemove('{"a":1}', "$.a"), array: jsonArray(1, null), object: jsonObject({ a: 1, b: null }) });
    expect(toSql(query, { dialect: "tsql" })).toContain("OPENJSON");
    expect(toSql(query, { dialect: "postgresql" })).toContain("jsonb_set");
    expect(toSql(query, { dialect: "mysql" })).toContain("JSON_SET");
    expect(toSql(query, { dialect: "sqlite" })).toContain("json_set");
  });
  it("rejects unsupported JSON paths and root removal", () => {
    for (const path of ["", "store", "$..name", "$.items[-1]", "$.items[*]", "$.items[0:2]"]) expect(() => jsonValue("{}", path)).toThrow(/Invalid portable JSON path/);
    expect(() => jsonRemove("{}", "$")).toThrow(/root/);
    expect(() => jsonObject(["a", 1], ["a", 2])).toThrow(/unique/);
  });
  it("rejects an omitted JSON value path", () => {
    expect(() => Reflect.apply(jsonValue, undefined, ["{}"])).toThrow(/Invalid portable JSON path/);
  });
  it("rejects an omitted JSON table-column path", () => {
    expect(() => Reflect.apply(jsonTableValueColumn, undefined, ["Id", undefined, exprTypeInt32])).toThrow(/Invalid portable JSON path/);
  });
  it("uses the parent object for typed T-SQL Boolean extraction", () => {
    const sql = toSql(jsonValue('{"item":{"active":true}}', "$.item.active", exprTypeBoolean), { dialect: "tsql" });
    expect(sql).toContain("OPENJSON('{\"item\":{\"active\":true}}','$.\"item\"')"); expect(sql).toContain("[key]='active' AND [type]=3");
  });
  it("preserves fractional precision for unspecified JSON decimals", () => {
    const value = jsonValue('{"price":12.5}', "$.price", exprTypeDecimal({ precisionScale: null }));
    expect(toSql(value, { dialect: "tsql" })).toContain("TRY_CONVERT(decimal(38,18),[value])"); expect(toSql(value, { dialect: "mysql" })).toContain("AS decimal(38,18)");
  });
  it("keeps JSON output columns terminal", () => {
    expect(() => toSql(jsonOutput(1, "$.nested.value"), { dialect: "tsql" })).toThrow(/only be exported as part/);
  });
  it("accepts JSON output columns in SELECT and rejects duplicate paths during export", () => {
    const query = forJson(selectJson(jsonOutput(1, "$.a"), jsonOutput(1, "$.a")));
    expect(() => toSql(query, { dialect: "tsql" })).toThrow(/paths must be unique/);
  });
  it("stores and exports portable FOR JSON options", () => {
    const query = forJson(select({ optional: null }), { withoutArrayWrapper: true, includeNullValues: false });
    expect(query).toMatchObject({ kind: "ExprQueryAsJson", withoutArrayWrapper: true, includeNullValues: false });
    const tsql = toSql(query, { dialect: "tsql" }); expect(tsql).toContain("JSON_QUERY(J1.Json,'$[0]')"); expect(tsql).not.toContain("INCLUDE_NULL_VALUES");
    expect(toSql(query, { dialect: "postgresql" })).toContain("jsonb_strip_nulls"); expect(toSql(query, { dialect: "mysql" })).toContain("JSON_MERGE_PATCH"); expect(toSql(query, { dialect: "sqlite" })).toContain("json_patch");
  });
});
