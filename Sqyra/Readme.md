# Sqyra

_For those who like SQL but hate raw strings._

Write SQL with the confidence of TypeScript. Sqyra brings the fluent experience of [SqExpress](../Readme.md) to your queries: readable code, typed columns, and SQL you can inspect before it reaches your database. Start with an existing SQL statement or build a query from scratch, then let Sqyra handle the dialect.

## Contents

- [Quick start](#quick-start)
- [One query, different databases](#one-query-different-databases)
- [1. Basic SELECT](#1-basic-select)
  - [Tables and aliases](#tables-and-aliases)
  - [Scalar functions](#scalar-functions)
  - [Conditional expressions](#conditional-expressions)
- [2. Updating the database](#2-updating-the-database)
  - [Insert](#inserting-data)
  - [Update](#updating-data)
  - [Batch update](#batch-update)
  - [Merge](#merging-data)
  - [Delete](#deleting-data)
  - [Creating tables](#creating-tables)
- [3. Advanced queries](#3-advanced-queries)
  - [Derived tables](#derived-tables)
  - [Set operations](#set-operations)
  - [Common table expressions](#common-table-expressions)
  - [Automatic joins](#automatic-joins-with-a-table-graph)
  - [Aggregates and grouping](#aggregates-and-grouping)
  - [Analytic functions](#analytic-functions)
  - [JSON](#json)
- [Execution and syntax trees](#execution-and-syntax-trees)
  - [Parameters](#parameters)
  - [Working with the AST](#working-with-the-ast)
- [Calling a database from Node.js](#calling-a-database-from-nodejs)

## Quick start

```sh
npm install sqyra
```

### 1. Parse SQL

Have a query already? Parse a SQL Server query and generate PostgreSQL SQL.

```ts
import { parseTSql, toSql } from "sqyra";

const query = parseTSql(`
  SELECT TOP 5
    u.Id,
    u.Name
  FROM dbo.Users AS u
  WHERE u.Id > 10
  ORDER BY u.Name
`);

const sql = toSql(query.ast, {
  dialect: "pgsql",
  formatting: "spacious",
});

console.log(sql);
```

PostgreSQL output:

```sql
SELECT
    "u"."Id",
    "u"."Name"
FROM "dbo"."Users"
    "u"
WHERE
    "u"."Id">10
ORDER BY
    "u"."Name" LIMIT 5
```

The parser reports an error if the SQL is invalid or uses unsupported syntax.

Pass `defaultSchema` when unqualified table names should be bound to a particular schema. Pass `existingTables` when parsed columns should be checked against table definitions already known to your application. Use `tryParseTSql` when invalid input should produce a result you can inspect instead of throwing.

### 2. Build a SELECT

Build your first query:

```ts
import { select } from "sqyra";

const query = select("Hello World!");

const sql = query.toSql("pgsql");

console.log(sql);
```

PostgreSQL output:

```sql
SELECT 'Hello World!'
```

## One query, different databases

The same query can target SQL Server (`tsql`), PostgreSQL (`pgsql`), MySQL (`mysql`), or SQLite (`sqlite`). Choose the dialect when you export: Sqyra adapts identifier quoting, pagination, and supported operations to the database.

You choose the output database whether you start from SQL or build a query in TypeScript. Execute the resulting SQL with your preferred database driver.

The examples below show actual PostgreSQL output. Longer queries use Sqyra’s `spacious` formatting for readability.

## 1. Basic SELECT

### Tables and aliases

`defineTable` creates a **table definition** describing the table and its typed columns. Use the definition directly in a query to reference the table without a SQL alias.

#### Without an alias

Pass the table definition directly to `.from(...)`. The SQL references the table by its name, without declaring an alias.

<!-- prettier-ignore -->
```ts
import { column, defineTable, select, sqlType } from "sqyra";

const usersDefinition = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
  },
});

const query = select(
  usersDefinition.Id,
  usersDefinition.Name,
)
  .from(usersDefinition);

console.log(query.toSql("pgsql"));
```

PostgreSQL output:

```sql
SELECT "Users"."Id","Users"."Name" FROM "public"."Users"
```

#### Explicit aliases

When the same table appears more than once, use its definition as a factory to create separate references. Here, `usersDefinition("employee")` and `usersDefinition("manager")` refer to the same table with different SQL aliases.

<!-- prettier-ignore -->
```ts
import { column, defineTable, nullableColumn, select, sqlType } from "sqyra";

const usersDefinition = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
    ManagerId: nullableColumn(sqlType.int32),
  },
});

const employee = usersDefinition("employee");
const manager = usersDefinition("manager");

const query = select({
  employeeName: employee.Name,
  managerName: manager.Name,
})
  .from(employee)
  .leftJoin(
    manager,
    employee.ManagerId.eq(manager.Id),
  );

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    "employee"."Name" "employeeName",
    "manager"."Name" "managerName"
FROM "public"."Users"
    "employee"
LEFT JOIN "public"."Users"
    "manager" ON
    "employee"."ManagerId"="manager"."Id"
```

The keys in `select({ ... })` become the output column names.

#### Automatic aliases

Call the factory without an argument to let Sqyra assign an alias. Each call creates a separate reference, so the employee and manager remain distinct in the generated SQL.

As in C#, you use the returned object reference to identify an automatically aliased table. Reuse that object wherever you mean the same table occurrence: `employee.Name` and `.from(employee)` share one alias. Calling `usersDefinition()` again creates a different reference with its own alias. Sqyra assigns SQL names such as `A0` and `A1` when it exports the query.

<!-- prettier-ignore -->
```ts
import { column, defineTable, nullableColumn, select, sqlType } from "sqyra";

const usersDefinition = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
    ManagerId: nullableColumn(sqlType.int32),
  },
});

// Each call creates a distinct automatically aliased table reference.
const employee = usersDefinition();
const manager = usersDefinition();

const query = select({
  employeeName: employee.Name,
  managerName: manager.Name,
})
  .from(employee)
  .leftJoin(
    manager,
    employee.ManagerId.eq(manager.Id),
  );

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    "A0"."Name" "employeeName",
    "A1"."Name" "managerName"
FROM "public"."Users"
    "A0"
LEFT JOIN "public"."Users"
    "A1" ON
    "A0"."ManagerId"="A1"."Id"
```

### Scalar functions

Use `call` for a database-specific function. Its result participates in fluent expressions, just like a column. Here, PostgreSQL's `TO_HEX` converts an integer to hexadecimal text.

```ts
import { call, select } from "sqyra";

const query = select({
  hexadecimal: call("TO_HEX", 255),
});

console.log(query.toSql("pgsql"));
```

PostgreSQL output:

```sql
SELECT TO_HEX(255) "hexadecimal"
```

#### Portable scalar functions

Portable functions are first-class builders. Sqyra chooses the SQL implementation for each database. For example, `len(...)` becomes `LEN` in SQL Server and `CHAR_LENGTH` in PostgreSQL.

```ts
import { len, select } from "sqyra";

const query = select({
  length: len("Hello World!"),
});

console.log(query.toSql("pgsql"));
```

PostgreSQL output:

```sql
SELECT CHAR_LENGTH('Hello World!') "length"
```

Portable function builders:

- Numeric: `abs`, `ceiling`, `floor`, `round`.
- Text: `dataLen`, `indexOf`, `left`, `len`, `lower`, `lTrim`, `repeat`, `replace`, `right`, `rTrim`, `substring`, `trim`, `upper`.
- Date and time parts: `day`, `hour`, `minute`, `month`, `second`, `year`.
- Null handling: `nullIf`.

### Conditional expressions

SQL expressions compose fluently. Comparisons, Boolean operators, arithmetic, `IN`, null checks, casts, and `CASE` expressions can be built without leaving the typed query.

```ts
import { caseWhen, column, defineTable, select, sqlType } from "sqyra";

const orders = defineTable({
  schema: "public",
  name: "Orders",
  columns: {
    Amount: column(sqlType.int32),
  },
})("o");

const query = select({
  size: caseWhen(orders.Amount.gte(100)).then("large").else("regular"),
}).from(orders);

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    CASE WHEN "o"."Amount">=100 THEN 'large' ELSE 'regular' END "size"
FROM "public"."Orders"
    "o"
```

## 2. Updating the database

### Inserting data

Insert several rows in one statement. The table descriptor checks the supplied column names and value types.

<!-- prettier-ignore -->
```ts
import { column, defineTable, insertInto, sqlType } from "sqyra";

const users = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
  },
});

const command = insertInto(users)
  .values(
    { Id: 1, Name: "Ada" },
    { Id: 2, Name: "Grace" },
  );

console.log(
  command.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
INSERT INTO "public"."Users"("Id","Name")
VALUES
    (1,'Ada'),
    (2,'Grace')
```

### Updating data

Set new values and use `.where(...)` to choose which rows to update.

```ts
import { column, defineTable, sqlType, update } from "sqyra";

const users = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
  },
});

const command = update(users).set({ Name: "Ada Lovelace" }).where(users.Id.eq(1));

console.log(command.toSql("pgsql"));
```

PostgreSQL output:

```sql
UPDATE "public"."Users" SET "Name"='Ada Lovelace' WHERE "Users"."Id"=1
```

### Batch update

Give each row its own replacement values in a single UPDATE. Build a typed `values` source from your data, join it to the target by its key, and assign from the source columns.

```ts
import { column, defineTable, sqlType, update, values } from "sqyra";

const users = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
  },
})("u");

const changes = values(
  [
    [1, "Ada Lovelace"],
    [2, "Grace Hopper"],
  ],
  "changes",
  {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
  },
);

// Join each target row to its replacement values.
const command = update(users)
  .set({ Name: changes.Name })
  .from(users)
  .innerJoin(changes, users.Id.eq(changes.Id));

console.log(
  command.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
UPDATE "public"."Users" "u"
SET
    "Name"="changes"."Name"
FROM (VALUES (1,'Ada Lovelace'),(2,'Grace Hopper'))"changes"("Id","Name")
WHERE
    "u"."Id"="changes"."Id"
```

### Merging data

`mergeInto` describes insert and update behavior for a typed source. The same command can also express conditional actions and deletes for rows missing from either side.

PostgreSQL gained native `MERGE` relatively recently, in [PostgreSQL 15](https://www.postgresql.org/docs/release/15.0/) (October 2022). Sqyra currently emits a writable-CTE polyfill for PostgreSQL instead of the native statement. The generated SQL below is therefore longer than the equivalent SQL Server `MERGE`, but it also remains usable with PostgreSQL versions from before 15.

```ts
import { column, defineTable, mergeInto, sqlType, values } from "sqyra";

const users = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
  },
})("target");

const incoming = values([[1, "Ada"]], "source", {
  Id: column(sqlType.int32),
  Name: column(sqlType.string(100)),
});

const command = mergeInto(users, incoming)
  .on(users.Id.eq(incoming.Id))
  // Existing rows are updated; new rows are inserted.
  .whenMatchedUpdate({ Name: incoming.Name })
  .whenNotMatchedInsert({
    Id: incoming.Id,
    Name: incoming.Name,
  });

console.log(
  command.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
WITH "__sqexpress_merge_source"("Id","Name") AS(
    VALUES (1,'Ada')
),
"__sqexpress_merge_matched" AS(
    UPDATE "public"."Users" "target"
    SET
        "Name"="source"."Name"
    FROM "__sqexpress_merge_source"
        "source"
    WHERE
        "target"."Id"="source"."Id" RETURNING 1
),
"__sqexpress_merge_not_matched_by_target" AS(
    INSERT INTO "public"."Users"("Id","Name") SELECT "source"."Id","source"."Name"
    FROM "__sqexpress_merge_source"
        "source"
    WHERE
        NOT EXISTS(
        SELECT
            1
        FROM "public"."Users"
            "target"
        WHERE
            "target"."Id"="source"."Id"
    ) RETURNING 1
)
SELECT
    (
        SELECT
            COUNT(*)
        FROM "__sqexpress_merge_matched"
    ),
    (
        SELECT
            COUNT(*)
        FROM "__sqexpress_merge_not_matched_by_target"
    )
```

The same command exported with `dialect: "tsql"` uses SQL Server's native `MERGE`:

```sql
MERGE [public].[Users] [target] USING (VALUES (1,'Ada'))[source]([Id],[Name]) ON
    [target].[Id]=[source].[Id] WHEN MATCHED THEN UPDATE
SET
    [target].[Name]=[source].[Name] WHEN NOT MATCHED THEN INSERT([Id],[Name]) VALUES([source].[Id],[source].[Name]);
```

### Deleting data

```ts
import { column, defineTable, deleteFrom, sqlType } from "sqyra";

const users = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
  },
});

const command = deleteFrom(users).where(users.Id.eq(2));

console.log(command.toSql("pgsql"));
```

PostgreSQL output:

```sql
DELETE FROM "public"."Users" WHERE "Users"."Id"=2
```

### Creating tables

A table definition can also produce schema commands. Column options describe keys, generated identities, defaults, and foreign keys; table options can add indexes. The same definition remains usable in queries.

```ts
import { column, defineTable, sqlType, tableIndex } from "sqyra";

const users = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32, {
      primaryKey: true,
      identity: true,
    }),
    Email: column(sqlType.string(200)),
  },
  indexes: (table) => [tableIndex(table.Email, { unique: true })],
});

console.log(
  users.$script.create().toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
CREATE TABLE "public"."Users"("Id" int4 NOT NULL  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),"Email" character varying(200) NOT NULL,CONSTRAINT "PK_public_Users" PRIMARY KEY ("Id"));CREATE UNIQUE INDEX "IX_public_Users_Email" ON "public"."Users"("Email");
```

The same `$script` object provides `.drop()`, `.dropIfExists()`, and `.dropAndCreate()`.

## 3. Advanced queries

### Derived tables

Call `.as(...)` on a query to use its result as a typed table source. The column definition maps the query output to properties available on the derived table.

```ts
import { column, select, sqlType } from "sqyra";

const numbers = select({ value: 1 })
  .unionAll(select({ value: 2 }))
  .as("numbers", {
    value: column(sqlType.int32),
  });

const query = select(numbers.value).from(numbers).where(numbers.value.gt(1));

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    "numbers"."value"
FROM (
    SELECT
        1 "value"
    UNION ALL
    SELECT
        2 "value"
)"numbers"("value")
WHERE
    "numbers"."value">1
```

### Set operations

Set operations are fluent query methods. Sqyra supports `.union(...)`, `.unionAll(...)`, `.except(...)`, and `.intersect(...)`.

```ts
import { select } from "sqyra";

const query = select({ value: 1 }).unionAll(select({ value: 2 }));

console.log(query.toSql("pgsql"));
```

PostgreSQL output:

```sql
SELECT 1 "value" UNION ALL SELECT 2 "value"
```

### Common table expressions

`cte` gives the common table expression typed columns. Its callback receives the CTE itself, which makes recursive queries natural to express.

```ts
import { column, cte, select, sqlType } from "sqyra";

// Start at 1, then feed each generated row back into the CTE.
const numbers = cte("Numbers", { Num: column(sqlType.int32) }, (self) =>
  select({ Num: 1 }).unionAll(
    select({ Num: self.Num.add(1) })
      .from(self)
      .where(self.Num.lt(3)),
  ),
);

const query = select(numbers.Num).from(numbers);

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
WITH RECURSIVE "Numbers" AS(
    SELECT
        1 "Num"
    UNION ALL
    SELECT
        "Numbers"."Num"+1 "Num"
    FROM "Numbers"
    WHERE
        "Numbers"."Num"<3
)
SELECT
    "Numbers"."Num"
FROM "Numbers"
```

### Automatic joins with a table graph

`tablesGraph` builds a map of the foreign-key relationships between your table definitions. Ask it to connect two tables and it finds the shortest path, adds any tables required between them, and builds every `JOIN ... ON` condition.

In this example, an order belongs to a customer and a customer belongs to a user. The query requests only orders and users. `TablesGraph` discovers that `Customers` is the connector.

<!-- prettier-ignore -->
```ts
import { column, defineTable, select, sqlType, tablesGraph } from "sqyra";

const usersDefinition = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32, { primaryKey: true }),
    Name: column(sqlType.string(100)),
  },
});

const customersDefinition = defineTable({
  schema: "public",
  name: "Customers",
  columns: {
    Id: column(sqlType.int32, { primaryKey: true }),
    UserId: column(sqlType.int32, { references: usersDefinition.Id }),
  },
});

const ordersDefinition = defineTable({
  schema: "public",
  name: "Orders",
  columns: {
    Id: column(sqlType.int32, { primaryKey: true }),
    CustomerId: column(sqlType.int32, { references: customersDefinition.Id }),
  },
});

const graph = tablesGraph([
  usersDefinition,
  customersDefinition,
  ordersDefinition,
]);

const orders = ordersDefinition("orders");
const users = usersDefinition("users");

const query = select({
  orderId: orders.Id,
  userName: users.Name,
})
  // Customers is inserted automatically as the connector table.
  .from(graph.toJoinTables(orders, users));

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    "orders"."Id" "orderId",
    "users"."Name" "userName"
FROM "public"."Orders"
    "orders"
JOIN "public"."Customers"
    "A0" ON
    "orders"."CustomerId"="A0"."Id"
JOIN "public"."Users"
    "users" ON
    "A0"."UserId"="users"."Id"
```

The endpoint objects keep the aliases you assigned. Connector tables receive automatic aliases, so their generated names cannot collide with the endpoints.

You can also pass several endpoint tables to `graph.toJoinTables([orders, users, anotherTable])`. The graph connects them into one join tree. An optional ordered list of intermediate tables can force a path through particular tables.

`getReferences`, `getReferencedBy`, `getAllReferences`, and `getAllReferencedBy` let you navigate the graph. If several equally short paths exist, the join options can choose the first path, reject the ambiguity, or select a path with a callback. `tryToJoinTables` returns `null` when no valid path exists; `toJoinTables` throws an error.

### Aggregates and grouping

Aggregate expressions work directly in `select`, while `.groupBy(...)` keeps the grouping columns explicit.

```ts
import { aggregate, column, defineTable, select, sqlType } from "sqyra";

const orders = defineTable({
  schema: "public",
  name: "Orders",
  columns: {
    CustomerId: column(sqlType.int32),
    Amount: column(sqlType.decimal(12, 2)),
  },
})("o");

const query = select({
  customerId: orders.CustomerId,
  total: aggregate("SUM", orders.Amount),
})
  .from(orders)
  .groupBy(orders.CustomerId);

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    "o"."CustomerId" "customerId",
    SUM("o"."Amount") "total"
FROM "public"."Orders"
    "o"
GROUP BY
    "o"."CustomerId"
```

Use `stringAgg(value, separator).orderBy(...)` for ordered text aggregation.

### Analytic functions

Window functions calculate over related rows without collapsing them into groups. Here, each order keeps its own amount alongside the total for its customer.

<!-- prettier-ignore -->
```ts
import { aggregate, column, defineTable, select, sqlType } from "sqyra";

const orders = defineTable({
  schema: "public",
  name: "Orders",
  columns: {
    Id: column(sqlType.int32),
    CustomerId: column(sqlType.int32),
    Amount: column(sqlType.int32),
  },
})("o");

const query = select({
  orderId: orders.Id,
  amount: orders.Amount,
  customerTotal: aggregate("SUM", orders.Amount)
    .over()
    .partitionBy(orders.CustomerId),
})
  .from(orders)
  .orderBy(orders.Id.asc());

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    "o"."Id" "orderId",
    "o"."Amount" "amount",
    SUM("o"."Amount")OVER(PARTITION BY "o"."CustomerId") "customerTotal"
FROM "public"."Orders"
    "o"
ORDER BY
    "o"."Id"
```

Add `.orderBy(...)` to the window when its calculation depends on row order.

### JSON

Sqyra’s JSON model builds on Microsoft SQL Server’s approach: select rows and shape their JSON output with `FOR JSON PATH`. The fluent equivalent is `.forJson()`. Sqyra translates this model to the other databases’ JSON facilities, so you can keep the same query structure across dialects.

```ts
import { select } from "sqyra";

const query = select({
  id: 1,
  name: "Ada",
}).forJson();

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    COALESCE(jsonb_agg(jsonb_build_object('id',J0."id",'name',J0."name")),'[]'::jsonb) Json
FROM (
    SELECT
        1 "id",
        'Ada' "name"
) J0
```

`.forJson()` returns an array. Pass `{ withoutArrayWrapper: true }` when one object is expected, and `{ includeNullValues: true }` when SQL `NULL` properties must remain in the result.

#### Nested output paths

Use `selectJson` with `jsonOutput` when a flat SQL row should become a nested JSON object. Each JSON path describes where its value belongs.

```ts
import { jsonOutput, selectJson } from "sqyra";

// Paths describe the desired JSON shape, not SQL column names.
const query = selectJson(
  jsonOutput(101, "$.id"),
  jsonOutput("The Hobbit", "$.book.title"),
  jsonOutput("J.R.R. Tolkien", "$.book.author"),
).forJson({ withoutArrayWrapper: true });

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    (
        SELECT
            jsonb_build_object('id',J0."id",'book',jsonb_build_object('title',J0."__sq_json_0",'author',J0."__sq_json_1"))
        FROM (
            SELECT
                101 "id",
                'The Hobbit' "__sq_json_0",
                'J.R.R. Tolkien' "__sq_json_1"
        ) J0
    ) Json
```

To embed one JSON query inside another, call `.forJson().scalarSubquery()` on the inner query and pass it to `jsonOutput`. Sqyra keeps the value as structured JSON instead of escaping it as text.

#### Read a scalar value

`.jsonValue(path)` extracts one scalar. Pass a SQL type when the result should be converted instead of returned as text.

```ts
import { exprTypeInt32, lit, select } from "sqyra";

const document = lit('{"book":{"pages":310}}');

const query = select({
  pages: document.jsonValue("$.book.pages", exprTypeInt32),
});

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    CASE WHEN jsonb_typeof(jsonb_path_query_first(CAST('{"book":{"pages":310}}' AS jsonb),'$.book.pages'))='number' AND (jsonb_path_query_first(CAST('{"book":{"pages":310}}' AS jsonb),'$.book.pages')#>>'{}')~'^-?[0-9]+$' THEN CAST(jsonb_path_query_first(CAST('{"book":{"pages":310}}' AS jsonb),'$.book.pages') #>> '{}' AS int4) END "pages"
```

#### Read a JSON fragment

`.jsonQuery(path)` extracts an object or array and marks the result as JSON. The path defaults to `$`, which marks the complete value as a JSON fragment.

```ts
import { lit, select } from "sqyra";

const document = lit('{"book":{"title":"The Hobbit"}}');

const query = select({
  book: document.jsonQuery("$.book"),
});

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    CASE WHEN jsonb_typeof(jsonb_path_query_first(CAST('{"book":{"title":"The Hobbit"}}' AS jsonb),'$.book')) IN ('object','array') THEN jsonb_path_query_first(CAST('{"book":{"title":"The Hobbit"}}' AS jsonb),'$.book') END "book"
```

#### Change a JSON document

`.jsonSet(path, value)` adds or replaces a value. `.jsonRemove(path)` removes a property or array item. Both return expressions, so modifications can be chained.

```ts
import { lit, select } from "sqyra";

const document = lit('{"name":"Ada","active":false}');

const query = select({
  profile: document.jsonSet("$.active", true).jsonRemove("$.name"),
});

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    CAST(jsonb_set(CAST('{"name":"Ada","active":false}' AS jsonb),string_to_array(trim(leading '$.' from '$.active'),'.'),to_jsonb(TRUE),true) AS jsonb)#-string_to_array(trim(leading '$.' from '$.name'),'.') "profile"
```

#### Build arrays and objects

`jsonArray` and `jsonObject` construct JSON values from SQL expressions. JavaScript `null` becomes a JSON null value.

```ts
import { jsonArray, jsonObject, select } from "sqyra";

const query = select({
  tags: jsonArray("database", "typescript"),
  author: jsonObject({
    name: "Ada",
    active: true,
  }),
});

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    jsonb_build_array('database','typescript') "tags",
    jsonb_build_object('name','Ada','active',TRUE) "author"
```

#### Expand a JSON array into rows

`jsonTable(document, path)` turns an array into a typed table source. Add scalar columns with `.value`, JSON fragments with `.query`, and an array index with `.ordinal`, then assign the source an alias with `.as`.

```ts
import { exprTypeInt32, exprTypeString, jsonTable, select } from "sqyra";

const books = jsonTable(
  '{"books":[{"id":1,"title":"The Hobbit"},{"id":2,"title":"1984"}]}',
  "$.books",
)
  // Declare the typed columns exposed by each array element.
  .value("Id", "$.id", exprTypeInt32)
  .value("Title", "$.title", exprTypeString({ size: 100, isUnicode: true, isText: false }))
  .ordinal("Position")
  .as("book");

const query = select(books.Id, books.Position).from(books);

console.log(
  query.toSql({
    dialect: "pgsql",
    formatting: "spacious",
  }),
);
```

PostgreSQL output:

```sql
SELECT
    "book"."Id",
    "book"."Position"
FROM (
    SELECT
        CAST(jsonb_path_query_first(J.value,'$.id')#>>'{}' AS int4) "Id",
        CAST(jsonb_path_query_first(J.value,'$.title')#>>'{}' AS character varying) "Title",
        J.ordinal-1 "Position"
    FROM jsonb_array_elements(jsonb_path_query_first(CAST('{"books":[{"id":1,"title":"The Hobbit"},{"id":2,"title":"1984"}]}' AS jsonb),'$.books')) WITH ORDINALITY J(value,ordinal)
)
    "book"
```

Sqyra validates portable JSON paths when the expression is built. Paths start with `$` and support object members and zero-based array indexes. Recursive descent, wildcards, negative indexes, and slices are rejected because they do not have consistent behavior across all supported databases.

## Execution and syntax trees

### Parameters

Export a command with parameters for your database driver. Sqyra returns the SQL and its parameter values together.

```ts
import { select } from "sqyra";

const query = select({
  name: "Ada",
});

const command = query.toSql({
  dialect: "pgsql",
  parameterize: true,
});

console.log(command.sql);
```

PostgreSQL output:

```sql
SELECT $1 "name"
```

The SQL contains the placeholder `$1`; its value, `"Ada"`, is in `command.parameters`.

Sqyra also supports named parameters and dialect-specific parameter styles. Keep parameterization enabled for application data; use literal SQL output mainly for inspection, migrations, and tests.

### Working with the AST

Every parsed or built command is an immutable syntax tree. This makes it possible to inspect, transform, store, and restore queries before exporting them.

```ts
import { lit, select } from "sqyra";

const query = select({ answer: 1 });

// Lazily enumerate only matching node types; the predicate receives a typed node.
const positiveIntegers = query.$ast.descendantsOfType(
  "ExprInt32Literal",
  (node) => node.value !== null && node.value > 0,
);

// Immutable modification returns a new query tree.
const changed = query.$ast.modify((node) => (node.kind === "ExprInt32Literal" ? lit(42) : node));

console.log(changed.toSql("pgsql"));
```

PostgreSQL output:

```sql
SELECT 42 "answer"
```

Use `query.$ast.walk()` to visit nodes, `query.$ast.find(...)` to locate a node, and `query.$ast.modify(...)` to return a transformed query with its inferred row type intact. `$ast.descendantsOfType(kind, predicate?)` lazily returns only the requested node type, while `$ast.descendants()`, `$ast.walkWithParent()`, and `$ast.serialize()` provide the remaining traversal and serialization operations. The same `$ast` namespace is available on fluent expressions and data-modification statements.

Standalone `walk`, `descendantsOfType`, `find`, `modify`, and `serializeAst` remain available for raw AST nodes. Use `deserializeAst(json)` to restore a serialized tree.

## Calling a database from Node.js

Sqyra builds SQL; it does not own the database connection. Export a parameterized command, then give its SQL and values to your preferred Node.js driver.

### PostgreSQL with `pg`

Install Sqyra and the driver:

```sh
npm install sqyra pg
```

```js
import { Pool } from "pg";
import { column, defineTable, select, sqlType } from "sqyra";

const users = defineTable({
  schema: "public",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: column(sqlType.string(100)),
  },
});

const query = select({
  id: users.Id,
  name: users.Name,
})
  .from(users)
  .where(users.Id.eq(42));

const command = query.toSql({
  dialect: "pgsql",
  parameterize: true,
});

const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
});

try {
  const result = await pool.query({
    text: command.sql,
    values: command.parameters.map((parameter) => parameter.value),
  });

  console.log(result.rows);
} finally {
  await pool.end();
}
```

`command.sql` contains PostgreSQL placeholders such as `$1`. `command.parameters` preserves each value together with its Sqyra AST type, which is useful when a driver needs explicit type binding.

### Other Node.js drivers

The integration tests use the same parameterized-command contract with `mysql2`, `better-sqlite3`, and `mssql`.

#### MySQL with `mysql2`

MySQL uses ordered `?` placeholders:

```js
const command = query.toSql({
  dialect: "mysql",
  parameterize: true,
});

const values = command.parameters.map((parameter) => parameter.value);
const [rows] = await mysqlPool.query(command.sql, values);
```

#### SQLite with `better-sqlite3`

SQLite also uses ordered `?` placeholders:

```js
const command = query.toSql({
  dialect: "sqlite",
  parameterize: true,
});

const values = command.parameters.map((parameter) => parameter.value);
const rows = sqlite.prepare(command.sql).all(...values);
```

Use `.run(...values)` instead of `.all(...values)` for a command that does not return rows.

#### SQL Server with `mssql`

SQL Server uses named parameters. The names in `command.parameters` match the `@name` placeholders generated in `command.sql`:

```js
const command = query.toSql({
  dialect: "tsql",
  parameterize: true,
});

const request = sqlServerPool.request();

for (const parameter of command.parameters) {
  request.input(parameter.name, parameter.value);
}

const result = await request.query(command.sql);
```

For exact decimals, 64-bit integers, binary values, and temporal values, map `parameter.type` to an explicit driver type at this boundary. This is the approach used by Sqyra's integration tests.
