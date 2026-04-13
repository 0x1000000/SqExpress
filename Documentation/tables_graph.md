# TablesGraph

`TablesGraph` builds a foreign-key navigation graph from a set of table descriptors.

It is useful when you need to:

- inspect descriptor relationships
- find direct and transitive references
- discover a join path between two tables
- build features such as automatic security filters

## Creating a Graph

If your table descriptors contain foreign keys, create a graph like this:

```cs
var graph = TablesGraph.Create(AllTables.BuildAllAliasedTableList());
```

`TablesGraph.Create(...)` accepts any list of table descriptors.

If you generated your descriptors with the `Gen-Tables` tool, `AllTables.BuildAllAliasedTableList()` is usually the best choice because it gives the graph a ready-to-use set of descriptors with automatic aliases.

`TablesGraph` treats foreign keys as table references:

- referenced table = foreign-key target
- referenced-by table = table containing the foreign key

The graph models the real foreign-key structure:

- a table can reference many tables
- a table can be referenced by many tables
- self-references are stored but hidden by default
- cycles between different tables are rejected during graph creation

## Identity Rules

Table identity is based on full table name, not object reference.

That means:

- another `ExprTable` instance with the same full name is treated as the same table
- `Contains(...)` and `References(...)` return `false` for tables outside the graph
- `GetReferences(...)`, `GetAllReferences(...)`, `GetReferencedBy(...)`, and `GetAllReferencedBy(...)` throw if the table does not belong to the graph

To resolve an input table to the canonical descriptor stored in the graph:

```cs
if (graph.TryGetTable(tCustomer, out var canonicalTable))
{
    Console.WriteLine(canonicalTable.GetType().Name);
}
```

## Basic Navigation

```cs
var tCustomer = new TableCustomer();
var tCompany = new TableCompany();

bool isInGraph = graph.Contains(tCustomer);
bool referencesCompany = graph.References(tCustomer, tCompany);

var directReferences = graph.GetReferences(tCustomer);
var allReferences = graph.GetAllReferences(tCustomer).ToArray();
var referencedBy = graph.GetReferencedBy(tCompany);
var allReferencedBy = graph.GetAllReferencedBy(tCompany).ToArray();
```

Direct methods return only immediate neighbors:

- `GetReferences(table)`
- `GetReferencedBy(table)`

Recursive methods walk the graph transitively:

- `GetAllReferences(table)`
- `GetAllReferencedBy(table)`

## Self-References

Self-referencing foreign keys are supported, but hidden by default.

That means:

- `GetReferences(table)` does not include `table` for a self-FK
- `GetReferencedBy(table)` does not include `table` for a self-FK
- `References(table, table)` returns `false`

If you want to include self-references explicitly:

```cs
var selfRefs = graph.GetReferences(tCategory, includeSelfRef: true);
var selfReferencedBy = graph.GetReferencedBy(tCategory, includeSelfRef: true);
var hasSelfReference = graph.References(tCategory, tCategory, includeSelfRef: true);
```

## Building Join Paths

`TablesGraph` can build a joinable table source for two tables connected by foreign keys:

```cs
var tCustomer = new TableCustomer("C");
var tUser = new TableUser("U");

if (graph.TryToJoinTables(tCustomer, tUser, out var joinedSource))
{
    var query = Select(AllColumns())
        .From(joinedSource)
        .Done();
}
```

`TryToJoinTables(...)` returns:

- `false` if either table is outside the graph
- `false` if no foreign-key path exists between the tables
- `true` with a joined `IExprTableSource` when a path can be built

Important behavior:

- the returned value is a table source, not a complete `SELECT`
- the exact `table1` and `table2` objects you pass are preserved at the endpoints
- intermediate canonical tables are copied with auto-generated aliases

## Choosing a Path

When several paths exist, `TablesGraph` chooses:

1. the shortest path
2. if several shortest paths exist, the first one discovered from the original table/reference order

You can also force the path through required intermediate tables:

```cs
if (graph.TryToJoinTables(
        tOrder,
        tCountry,
        new ExprTable[] { tCustomer, tAddress },
        out var joinedSource))
{
    var query = Select(AllColumns())
        .From(joinedSource)
        .Done();
}
```

The intermediate list is:

- ordered
- mandatory
- interpreted as checkpoints in the final path

So the resulting path must be:

- `table1 -> intermediate[0] -> intermediate[1] -> ... -> table2`

If any checkpoint cannot be reached, the method returns `false`.

## When to Use It

`TablesGraph` is especially useful for:

- automatic security filters
- dynamic join discovery
- schema-aware query builders
- reasoning about descriptor relationships in tooling or analyzers
