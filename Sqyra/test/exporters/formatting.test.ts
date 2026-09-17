import { describe, expect, it } from "vitest";
import {
  aliasTable,
  column,
  columnExpression,
  cte,
  defineTable,
  deleteFrom,
  exprInt32Literal,
  exprOffsetFetch,
  exprOrderByOffsetFetch,
  exprQueryList,
  exprQuerySpecification,
  exprSelectOffsetFetch,
  exprValueQuery,
  insertInto,
  select,
  sqlType,
  SqlFormattingProfile,
  toSql,
  update,
} from "../../src/index.js";

const query = exprQuerySpecification({
  selectList: [exprInt32Literal({ value: 1 }), exprInt32Literal({ value: 2 })],
  top: null,
  from: null,
  where: null,
  groupBy: null,
  distinct: false,
});
function tokens(sql: string): string {
  let result = "",
    end = "";
  for (let i = 0; i < sql.length; i++) {
    const c = sql[i]!;
    if (end !== "") {
      result += c;
      if (c === end) {
        if (sql[i + 1] === end) result += sql[++i];
        else end = "";
      } else if (c === "\\" && i + 1 < sql.length) result += sql[++i];
    } else if (c === "'" || c === '"' || c === "`" || c === "[") {
      end = c === "[" ? "]" : c;
      result += c;
    } else if (!/\s/.test(c)) result += c;
  }
  return result;
}

