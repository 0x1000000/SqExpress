import { describe, expect, it } from "vitest";
import { add, aliasTable, and, cast, column, defineTable, derivedTable, desc, eq, exists, exprTypeString, gt, inList, inQuery, joinAnd, joinOr, lit, modify, neq, nullableColumn, orderQuery, scalarSubquery, select, SqlFormattingProfile, sqlType, toExpr, toSql, unionAll, values } from "../../src/index.js";

describe("typed select builder slice", () => {
  const users = aliasTable(defineTable({ schema: "dbo", name: "Users", columns: { id: column(sqlType.int32), name: nullableColumn(sqlType.string(100)) } }), "u");
  it("escapes SQL projection names that collide with builder methods", () => {
    const source = defineTable({ schema: "dbo", name: "Users", columns: { from: column(sqlType.int32) } });
    const query = select({ from: source.from }).from(source);
    expect(query.$from.name).toBe("from");
    expect(query.toSql()).toBe("SELECT [Users].[from] [from] FROM [dbo].[Users]");
  });
  it("returns the completed AST directly", () => {
    const query = select(users.id).from(users).where(users.id.eq(1));
    expect(query.kind).toBe("ExprQuerySpecification");
    expect("done" in query).toBe(false);
    expect("ast" in query).toBe(false);
    expect(toSql(query, { dialect: "tsql" })).toContain("WHERE [u].[id]=1");
    expect(query.toSql()).toBe("SELECT [u].[id] FROM [dbo].[Users] [u] WHERE [u].[id]=1");
    expect(query.toSql({ formatting: SqlFormattingProfile.spacious })).toContain("\nWHERE\n");
    expect(select(users.id).from(users).where(users.id.eq(1)).toSql()).toBe(query.toSql());
  });
  it("modifies a query while preserving query behavior and row typing", () => {
    const query = select(lit(1).as("one"));
    const changed = query.modify(node => node.kind === "ExprInt32Literal" ? lit(2) : node);
    expect(changed.toSql()).toBe("SELECT 2 [one]");
    expect(() => query.modify(node => node.kind === "ExprQuerySpecification" ? null : node)).toThrow(/retain a query root/);
  });
  it("selects primitive and expression values without aliases", () => {
    expect(select(1).toSql()).toBe("SELECT 1");
    expect(select(lit(1)).toSql()).toBe("SELECT 1");
    expect(select(1, add(1, 2)).toSql()).toBe("SELECT 1,1+2");
    expect(select([lit(1), "text"], { distinct: true }).toSql()).toBe("SELECT DISTINCT 1,'text'");
  });
  it("selects fluent aliased expressions", () => {
    const query = select(lit(1).as("one"));
    expect(query.one.name).toBe("one");
    expect(query.toSql()).toBe("SELECT 1 [one]");
    expect(select(lit(1).as("one"), lit("two").as("second")).toSql()).toBe("SELECT 1 [one],'two' [second]");
  });
  it("selects all columns from a query source", () => {
    const query = select(1, "1");
    expect(select(query.$all).from(query).toSql({ dialect: "tsql" })).toBe("SELECT [A0].* FROM (SELECT 1,'1')[A0]");
    const named = select(lit(1).as("one"));
    const outer = select(named.$all).from(named);
    expect(outer.one.name).toBe("one");
    expect(outer.toSql()).toBe("SELECT [A0].* FROM (SELECT 1 [one])[A0]");
  });
  it("selects typed columns from variadic and tuple shorthand", () => {
    const variadic = select(users.id, users.name).from(users).toSql();
    const tuple = select([users.id, users.name], { distinct: true }).from(users).toSql();
    expect(variadic).toBe("SELECT [u].[id],[u].[name] FROM [dbo].[Users] [u]");
    expect(tuple).toBe("SELECT DISTINCT [u].[id],[u].[name] FROM [dbo].[Users] [u]");
  });
  it("assigns deterministic SQL aliases to automatic table references", () => {
    const Users = defineTable({ schema: "dbo", name: "Users", columns: { Id: column(sqlType.int32), ManagerId: column(sqlType.int32) } });
    const employee = Users(); const manager = Users();
    expect(select(employee.Id, manager.Id).from(employee).leftJoin(manager, employee.ManagerId.eq(manager.Id)).toSql())
      .toBe("SELECT [A0].[Id],[A1].[Id] FROM [dbo].[Users] [A0] LEFT JOIN [dbo].[Users] [A1] ON [A0].[ManagerId]=[A1].[Id]");
  });
  it("uses an unfinished SELECT as a typed derived-table source", () => {
    const Users = defineTable({ schema: "dbo", name: "Users", columns: { Id: column(sqlType.int32), Name: column(sqlType.string()) } });
    const query = select({ Id: Users.Id, Name: Users.Name }).from(Users);
    const sql = select(query.Id, query.Name).from(query).toSql();
    expect(sql).toBe("SELECT [A0].[Id],[A0].[Name] FROM (SELECT [Users].[Id] [Id],[Users].[Name] [Name] FROM [dbo].[Users])[A0]");
  });
  it("chains boolean helpers directly", () => {
    const predicate = eq(1, "1").and(neq(1, "1").or(gt(1, 2)));
    expect(toSql(predicate, { dialect: "tsql" })).toBe("1='1' AND (1!='1' OR 1>2)");
    expect(Object.keys(predicate)).not.toContain("and");
    expect(Object.isFrozen(predicate)).toBe(true);
  });
  it("ports JoinAsAnd and JoinAsOr for one, two, and four predicates", () => {
    const predicates = [eq(1, 1), eq(2, 2), eq(3, 3), eq(4, 4)];
    expect(toSql(joinAnd(predicates.slice(0, 1)), { dialect: "tsql" })).toBe("1=1");
    expect(toSql(joinAnd(predicates.slice(0, 2)), { dialect: "tsql" })).toBe("1=1 AND 2=2");
    expect(toSql(joinAnd(predicates), { dialect: "tsql" })).toBe("1=1 AND 2=2 AND 3=3 AND 4=4");
    expect(toSql(joinOr(predicates.slice(0, 1)), { dialect: "tsql" })).toBe("1=1");
    expect(toSql(joinOr(predicates.slice(0, 2)), { dialect: "tsql" })).toBe("1=1 OR 2=2");
    expect(toSql(joinOr(predicates), { dialect: "tsql" })).toBe("1=1 OR 2=2 OR 3=3 OR 4=4");
  });
  it("ports empty predicate, GROUP BY, and ORDER BY rejection", () => {
    expect(() => joinAnd([])).toThrow(/at least one/); expect(() => joinOr([])).toThrow(/at least one/);
    expect(() => select({ id: users.id }).from(users).groupBy()).toThrow(/GROUP BY/);
    expect(() => select({ id: users.id }).from(users).orderBy()).toThrow(/ORDER BY/);
  });
  it("ports nullable WHERE and scalar subquery projection", () => {
    const withoutFilter = select({ id: users.id }).from(users).where(null).orderBy(users.id);
    expect(toSql(withoutFilter, { dialect: "tsql" })).not.toContain(" WHERE ");
    const query = select({ nested: scalarSubquery(select({ one: 1 })) });
    expect(toSql(query, { dialect: "tsql" })).toContain("(SELECT 1 [one])");
  });
  it("ports EXISTS and IN subquery predicates", () => {
    const subquery = select({ id: users.id }).from(users).where(gt(users.id, 0));
    expect(toSql(exists(subquery), { dialect: "tsql" })).toBe("EXISTS(SELECT [u].[id] [id] FROM [dbo].[Users] [u] WHERE [u].[id]>0)");
    expect(toSql(inQuery(1, subquery), { dialect: "tsql" })).toBe("1 IN(SELECT [u].[id] [id] FROM [dbo].[Users] [u] WHERE [u].[id]>0)");
  });
  it("ports derived-table column-count validation and no-op modification", () => {
    const query = select({ one: 1, two: 2 });
    const inferred = derivedTable(query, "I");
    expect(Object.keys(inferred.$metadata.columns)).toEqual(["one", "two"]);
    expect(() => derivedTable(query, "D", { one: column(sqlType.int32) })).toThrow(/declared columns/);
    const derived = derivedTable(query, "D", { one: column(sqlType.int32), two: column(sqlType.int32) });
    expect(modify(derived.$metadata.source, node => node)).toBe(derived.$metadata.source);
  });
  it("ports nullable VALUES rows", () => {
    const source = values([[1, null, null, null], [2, null, null, 0], [3, null, null, null]], "V", {
      Id: column(sqlType.int32), Text: nullableColumn(sqlType.string()), Date: nullableColumn(sqlType.dateTime), Optional: nullableColumn(sqlType.int32),
    });
    expect(toSql(source.$metadata.source, { dialect: "tsql" })).toBe("(VALUES (1,NULL,NULL,NULL),(2,NULL,NULL,0),(3,NULL,NULL,NULL))[V]([Id],[Text],[Date],[Optional])");
  });
  it("preserves selected derived-column order independently of descriptor declaration order", () => {
    const source = defineTable({ schema: "dbo", name: "user", columns: { FirstName: column(sqlType.string()), LastName: column(sqlType.string()) } });
    const query = select({ LastName: source.LastName, FirstName: source.FirstName }).from(source);
    const derived = derivedTable(query, "D", { FirstName: column(sqlType.string()), LastName: column(sqlType.string()) });
    expect(toSql(derived.$metadata.source, { dialect: "tsql" })).toContain("SELECT [user].[LastName] [LastName],[user].[FirstName] [FirstName]");
  });
  it("ports list ordering, joined ordering, and offset-only pagination", () => {
    const orders = aliasTable(defineTable({ schema: "dbo", name: "Orders", columns: { userId: column(sqlType.int32) } }), "o");
    const ordered = select({ id: users.id }).from(users).innerJoin(orders, eq(users.id, orders.userId)).orderByList([users.id, desc(orders.userId)]).offsetFetch(5);
    expect(toSql(ordered, { dialect: "tsql" })).toBe("SELECT [u].[id] [id] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [u].[id]=[o].[userId] ORDER BY [u].[id],[o].[userId] DESC OFFSET 5 ROW");
  });
  it("ports ordering a set operation", () => {
    const set = unionAll(select({ Id: 1 }), select({ Id: 2 }));
    expect(toSql(orderQuery(set, [1]), { dialect: "tsql" })).toBe("SELECT 1 [Id] UNION ALL SELECT 2 [Id] ORDER BY 1");
  });
  it("ports Oracle MySQL string casts", () => {
    expect(toSql(cast(7, exprTypeString({ size: 5, isUnicode: false, isText: false })), { dialect: "mysql", mysqlFlavor: "oracle" })).toBe("CAST(7 AS CHAR(5))");
  });
  it("ports database-qualified table names across dialects", () => {
    const table = defineTable({ database: "SomeDB", schema: "dbo", name: "user", columns: { id: column(sqlType.int32) } }); const ast = select({ one: 1 }).from(table);
    expect(toSql(ast, { dialect: "tsql" })).toContain("FROM [SomeDB].[dbo].[user]");
    expect(toSql(ast, { dialect: "postgresql", schemaMap: [{ from: "dbo", to: "public" }] })).toContain('FROM "SomeDB"."public"."user"');
    expect(toSql(ast, { dialect: "mysql" })).toContain("FROM `SomeDB`.`user`");
  });
  it("builds generated AST and exports it", () => {
    const query = select({ id: users.id, name: users.name }).from(users).where(eq(users.id, 1));
    expect(toSql(query, { dialect: "tsql" })).toBe("SELECT [u].[id] [id],[u].[name] [name] FROM [dbo].[Users] [u] WHERE [u].[id]=1");
  });
  it("coerces primitive values into generated AST nodes", () => {
    expect(eq(1, 2)).toMatchObject({ kind: "ExprBooleanEq", left: { kind: "ExprInt32Literal", value: 1 }, right: { kind: "ExprInt32Literal", value: 2 } });
    expect(eq("a", "b")).toMatchObject({ left: { kind: "ExprStringLiteral" }, right: { kind: "ExprStringLiteral" } });
    expect(eq(true, false)).toMatchObject({ left: { kind: "ExprBoolLiteral" }, right: { kind: "ExprBoolLiteral" } });
    expect(lit(1n)).toMatchObject({ kind: "ExprInt64Literal", value: 1n });
    expect(toExpr(1.5)).toMatchObject({ kind: "ExprDoubleLiteral", value: 1.5 });
  });
  it("builds joins, filters, grouping, ordering, TOP, and pagination", () => {
    const orders = aliasTable(defineTable({ schema: "dbo", name: "Orders", columns: { userId: column(sqlType.int32), amount: column(sqlType.decimal(18, 2)) } }), "o");
    const joined = select({ id: users.id, adjusted: add(orders.amount, 1) }, { distinct: true })
      .from(users).innerJoin(orders, eq(users.id, orders.userId))
      .where(and(gt(orders.amount, "1.5"), inList(users.id, 1, 2, 3)))
      .groupBy(users.id, orders.amount).orderBy(desc(users.id)).offsetFetch(5, 10);
    expect(toSql(joined, { dialect: "tsql" })).toBe("SELECT DISTINCT [u].[id] [id],[o].[amount]+1 [adjusted] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [u].[id]=[o].[userId] WHERE [o].[amount]>'1.5' AND [u].[id] IN(1,2,3) GROUP BY [u].[id],[o].[amount] ORDER BY [u].[id] DESC OFFSET 5 ROW FETCH NEXT 10 ROW ONLY");
    expect(toSql(select({ id: users.id }, { top: 10 }).from(users), { dialect: "tsql" })).toBe("SELECT TOP 10 [u].[id] [id] FROM [dbo].[Users] [u]");
  });
  it("builds derived tables and set operations", () => {
    const first = select({ Id: 1 }); const second = select({ Id: 2 });
    expect(toSql(unionAll(first, second), { dialect: "tsql" })).toBe("SELECT 1 [Id] UNION ALL SELECT 2 [Id]");
    const derived = derivedTable(unionAll(first, second), "v", { Id: column(sqlType.int32) });
    const query = select({ Id: derived.Id }).from(derived);
    expect(toSql(query, { dialect: "tsql" })).toBe("SELECT [v].[Id] [Id] FROM (SELECT 1 [Id] UNION ALL SELECT 2 [Id])[v]([Id])");
  });
});
