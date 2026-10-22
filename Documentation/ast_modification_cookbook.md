# AST modification cookbook

Transform an existing SqExpress syntax tree, from replacing a filter to expanding logical predicates and changing nested query specifications. Each recipe gives a goal, complete C# code, actual before/after spacious T-SQL output, and a composition note.

All **18 recipes** are extracted from this document, compiled in a temporary .NET 8 console project, and run with assertions. All **36 before/after variants** export to both T-SQL and PostgreSQL (**72 exports**); the **36 published SQL blocks** are captured T-SQL exporter output. Verification covers compilation, assertions, and export, **not database execution or general SQL semantic validation**. Recipe 13 intentionally exports an unresolved logical representation before rewriting it.

## Contents

- [Setup](#setup)
- [Choosing a modification API](#choosing-a-modification-api)
- [1. Replace a WHERE clause](#1-replace-a-where-clause)
- [2. Extend a filter and keep independent variants](#2-extend-a-filter-and-keep-independent-variants)
- [3. Change the projection](#3-change-the-projection)
- [4. Replace ordering and add pagination](#4-replace-ordering-and-add-pagination)
- [5. Add a join to an existing query](#5-add-a-join-to-an-existing-query)
- [6. Rewrite matching literals throughout a tree](#6-rewrite-matching-literals-throughout-a-tree)
- [7. Replace a column by source and name](#7-replace-a-column-by-source-and-name)
- [8. Adapt a predicate to a derived-table alias](#8-adapt-a-predicate-to-a-derived-table-alias)
- [9. Distinguish the root from descendants](#9-distinguish-the-root-from-descendants)
- [10. Understand bottom-up callbacks and replacement visitation](#10-understand-bottom-up-callbacks-and-replacement-visitation)
- [11. Remove optional nodes and reject invalid replacements](#11-remove-optional-nodes-and-reject-invalid-replacements)
- [12. Remove collection items without emptying a required list](#12-remove-collection-items-without-emptying-a-required-list)
- [13. Expand a logical IN filter into correlated EXISTS](#13-expand-a-logical-in-filter-into-correlated-exists)
- [14. Respect nested scopes with repeated aliases](#14-respect-nested-scopes-with-repeated-aliases)
- [15. Apply access predicates to a bounded query shape](#15-apply-access-predicates-to-a-bounded-query-shape)
- [16. Preserve unmatched rows when filtering a LEFT JOIN](#16-preserve-unmatched-rows-when-filtering-a-left-join)
- [17. Rewrite set-operation branches and handle join boundaries](#17-rewrite-set-operation-branches-and-handle-join-boundaries)
- [18. Modify a CTE definition through its containing query](#18-modify-a-cte-definition-through-its-containing-query)
- [Troubleshooting and boundaries](#troubleshooting-and-boundaries)
- [Reproduce the verification](#reproduce-the-verification)

## Setup

Use the APIs in this checkout. Each recipe is independent with the imports, descriptors, CTE, and helper below. Put imports first, recipe statements in separate methods or local functions, and supporting types after all top-level statements. The verification script does this automatically and references both the core library and the table-descriptor source generator.

The small schema assumes unique order and customer IDs and an Orders.CustomerId relationship to Customers. These descriptors declare shapes, not database constraints. Explicit aliases keep output reproducible. Both exporters preserve the explicit `dbo` schema; PostgreSQL execution would require corresponding quoted identifiers.

### Imports

<!-- ast-cookbook:imports -->
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using SqExpress;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.TableDeclarationAttributes;
using static SqExpress.SqQueryBuilder;
using static CookbookOutput;
```

### Shared descriptors and CTE

<!-- ast-cookbook:types -->
```csharp
[TableDescriptor("dbo", "Orders")]
[Int32Column("OrderId")]
[Int32Column("CustomerId")]
[Int32Column("TenantId")]
[StringColumn("Status", MaxLength = 20)]
public sealed partial class Orders
{
}

[TableDescriptor("dbo", "Customers")]
[Int32Column("CustomerId")]
[Int32Column("TenantId")]
[StringColumn("Name", MaxLength = 100)]
[StringColumn("DisplayName", MaxLength = 100)]
[StringColumn("Region", MaxLength = 50)]
public sealed partial class Customers
{
}

public sealed class PaidOrders : CteBase
{
    public PaidOrders(Alias alias = default) : base("PaidOrders", alias)
    {
        OrderId = CreateInt32Column("OrderId");
    }

    public Int32CustomColumn OrderId { get; }

    public override IExprSubQuery CreateQuery()
    {
        var orders = new Orders("sourceOrder");

        return Select(orders.OrderId)
            .From(orders)
            .Where(orders.Status == "Paid" & orders.TenantId == 7)
            .Done();
    }
}
```

### Assertions and SQL export

<!-- ast-cookbook:types -->
```csharp
public static class CookbookOutput
{
    private static readonly HashSet<string> Ids = new();

    public static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void ExpectSqExpressFailure(Action action, string scenario)
    {
        try
        {
            action();
        }
        catch (SqExpressException)
        {
            return;
        }

        throw new InvalidOperationException("Expected SqExpressException: " + scenario);
    }

    public static void EmitPair(string id, IExpr original, IExpr modified)
    {
        Emit(id + "-before", original);
        Emit(id + "-after", modified);
    }

    private static void Emit(string id, IExpr expression)
    {
        Check(Ids.Add(id), "Duplicate output: " + id);
        Directory.CreateDirectory("sql");
        File.WriteAllText(
            Path.Combine("sql", id + ".tsql.sql"),
            expression.ToSql(TSqlExporter.Default.WithFormatting(SqlFormattingProfile.Spacious)));
        File.WriteAllText(
            Path.Combine("sql", id + ".pgsql.sql"),
            expression.ToSql(PgSqlExporter.Default.WithFormatting(SqlFormattingProfile.Spacious)));
    }
}
```

## Choosing a modification API

Use `With...` when you already have the node whose property needs changing. These methods return a new node; save it and, if necessary, put it back into its parent. For example, an ordered SELECT wraps a query specification rather than storing WHERE itself.

Use `SyntaxTree().Modify(...)` to transform matching nodes throughout a tree. The typed overload filters callbacks by node type. The return type is `IExpr?`, because a root can change type or disappear; cast only when your transformation guarantees the resulting shape. A callback should return the provided node for nonmatches. Unchanged branches are shared rather than deeply cloned.

The modifier processes children before parents and does not revisit returned replacements during the same pass. It enforces required child types and some nonempty collections; it is not a SQL binder, optimizer, or authorization engine. `ModifyDescendants` has a rebuilt-root limitation in this checkout, demonstrated in recipe 9.

## 1. Replace a WHERE clause

Replace an existing filter deliberately.

SqExpress C#:

<!-- ast-cookbook:recipe:01 -->
```csharp
var orders = new Orders("o");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(orders.Status == "Draft")
    .Done();

var modified = original.WithWhere(orders.Status == "Paid");

Check(original.Where!.ToSql(TSqlExporter.Default).Contains("Draft"), "Original filter changed.");
EmitPair("01", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:01-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Draft'
```

After (T-SQL):

<!-- ast-cookbook:sql:01-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Paid'
```

`WithWhere` replaces the whole predicate. It does not append a condition. Keep the returned node; ignoring it leaves your query unchanged.

## 2. Extend a filter and keep independent variants

Preserve an existing OR expression while adding a tenant restriction.

SqExpress C#:

<!-- ast-cookbook:recipe:02 -->
```csharp
var orders = new Orders("o");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(orders.Status == "Paid" | orders.Status == "Shipped")
    .Done();
var originalSql = original.ToSql(TSqlExporter.Default);

var modified = original.WithWhere(
    original.Where == null
        ? orders.TenantId == 7
        : original.Where & (orders.TenantId == 7));
var anotherTenant = modified.WithWhere(original.Where & (orders.TenantId == 8));

Check(original.ToSql(TSqlExporter.Default) == originalSql, "Original query changed.");
Check(!modified.ToSql(TSqlExporter.Default).Contains("=8"), "Variants interfered.");
Check(anotherTenant.ToSql(TSqlExporter.Default).Contains("=8"), "Second variant missing.");
EmitPair("02", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:02-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Paid'
    OR
    [o].[Status]='Shipped'
```

After (T-SQL):

<!-- ast-cookbook:sql:02-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    (
        [o].[Status]='Paid'
        OR
        [o].[Status]='Shipped'
    )
    AND
    [o].[TenantId]=7
```

The exporter preserves boolean grouping. `With...` creates a new node and shares unchanged children; do not mutate lists you supplied to AST constructors. The null check also handles a query without WHERE.

## 3. Change the projection

Reuse FROM and WHERE while returning a different result shape.

SqExpress C#:

<!-- ast-cookbook:recipe:03 -->
```csharp
var orders = new Orders("o");
var original = Select(orders.OrderId, orders.Status)
    .From(orders)
    .Where(orders.TenantId == 7)
    .Done();

var modified = original.WithSelectList(
    orders.OrderId,
    orders.Status.As("OrderStatus"),
    (orders.OrderId + 1000).As("DisplayId"));

Check(modified.SelectList.Count == 3, "Projection was not replaced.");
EmitPair("03", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:03-before:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Status]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=7
```

After (T-SQL):

<!-- ast-cookbook:sql:03-after:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Status] [OrderStatus],
    [o].[OrderId]+1000 [DisplayId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=7
```

Update the execution reader or DTO mapping when changing the projection. This convenience overload operates on `ExprQuerySpecification`; an ordered query wraps that specification in another node.

## 4. Replace ordering and add pagination

Change an ordered query and introduce a deterministic page boundary.

SqExpress C#:

<!-- ast-cookbook:recipe:04 -->
```csharp
var orders = new Orders("o");
var original = Select(orders.OrderId, orders.Status)
    .From(orders)
    .OrderBy(orders.OrderId)
    .Done();

var reordered = original.WithOrderBy(new ExprOrderBy(
    new ExprOrderByItem[]
    {
        new ExprOrderByItem(orders.Status, false),
        new ExprOrderByItem(orders.OrderId, true)
    }));
var modified = reordered.AddOffsetFetch(offset: 10, fetch: 5);

Check(original.OrderBy.OrderList.Count == 1, "Original ordering changed.");
EmitPair("04", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:04-before:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Status]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[OrderId]
```

After (T-SQL):

<!-- ast-cookbook:sql:04-after:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Status]
FROM [dbo].[Orders]
    [o]
ORDER BY
    [o].[Status],
    [o].[OrderId] DESC
OFFSET 10 ROW
FETCH NEXT 5 ROW ONLY
```

ORDER BY belongs to the outer `ExprSelect`, while the projection and WHERE belong to its `SelectQuery`. `AddOffsetFetch` returns `ExprSelectOffsetFetch`. Use a unique tie-breaker for stable pages.

## 5. Add a join to an existing query

Attach a customer source without rebuilding the query.

SqExpress C#:

<!-- ast-cookbook:recipe:05 -->
```csharp
var orders = new Orders("o");
var customers = new Customers("c");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(orders.Status == "Paid")
    .Done();

var modified = original
    .WithInnerJoin(customers, orders.CustomerId == customers.CustomerId)
    .WithSelectList(orders.OrderId, customers.Name);

EmitPair("05", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:05-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Paid'
```

After (T-SQL):

<!-- ast-cookbook:sql:05-after:tsql -->
```sql
SELECT
    [o].[OrderId],
    [c].[Name]
FROM [dbo].[Orders]
    [o]
JOIN [dbo].[Customers]
    [c] ON
    [o].[CustomerId]=[c].[CustomerId]
WHERE
    [o].[Status]='Paid'
```

Adding a join can multiply rows. The convenience join methods require a FROM source and a simple query specification, including when it is wrapped by an ordered SELECT. Recipe 17 demonstrates the set-operation boundary.

## 6. Rewrite matching literals throughout a tree

Change every integer literal equal to 7, including those in a nested query.

SqExpress C#:

<!-- ast-cookbook:recipe:06 -->
```csharp
var orders = new Orders("o");
var customers = new Customers("c");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(
        orders.TenantId == 7
        & Exists(SelectOne()
            .From(customers)
            .Where(customers.TenantId == 7)))
    .Done();
var replacements = 0;

var modified = original.SyntaxTree()
    .Modify<ExprInt32Literal>(literal =>
    {
        if (literal.Value != 7)
        {
            return literal;
        }

        replacements++;
        return literal.WithValue(8);
    });

Check(replacements == 2, "Expected both tenant literals.");
EmitPair("06", original, modified!);
```

Before (T-SQL):

<!-- ast-cookbook:sql:06-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=7
    AND
    EXISTS(
        SELECT
            1
        FROM [dbo].[Customers]
            [c]
        WHERE
            [c].[TenantId]=7
    )
```

After (T-SQL):

<!-- ast-cookbook:sql:06-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=8
    AND
    EXISTS(
        SELECT
            1
        FROM [dbo].[Customers]
            [c]
        WHERE
            [c].[TenantId]=8
    )
```

Typed modification includes all matching descendants and the root if it matches. A literal has no knowledge of its purpose: this would also replace a price or page size equal to 7. For application changes, match the containing predicate or a precisely identified column.

## 7. Replace a column by source and name

Change one projected field without renaming similarly named columns everywhere.

SqExpress C#:

<!-- ast-cookbook:recipe:07 -->
```csharp
var customers = new Customers("c");
var original = Select(customers.CustomerId, customers.Name)
    .From(customers)
    .Done();

var modified = original.SyntaxTree()
    .Modify<ExprColumn>(column =>
        column.Equals(customers.Name)
            ? customers.DisplayName
            : column);

EmitPair("07", original, modified!);
```

Before (T-SQL):

<!-- ast-cookbook:sql:07-before:tsql -->
```sql
SELECT
    [c].[CustomerId],
    [c].[Name]
FROM [dbo].[Customers]
    [c]
```

After (T-SQL):

<!-- ast-cookbook:sql:07-after:tsql -->
```sql
SELECT
    [c].[CustomerId],
    [c].[DisplayName]
FROM [dbo].[Customers]
    [c]
```

`ExprColumn.Equals` compares its source and column name. Matching `ExprColumnName` text alone can also rename columns on unrelated sources. Structural equality is still not SQL binding: repeated aliases in nested scopes can compare equal, as recipe 14 shows.

## 8. Adapt a predicate to a derived-table alias

Move a reusable predicate outside a derived table that projects the required columns.

SqExpress C#:

<!-- ast-cookbook:recipe:08 -->
```csharp
var orders = new Orders("o");
ExprBoolean predicate = orders.TenantId == 7 & orders.Status == "Paid";
var original = Select(orders.OrderId, orders.TenantId, orders.Status)
    .From(orders)
    .Where(predicate)
    .Done();
var derived = Select(orders.OrderId, orders.TenantId, orders.Status)
    .From(orders)
    .As(TableAlias("d"));

var adapted = (ExprBoolean)predicate.SyntaxTree()
    .Modify<ExprColumn>(column =>
        Equals(column.Source, orders.Alias)
            ? column.WithSource(derived.Alias)
            : column)!;
var modified = Select(derived.AllColumns())
    .From(derived)
    .Where(adapted)
    .Done();

EmitPair("08", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:08-before:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[TenantId],
    [o].[Status]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=7
    AND
    [o].[Status]='Paid'
```

After (T-SQL):

<!-- ast-cookbook:sql:08-after:tsql -->
```sql
SELECT
    [d].*
FROM (
    SELECT
        [o].[OrderId],
        [o].[TenantId],
        [o].[Status]
    FROM [dbo].[Orders]
        [o]
)
    [d]
WHERE
    [d].[TenantId]=7
    AND
    [d].[Status]='Paid'
```

Adapt the predicate fragment, not the complete query: rewriting every source reference in the complete tree could also change references inside the derived query. Every remapped column must be projected under the same name. This follows the `WithSource` composition pattern used in SqGoods.

## 9. Distinguish the root from descendants

Verify root behavior, including the rebuilt-root edge case in this checkout.

SqExpress C#:

<!-- ast-cookbook:recipe:09 -->
```csharp
var literal = new ExprInt32Literal(1);
var untouched = literal.SyntaxTree()
    .ModifyDescendants<ExprInt32Literal>(node => node.WithValue(2));
var replaced = literal.SyntaxTree()
    .Modify<ExprInt32Literal>(node => node.WithValue(2));

Check(ReferenceEquals(literal, untouched), "Leaf has no descendants.");
Check(((ExprInt32Literal)replaced!).Value == 2, "Root was not replaced.");

var orders = new Orders("o");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(orders.TenantId == 1)
    .Done();
var rootCallbacks = 0;
var modified = original.SyntaxTree()
    .ModifyDescendants(node =>
    {
        if (node is ExprInt32Literal value && value.Value == 1)
        {
            return value.WithValue(2);
        }

        if (node is ExprQuerySpecification)
        {
            rootCallbacks++;
        }

        return node;
    });

Check(rootCallbacks == 1, "Rebuilt-root behavior changed; review this recipe.");
EmitPair("09", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:09-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=1
```

After (T-SQL):

<!-- ast-cookbook:sql:09-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=2
```

**Current limitation:** `ModifyDescendants` excludes the original root by reference identity. When changed children cause a new root to be constructed, the callback can receive that rebuilt root. It retains the compile-time return type, so replacing that rebuilt root with another type can also fail. For precise boundaries, modify an explicitly selected child or fragment and put it back with `With...`, rather than relying on this method to exclude a reconstructed root.

## 10. Understand bottom-up callbacks and replacement visitation

Observe updated children in a parent callback and keep inserted expressions out of the current pass.

SqExpress C#:

<!-- ast-cookbook:recipe:10 -->
```csharp
var orders = new Orders("o");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(orders.TenantId == 1)
    .Done();
var parentSawUpdatedChild = false;
var literalCallbacks = 0;

var modified = original.SyntaxTree()
    .Modify(node =>
    {
        if (node is ExprInt32Literal literal)
        {
            literalCallbacks++;
            return literal.Value == 1 ? literal.WithValue(2) : literal;
        }

        if (node is ExprQuerySpecification query)
        {
            parentSawUpdatedChild = query.Where!.ToSql(TSqlExporter.Default).Contains("=2");
            return query.WithWhere(query.Where & (orders.OrderId > 1));
        }

        return node;
    });

Check(parentSawUpdatedChild, "Parent did not see the updated child.");
Check(literalCallbacks == 1, "Inserted literal was revisited.");
Check(modified!.ToSql(TSqlExporter.Default).Contains("[OrderId]>1"), "Inserted literal changed.");
EmitPair("10", original, modified!);
```

Before (T-SQL):

<!-- ast-cookbook:sql:10-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=1
```

After (T-SQL):

<!-- ast-cookbook:sql:10-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=2
    AND
    [o].[OrderId]>1
```

Children are processed before their parent. The replacement returned by a callback is not traversed again during that pass. A deliberate second pass would see the newly inserted predicate; ensure repeated passes converge and do not append the same predicate indefinitely.

## 11. Remove optional nodes and reject invalid replacements

Remove a whole WHERE predicate while checking the failure modes of required child properties.

SqExpress C#:

<!-- ast-cookbook:recipe:11 -->
```csharp
var orders = new Orders("o");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(orders.TenantId == 7)
    .Done();

var modified = original.SyntaxTree()
    .Modify<ExprBooleanEq>(_ => null);

ExpectSqExpressFailure(() => original.SyntaxTree()
    .Modify<ExprColumnName>(_ => null), "required column name");
ExpectSqExpressFailure(() => original.SyntaxTree()
    .Modify<ExprColumnName>(_ => new ExprInt32Literal(1)), "incompatible child type");
var removedRoot = original.SyntaxTree()
    .Modify<ExprQuerySpecification>(_ => null);

Check(removedRoot == null, "Root should be removable.");
EmitPair("11", original, modified!);
```

Before (T-SQL):

<!-- ast-cookbook:sql:11-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=7
```

After (T-SQL):

<!-- ast-cookbook:sql:11-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
```

`null` removes a nullable property such as WHERE; required children reject it. Replacements must implement the type required by their parent slot. Returning null for one operand of AND does not automatically simplify AND. Removing the root returns null, which must be handled before export. The caught exceptions here are runtime demonstrations; the C# itself compiles.

## 12. Remove collection items without emptying a required list

Drop a projection item and verify that removing every item is rejected.

SqExpress C#:

<!-- ast-cookbook:recipe:12 -->
```csharp
var orders = new Orders("o");
var original = Select(orders.OrderId, orders.Status)
    .From(orders)
    .Done();

var modified = original.SyntaxTree()
    .Modify<ExprColumn>(column =>
        column.Equals(orders.Status) ? null : column);

ExpectSqExpressFailure(() => original.SyntaxTree()
    .Modify<ExprColumn>(_ => null), "empty SELECT list");

EmitPair("12", original, modified!);
```

Before (T-SQL):

<!-- ast-cookbook:sql:12-before:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[Status]
FROM [dbo].[Orders]
    [o]
```

After (T-SQL):

<!-- ast-cookbook:sql:12-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
```

A null replacement removes a collection element. The SELECT list must retain at least one item; the modifier rejects an empty required collection. Optional collections have different rules. Transforming a projection also changes its result contract.

## 13. Expand a logical IN filter into correlated EXISTS

Translate a virtual relation field into a real relationship query, following the SqGoods pattern.

SqExpress C#:

<!-- ast-cookbook:recipe:13 -->
```csharp
var orders = new Orders("o");
var logicalCustomer = new ExprColumn(TableAlias("logical"), "CustomerRegion");
ExprBoolean logicalFilter = logicalCustomer.In("North", "West");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(logicalFilter)
    .Done();

var expanded = (ExprBoolean)logicalFilter.SyntaxTree()
    .Modify<ExprInValues>(predicate =>
    {
        if (predicate.TestExpression is not ExprColumn column
            || !column.Equals(logicalCustomer))
        {
            return predicate;
        }

        var customers = new Customers("relationCustomer");
        return Exists(SelectOne()
            .From(customers)
            .Where(
                customers.CustomerId == orders.CustomerId
                & customers.Region.In(predicate.Items)));
    })!;
var modified = original.WithWhere(expanded);

Check(!modified.ToSql(TSqlExporter.Default).Contains("[logical]"), "Logical field survived.");
EmitPair("13", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:13-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [logical].[CustomerRegion] IN('North','West')
```

After (T-SQL):

<!-- ast-cookbook:sql:13-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    EXISTS(
        SELECT
            1
        FROM [dbo].[Customers]
            [relationCustomer]
        WHERE
            [relationCustomer].[CustomerId]=[o].[CustomerId]
            AND
            [relationCustomer].[Region] IN('North','West')
    )
```

The before SQL is an intermediate logical representation: its `logical` source is intentionally unresolved and must not be executed. The completed rewrite introduces a local alias and correlates to the host order. Preserve the IN item list and distinguish IN from NOT IN; negation and SQL NULL semantics require separate design.

## 14. Respect nested scopes with repeated aliases

Change only the outer query when an inner query reuses the same alias and column names.

SqExpress C#:

<!-- ast-cookbook:recipe:14 -->
```csharp
var outer = new Orders("o");
var inner = new Orders("o");
var nested = SelectOne()
    .From(inner)
    .Where(inner.TenantId == 7)
    .Done();
var original = Select(outer.OrderId)
    .From(outer)
    .Where(outer.TenantId == 7 & new ExprExists(nested))
    .Done();

Check(outer.TenantId.Equals(inner.TenantId), "Expected structural equality.");
var unsafeRewrite = original.SyntaxTree()
    .Modify<ExprColumn>(column =>
        column.Equals(outer.TenantId) ? column.WithColumnName("OtherTenantId") : column);
Check(unsafeRewrite!.ToSql(TSqlExporter.Default).Split("OtherTenantId").Length - 1 == 2,
    "Expected both scopes to match.");

// Keep the nested query intact and reconstruct only the known outer predicate.
var modified = original.WithWhere(outer.TenantId == 8 & new ExprExists(nested));

Check(ReferenceEquals(original.From, modified.From), "FROM was replaced.");
Check(nested.ToSql(TSqlExporter.Default).Contains("=7"), "Nested scope changed.");
EmitPair("14", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:14-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=7
    AND
    EXISTS(
        SELECT
            1
        FROM [dbo].[Orders]
            [o]
        WHERE
            [o].[TenantId]=7
    )
```

After (T-SQL):

<!-- ast-cookbook:sql:14-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[TenantId]=8
    AND
    EXISTS(
        SELECT
            1
        FROM [dbo].[Orders]
            [o]
        WHERE
            [o].[TenantId]=7
    )
```

Aliases are local SQL names, not globally unique identities. Structural column equality does not identify a binding across scopes. For this known shape, reconstruct the outer predicate explicitly. General scope-aware rewriting requires tracking query boundaries and their sources; a name-only callback is insufficient.

## 15. Apply access predicates to a bounded query shape

Extend each single-table Orders specification, including specifications inside EXISTS, with a tenant predicate.

SqExpress C#:

<!-- ast-cookbook:recipe:15 -->
```csharp
var orders = new Orders("o");
var nestedOrders = new Orders("nestedOrder");
var original = Select(orders.OrderId)
    .From(orders)
    .Where(
        orders.Status == "Paid"
        & Exists(SelectOne()
            .From(nestedOrders)
            .Where(nestedOrders.Status == "Shipped")))
    .Done();
var specifications = 0;

var modified = original.SyntaxTree()
    .Modify<ExprQuerySpecification>(query =>
    {
        // This example accepts exactly one direct dbo.Orders source per SELECT.
        if (query.From is not ExprTable source
            || !source.FullName.Equals(orders.FullName)
            || source.Alias == null)
        {
            throw new InvalidOperationException("Unsupported access-filter query shape.");
        }

        var tenantColumn = new ExprColumn(source.Alias, "TenantId");
        var access = tenantColumn == 7;
        specifications++;
        return query.WithWhere(query.Where == null ? access : query.Where & access);
    });

Check(specifications == 2, "Expected both query scopes.");
Check(modified!.ToSql(TSqlExporter.Default).Split("[TenantId]=7").Length - 1 == 2,
    "Expected two access predicates.");
EmitPair("15", original, modified!);
```

Before (T-SQL):

<!-- ast-cookbook:sql:15-before:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Paid'
    AND
    EXISTS(
        SELECT
            1
        FROM [dbo].[Orders]
            [nestedOrder]
        WHERE
            [nestedOrder].[Status]='Shipped'
    )
```

After (T-SQL):

<!-- ast-cookbook:sql:15-after:tsql -->
```sql
SELECT
    [o].[OrderId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Paid'
    AND
    EXISTS(
        SELECT
            1
        FROM [dbo].[Orders]
            [nestedOrder]
        WHERE
            [nestedOrder].[Status]='Shipped'
            AND
            [nestedOrder].[TenantId]=7
    )
    AND
    [o].[TenantId]=7
```

This adapts SqDbAiAgent’s per-specification predicate extension into a deliberately bounded example. Each predicate uses the source alias from its own scope. It rejects joins, derived sources, CTE sources, and other direct tables. Production enforcement also needs an allowed grammar and schema, validated column bindings, and deliberate policy for every accepted query shape; successful export alone does not establish security. Apply this pass once, because it appends predicates on every run.

## 16. Preserve unmatched rows when filtering a LEFT JOIN

Restrict eligible right-hand rows in ON while retaining orders without an eligible customer.

SqExpress C#:

<!-- ast-cookbook:recipe:16 -->
```csharp
var orders = new Orders("o");
var customers = new Customers("c");
var original = Select(orders.OrderId, customers.Name)
    .From(orders)
    .LeftJoin(customers, orders.CustomerId == customers.CustomerId)
    .Done();
var join = (ExprJoinedTable)original.From!;

var modified = original.WithFrom(join.WithSearchCondition(
    join.SearchCondition & (customers.Region == "North")));
var differentMeaning = original.WithWhere(customers.Region == "North");

Check(modified.Where == null, "Right-side restriction moved to WHERE.");
Check(differentMeaning.Where != null, "Expected a WHERE restriction.");
EmitPair("16", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:16-before:tsql -->
```sql
SELECT
    [o].[OrderId],
    [c].[Name]
FROM [dbo].[Orders]
    [o]
LEFT JOIN [dbo].[Customers]
    [c] ON
    [o].[CustomerId]=[c].[CustomerId]
```

After (T-SQL):

<!-- ast-cookbook:sql:16-after:tsql -->
```sql
SELECT
    [o].[OrderId],
    [c].[Name]
FROM [dbo].[Orders]
    [o]
LEFT JOIN [dbo].[Customers]
    [c] ON
    [o].[CustomerId]=[c].[CustomerId]
    AND
    [c].[Region]='North'
```

A right-side restriction in ON controls matching and retains the left row with NULL customer columns. The same restriction in WHERE rejects those NULL-extended rows. These express different policies: choose deliberately, especially for access rules. This example demonstrates predicate placement, not a general tenant-security rule.

## 17. Rewrite set-operation branches and handle join boundaries

Apply a transformation to both UNION ALL inputs and join the combined result through a derived table.

SqExpress C#:

<!-- ast-cookbook:recipe:17 -->
```csharp
var orders = new Orders("o");
var customers = new Customers("c");
var original = Select(orders.OrderId, orders.CustomerId)
    .From(orders)
    .Where(orders.Status == "Paid")
    .UnionAll(Select(orders.OrderId, orders.CustomerId)
        .From(orders)
        .Where(orders.Status == "Shipped"))
    .Done();
var branchCount = 0;

var restricted = (IExprSubQuery)original.SyntaxTree()
    .Modify<ExprQuerySpecification>(query =>
    {
        branchCount++;
        return query.WithWhere(query.Where & (orders.TenantId == 7));
    })!;

var orderedSet = restricted.AddOrderBy(new ExprOrderByItem(
    new ExprColumn(null, "OrderId"), false));
ExpectSqExpressFailure(() => orderedSet.WithInnerJoin(
    customers, orders.CustomerId == customers.CustomerId), "join over a set operation");

var derived = new ExprDerivedTableQuery(restricted, TableAlias("combined"), null);
var customerId = new ExprColumn(derived.Alias, "CustomerId");
var modified = Select(derived.AllColumns(), customers.Name)
    .From(derived)
    .InnerJoin(customers, customerId == customers.CustomerId)
    .Done();

Check(branchCount == 2, "Expected both UNION ALL inputs.");
EmitPair("17", original, modified);
```

Before (T-SQL):

<!-- ast-cookbook:sql:17-before:tsql -->
```sql
SELECT
    [o].[OrderId],
    [o].[CustomerId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Paid'
UNION ALL
SELECT
    [o].[OrderId],
    [o].[CustomerId]
FROM [dbo].[Orders]
    [o]
WHERE
    [o].[Status]='Shipped'
```

After (T-SQL):

<!-- ast-cookbook:sql:17-after:tsql -->
```sql
SELECT
    [combined].*,
    [c].[Name]
FROM (
    SELECT
        [o].[OrderId],
        [o].[CustomerId]
    FROM [dbo].[Orders]
        [o]
    WHERE
        [o].[Status]='Paid'
        AND
        [o].[TenantId]=7
    UNION ALL
    SELECT
        [o].[OrderId],
        [o].[CustomerId]
    FROM [dbo].[Orders]
        [o]
    WHERE
        [o].[Status]='Shipped'
        AND
        [o].[TenantId]=7
)
    [combined]
JOIN [dbo].[Customers]
    [c] ON
    [combined].[CustomerId]=[c].[CustomerId]
```

A set-operation root is not an `ExprQuerySpecification`. Tree rewriting reaches its branches, but the direct join convenience on an ordered set rejects that shape. Wrap the set in a derived table before joining it. This adds the join once to the combined result; applying joins to individual branches would be a different transformation.

## 18. Modify a CTE definition through its containing query

Rewrite the underlying Orders filter while preserving the named CTE reference.

SqExpress C#:

<!-- ast-cookbook:recipe:18 -->
```csharp
var cte = new PaidOrders("p");
var original = Select(cte.OrderId)
    .From(cte)
    .Done();
var literalCount = 0;

var modified = original.SyntaxTree()
    .Modify<ExprInt32Literal>(literal =>
    {
        if (literal.Value != 7)
        {
            return literal;
        }

        literalCount++;
        return literal.WithValue(8);
    });

Check(literalCount == 1, "Expected the literal inside the CTE definition.");
Check(original.ToSql(TSqlExporter.Default).Contains("=7"), "Original CTE changed.");
Check(modified!.ToSql(TSqlExporter.Default).Contains("=8"), "CTE definition was not rewritten.");
EmitPair("18", original, modified!);
```

Before (T-SQL):

<!-- ast-cookbook:sql:18-before:tsql -->
```sql
WITH [PaidOrders] AS(
    SELECT
        [sourceOrder].[OrderId]
    FROM [dbo].[Orders]
        [sourceOrder]
    WHERE
        [sourceOrder].[Status]='Paid'
        AND
        [sourceOrder].[TenantId]=7
)
SELECT
    [p].[OrderId]
FROM [PaidOrders]
    [p]
```

After (T-SQL):

<!-- ast-cookbook:sql:18-after:tsql -->
```sql
WITH [PaidOrders] AS(
    SELECT
        [sourceOrder].[OrderId]
    FROM [dbo].[Orders]
        [sourceOrder]
    WHERE
        [sourceOrder].[Status]='Paid'
        AND
        [sourceOrder].[TenantId]=8
)
SELECT
    [p].[OrderId]
FROM [PaidOrders]
    [p]
```

Traversal enters the CTE definition, not just the reference visible in FROM. The rewritten tree can contain an AST CTE node rather than your original `CteBase` subclass; do not cast it back to `PaidOrders`. Recursive traversal has a name-based recursion guard; this nonrecursive example does not establish callback counts for repeated or recursive CTE references. Use distinct CTE names for distinct definitions.

## Troubleshooting and boundaries

- **Save the new AST.** Modification returns a result. Reusing the original variable without assigning the result exports the old query.
- **Know the root shape.** Simple SELECTs, ordered SELECTs, paged SELECTs, and set operations use different nodes. Inspect the node you actually have before calling a shape-specific helper.
- **Keep scopes explicit.** Renaming an alias or column globally does not resolve bindings. Transform a known fragment, track scopes deliberately, or reject unsupported shapes.
- **Preserve SQL meaning.** WHERE/ON placement, correlation, projection contracts, NULL behavior, row multiplication, grouping, and set-operation boundaries need application-level decisions.
- **Handle null results and exceptions.** Root removal is allowed. Required children, incompatible replacements, empty required collections, and unsupported convenience-method shapes fail at runtime.
- **Do not infer recursive behavior from one CTE.** Name-based recursion guards prevent infinite definition traversal; repeated references are not a promise of one callback per logical definition.
- **Keep passes deliberate.** Inserted nodes are not revisited immediately. Re-running an appending pass can duplicate predicates. Assertions should describe the transformation’s intended invariants.

The relationship-filter recipes are adapted from SqGoods `RelationFieldsBase.ReplaceFields` and `FilterProcessor.Prepare`; the access-filter recipe is inspired by SqDbAiAgent `HarborFlowSecurityFilter.ValidateQuery`. The examples here have their own shared schema and do not depend on those applications. For query construction, see the [SELECT cookbook](select_cookbook.md); for the public traversal introduction, see [Syntax Tree](../Readme.md#syntax-tree).

## Reproduce the verification

Save the following script as `verify-ast-cookbook.ps1` outside the repository and run it from the repository root. It extracts every C# block, creates a temporary .NET 8 project referencing this checkout and the analyzer, runs all assertions, exports both dialects, and compares every published SQL block. Only line endings are normalized. It leaves the temporary project for inspection and prints its location. No database is required, and no NuGet cache or CLI home overrides are used.

The optional `-TemporaryRoot` parameter selects a writable directory outside the repository. The script rejects a build location inside the checkout. No project is added to the solution.

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
$document = Join-Path $repo 'Documentation/ast_modification_cookbook.md'
$markdown = Get-Content -LiteralPath $document -Raw
$pattern = '(?ms)<!-- ast-cookbook:(?<kind>imports|types|recipe)(?::(?<id>\d+))? -->\r?\n```csharp\r?\n(?<code>.*?)\r?\n```'
$blocks = [regex]::Matches($markdown, $pattern)
$imports = @($blocks | Where-Object { $_.Groups['kind'].Value -eq 'imports' })
$recipes = @($blocks | Where-Object { $_.Groups['kind'].Value -eq 'recipe' })
$types = @($blocks | Where-Object { $_.Groups['kind'].Value -eq 'types' })
if ($imports.Count -ne 1 -or $recipes.Count -ne 18 -or $types.Count -ne 2) {
    throw 'Expected imports, 18 recipe blocks, and two supporting type blocks.'
}
$methods = foreach ($block in $recipes) {
    'void Recipe' + $block.Groups['id'].Value + "()`n{`n" + $block.Groups['code'].Value + "`n}"
}
$calls = foreach ($block in $recipes) { 'Recipe' + $block.Groups['id'].Value + '();' }
$source = $imports[0].Groups['code'].Value + "`n`n" + ($methods -join "`n`n") +
    "`n`n" + ($calls -join "`n") + "`n`n" +
    (($types | ForEach-Object { $_.Groups['code'].Value }) -join "`n`n")
$temporaryBase = [IO.Path]::GetFullPath($TemporaryRoot)
$repoPrefix = [IO.Path]::GetFullPath($repo).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
if (($temporaryBase.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar).StartsWith(
    $repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Choose a temporary directory outside the repository.'
}
$csharpBlocks = [regex]::Matches($markdown, '(?m)^```csharp\r?$')
if ($csharpBlocks.Count -ne $blocks.Count) { throw 'Unmarked C# block found.' }
$work = Join-Path $TemporaryRoot ('SqExpress.AstCookbook.' + [guid]::NewGuid().ToString('N'))
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
$projectPath = Join-Path $work 'AstCookbookVerification.csproj'
Set-Content -LiteralPath $projectPath -Value $project -Encoding utf8
# A single build worker and an in-process compiler also work in isolated shells.
dotnet build $projectPath -m:1 -nr:false -p:UseSharedCompilation=false --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Cookbook compilation failed.' }
Push-Location $work
try {
    dotnet (Join-Path $work 'bin/Debug/net8.0/AstCookbookVerification.dll')
    if ($LASTEXITCODE -ne 0) { throw 'Cookbook export failed.' }
    $sqlPattern = '(?ms)<!-- ast-cookbook:sql:(?<id>[\w-]+):(?<dialect>tsql|pgsql) -->\r?\n```sql\r?\n(?<sql>.*?)\r?\n```'
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
    if ($exports.Count -ne 72 -or $sqlBlocks.Count -ne 36) { throw 'Unexpected output count.' }
    Write-Host "Verified 18 recipes, 72 exports, and all $($sqlBlocks.Count) published SQL blocks."
    Write-Host "Temporary project: $work"
}
finally { Pop-Location }
```
