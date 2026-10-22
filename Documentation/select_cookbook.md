# SELECT query cookbook

Build SELECT queries with SqExpress, from small projections to multi-stage reports. Each recipe gives a goal, C# construction, actual spacious T-SQL output, and a composition note. PostgreSQL output is included when it illustrates a useful dialect difference.

All 30 recipes and their 39 query variants were compiled against the repository source and exported to **both T-SQL and PostgreSQL**. The SQL blocks below are captured exporter output using `SqlFormattingProfile.Spacious`; they have not been manually reformatted. Verification covers compilation and export, **not execution against a database**. See [Reproduce the verification](#reproduce-the-verification) to rebuild every C# example directly from this document and compare every published SQL block.

## Contents

- [Setup: descriptors, imports, and SQL export](#setup-descriptors-imports-and-sql-export)
- [1. Select columns, expressions, and aliases](#1-select-columns-expressions-and-aliases)
- [2. Combine filters without losing parentheses](#2-combine-filters-without-losing-parentheses)
- [3. Handle NULL with COALESCE and CASE](#3-handle-null-with-coalesce-and-case)
- [4. Filter by lists, patterns, and date ranges](#4-filter-by-lists-patterns-and-date-ranges)
- [5. Select distinct values and the first N rows](#5-select-distinct-values-and-the-first-n-rows)
- [6. Page through an ordered result](#6-page-through-an-ordered-result)
- [7. Continue from a keyset cursor](#7-continue-from-a-keyset-cursor)
- [8. Build optional filters dynamically](#8-build-optional-filters-dynamically)
- [9. Join related tables without ambiguous columns](#9-join-related-tables-without-ambiguous-columns)
- [10. Preserve unmatched rows with LEFT JOIN](#10-preserve-unmatched-rows-with-left-join)
- [11. Join a table to itself](#11-join-a-table-to-itself)
- [12. Find matching and missing relationships with EXISTS](#12-find-matching-and-missing-relationships-with-exists)
- [13. Project a correlated scalar subquery](#13-project-a-correlated-scalar-subquery)
- [14. Group rows and calculate aggregates](#14-group-rows-and-calculate-aggregates)
- [15. Filter aggregate results through a derived table](#15-filter-aggregate-results-through-a-derived-table)
- [16. Calculate conditional totals and safe ratios](#16-calculate-conditional-totals-and-safe-ratios)
- [17. Aggregate ordered text](#17-aggregate-ordered-text)
- [18. Join an inline VALUES table](#18-join-an-inline-values-table)
- [19. Combine results with set operators](#19-combine-results-with-set-operators)
- [20. Organize a query with a reusable CTE](#20-organize-a-query-with-a-reusable-cte)
- [21. Traverse a hierarchy with a recursive CTE](#21-traverse-a-hierarchy-with-a-recursive-cte)
- [22. Find the latest related row with APPLY](#22-find-the-latest-related-row-with-apply)
- [23. Keep detail rows alongside partition totals](#23-keep-detail-rows-alongside-partition-totals)
- [24. Compose COALESCE around a windowed MAX](#24-compose-coalesce-around-a-windowed-max)
- [25. Calculate running totals and moving averages](#25-calculate-running-totals-and-moving-averages)
- [26. Rank rows and select the top N per group](#26-rank-rows-and-select-the-top-n-per-group)
- [27. Compare a row with its previous and next rows](#27-compare-a-row-with-its-previous-and-next-rows)
- [28. Get the actual first and last values in a partition](#28-get-the-actual-first-and-last-values-in-a-partition)
- [29. Calculate each grouped total’s share of the whole](#29-calculate-each-grouped-totals-share-of-the-whole)
- [30. Build a paged customer report from multiple query stages](#30-build-a-paged-customer-report-from-multiple-query-stages)
- [Composition rules and troubleshooting](#composition-rules-and-troubleshooting)
- [Reproduce the verification](#reproduce-the-verification)

## Setup: descriptors, imports, and SQL export

Use a .NET 8 console project referencing `SqExpress/SqExpress.csproj` and `SqExpress.Analyzers/SqExpress.Analyzers.csproj`. For the analyzer project reference, set `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` so the table source generator runs during compilation. The verification script below creates both references using absolute paths to your checkout. These examples target the APIs in this checkout. Copy the imports to the beginning of `Program.cs`, put the recipe statements in the top-level statement section, and place supporting classes after all top-level statements. Each recipe is independent with the shared setup; recipes 20 and 30 also need `CustomerTotals`, and recipe 21 needs `EmployeeTree`, defined alongside their recipes. To paste several recipes into one file, give each its own local function or block so local variable names do not collide.

### Imports

<!-- cookbook:imports -->
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using SqExpress;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.TableDeclarationAttributes;
using static SqExpress.SqQueryBuilder;
using static CookbookOutput;
```

### Shared table descriptors

These attribute-based declarations use the built-in source generator to produce `TableBase` implementations, typed column properties, and constructors accepting `Alias`. Query code such as `new Orders("o")` works with these generated constructors. No handwritten constructors or column properties are needed. See the [Table Description Reference](table_description.md) for the declaration attributes. The CTE definitions in recipes 20 and 21 retain their handwritten query logic.

The illustrative schema uses unique customer, order, and employee IDs, plus unique `(OrderId, LineNo)` pairs for order lines. `Orders.CustomerId` refers to a customer, `OrderLines.OrderId` to an order, and nullable `Employees.ManagerId` to an employee. `Orders.Amount` and `Customers.Region` can be NULL; `OrderedAt` and all ordering IDs are non-null. The descriptors declare column shapes, not database constraints or sample data.

Both exporters retain the explicit `dbo` schema. To execute the PostgreSQL forms, provide the corresponding quoted schema/table/column names; the exporter does not substitute PostgreSQL’s `public` schema. No database or DDL is needed to compile and export these examples.

<!-- cookbook:types -->
```csharp
[TableDescriptor("dbo", "Customers")]
[Int32Column("CustomerId")]
[StringColumn("Name", MaxLength = 100)]
[NullableStringColumn("Region", MaxLength = 50)]
public sealed partial class Customers
{
}

[TableDescriptor("dbo", "Orders")]
[Int32Column("OrderId")]
[Int32Column("CustomerId")]
[DateTimeColumn("OrderedAt")]
[NullableDecimalColumn("Amount", Precision = 18, Scale = 2)]
[StringColumn("Status", MaxLength = 20)]
public sealed partial class Orders
{
}

[TableDescriptor("dbo", "OrderLines")]
[Int32Column("OrderId")]
[Int32Column("LineNo")]
[Int32Column("Quantity")]
public sealed partial class OrderLines
{
}

[TableDescriptor("dbo", "Employees")]
[Int32Column("EmployeeId")]
[NullableInt32Column("ManagerId")]
[StringColumn("Name", MaxLength = 100)]
public sealed partial class Employees
{
}
```

### SQL export helper

`Emit` writes both dialects to a `sql` directory under the console process’s working directory. Its example ID connects each C# query to the SQL block labels below. Explicit aliases and fixed inputs make output reproducible. `WithFormatting` returns an exporter configured for spacious formatting; it does not change the query.

<!-- cookbook:types -->
```csharp
public static class CookbookOutput
{
    private static readonly HashSet<string> Ids = new();

    public static void Emit(string id, IExpr query)
    {
        if (!Ids.Add(id))
            throw new InvalidOperationException("Duplicate example: " + id);

        Directory.CreateDirectory("sql");

        File.WriteAllText(
            Path.Combine("sql", id + ".tsql.sql"),
            query.ToSql(TSqlExporter.Default.WithFormatting(SqlFormattingProfile.Spacious)));

        File.WriteAllText(
            Path.Combine("sql", id + ".pgsql.sql"),
            query.ToSql(PgSqlExporter.Default.WithFormatting(SqlFormattingProfile.Spacious)));

        Console.WriteLine("Exported " + id);
    }
}
```

The examples use literal inputs so the emitted SQL is self-contained. For application execution, see [parameterization modes](../Readme.md#parametrization-modes). For selecting and expanding JSON, see [Cross-Dialect JSON](cross_dialect_json.md).

## 1. Select columns, expressions, and aliases

Project customer fields and a calculated display label; also select a literal without a table.

SqExpress C#:

<!-- cookbook:recipe:01 -->
```csharp
var c = new Customers("c");

var query = Select(
        c.CustomerId,
        (c.Name + " / " + Coalesce(c.Region, "unknown")).As("Display"))
    .From(c)
    .Done();

Emit("01", query);
Emit("01-literal", Select("Hello from SqExpress").Done());
```

T-SQL — Literal without FROM:

<!-- cookbook:sql:01-literal:tsql -->
```sql
SELECT
    'Hello from SqExpress'
```

PostgreSQL — Literal without FROM:

<!-- cookbook:sql:01-literal:pgsql -->
```sql
SELECT
    'Hello from SqExpress'
```

T-SQL — Customer display:

<!-- cookbook:sql:01:tsql -->
```sql
SELECT
    [c].[CustomerId],
    [c].[Name]+' / '+COALESCE([c].[Region],'unknown') [Display]
FROM [dbo].[Customers]
    [c]
```

PostgreSQL — Customer display:

<!-- cookbook:sql:01:pgsql -->
```sql
SELECT
    "c"."CustomerId",
    "c"."Name"||' / '||COALESCE("c"."Region",'unknown') "Display"
FROM "dbo"."Customers"
    "c"
```

`.As(...)` names an output column. `.Done()` builds the syntax tree; it does not execute a database command.

## 2. Combine filters without losing parentheses

Select paid or shipped orders above a threshold, excluding a customer.

SqExpress C#:

<!-- cookbook:recipe:02 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.OrderId,
        o.Amount)
    .From(o)
    .Where(
        ((o.Status == "Paid") | (o.Status == "Shipped"))
        & (o.Amount >= 100m)
        & !(o.CustomerId == 7))
    .Done();

Emit("02", query);
```

T-SQL:

<!-- cookbook:sql:02:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Amount]
FROM [dbo].[Orders]
    [o]
WHERE
    (
        [o].[Status]='Paid'
        OR
        [o].[Status]='Shipped'
    )
    AND
    [o].[Amount]>=100
    AND
    NOT [o].[CustomerId]=7
```

Use `&`, `|`, and `!` for SQL predicates. Parenthesize comparisons explicitly; the exporter preserves the Boolean expression tree.

## 3. Handle NULL with COALESCE and CASE

Replace a missing amount and classify each order.

SqExpress C#:

<!-- cookbook:recipe:03 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.OrderId,
        Coalesce(o.Amount, 0m).As("AmountOrZero"),
        Case()
            .When(IsNull(o.Amount)).Then("Unknown")
            .When(o.Amount >= 100m).Then("Large")
            .Else("Small")
            .As("Size"))
    .From(o)
    .Done();

Emit("03", query);
```

T-SQL:

<!-- cookbook:sql:03:tsql -->
```sql
SELECT
    [o].[OrderId],
    COALESCE([o].[Amount],0) [AmountOrZero],
    CASE WHEN [o].[Amount] IS NULL THEN 'Unknown' WHEN [o].[Amount]>=100 THEN 'Large' ELSE 'Small' END [Size]
FROM [dbo].[Orders]
    [o]
```

Use `IsNull(value)` / `IsNotNull(value)` for SQL null checks. COALESCE replaces NULL, not zero or an empty string. CASE evaluates conditions in order.

## 4. Filter by lists, patterns, and date ranges

Combine an ID list and a pattern with an inclusive start and exclusive end date.

SqExpress C#:

<!-- cookbook:recipe:04 -->
```csharp
var o = new Orders("o");
int[] customerIds = { 1, 2, 3 };
var start = new DateTime(2026, 1, 1);
var end = new DateTime(2026, 2, 1);

ExprBoolean ids = customerIds.Length == 0
    ? Literal(1) == Literal(0)
    : o.CustomerId.In(customerIds);

var query = Select(o.OrderId)
    .From(o)
    .Where(
        ids
        & o.Status.Like("Paid%")
        & (o.OrderedAt >= start)
        & (o.OrderedAt < end))
    .Done();

Emit("04", query);

int[] emptyIds = Array.Empty<int>();
ExprBoolean emptyFilter = emptyIds.Length == 0
    ? Literal(1) == Literal(0)
    : o.CustomerId.In(emptyIds);

Emit(
    "04-empty",
    Select(o.OrderId)
        .From(o)
        .Where(emptyFilter)
        .Done());
```

T-SQL — Empty ID list:

<!-- cookbook:sql:04-empty:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    1=0
```

PostgreSQL — Empty ID list:

<!-- cookbook:sql:04-empty:pgsql -->
```sql
SELECT
    "o"."OrderId"
FROM "dbo"."Orders"
    "o"
WHERE
    1=0
```

T-SQL — Non-empty ID list:

<!-- cookbook:sql:04:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[CustomerId] IN(1,2,3)
    AND
    [o].[Status] LIKE 'Paid%'
    AND
    [o].[OrderedAt]>='2026-01-01'
    AND
    [o].[OrderedAt]<'2026-02-01'
```

PostgreSQL — Non-empty ID list:

<!-- cookbook:sql:04:pgsql -->
```sql
SELECT
    "o"."OrderId"
FROM "dbo"."Orders"
    "o"
WHERE
    "o"."CustomerId" IN(1,2,3)
    AND
    "o"."Status" LIKE 'Paid%'
    AND
    "o"."OrderedAt">='2026-01-01'::timestamp
    AND
    "o"."OrderedAt"<'2026-02-01'::timestamp
```

An empty requested-ID list deliberately matches no rows. The half-open interval includes every time on the last day without guessing timestamp precision. LIKE wildcards are intentional; escape user-entered wildcards for a literal search.

## 5. Select distinct values and the first N rows

List distinct statuses and retrieve the five newest orders.

SqExpress C#:

<!-- cookbook:recipe:05 -->
```csharp
var o = new Orders("o");

Emit(
    "05-distinct",
    SelectDistinct(o.Status)
        .From(o)
        .Done());

var query = SelectTop(
        5,
        o.OrderId,
        o.OrderedAt)
    .From(o)
    .OrderBy(
        Desc(o.OrderedAt),
        Desc(o.OrderId))
    .Done();

Emit("05", query);
```

T-SQL — Distinct statuses:

<!-- cookbook:sql:05-distinct:tsql -->
```sql
SELECT DISTINCT
    [o].[Status]
FROM [dbo].[Orders]
    [o]
```

PostgreSQL — Distinct statuses:

<!-- cookbook:sql:05-distinct:pgsql -->
```sql
SELECT DISTINCT
    "o"."Status"
FROM "dbo"."Orders"
    "o"
```

T-SQL — First five orders:

<!-- cookbook:sql:05:tsql -->
```sql
SELECT TOP 5
    [o].[OrderId],
    [o].[OrderedAt]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[OrderedAt] DESC,
    [o].[OrderId] DESC
```

PostgreSQL — First five orders:

<!-- cookbook:sql:05:pgsql -->
```sql
SELECT
    "o"."OrderId",
    "o"."OrderedAt"
FROM "dbo"."Orders"
    "o"
ORDER BY
    "o"."OrderedAt" DESC,
    "o"."OrderId" DESC
LIMIT 5
```

The unique OrderId breaks timestamp ties. TOP without ORDER BY does not define which rows are returned.

## 6. Page through an ordered result

Skip twenty rows and return the next ten.

SqExpress C#:

<!-- cookbook:recipe:06 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.OrderId,
        o.OrderedAt)
    .From(o)
    .OrderBy(
        Desc(o.OrderedAt),
        Desc(o.OrderId))
    .OffsetFetch(20, 10)
    .Done();

Emit("06", query);
```

T-SQL:

<!-- cookbook:sql:06:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[OrderedAt]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[OrderedAt] DESC,
    [o].[OrderId] DESC
OFFSET 20 ROW
FETCH NEXT 10 ROW ONLY
```

PostgreSQL:

<!-- cookbook:sql:06:pgsql -->
```sql
SELECT
    "o"."OrderId",
    "o"."OrderedAt"
FROM "dbo"."Orders"
    "o"
ORDER BY
    "o"."OrderedAt" DESC,
    "o"."OrderId" DESC
OFFSET 20 ROW
FETCH NEXT 10 ROW ONLY
```

Use the same complete ordering on every page. Concurrent inserts or deletes can shift offset pages; use keyset continuation when that is unsuitable.

PostgreSQL accepts the SQL-standard OFFSET/FETCH form emitted here; a separate LIMIT rewrite is unnecessary.

## 7. Continue from a keyset cursor

Fetch the next ten orders after a date-and-ID cursor in descending order.

SqExpress C#:

<!-- cookbook:recipe:07 -->
```csharp
var o = new Orders("o");
var cursorTime = new DateTime(2026, 1, 15, 12, 0, 0);
var cursorId = 120;

var query = SelectTop(
        10,
        o.OrderId,
        o.OrderedAt)
    .From(o)
    .Where(
        (o.OrderedAt < cursorTime)
        | ((o.OrderedAt == cursorTime) & (o.OrderId < cursorId)))
    .OrderBy(
        Desc(o.OrderedAt),
        Desc(o.OrderId))
    .Done();

Emit("07", query);
```

T-SQL:

<!-- cookbook:sql:07:tsql -->
```sql
SELECT TOP 10
    [o].[OrderId],
    [o].[OrderedAt]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[OrderedAt]<'2026-01-15T12:00:00.000'
    OR
    [o].[OrderedAt]='2026-01-15T12:00:00.000'
    AND
    [o].[OrderId]<120
ORDER BY
    [o].[OrderedAt] DESC,
    [o].[OrderId] DESC
```

PostgreSQL:

<!-- cookbook:sql:07:pgsql -->
```sql
SELECT
    "o"."OrderId",
    "o"."OrderedAt"
FROM "dbo"."Orders"
    "o"
WHERE
    "o"."OrderedAt"<'2026-01-15T12:00:00.000'::timestamp
    OR
    "o"."OrderedAt"='2026-01-15T12:00:00.000'::timestamp
    AND
    "o"."OrderId"<120
ORDER BY
    "o"."OrderedAt" DESC,
    "o"."OrderId" DESC
LIMIT 10
```

Both cursor columns are non-null. The predicate follows the lexicographic ORDER BY; reverse comparisons for ascending traversal. Keyset continuation does not provide arbitrary page-number access.

## 8. Build optional filters dynamically

Apply only supplied customer and status filters using expression composition.

SqExpress C#:

<!-- cookbook:recipe:08 -->
```csharp
var o = new Orders("o");

foreach (var input in new (string Id, int? CustomerId, string? Status)[]
    {
        ("08-none", null, null),
        ("08-filtered", 2, "Paid")
    })
{
    var predicates = new List<ExprBoolean>();

    if (input.CustomerId is int customerId)
        predicates.Add(o.CustomerId == customerId);

    if (input.Status is string status)
        predicates.Add(o.Status == status);

    ExprBoolean? filter = predicates.Count == 0
        ? null
        : predicates.JoinAsAnd();

    var query = Select(
            o.OrderId,
            o.Status)
        .From(o)
        .Where(filter)
        .Done();

    Emit(input.Id, query);
}
```

T-SQL — Customer and status filters:

<!-- cookbook:sql:08-filtered:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Status]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[CustomerId]=2
    AND
    [o].[Status]='Paid'
```

T-SQL — No optional filters:

<!-- cookbook:sql:08-none:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Status]
FROM [dbo].[Orders]
    [o]
```

`.Where(null)` omits WHERE. Keep user data in value expressions rather than concatenating SQL syntax. These exports use literals; production parameterization is a separate execution concern.

## 9. Join related tables without ambiguous columns

Return one row per order line, including customer and order fields.

SqExpress C#:

<!-- cookbook:recipe:09 -->
```csharp
var c = new Customers("c");
var o = new Orders("o");
var l = new OrderLines("l");

var query = Select(
        c.Name,
        o.OrderId,
        l.LineNo,
        l.Quantity)
    .From(c)
    .InnerJoin(o, on: o.CustomerId == c.CustomerId)
    .InnerJoin(l, on: l.OrderId == o.OrderId)
    .Done();

Emit("09", query);
```

T-SQL:

<!-- cookbook:sql:09:tsql -->
```sql
SELECT
    [c].[Name],
    [o].[OrderId],
    [l].[LineNo],
    [l].[Quantity]
FROM [dbo].[Customers]
    [c]
JOIN [dbo].[Orders]
    [o] ON
    [o].[CustomerId]=[c].[CustomerId]
JOIN [dbo].[OrderLines]
    [l] ON
    [l].[OrderId]=[o].[OrderId]
```

The result grain is an order line. Summing an order-level Amount here would count it once per line; aggregate at the intended grain first.

## 10. Preserve unmatched rows with LEFT JOIN

Compare a right-side status filter in ON with the same filter in WHERE.

SqExpress C#:

<!-- cookbook:recipe:10 -->
```csharp
var c = new Customers("c");
var o = new Orders("o");

var preserved = Select(
        c.CustomerId,
        o.OrderId)
    .From(c)
    .LeftJoin(
        o,
        on: (o.CustomerId == c.CustomerId) & (o.Status == "Paid"))
    .Done();

Emit("10-on", preserved);

var filtered = Select(
        c.CustomerId,
        o.OrderId)
    .From(c)
    .LeftJoin(o, on: o.CustomerId == c.CustomerId)
    .Where(o.Status == "Paid")
    .Done();

Emit("10-where", filtered);
```

T-SQL — Predicate in ON:

<!-- cookbook:sql:10-on:tsql -->
```sql
SELECT
    [c].[CustomerId],
    [o].[OrderId]
FROM [dbo].[Customers]
    [c]
LEFT JOIN [dbo].[Orders]
    [o] ON
    [o].[CustomerId]=[c].[CustomerId]
    AND
    [o].[Status]='Paid'
```

T-SQL — Predicate in WHERE:

<!-- cookbook:sql:10-where:tsql -->
```sql
SELECT
    [c].[CustomerId],
    [o].[OrderId]
FROM [dbo].[Customers]
    [c]
LEFT JOIN [dbo].[Orders]
    [o] ON
    [o].[CustomerId]=[c].[CustomerId]
WHERE
    [o].[Status]='Paid'
```

ON preserves customers with no paid orders; WHERE removes their null-extended rows. Right-side outputs can be NULL after a left join even when the base descriptor declares a non-null column.

## 11. Join a table to itself

Display employees and managers, preserving employees without managers.

SqExpress C#:

<!-- cookbook:recipe:11 -->
```csharp
var e = new Employees("e");
var m = new Employees("m");

var query = Select(
        e.EmployeeId,
        e.Name.As("Employee"),
        m.Name.As("Manager"))
    .From(e)
    .LeftJoin(m, on: e.ManagerId == m.EmployeeId)
    .Done();

Emit("11", query);
```

T-SQL:

<!-- cookbook:sql:11:tsql -->
```sql
SELECT
    [e].[EmployeeId],
    [e].[Name] [Employee],
    [m].[Name] [Manager]
FROM [dbo].[Employees]
    [e]
LEFT JOIN [dbo].[Employees]
    [m] ON
    [e].[ManagerId]=[m].[EmployeeId]
```

Use separate descriptor instances and aliases. Manager is nullable in the joined result; choose a nullable result reader when executing it.

## 12. Find matching and missing relationships with EXISTS

Find customers with paid orders, then customers without paid orders.

SqExpress C#:

<!-- cookbook:recipe:12 -->
```csharp
var c = new Customers("c");
var o = new Orders("o");

var paid = SelectOne()
    .From(o)
    .Where(
        (o.CustomerId == c.CustomerId)
        & (o.Status == "Paid"));

Emit(
    "12-exists",
    Select(c.CustomerId)
        .From(c)
        .Where(Exists(paid))
        .Done());

Emit(
    "12-not-exists",
    Select(c.CustomerId)
        .From(c)
        .Where(!Exists(paid))
        .Done());
```

T-SQL — Customers with paid orders:

<!-- cookbook:sql:12-exists:tsql -->
```sql
SELECT
    [c].[CustomerId]
FROM [dbo].[Customers]
    [c]
WHERE
    EXISTS(
        SELECT
            1
        FROM [dbo].[Orders]
            [o]
        WHERE
            [o].[CustomerId]=[c].[CustomerId]
            AND
            [o].[Status]='Paid'
    )
```

T-SQL — Customers without paid orders:

<!-- cookbook:sql:12-not-exists:tsql -->
```sql
SELECT
    [c].[CustomerId]
FROM [dbo].[Customers]
    [c]
WHERE
    NOT EXISTS(
        SELECT
            1
        FROM [dbo].[Orders]
            [o]
        WHERE
            [o].[CustomerId]=[c].[CustomerId]
            AND
            [o].[Status]='Paid'
    )
```

The inner predicate correlates to the outer customer. EXISTS does not multiply customer rows. NOT EXISTS avoids the unknown result that a NULL in a NOT IN subquery can introduce.

## 13. Project a correlated scalar subquery

Return one maximum order amount for each customer, falling back to zero.

SqExpress C#:

<!-- cookbook:recipe:13 -->
```csharp
var c = new Customers("c");
var o = new Orders("o");

var maximum = ValueQuery(
    Select(Max(o.Amount))
        .From(o)
        .Where(o.CustomerId == c.CustomerId));

var query = Select(
        c.CustomerId,
        Coalesce(maximum, 0m).As("MaxAmount"))
    .From(c)
    .Done();

Emit("13", query);
```

T-SQL:

<!-- cookbook:sql:13:tsql -->
```sql
SELECT
    [c].[CustomerId],
    COALESCE((
        SELECT
            MAX([o].[Amount])
        FROM [dbo].[Orders]
            [o]
        WHERE
            [o].[CustomerId]=[c].[CustomerId]
    ),0) [MaxAmount]
FROM [dbo].[Customers]
    [c]
```

An aggregate without GROUP BY returns one row even when no orders match; MAX is then NULL. A general scalar subquery must return at most one row or the database rejects it.

## 14. Group rows and calculate aggregates

Summarize orders by customer with count and numeric aggregates.

SqExpress C#:

<!-- cookbook:recipe:14 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.CustomerId,
        CountOne().As("Rows"),
        Count(o.Amount).As("KnownAmounts"),
        CountDistinct(o.Status).As("Statuses"),
        Sum(o.Amount).As("Total"),
        Avg(o.Amount).As("Average"))
    .From(o)
    .GroupBy(o.CustomerId)
    .Done();

Emit("14", query);
```

T-SQL:

<!-- cookbook:sql:14:tsql -->
```sql
SELECT
    [o].[CustomerId],
    COUNT(1) [Rows],
    COUNT([o].[Amount]) [KnownAmounts],
    COUNT(DISTINCT [o].[Status]) [Statuses],
    SUM([o].[Amount]) [Total],
    AVG([o].[Amount]) [Average]
FROM [dbo].[Orders]
    [o]
GROUP BY
    [o].[CustomerId]
```

COUNT(1) counts rows; COUNT(Amount) counts non-null amounts. SUM and AVG ignore NULL and can return NULL for an all-null group. Every non-aggregate selected expression must be grouped.

## 15. Filter aggregate results through a derived table

Keep only customers whose total exceeds 1,000.

SqExpress C#:

<!-- cookbook:recipe:15 -->
```csharp
var o = new Orders("o");

var totals = Select(
        o.CustomerId,
        Sum(o.Amount).As("Total"))
    .From(o)
    .GroupBy(o.CustomerId)
    .As(TableAlias("totals"));

var query = Select(
        totals.Column("CustomerId"),
        totals.Column("Total"))
    .From(totals)
    .Where(totals.Column("Total") > 1000m)
    .Done();

Emit("15", query);
```

T-SQL:

<!-- cookbook:sql:15:tsql -->
```sql
SELECT
    [totals].[CustomerId],
    [totals].[Total]
FROM (
    SELECT
        [o].[CustomerId],
        SUM([o].[Amount]) [Total]
    FROM [dbo].[Orders]
        [o]
    GROUP BY
        [o].[CustomerId]
)
    [totals]
WHERE
    [totals].[Total]>1000
```

The current SELECT builder has no HAVING method. An outer WHERE over a grouped derived table provides this pattern. Total is a column of totals, not an alias available to the inner WHERE.

## 16. Calculate conditional totals and safe ratios

Calculate paid totals and the fraction of orders marked paid.

SqExpress C#:

<!-- cookbook:recipe:16 -->
```csharp
var o = new Orders("o");

var paidCount = Sum(
    Case()
        .When(o.Status == "Paid").Then(1)
        .Else(0));

var query = Select(
        o.CustomerId,
        Sum(
            Case()
                .When(o.Status == "Paid").Then(Coalesce(o.Amount, 0m))
                .Else(0m))
            .As("PaidTotal"),
        Coalesce(
            Cast(paidCount, SqlType.Decimal(new DecimalPrecisionScale(18, 4)))
                / NullIf(CountOne().AsValue(), 0),
            0m)
            .As("PaidFraction"))
    .From(o)
    .GroupBy(o.CustomerId)
    .Done();

Emit("16", query);
```

T-SQL:

<!-- cookbook:sql:16:tsql -->
```sql
SELECT
    [o].[CustomerId],
    SUM(CASE WHEN [o].[Status]='Paid' THEN COALESCE([o].[Amount],0) ELSE CAST(0 AS decimal) END) [PaidTotal],
    COALESCE(CAST(SUM(CASE WHEN [o].[Status]='Paid' THEN 1 ELSE 0 END) AS decimal(18,4))/NULLIF(COUNT(1),0),0) [PaidFraction]
FROM [dbo].[Orders]
    [o]
GROUP BY
    [o].[CustomerId]
```

Cast before division to avoid integer truncation. NULLIF makes a zero denominator NULL; outer COALESCE supplies a fallback. This fraction measures paid row count, not paid revenue.

## 17. Aggregate ordered text

List each customer’s order statuses in order-ID order.

SqExpress C#:

<!-- cookbook:recipe:17 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.CustomerId,
        StringAgg(o.Status, ", ")
            .OrderBy(o.OrderId)
            .As("Statuses"))
    .From(o)
    .GroupBy(o.CustomerId)
    .Done();

Emit("17", query);
```

T-SQL:

<!-- cookbook:sql:17:tsql -->
```sql
SELECT
    [o].[CustomerId],
    STRING_AGG([o].[Status],', ') WITHIN GROUP (ORDER BY [o].[OrderId]) [Statuses]
FROM [dbo].[Orders]
    [o]
GROUP BY
    [o].[CustomerId]
```

PostgreSQL:

<!-- cookbook:sql:17:pgsql -->
```sql
SELECT
    "o"."CustomerId",
    STRING_AGG("o"."Status",', '
    ORDER BY "o"."OrderId") "Statuses"
FROM "dbo"."Orders"
    "o"
GROUP BY
    "o"."CustomerId"
```

Aggregate-local ORDER BY controls concatenation independently of result ordering. Use a SQL Server version and compatibility level supporting ordered STRING_AGG. Repeated statuses remain repeated; nullable input values would be skipped.

## 18. Join an inline VALUES table

Restrict orders to a small requested-customer table.

SqExpress C#:

<!-- cookbook:recipe:18 -->
```csharp
var o = new Orders("o");
var requested = Values(1, 3, 5)
    .As("requested", "CustomerId");

var query = Select(
        o.OrderId,
        o.CustomerId)
    .From(o)
    .InnerJoin(requested, on: o.CustomerId == requested.Column("CustomerId"))
    .Done();

Emit("18", query);
```

T-SQL:

<!-- cookbook:sql:18:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[CustomerId]
FROM [dbo].[Orders]
    [o]
JOIN (VALUES
    (1),
    (3),
    (5))
    [requested]([CustomerId]) ON
    [o].[CustomerId]=[requested].[CustomerId]
```

Each VALUES row needs the same shape and compatible types. Duplicate requested IDs would duplicate matching orders; remove duplicates when that is not intended.

## 19. Combine results with set operators

Compare duplicate-preserving and duplicate-removing combinations, then compose set operations.

SqExpress C#:

<!-- cookbook:recipe:19 -->
```csharp
var a = Select(Literal(1).As("Value"));
var b = Select(Literal(2).As("Value"));

Emit("19-all", a.UnionAll(a).Done());
Emit("19-union", a.Union(a).Done());

var combined = a
    .Union(b)
    .As(TableAlias("combined"));

var query = Select(combined.Column("Value"))
    .From(combined)
    .Intersect(a.UnionAll(b))
    .Except(Select(3))
    .OrderBy(CustomColumnFactory.Int32("Value"))
    .Done();

Emit("19-mixed", query);
```

T-SQL — UNION ALL preserves duplicates:

<!-- cookbook:sql:19-all:tsql -->
```sql
SELECT
    1 [Value]
UNION ALL
SELECT
    1 [Value]
```

T-SQL — Explicit intermediate grouping:

<!-- cookbook:sql:19-mixed:tsql -->
```sql
SELECT
    [combined].[Value]
FROM (
    SELECT
        1 [Value]
    UNION
    SELECT
        2 [Value]
)
    [combined]
INTERSECT
(
    SELECT
        1 [Value]
    UNION ALL
    SELECT
        2 [Value]
)
EXCEPT
SELECT
    3
ORDER BY
    [Value]
```

T-SQL — UNION removes duplicates:

<!-- cookbook:sql:19-union:tsql -->
```sql
SELECT
    1 [Value]
UNION
SELECT
    1 [Value]
```

Operands need matching column counts and compatible types. UNION removes duplicates; UNION ALL retains them. SQL gives INTERSECT higher precedence than UNION and EXCEPT. The derived table forces the initial UNION to be evaluated as one source, and the right-hand UNION ALL is explicitly grouped. Final ORDER BY addresses the combined output name from the first projection.

## 20. Organize a query with a reusable CTE

Define a typed customer-total CTE and query its columns.

Supporting CTE definition (place after top-level statements):

<!-- cookbook:types -->
```csharp
public sealed class CustomerTotals : CteBase
{
    public CustomerTotals(Alias alias = default) : base("CustomerTotals", alias)
    {
        CustomerId = CreateInt32Column("CustomerId");
        Total = CreateNullableDecimalColumn("Total");
    }

    public Int32CustomColumn CustomerId { get; }

    public NullableDecimalCustomColumn Total { get; }

    public override IExprSubQuery CreateQuery()
    {
        var o = new Orders("sourceOrders");

        return Select(
                o.CustomerId,
                Sum(o.Amount).As(Total))
            .From(o)
            .GroupBy(o.CustomerId)
            .Done();
    }
}
```

SqExpress C#:

<!-- cookbook:recipe:20 -->
```csharp
var t = new CustomerTotals("t");

var query = Select(
        t.CustomerId,
        t.Total)
    .From(t)
    .Where(t.Total > 1000m)
    .Done();

Emit("20", query);
```

T-SQL:

<!-- cookbook:sql:20:tsql -->
```sql
WITH [CustomerTotals] AS(
    SELECT
        [sourceOrders].[CustomerId],
        SUM([sourceOrders].[Amount]) [Total]
    FROM [dbo].[Orders]
        [sourceOrders]
    GROUP BY
        [sourceOrders].[CustomerId]
)
SELECT
    [t].[CustomerId],
    [t].[Total]
FROM [CustomerTotals]
    [t]
WHERE
    [t].[Total]>1000
```

CteBase exposes a reusable SQL definition and typed result columns. It does not imply database materialization or caching. Projection names and types must match the descriptor.

## 21. Traverse a hierarchy with a recursive CTE

Start at employees without managers and walk down to a maximum depth of ten.

Supporting CTE definition (place after top-level statements):

<!-- cookbook:types -->
```csharp
public sealed class EmployeeTree : CteBase
{
    public EmployeeTree(Alias alias = default) : base("EmployeeTree", alias)
    {
        EmployeeId = CreateInt32Column("EmployeeId");
        Depth = CreateInt32Column("Depth");
    }

    public Int32CustomColumn EmployeeId { get; }

    public Int32CustomColumn Depth { get; }

    public override IExprSubQuery CreateQuery()
    {
        var root = new Employees("root");
        var child = new Employees("child");
        var parent = new EmployeeTree("parent");

        return Select(
                root.EmployeeId,
                Literal(0).As(Depth))
            .From(root)
            .Where(IsNull(root.ManagerId))
            .UnionAll(
                Select(
                        child.EmployeeId,
                        (parent.Depth + 1).As(Depth))
                    .From(child)
                    .InnerJoin(parent, on: child.ManagerId == parent.EmployeeId)
                    .Where(parent.Depth < 10))
            .Done();
    }
}
```

SqExpress C#:

<!-- cookbook:recipe:21 -->
```csharp
var tree = new EmployeeTree("tree");

var query = Select(
        tree.EmployeeId,
        tree.Depth)
    .From(tree)
    .OrderBy(
        tree.Depth,
        tree.EmployeeId)
    .Done();

Emit("21", query);
```

T-SQL:

<!-- cookbook:sql:21:tsql -->
```sql
WITH [EmployeeTree] AS(
    SELECT
        [root].[EmployeeId],
        0 [Depth]
    FROM [dbo].[Employees]
        [root]
    WHERE
        [root].[ManagerId] IS NULL
    UNION ALL
    SELECT
        [child].[EmployeeId],
        [parent].[Depth]+1 [Depth]
    FROM [dbo].[Employees]
        [child]
    JOIN [EmployeeTree]
        [parent] ON
        [child].[ManagerId]=[parent].[EmployeeId]
    WHERE
        [parent].[Depth]<10
)
SELECT
    [tree].[EmployeeId],
    [tree].[Depth]
FROM [EmployeeTree]
    [tree]
ORDER BY
    [tree].[Depth],
    [tree].[EmployeeId]
```

PostgreSQL:

<!-- cookbook:sql:21:pgsql -->
```sql
WITH RECURSIVE "EmployeeTree" AS(
    SELECT
        "root"."EmployeeId",
        0 "Depth"
    FROM "dbo"."Employees"
        "root"
    WHERE
        "root"."ManagerId" IS NULL
    UNION ALL
    SELECT
        "child"."EmployeeId",
        "parent"."Depth"+1 "Depth"
    FROM "dbo"."Employees"
        "child"
    JOIN "EmployeeTree"
        "parent" ON
        "child"."ManagerId"="parent"."EmployeeId"
    WHERE
        "parent"."Depth"<10
)
SELECT
    "tree"."EmployeeId",
    "tree"."Depth"
FROM "EmployeeTree"
    "tree"
ORDER BY
    "tree"."Depth",
    "tree"."EmployeeId"
```

Anchor rows have depth zero; expansion stops at depth ten. UNION ALL preserves paths. A depth bound does not detect cycles; rootless cyclic components are not reached. This bound stays below SQL Server’s default recursion limit.

## 22. Find the latest related row with APPLY

Choose one most recent order per customer, optionally retaining customers without orders.

SqExpress C#:

<!-- cookbook:recipe:22 -->
```csharp
var c = new Customers("c");
var o = new Orders("o");

var latest = Select(
        o.OrderId,
        o.OrderedAt)
    .From(o)
    .Where(o.CustomerId == c.CustomerId)
    .OrderBy(
        Desc(o.OrderedAt),
        Desc(o.OrderId))
    .OffsetFetch(0, 1)
    .As(TableAlias("latest"));

Emit(
    "22-cross",
    Select(
            c.CustomerId,
            latest.Column("OrderId"))
        .From(c)
        .CrossApply(latest)
        .Done());

Emit(
    "22-outer",
    Select(
            c.CustomerId,
            latest.Column("OrderId"))
        .From(c)
        .OuterApply(latest)
        .Done());
```

T-SQL — Required related row:

<!-- cookbook:sql:22-cross:tsql -->
```sql
SELECT
    [c].[CustomerId],
    [latest].[OrderId]
FROM [dbo].[Customers]
    [c]
CROSS APPLY (
    SELECT
        [o].[OrderId],
        [o].[OrderedAt]
    FROM [dbo].[Orders]
        [o]
    WHERE
        [o].[CustomerId]=[c].[CustomerId]
    ORDER BY
        [o].[OrderedAt] DESC,
        [o].[OrderId] DESC
    OFFSET 0 ROW
    FETCH NEXT 1 ROW ONLY
)
    [latest]
```

PostgreSQL — Required related row:

<!-- cookbook:sql:22-cross:pgsql -->
```sql
SELECT
    "c"."CustomerId",
    "latest"."OrderId"
FROM "dbo"."Customers"
    "c"
CROSS JOIN LATERAL (
    SELECT
        "o"."OrderId",
        "o"."OrderedAt"
    FROM "dbo"."Orders"
        "o"
    WHERE
        "o"."CustomerId"="c"."CustomerId"
    ORDER BY
        "o"."OrderedAt" DESC,
        "o"."OrderId" DESC
    OFFSET 0 ROW
    FETCH NEXT 1 ROW ONLY
)
    "latest"
```

T-SQL — Optional related row:

<!-- cookbook:sql:22-outer:tsql -->
```sql
SELECT
    [c].[CustomerId],
    [latest].[OrderId]
FROM [dbo].[Customers]
    [c]
OUTER APPLY (
    SELECT
        [o].[OrderId],
        [o].[OrderedAt]
    FROM [dbo].[Orders]
        [o]
    WHERE
        [o].[CustomerId]=[c].[CustomerId]
    ORDER BY
        [o].[OrderedAt] DESC,
        [o].[OrderId] DESC
    OFFSET 0 ROW
    FETCH NEXT 1 ROW ONLY
)
    [latest]
```

PostgreSQL — Optional related row:

<!-- cookbook:sql:22-outer:pgsql -->
```sql
SELECT
    "c"."CustomerId",
    "latest"."OrderId"
FROM "dbo"."Customers"
    "c"
LEFT JOIN LATERAL(
    SELECT
        "o"."OrderId",
        "o"."OrderedAt"
    FROM "dbo"."Orders"
        "o"
    WHERE
        "o"."CustomerId"="c"."CustomerId"
    ORDER BY
        "o"."OrderedAt" DESC,
        "o"."OrderId" DESC
    OFFSET 0 ROW
    FETCH NEXT 1 ROW ONLY
)
    "latest" ON TRUE
```

CROSS APPLY requires an inner result; OUTER APPLY preserves the customer when none exists. PostgreSQL uses CROSS JOIN LATERAL or LEFT JOIN LATERAL ... ON TRUE. A plain derived table cannot use this left-side correlation. The ordered derived source uses `.OffsetFetch(0, 1)` to complete a subquery-capable builder stage.

## 23. Keep detail rows alongside partition totals

Return each order alongside its customer’s count, sum, and maximum.

SqExpress C#:

<!-- cookbook:recipe:23 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.OrderId,
        o.CustomerId,
        o.Amount,
        CountOne()
            .OverPartitionBy(o.CustomerId)
            .NoOrderBy()
            .As("CustomerOrders"),
        Sum(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .NoOrderBy()
            .As("CustomerTotal"),
        Max(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .NoOrderBy()
            .As("CustomerMaximum"))
    .From(o)
    .OrderBy(
        o.CustomerId,
        o.OrderId)
    .Done();

Emit("23", query);
```

T-SQL:

<!-- cookbook:sql:23:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[CustomerId],
    [o].[Amount],
    COUNT(1)OVER(PARTITION BY [o].[CustomerId]) [CustomerOrders],
    SUM([o].[Amount])OVER(PARTITION BY [o].[CustomerId]) [CustomerTotal],
    MAX([o].[Amount])OVER(PARTITION BY [o].[CustomerId]) [CustomerMaximum]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[CustomerId],
    [o].[OrderId]
```

Windows retain detail rows; GROUP BY collapses them. NoOrderBy completes the partition builder without introducing a running frame. The final ORDER BY controls presentation.

## 24. Compose COALESCE around a windowed MAX

Supply zero only when the partition maximum is NULL; compare putting the fallback inside MAX.

SqExpress C#:

<!-- cookbook:recipe:24 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.OrderId,
        o.CustomerId,
        o.Amount,
        Coalesce(
            Max(o.Amount)
                .OverPartitionBy(o.CustomerId)
                .NoOrderBy()
                .AsValue(),
            0m)
            .As("FallbackAfterMax"),
        Max(Coalesce(o.Amount, 0m))
            .OverPartitionBy(o.CustomerId)
            .NoOrderBy()
            .As("FallbackBeforeMax"))
    .From(o)
    .OrderBy(
        o.CustomerId,
        o.OrderId)
    .Done();

Emit("24", query);
```

T-SQL:

<!-- cookbook:sql:24:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[CustomerId],
    [o].[Amount],
    COALESCE(MAX([o].[Amount])OVER(PARTITION BY [o].[CustomerId]),0) [FallbackAfterMax],
    MAX(COALESCE([o].[Amount],0))OVER(PARTITION BY [o].[CustomerId]) [FallbackBeforeMax]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[CustomerId],
    [o].[OrderId]
```

For a partition containing NULL and -5, FallbackAfterMax is -5, while FallbackBeforeMax is 0. For an all-null partition both are 0. With only negative non-null values, both return the greatest negative value. COALESCE wraps the completed window expression; `.NoOrderBy()` finishes this fluent stage. Use `.AsValue()` to adapt the completed selectable window node for COALESCE; this wrapper adds no SQL cast.

Expected values by partition (reasoning examples, not database execution results):

| Amounts in a partition | Fallback after MAX | Fallback before MAX |
|---|---:|---:|
| `NULL, -5` | -5 | 0 |
| `NULL, NULL` | 0 | 0 |
| `-8, -5` | -5 | -5 |
| `NULL, 7` | 7 | 7 |

An empty input produces no detail rows, so neither expression creates a row for a customer with no orders. Start from customers with a left join or APPLY when those customers must appear.

## 25. Calculate running totals and moving averages

Compute a cumulative amount and an average over the current and preceding two rows.

SqExpress C#:

<!-- cookbook:recipe:25 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.OrderId,
        o.CustomerId,
        Sum(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .OrderBy(o.OrderedAt, o.OrderId)
            .FrameClause(
                FrameBorder.UnboundedPreceding,
                FrameBorder.CurrentRow)
            .As("RunningTotal"),
        Avg(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .OrderBy(o.OrderedAt, o.OrderId)
            .FrameClause(
                FrameBorder.Preceding(2),
                FrameBorder.CurrentRow)
            .As("MovingAverage"))
    .From(o)
    .OrderBy(
        o.CustomerId,
        o.OrderedAt,
        o.OrderId)
    .Done();

Emit("25", query);
```

T-SQL:

<!-- cookbook:sql:25:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[CustomerId],
    SUM([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [RunningTotal],
    AVG([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId] ROWS BETWEEN 2 PRECEDING AND CURRENT ROW) [MovingAverage]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[CustomerId],
    [o].[OrderedAt],
    [o].[OrderId]
```

ROWS counts positions, not days. The first rows have shorter frames. OrderId resolves timestamp ties. Explicit ROWS avoids the peer-group behavior of an implicit ordered RANGE frame; AVG still ignores NULL amounts.

## 26. Rank rows and select the top N per group

Compare three ranking functions, then keep two rows per customer.

SqExpress C#:

<!-- cookbook:recipe:26 -->
```csharp
var o = new Orders("o");

var ranked = Select(
        o.OrderId,
        o.CustomerId,
        o.OrderedAt,
        RowNumber()
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(Desc(o.OrderedAt), Desc(o.OrderId))
            .As("RowNum"),
        Rank()
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(Desc(o.OrderedAt))
            .As("RankValue"),
        DenseRank()
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(Desc(o.OrderedAt))
            .As("DenseRankValue"))
    .From(o)
    .As(TableAlias("ranked"));

var query = Select(ranked.AllColumns())
    .From(ranked)
    .Where(ranked.Column("RowNum") <= 2)
    .OrderBy(
        ranked.Column("CustomerId"),
        ranked.Column("RowNum"))
    .Done();

Emit("26", query);
```

T-SQL:

<!-- cookbook:sql:26:tsql -->
```sql
SELECT
    [ranked].*
FROM (
    SELECT
        [o].[OrderId],
        [o].[CustomerId],
        [o].[OrderedAt],
        ROW_NUMBER()OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt] DESC,[o].[OrderId] DESC) [RowNum],
        RANK()OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt] DESC) [RankValue],
        DENSE_RANK()OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt] DESC) [DenseRankValue]
    FROM [dbo].[Orders]
        [o]
)
    [ranked]
WHERE
    [ranked].[RowNum]<=2
ORDER BY
    [ranked].[CustomerId],
    [ranked].[RowNum]
```

ROW_NUMBER uses a unique tie-breaker and returns at most two rows per customer. RANK and DENSE_RANK use only timestamps so ties remain; RANK leaves gaps. Filtering a rank can return more than N rows. Window results require an outer query for WHERE filtering.

## 27. Compare a row with its previous and next rows

Calculate change from the previous order and inspect neighboring amounts.

SqExpress C#:

<!-- cookbook:recipe:27 -->
```csharp
var o = new Orders("o");

var previous = Lag(o.Amount)
    .OverPartitionBy(o.CustomerId)
    .OverOrderBy(o.OrderedAt, o.OrderId);

var query = Select(
        o.OrderId,
        previous.As("PreviousAmount"),
        (o.Amount - previous.AsValue()).As("Change"),
        Lag(o.Amount, 1, 0m)
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(o.OrderedAt, o.OrderId)
            .As("PreviousOrBoundaryZero"),
        Lead(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(o.OrderedAt, o.OrderId)
            .As("NextAmount"))
    .From(o)
    .OrderBy(
        o.CustomerId,
        o.OrderedAt,
        o.OrderId)
    .Done();

Emit("27", query);
```

T-SQL:

<!-- cookbook:sql:27:tsql -->
```sql
SELECT
    [o].[OrderId],
    LAG([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId]) [PreviousAmount],
    [o].[Amount]-LAG([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId]) [Change],
    LAG([o].[Amount],1,0)OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId]) [PreviousOrBoundaryZero],
    LEAD([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId]) [NextAmount]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[CustomerId],
    [o].[OrderedAt],
    [o].[OrderId]
```

The default argument supplies zero only when the preceding row does not exist, not when its amount is NULL. Without a default, the first previous value and last next value are NULL. Arithmetic involving NULL returns NULL.

## 28. Get the actual first and last values in a partition

Compare the default LAST_VALUE frame with an explicit full-partition frame.

SqExpress C#:

<!-- cookbook:recipe:28 -->
```csharp
var o = new Orders("o");

var query = Select(
        o.OrderId,
        FirstValue(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(o.OrderedAt, o.OrderId)
            .FrameClause(
                FrameBorder.UnboundedPreceding,
                FrameBorder.UnboundedFollowing)
            .As("FirstAmount"),
        LastValue(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(o.OrderedAt, o.OrderId)
            .FrameClauseEmpty()
            .As("DefaultLast"),
        LastValue(o.Amount)
            .OverPartitionBy(o.CustomerId)
            .OverOrderBy(o.OrderedAt, o.OrderId)
            .FrameClause(
                FrameBorder.UnboundedPreceding,
                FrameBorder.UnboundedFollowing)
            .As("LastAmount"))
    .From(o)
    .OrderBy(
        o.CustomerId,
        o.OrderedAt,
        o.OrderId)
    .Done();

Emit("28", query);
```

T-SQL:

<!-- cookbook:sql:28:tsql -->
```sql
SELECT
    [o].[OrderId],
    FIRST_VALUE([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) [FirstAmount],
    LAST_VALUE([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId]) [DefaultLast],
    LAST_VALUE([o].[Amount])OVER(PARTITION BY [o].[CustomerId] ORDER BY [o].[OrderedAt],[o].[OrderId] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) [LastAmount]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[CustomerId],
    [o].[OrderedAt],
    [o].[OrderId]
```

With unique ordering, the default ordered frame ends at the current row, so DefaultLast is the current amount. UNBOUNDED FOLLOWING reaches the final partition row. These functions respect NULL values; neither automatically finds the first or last non-null amount.

## 29. Calculate each grouped total’s share of the whole

Aggregate per customer first, then calculate each total’s fraction of all totals.

SqExpress C#:

<!-- cookbook:recipe:29 -->
```csharp
var o = new Orders("o");

var totals = Select(
        o.CustomerId,
        Sum(o.Amount).As("Total"))
    .From(o)
    .GroupBy(o.CustomerId)
    .As(TableAlias("totals"));

var total = totals.Column("Total");

var query = Select(
        totals.Column("CustomerId"),
        total,
        Coalesce(
            Cast(total, SqlType.Decimal(new DecimalPrecisionScale(18, 4)))
                / NullIf(Sum(total).Over().AsValue(), 0m),
            0m)
            .As("Share"))
    .From(totals)
    .Done();

Emit("29", query);
```

T-SQL:

<!-- cookbook:sql:29:tsql -->
```sql
SELECT
    [totals].[CustomerId],
    [totals].[Total],
    COALESCE(CAST([totals].[Total] AS decimal(18,4))/NULLIF(SUM([totals].[Total])OVER(),0),0) [Share]
FROM (
    SELECT
        [o].[CustomerId],
        SUM([o].[Amount]) [Total]
    FROM [dbo].[Orders]
        [o]
    GROUP BY
        [o].[CustomerId]
)
    [totals]
```

The inner query establishes one row per customer; the outer window sees those rows. Cast before division and guard a zero denominator. Null totals receive a zero share; negative amounts can produce negative shares or a zero grand total. No window is nested inside another window.

## 30. Build a paged customer report from multiple query stages

Combine totals, the latest order, ranking, total-result count, and pagination at customer grain.

SqExpress C#:

<!-- cookbook:recipe:30 -->
```csharp
var c = new Customers("c");
var t = new CustomerTotals("t");
var o = new Orders("o");

var latest = Select(o.OrderId)
    .From(o)
    .Where(o.CustomerId == c.CustomerId)
    .OrderBy(
        Desc(o.OrderedAt),
        Desc(o.OrderId))
    .OffsetFetch(0, 1)
    .As(TableAlias("latest"));

var total = Coalesce(t.Total, 0m);

var report = Select(
        c.CustomerId,
        c.Name,
        total.As("Total"),
        latest.Column("OrderId").As("LatestOrderId"),
        RowNumber()
            .OverOrderBy(Desc(total), c.CustomerId)
            .As("Position"),
        CountOne()
            .Over()
            .As("TotalCustomers"))
    .From(c)
    .LeftJoin(t, on: t.CustomerId == c.CustomerId)
    .OuterApply(latest)
    .As(TableAlias("report"));

var query = Select(report.AllColumns())
    .From(report)
    .OrderBy(report.Column("Position"))
    .OffsetFetch(10, 10)
    .Done();

Emit("30", query);
```

T-SQL:

<!-- cookbook:sql:30:tsql -->
```sql
WITH [CustomerTotals] AS(
    SELECT
        [sourceOrders].[CustomerId],
        SUM([sourceOrders].[Amount]) [Total]
    FROM [dbo].[Orders]
        [sourceOrders]
    GROUP BY
        [sourceOrders].[CustomerId]
)
SELECT
    [report].*
FROM (
    SELECT
        [c].[CustomerId],
        [c].[Name],
        COALESCE([t].[Total],0) [Total],
        [latest].[OrderId] [LatestOrderId],
        ROW_NUMBER()OVER(ORDER BY COALESCE([t].[Total],0) DESC,[c].[CustomerId]) [Position],
        COUNT(1)OVER() [TotalCustomers]
    FROM [dbo].[Customers]
        [c]
    LEFT JOIN [CustomerTotals]
        [t] ON
        [t].[CustomerId]=[c].[CustomerId]
    OUTER APPLY (
        SELECT
            [o].[OrderId]
        FROM [dbo].[Orders]
            [o]
        WHERE
            [o].[CustomerId]=[c].[CustomerId]
        ORDER BY
            [o].[OrderedAt] DESC,
            [o].[OrderId] DESC
        OFFSET 0 ROW
        FETCH NEXT 1 ROW ONLY
    )
        [latest]
)
    [report]
ORDER BY
    [report].[Position]
OFFSET 10 ROW
FETCH NEXT 10 ROW ONLY
```

PostgreSQL:

<!-- cookbook:sql:30:pgsql -->
```sql
WITH "CustomerTotals" AS(
    SELECT
        "sourceOrders"."CustomerId",
        SUM("sourceOrders"."Amount") "Total"
    FROM "dbo"."Orders"
        "sourceOrders"
    GROUP BY
        "sourceOrders"."CustomerId"
)
SELECT
    "report".*
FROM (
    SELECT
        "c"."CustomerId",
        "c"."Name",
        COALESCE("t"."Total",0) "Total",
        "latest"."OrderId" "LatestOrderId",
        ROW_NUMBER()OVER(ORDER BY COALESCE("t"."Total",0) DESC,"c"."CustomerId") "Position",
        COUNT(1)OVER() "TotalCustomers"
    FROM "dbo"."Customers"
        "c"
    LEFT JOIN "CustomerTotals"
        "t" ON
        "t"."CustomerId"="c"."CustomerId"
    LEFT JOIN LATERAL(
        SELECT
            "o"."OrderId"
        FROM "dbo"."Orders"
            "o"
        WHERE
            "o"."CustomerId"="c"."CustomerId"
        ORDER BY
            "o"."OrderedAt" DESC,
            "o"."OrderId" DESC
        OFFSET 0 ROW
        FETCH NEXT 1 ROW ONLY
    )
        "latest" ON TRUE
)
    "report"
ORDER BY
    "report"."Position"
OFFSET 10 ROW
FETCH NEXT 10 ROW ONLY
```

The CTE has at most one row per customer; APPLY has at most one order, so report rows remain customers. Count and rank precede pagination. Customers without orders have Total = 0 and LatestOrderId = NULL. An empty page carries no TotalCustomers value; use a separate count query if that count is always required. Apply report filters before the count/rank stage when they should affect those values. The ordered derived source uses `.OffsetFetch(0, 1)` to complete a subquery-capable builder stage.

## Composition rules and troubleshooting

- **Complete the correct stage.** `.Done()` builds an AST. `.NoOrderBy()` completes a partition-only aggregate window. `FirstValue`/`LastValue` need `.FrameClause(...)` or `.FrameClauseEmpty()` to finish their frame stage.
- **Adapt selectable expressions when composing values.** Aggregate and analytic nodes implement `IExprSelecting`; scalar helpers such as `Coalesce` and `NullIf`, and arithmetic operators, accept `ExprValue`. Use `.AsValue()` on the completed aggregate/window node, as in recipes 16, 24, 27, and 29. This adapts the AST without emitting a SQL CAST. `Cast(...)` is for a real SQL type conversion and also accepts selectable expressions.
- **Use `ValueQuery(...)` for scalar subqueries.** It creates the scalar-subquery node, including its SQL parentheses. `.AsValue()` on a window or aggregate serves a different purpose.
- **Distinguish aliases from expressions.** `.As("Total")` names a projection. Reuse the original expression in that SELECT’s predicates or ordering when appropriate; use a qualified derived-table/CTE column in an outer query. A projection alias is not available to the same SELECT’s WHERE.
- **Make query stages explicit.** Aggregate filtering uses a derived table because the builder has no HAVING method. Window-result filtering also needs an outer query. SQL still controls which aggregate/window combinations are legal; compiling an AST alone does not prove semantic validity.
- **Use an ordered subquery form for APPLY.** An `ISelectBuilder` after `.OrderBy(...)` cannot directly become a derived source through `.As(...)`. Recipes 22 and 30 finish an ordered, limited subquery using `.OffsetFetch(0, 1).As(...)`. Their T-SQL uses OFFSET/FETCH rather than TOP 1. Avoid casting a builder merely to bypass this stage distinction.
- **Respect correlation scope.** Correlated scalar subqueries and EXISTS can refer to their outer query. APPLY/LATERAL sources can refer to preceding sources. A plain derived table cannot.
- **Define ordering at each level.** A window’s ORDER BY defines ranking/frame behavior; the final SELECT’s ORDER BY defines output order. Add a unique tie-breaker when row choice must be deterministic. Preserve ties deliberately when demonstrating RANK/DENSE_RANK.
- **Track row counts and nullability.** One-to-many joins multiply rows. Outer joins and OUTER APPLY introduce NULL outputs even for columns declared non-null in base descriptors. Match execution readers to the resulting projection’s nullability.
- **Force mixed set-operation grouping when necessary.** SQL operator precedence can differ from the apparent left-to-right fluent chain. Recipe 19 uses a derived table as an explicit intermediate boundary and groups its right-hand operand.

## Reproduce the verification

Save the following PowerShell as `verify-cookbook.ps1` outside the repository, then run it from the repository root using the existing system `dotnet` SDK. Its optional `-TemporaryRoot` argument selects a writable build location when an isolated environment restricts the system temporary directory. It creates a temporary .NET 8 project outside the repository, extracts every marked C# block from this document, wraps each recipe in a named local function, builds, runs all examples, and compares all published SQL blocks with the newly exported files. Only line endings are normalized. No NuGet cache or CLI home overrides are needed.

Every query variant exports to both dialects even when only T-SQL is printed in the recipe. The expected totals are **30 recipes, 39 query variants, 78 SQL exports, and 52 published SQL blocks**. The temporary directory is printed for inspection and can be removed afterward. No project is added to the solution.

```powershell
param([string] $TemporaryRoot = [IO.Path]::GetTempPath())

$ErrorActionPreference = 'Stop'
# Run from the repository root with the system .NET SDK.
$repo = (Get-Location).Path
$core = Join-Path $repo 'SqExpress/SqExpress.csproj'
$analyzer = Join-Path $repo 'SqExpress.Analyzers/SqExpress.Analyzers.csproj'
if (-not (Test-Path -LiteralPath $core) -or -not (Test-Path -LiteralPath $analyzer)) {
    throw 'Run from the SqExpress repository root.'
}
$document = Join-Path $repo 'Documentation/select_cookbook.md'
$markdown = Get-Content -LiteralPath $document -Raw
$pattern = '(?ms)<!-- cookbook:(?<kind>imports|types|recipe)(?::(?<id>\d+))? -->\r?\n```csharp\r?\n(?<code>.*?)\r?\n```'
$blocks = [regex]::Matches($markdown, $pattern)
$imports = @($blocks | Where-Object { $_.Groups['kind'].Value -eq 'imports' })
$recipes = @($blocks | Where-Object { $_.Groups['kind'].Value -eq 'recipe' })
$types = @($blocks | Where-Object { $_.Groups['kind'].Value -eq 'types' })
if ($imports.Count -ne 1 -or $recipes.Count -ne 30 -or $types.Count -ne 4) {
    throw 'Expected imports, 30 recipe blocks, and four supporting type blocks.'
}
$methods = foreach ($block in $recipes) {
    'void Recipe' + $block.Groups['id'].Value + "()`n{`n" + $block.Groups['code'].Value + "`n}"
}
$calls = foreach ($block in $recipes) { 'Recipe' + $block.Groups['id'].Value + '();' }
$source = $imports[0].Groups['code'].Value + "`n`n" + ($methods -join "`n`n") +
    "`n`n" + ($calls -join "`n") + "`n`n" +
    (($types | ForEach-Object { $_.Groups['code'].Value }) -join "`n`n")
$work = Join-Path $TemporaryRoot ('SqExpress.SelectCookbook.' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
Set-Content -LiteralPath (Join-Path $work 'Program.cs') -Value $source -Encoding utf8
$escapedCore = [System.Security.SecurityElement]::Escape($core)
$escapedAnalyzer = [System.Security.SecurityElement]::Escape($analyzer)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$escapedCore" />
    <ProjectReference Include="$escapedAnalyzer" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $work 'SelectCookbookVerification.csproj'
Set-Content -LiteralPath $projectPath -Value $project -Encoding utf8
# A single build worker and an in-process compiler also work in isolated shells.
dotnet build $projectPath -m:1 -nr:false -p:UseSharedCompilation=false --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Cookbook compilation failed.' }
Push-Location $work
try {
    dotnet (Join-Path $work 'bin/Debug/net8.0/SelectCookbookVerification.dll')
    if ($LASTEXITCODE -ne 0) { throw 'Cookbook export failed.' }
    $sqlPattern = '(?ms)<!-- cookbook:sql:(?<id>[\w-]+):(?<dialect>tsql|pgsql) -->\r?\n```sql\r?\n(?<sql>.*?)\r?\n```'
    $sqlBlocks = [regex]::Matches($markdown, $sqlPattern)
    foreach ($block in $sqlBlocks) {
        $name = $block.Groups['id'].Value + '.' + $block.Groups['dialect'].Value + '.sql'
        $actual = [IO.File]::ReadAllText((Join-Path $work ('sql/' + $name)))
        $expected = $block.Groups['sql'].Value
        if ($actual.Replace("`r`n", "`n") -cne $expected.Replace("`r`n", "`n")) {
            throw "SQL mismatch: $name"
        }
    }
    $exports = @(Get-ChildItem -LiteralPath (Join-Path $work 'sql') -Filter '*.sql')
    if ($exports.Count -ne 78 -or $sqlBlocks.Count -ne 52) { throw 'Unexpected output count.' }
    Write-Host "Verified 30 recipes, 78 exports, and all $($sqlBlocks.Count) published SQL blocks."
    Write-Host "Temporary project: $work"
}
finally { Pop-Location }
```