describe("SQL formatting profiles", () => {
  it("ports immutable profile patches and inherited values", () => {
    const patch: { indentationSize: number; newLineBeforeClauses: boolean; selectBody?: "inline" } =
      { indentationSize: 0, newLineBeforeClauses: false };
    const profile = SqlFormattingProfile.spacious.withOptions(patch);
    patch.indentationSize = 99;
    patch.selectBody = "inline";
    expect(profile.indentationSize).toBe(0);
    expect(profile.newLineBeforeClauses).toBe(false);
    expect(profile.selectBody).toBe("next-line-indented");
    const second = profile.withOptions({ newLineBeforeJoins: false });
    expect(second.indentationSize).toBe(0);
    expect(second.newLineBeforeClauses).toBe(false);
    expect(second.newLineBeforeJoins).toBe(false);
    expect(profile.newLineBeforeJoins).toBe(true);
    expect(SqlFormattingProfile.spacious.indentationSize).toBe(4);
    expect(SqlFormattingProfile.unformatted.newLineStyle).toBe("platform");
    expect(Object.isFrozen(profile)).toBe(true);
  });
  it("ports invalid formatting option rejection", () => {
    expect(() => SqlFormattingProfile.spacious.withOptions(null!)).toThrow(TypeError);
    expect(() => SqlFormattingProfile.spacious.withOptions({ indentationSize: -1 })).toThrow(
      RangeError,
    );
    for (const [key, value] of [
      ["newLineStyle", "bad"],
      ["selectBody", "bad"],
      ["whereBody", "bad"],
      ["groupByBody", "bad"],
      ["orderByBody", "bad"],
      ["setBody", "bad"],
      ["joinOnBody", "bad"],
      ["outputBody", "bad"],
      ["valuesBody", "bad"],
      ["tableAliasPlacement", "bad"],
      ["booleanOperatorPlacement", "bad"],
      ["selectList", "bad"],
      ["groupByList", "bad"],
      ["orderByList", "bad"],
      ["setList", "bad"],
      ["outputList", "bad"],
      ["valuesRows", "bad"],
    ] as const)
      expect(() => SqlFormattingProfile.spacious.withOptions({ [key]: value })).toThrow(RangeError);
  });
  it("ports independent SELECT body and list layout", () => {
    const cases = [
      [{ selectBody: "inline", selectList: "inline" }, "SELECT 1,2"],
      [{ selectBody: "inline", selectList: "one-item-per-line" }, "SELECT 1,\n  2"],
      [{ selectBody: "next-line-indented", selectList: "inline" }, "SELECT\n  1,2"],
      [{ selectBody: "next-line-indented", selectList: "one-item-per-line" }, "SELECT\n  1,\n  2"],
    ] as const;
    for (const [options, expected] of cases)
      for (const dialect of ["tsql", "pgsql", "mysql", "sqlite"] as const)
        expect(
          toSql(query, {
            dialect,
            formatting: SqlFormattingProfile.unformatted.withOptions({
              indentationSize: 2,
              newLineStyle: "lf",
              ...options,
            }),
          }),
        ).toBe(expected);
  });
  it("ports LF, CRLF, and zero indentation", () => {
    const one = exprQuerySpecification({
      selectList: [exprInt32Literal({ value: 1 })],
      top: null,
      from: null,
      where: null,
      groupBy: null,
      distinct: false,
    });
    expect(
      toSql(one, {
        dialect: "tsql",
        formatting: SqlFormattingProfile.spacious.withOptions({
          indentationSize: 0,
          newLineStyle: "lf",
        }),
      }),
    ).toBe("SELECT\n1");
    expect(
      toSql(one, {
        dialect: "tsql",
        formatting: SqlFormattingProfile.spacious.withOptions({
          indentationSize: 0,
          newLineStyle: "crlf",
        }),
      }),
    ).toBe("SELECT\r\n1");
  });
  it("ports spacious SELECT formatting for every dialect", () => {
    for (const dialect of ["tsql", "pgsql", "mysql", "sqlite"] as const)
      expect(
        toSql(query, {
          dialect,
          formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
        }),
      ).toBe("SELECT\n    1,\n    2");
  });
  it("ports compact output and trailing-whitespace guarantees", () => {
    expect(toSql(query, { dialect: "tsql" })).toBe("SELECT 1,2");
    expect(toSql(query, { dialect: "tsql", formatting: SqlFormattingProfile.unformatted })).toBe(
      "SELECT 1,2",
    );
    const formatted = toSql(query, { dialect: "tsql", formatting: SqlFormattingProfile.spacious });
    expect(formatted.endsWith("\n")).toBe(false);
    for (const line of formatted.split(/\r?\n/)) expect(line).toBe(line.trimEnd());
  });
  it("ports preservation of non-formatting exporter options", () => {
    expect(
      toSql(query, {
        dialect: "tsql",
        avoidNameQuoting: true,
        formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
      }),
    ).toBe("SELECT\n    1,\n    2");
  });
  it("ports spacious query structure and reused columns", () => {
    const base = defineTable({
      schema: "dbo",
      name: "Users",
      columns: { id: column(sqlType.int32), name: column(sqlType.string()) },
    });
    const users = aliasTable(base, "u");
    const other = aliasTable(base, "u2");
    const ast = select({ id: users.id, name: users.name })
      .from(users)
      .innerJoin(other, other.id.eq(users.id))
      .where(users.id.eq(1).and(users.name.eq("First")))
      .orderBy(users.id);
    expect(
      ast.toSql({
        dialect: "tsql",
        formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
      }),
    ).toBe(
      "SELECT\n    [u].[id] [id],\n    [u].[name] [name]\nFROM [dbo].[Users]\n    [u]\nJOIN [dbo].[Users]\n    [u2] ON\n    [u2].[id]=[u].[id]\nWHERE\n    [u].[id]=1\n    AND\n    [u].[name]='First'\nORDER BY\n    [u].[id]",
    );
  });
  it("ports spacious INSERT, UPDATE, DELETE, and set operators", () => {
    const users = defineTable({
      schema: "dbo",
      name: "Users",
      columns: { id: column(sqlType.int32), name: column(sqlType.string()) },
    });
    const formatting = SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" });
    expect(
      insertInto(users)
        .values({ id: 1, name: "A" }, { id: 2, name: "B" })
        .toSql({ dialect: "tsql", formatting }),
    ).toBe("INSERT INTO [dbo].[Users]([id],[name])\nVALUES\n    (1,'A'),\n    (2,'B')");
    expect(
      update(users)
        .set({ id: 1, name: "A" })
        .where(users.id.eq(2))
        .toSql({ dialect: "tsql", formatting }),
    ).toBe(
      "UPDATE [dbo].[Users]\nSET\n    [Users].[id]=1,\n    [Users].[name]='A'\nWHERE\n    [Users].[id]=2",
    );
    expect(deleteFrom(users).where(users.id.eq(2)).toSql({ dialect: "tsql", formatting })).toBe(
      "DELETE [dbo].[Users]\nWHERE\n    [Users].[id]=2",
    );
    expect(
      select({ one: 1 })
        .unionAll(select({ one: 2 }))
        .toSql({ dialect: "tsql", formatting }),
    ).toBe("SELECT\n    1 [one]\nUNION ALL\nSELECT\n    2 [one]");
  });
  it("ports inline, line-start, and separate-line Boolean operators", () => {
    const users = defineTable({
      schema: "dbo",
      name: "Users",
      columns: { id: column(sqlType.int32) },
    });
    const ast = select({ one: 1 })
      .from(users)
      .where(users.id.eq(1).or(users.id.eq(2)));
    const expected = {
      inline: "[Users].[id]=1 OR [Users].[id]=2",
      "line-start": "[Users].[id]=1\n    OR [Users].[id]=2",
      "separate-line": "[Users].[id]=1\n    OR\n    [Users].[id]=2",
    } as const;
    for (const placement of ["inline", "line-start", "separate-line"] as const)
      expect(
        ast.toSql({
          dialect: "tsql",
          formatting: SqlFormattingProfile.spacious.withOptions({
            newLineStyle: "lf",
            booleanOperatorPlacement: placement,
          }),
        }),
      ).toContain(`WHERE\n    ${expected[placement]}`);
  });
  it("ports every formatting option without changing SQL tokens", () => {
    const users = aliasTable(
      defineTable({
        schema: "dbo",
        name: "Users",
        columns: { id: column(sqlType.int32), name: column(sqlType.string()) },
      }),
      "U",
    );
    const statements = [
      select({ id: users.id, again: users.id, text: "a \n b" })
        .from(users)
        .where(users.id.eq(1).or(users.id.eq(2)))
        .groupBy(users.id)
        .orderBy(users.id),
      select({ one: 1 }).unionAll(select({ one: 2 })),
      insertInto(users).values({ id: 1, name: "a" }, { id: 2, name: "b" }).ast,
      update(users).set({ name: "x" }).where(users.id.eq(1)).ast,
      deleteFrom(users).where(users.id.eq(1)).ast,
    ];
    const patches = [
      { indentationSize: 0 },
      { newLineBeforeClauses: false },
      { newLineBeforeJoins: false },
      { newLineAroundSetOperators: false },
      { multilineSubqueries: false },
      { multilineBooleanParentheses: false },
      { multilineCteBodies: false },
      { newLineBetweenCtes: false },
      { newLineBetweenStatements: false },
    ] as const;
    for (const dialect of ["tsql", "pgsql", "mysql", "sqlite"] as const)
      for (const statement of statements) {
        const compact = toSql(statement, { dialect });
        expect(toSql(statement, { dialect, formatting: SqlFormattingProfile.unformatted })).toBe(
          compact,
        );
        for (const patch of patches) {
          const formatted = toSql(statement, {
            dialect,
            formatting: SqlFormattingProfile.spacious.withOptions(patch),
          });
          expect(tokens(formatted)).toBe(tokens(compact));
          expect(formatted.endsWith("\n")).toBe(false);
        }
      }
  });
  it("ports query-list statement separation without a final newline", () => {
    const users = defineTable({
      schema: "dbo",
      name: "user",
      columns: { id: column(sqlType.int32) },
    });
    const list = exprQueryList({
      expressions: [
        deleteFrom(users).ast,
        exprQuerySpecification({
          selectList: [exprInt32Literal({ value: 1 })],
          top: null,
          from: null,
          where: null,
          groupBy: null,
          distinct: false,
        }),
      ],
    });
    const sql = toSql(list, {
      dialect: "tsql",
      formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
    });
    expect(sql).toBe("DELETE [dbo].[user];\nSELECT\n    1");
    expect(sql.endsWith("\n")).toBe(false);
  });
  it("ports pagination token preservation and TOP/FETCH rejection", () => {
    const users = aliasTable(
      defineTable({ schema: "dbo", name: "user", columns: { UserId: column(sqlType.int32) } }),
      "U",
    );
    const orderedOffset = select({ UserId: users.UserId }, { top: 7 })
      .from(users)
      .orderBy(users.UserId)
      .offsetFetch(5);
    const offsetFetch = select({ UserId: users.UserId })
      .from(users)
      .orderBy(users.UserId)
      .offsetFetch(5, 9);
    const base = exprQuerySpecification({
      selectList: [columnExpression(users.UserId)],
      top: exprInt32Literal({ value: 7 }),
      from: null,
      where: users.UserId.gt(0),
      groupBy: null,
      distinct: false,
    });
    const unorderedOffset = exprSelectOffsetFetch({
      selectQuery: base,
      orderBy: exprOrderByOffsetFetch({
        orderList: [],
        offsetFetch: exprOffsetFetch({ offset: exprInt32Literal({ value: 5 }), fetch: null }),
      }),
    });
    const invalid = select({ UserId: users.UserId }, { top: 7 })
      .from(users)
      .orderBy(users.UserId)
      .offsetFetch(5, 9);
    const dialects = [
      { dialect: "tsql" },
      { dialect: "pgsql" },
      { dialect: "mysql", mysqlFlavor: "mariadb" },
      { dialect: "mysql", mysqlFlavor: "oracle" },
      { dialect: "sqlite" },
    ] as const;
    for (const options of dialects) {
      for (const ast of [orderedOffset, unorderedOffset, offsetFetch]) {
        const compact = toSql(ast, options);
        const formatted = toSql(ast, {
          ...options,
          formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
        });
        expect(tokens(formatted)).toBe(tokens(compact));
      }
      expect(() => invalid.toSql(options)).toThrow(/TOP and OFFSET\/FETCH/);
      expect(() =>
        invalid.toSql({ ...options, formatting: SqlFormattingProfile.spacious }),
      ).toThrow(/TOP and OFFSET\/FETCH/);
    }
  });
  it("ports recursive and multiple CTE whitespace for every dialect", () => {
    const first = cte("First", { Num: column(sqlType.int32) }, (self) =>
      select({ Num: 1 }).unionAll(
        select({ Num: self.Num.add(1) })
          .from(self)
          .where(self.Num.lt(3)),
      ),
    );
    const second = cte("Second", { Num: column(sqlType.int32) }, () => select({ Num: 2 }));
    const ast = select({ First: first.Num, Second: second.Num }).from(first).crossJoin(second);
    for (const options of [
      { dialect: "tsql" },
      { dialect: "pgsql" },
      { dialect: "mysql", mysqlFlavor: "mariadb" },
      { dialect: "mysql", mysqlFlavor: "oracle" },
      { dialect: "sqlite" },
    ] as const) {
      const compact = ast.toSql(options);
      const formatted = ast.toSql({
        ...options,
        formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
      });
      expect(tokens(formatted)).toBe(tokens(compact));
      expect(formatted).toContain("AS(\n    SELECT\n");
      expect(formatted).toContain("),\n");
      expect(formatted).toContain(")\nSELECT\n");
      expect(formatted).not.toContain("\n\n");
    }
  });
  it("ports compact and line-start Boolean parenthesis spacing", () => {
    const users = defineTable({
      schema: "dbo",
      name: "user",
      columns: { Id: column(sqlType.int32) },
    });
    const predicate = users.Id.eq(1)
      .or(users.Id.eq(2))
      .and(users.Id.eq(3).or(users.Id.eq(4)));
    expect(
      toSql(predicate, { dialect: "tsql", formatting: SqlFormattingProfile.unformatted }),
    ).toContain(")AND(");
    const profile = SqlFormattingProfile.unformatted.withOptions({
      booleanOperatorPlacement: "line-start",
      newLineStyle: "lf",
    });
    expect(toSql(predicate, { dialect: "tsql", formatting: profile })).toContain(")\nAND (");
  });
  it("ports spacious formatting of only required Boolean parentheses", () => {
    const users = defineTable({
      schema: "dbo",
      name: "user",
      columns: { UserId: column(sqlType.int32) },
    });
    const filter = users.UserId.eq(1).or(users.UserId.eq(2)).and(users.UserId.eq(3));
    const ast = select({ UserId: users.UserId }).from(users).where(filter);
    expect(
      ast.toSql({
        dialect: "tsql",
        formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
      }),
    ).toBe(
      "SELECT\n    [user].[UserId] [UserId]\nFROM [dbo].[user]\nWHERE\n    (\n        [user].[UserId]=1\n        OR\n        [user].[UserId]=2\n    )\n    AND\n    [user].[UserId]=3",
    );
  });
  it("indents each reused subquery occurrence by its actual depth", () => {
    const inner = exprQuerySpecification({
      selectList: [exprInt32Literal({ value: 1 })],
      top: null,
      from: null,
      where: null,
      groupBy: null,
      distinct: false,
    });
    const outer = exprQuerySpecification({
      selectList: [exprValueQuery({ query: inner })],
      top: null,
      from: null,
      where: null,
      groupBy: null,
      distinct: false,
    });
    const ast = exprQuerySpecification({
      selectList: [exprValueQuery({ query: inner }), exprValueQuery({ query: outer })],
      top: null,
      from: null,
      where: null,
      groupBy: null,
      distinct: false,
    });
    expect(
      toSql(ast, {
        dialect: "tsql",
        formatting: SqlFormattingProfile.spacious.withOptions({ newLineStyle: "lf" }),
      }),
    ).toBe(
      "SELECT\n    (\n        SELECT\n            1\n    ),\n    (\n        SELECT\n            (\n                SELECT\n                    1\n            )\n    )",
    );
  });
});
