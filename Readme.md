# SqExpress

![Logo](https://github.com/0x1000000/SqExpress/blob/main/SqExpress/Icon.png)

The library provides a generic SQL syntax tree with export to MS T-SQL, PostgreSQL, and MySQL text. It includes polyfills to compensate for features lacking in certain databases, such as the "MERGE" command. It also provides a set of builders and operators that will help you build complex SQL expressions.

It does not use LINQ, and your C# code will be as close to real SQL as possible. This makes it ideal when you need full SQL flexibility to create efficient DB requests.

SqExpress comes with a simple but efficient data access mechanism that wraps ADO.Net DbConnection and can be used with MS SQL Client, Npgsql, or MySQL Connector.

You can use SqExpress together with the "Code First" concept when you declare SQL tables as C# classes with the possibility to generate recreation scripts for a target platform (MS SQL or PostgreSQL or MySQL).

You can also use it in conjunction with the "Database First" concept using an included code modification utility. The utility can also be used to generate flexible DTO classes with all required database mappings.

# Content

### Resources

1. [Video Tutorials](#video-tutorials)
2. [Articles](#articles)
3. [T-SQL to SqExpress Transpiler (Web Tool) **NEW!!!**](#t-sql-to-sqexpress-transpiler-web-tool)
4. [Demo Application](#demo-application)

### Intro

1. [Get Started](#get-started)
2. [When to Use SqExpress (and When Not)](#when-to-use-sqexpress-and-when-not)

### Basics

1. [Creating Table Descriptors](#creating-table-descriptors)
2. [Inserting Data](#inserting-data)
3. [Selecting Data](#selecting-data)
4. [Updating Data](#updating-data)
5. [Deleting Data](#deleting-data)
6. [More Tables and foreign keys](#more-tables-and-foreign-keys)
7. [Data Selection](#data-selection)
8. [Joining Tables](#joining-tables)
9. [Aliasing](#aliasing)
10. [Derived Tables](#derived-tables)
11. [Subqueries](#subqueries)
12. [CTE](#cte)
13. [Analytic And Window Functions](#analytic-and-window-functions)
14. [Set Operators](#set-operators)

### Advanced Data Modification

1. [Merge](#merge)
2. [Temporary Tables](#temporary-tables)
3. [Database Data Export/Import](#database-data-export-import)
4. [Getting and Comparing Database Table Metadata](#getting-and-comparing-database-table-metadata)

### Database Table Metadata

1. [Retrieving Database Table Metadata](#retrieving-database-table-metadata)

### Working with Expressions

1. [Syntax Tree](#syntax-tree) (Traversal and Modification)
2. [Serialization to XML](#serialization-to-xml)
3. [Serialization to JSON](#serialization-to-json)
4. [Serialization to Plain List](#serialization-to-plain-list)

### Code-generation

1. [Table Descriptors Scaffolding](#table-descriptors-scaffolding)
2. [DTOs Scaffolding](#dtos-scaffolding)
3. [Model Selection](#model-selection)

### Usage

1. [Using in ASP.NET](#using-in-aspnet)
2. [PostgreSQL](#postgresql)
3. [MySQL](#mysql)
4. [AutoMapper](#automapper)

---
---


# Video Tutorials

1. [Basics of SqExpress](https://www.youtube.com/watch?v=Zd-fCb8NimA)
2. [Working with Database Metadata Using SqExpress](https://youtu.be/vGVpTCt4aqc?si=AWK8GzvoiVlX7vET)

# Articles

1. ["Syntax Tree and Alternative to LINQ in Interaction with SQL Databases"](https://itnext.io/syntax-tree-and-alternative-to-linq-in-interaction-with-sql-databases-656b78fe00dc?source=friends_link&sk=f5f0587c08166d8824b96b48fe2cf33c) - explains the library principles;
2. ["Filtering by Dynamic Attributes"](https://itnext.io/filtering-by-dynamic-attributes-90ada3504361?source=friends_link&sk=35e273a9f499e6b62bacbac75873a7d2) - shows how to create dynamic queries using the library.

# T-SQL to SqExpress Transpiler (Web Tool)

SqExpress includes a dedicated transpiler tool that converts T-SQL into ready-to-use SqExpress C# code:

- **Open the tool**: [https://0x1000000.github.io/SqExpress/](https://0x1000000.github.io/SqExpress/)

Why this tool is important:

- It significantly reduces migration time from raw SQL or old stored-query code to SqExpress.
- It generates both query code and declaration code (table descriptors and generated query helpers) so users can start from working scaffolding instead of a blank file.
- It helps teams keep SQL-first workflows while still getting strongly typed C# output.

How to use it effectively:

- Paste T-SQL into the editor.
- Review generated query code and descriptor code.
- Adjust naming/options (descriptor prefix/suffix, default schema, static builder usage) from the option bar.
- For best type accuracy, pair transpiled output with the SqExpress table descriptor scaffolding workflow.

The tool runs entirely in the browser (WebAssembly). Your SQL is processed client-side.

# Demo Application

You can find a realistic usage of the library in this ASP.NET demo application - [SqGoods](https://github.com/0x1000000/SqGoods)

# Get Started

Add a reference to [the library package on Nuget.org](https://www.nuget.org/packages/SqExpress/):

```
Install-Package SqExpress
```

and start with "Hello World":

```cs
static void Main()
{
    var query = SqQueryBuilder.Select("Hello World!").Done();

    Console.WriteLine(TSqlExporter.Default.ToSql(query));
}
```

Now let's get rid of the necessity in writing **"SqQueryBuilder."**:

```cs
using static SqExpress.SqQueryBuilder;
...
    var query = /*SqQueryBuilder.*/Select("Hello World!").Done();

    Console.WriteLine(TSqlExporter.Default.ToSql(query));

```

The result will be:

```
SELECT 'Hello World!'
```

## When to Use SqExpress (and When Not)

If your project is SQL-heavy, SqExpress is usually the strongest choice.

Use SqExpress when:

- SQL is core to your business logic, not just persistence plumbing.
- You need complex queries (CTE, window functions, set operations, MERGE-style workflows).
- You want code to stay close to SQL while still getting strong typing and IntelliSense.
- You want table descriptors, metadata comparison, and code generation in one workflow.
- You need one query model that can export to MS SQL, PostgreSQL, and MySQL.

SqExpress shines the most in two areas:

1. **Reports and analytics**: dynamic SQL generation with access to native SQL features (analytic/window functions, CTEs, set operators, db-specific functions).
2. **Mass updates/upserts**: large set-based data modification with `MERGE` semantics, including cross-dialect polyfills where native `MERGE` is unavailable.

### SqExpress vs Entity Framework (EF Core)

Choose **SqExpress** when:

- You treat the database as a relational engine and optimize for set-based queries.
- You care more about precise SQL shape than ORM abstractions.
- You want full control over joins, projections, and query patterns.
- Your bottlenecks are in query quality and SQL execution plans.
- You need reporting queries that rely on native analytic/window functions and db-specific SQL features.
- You need large set-based write workflows (upsert/sync) and dynamic SQL composition as first-class capabilities.
- You need more flexibility than EF Core batch APIs for complex set-based writes (`UpdateData`, `MergeDataInto`, custom mappings, sync/upsert flows).

Choose **EF Core** when:

- You model your domain mainly as object graphs and treat the database primarily as persistence storage.
- Your app is mostly CRUD with rich domain graph tracking.
- You want unit-of-work/change tracking as the primary model.
- Your set-based writes are relatively simple and are well covered by `ExecuteUpdate` / `ExecuteDelete`.

### SqExpress vs SqlKata

Choose **SqExpress** when:

- You want a strongly typed SQL model (tables/columns as C# types), not mostly string-based query composition.
- You need expression tree traversal/modification and deeper SQL tooling.
- You want long-term maintainability in large SQL codebases.
- You need robust dynamic query scenarios and `MERGE`-oriented update pipelines.

Choose **SqlKata** when:

- You prefer a lightweight fluent builder with minimal upfront structure.

### SqExpress vs Dapper

Choose **SqExpress** when:

- You want SQL power without maintaining large amounts of raw SQL strings.
- You want compile-time help for schema-level refactoring.
- You want higher-level SQL composition, metadata tooling, and codegen.
- You need expressive set-based update/upsert logic and reusable dynamic query builders.

Choose **Dapper** when:

- You prefer manual SQL strings and the thinnest possible data access layer.

### SqExpress vs linq2db

Choose **SqExpress** when:

- Your team thinks in SQL first and prefers SQL-like C# over LINQ translation.
- You want explicit SQL control and predictable output for every query.
- You need to build/transform dynamic SQL expression trees and run large set-based data modifications.

Choose **linq2db** when:

- Your team prefers LINQ as the primary way of writing queries.

### When Not to Use SqExpress

SqExpress is not the best default if:

- Your project is mostly simple CRUD and low SQL complexity.
- Your team is not comfortable owning SQL design decisions.
- You primarily want full ORM behavior (tracked entities, relationship graph lifecycle, etc.).
- You treat the database mainly as an object store, rather than a relational engine for set-based facts and queries.

### Rule of Thumb

If SQL is strategic in your system, SqExpress gives you the most leverage.  
If SQL is incidental, a higher-level ORM can be simpler.

## Creating Table Descriptors

Ok, let's try to select some data from a real table, but first we need to describe the table:

*Note: Such classes can be auto-generated (updated) using information from an existing database. [See "Table Descriptors Scaffolding"](#table-descriptors-scaffolding)*

```Cs
public class TableUser : TableBase
{
    public readonly Int32TableColumn UserId;
    public readonly StringTableColumn FirstName;
    public readonly StringTableColumn LastName;
    //Audit Columns
    public readonly Int32TableColumn Version;
    public readonly DateTimeTableColumn ModifiedAt;

    public TableUser(): this(default){}

    public TableUser(Alias alias) : base("dbo", "User", alias)
    {
        this.UserId = this.CreateInt32Column("UserId", 
            ColumnMeta.PrimaryKey().Identity());

        this.FirstName = this.CreateStringColumn("FirstName", 
            size: 255, isUnicode: true);

        this.LastName = this.CreateStringColumn("LastName", 
            size: 255, isUnicode: true);

        this.Version = this.CreateInt32Column("Version",
            ColumnMeta.DefaultValue(0));

        this.ModifiedAt = this.CreateDateTimeColumn("ModifiedAt",
            columnMeta: ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));

        //Indexes
        this.AddIndex(this.FirstName);
        this.AddIndex(this.LastName);
    }
}
```

and if the table does not exist let's create it:

```cs
static async Task Main()
{
    using var connection = new SqlConnection("connection_string");
    {
        using (var database = new SqDatabase<SqlConnection>(
            connection: connection,
            commandFactory: (conn, sql) 
                => new SqlCommand(cmdText: sql, connection: conn),
            sqlExporter: TSqlExporter.Default))
        {
            var tUser = new TableUser();

            await database.Statement(tUser.Script.DropAndCreate());
        }
    }
}
```

*Note: See [PostgreSQL](#postgresql) or [MySQL](#mysql)* sections if you need to work with these databases.

*Actual T-SQL:*

```sql

IF EXISTS
(
    SELECT TOP 1 1 
    FROM [INFORMATION_SCHEMA].[TABLES] 
    WHERE [TABLE_SCHEMA]='dbo' AND [TABLE_NAME]='User'
) 
    DROP TABLE [dbo].[User];

CREATE TABLE [dbo].[User]
(
    [UserId] int NOT NULL  IDENTITY (1, 1),
    [FirstName] [nvarchar](255) NOT NULL,
    [LastName] [nvarchar](255) NOT NULL,
    [Version] int NOT NULL,
    [ModifiedAt] datetime NOT NULL,
    CONSTRAINT [PK_dbo_User] PRIMARY KEY ([UserId])
);
```

## Inserting Data

Now it is time to insert some data into the table:

```cs
...
var data = new[]
{
    new {FirstName = "Francois", LastName = "Sturman"},
    new {FirstName = "Allina", LastName = "Freeborne"},
    new {FirstName = "Maye", LastName = "Maloy"},
};

await /*SqQueryBuilder.*/InsertDataInto(tUser, data)
    .MapData(s => s
        .Set(s.Target.FirstName, s.Source.FirstName)
        .Set(s.Target.LastName, s.Source.LastName))
    .AlsoInsert(s => s
        .Set(s.Target.Version, 1)
        .Set(s.Target.ModifiedAt, GetUtcDate()))
    .Exec(database);
...
```

*Actual T-SQL:*

```sql
INSERT INTO [dbo].[User]([FirstName],[LastName],[Version],[ModifiedAt]) 
SELECT [FirstName],[LastName],1,GETUTCDATE() 
FROM 
(VALUES 
    ('Francois','Sturman'),
    ('Allina','Freeborne')
    ,('Maye','Maloy')
)[A0]([FirstName],[LastName])
```

## Selecting data

and select it:

```cs
var selectResult = await /*SqQueryBuilder.*/Select(tUser.UserId, tUser.FirstName, tUser.LastName)
    .From(tUser)
    .OrderBy(tUser.FirstName, tUser.LastName)
    .QueryList(database,
        r => (
            Id: tUser.UserId.Read(r),
            FirstName: tUser.FirstName.Read(r),
            LastName: tUser.LastName.Read(r)));
foreach (var record in selectResult)
{
    Console.WriteLine(record);
}
```

*Actual T-SQL:*

```sql
SELECT [A0].[UserId],[A0].[FirstName],[A0].[LastName] 
FROM [dbo].[User] [A0] 
ORDER BY [A0].[FirstName],[A0].[LastName]
```

*Result:*

```
(2, Allina, Freeborne)
(1, Francois, Sturman)
(3, Maye, Maloy)
```

## Updating data

Now let's fix the typo:

```cs
await /*SqQueryBuilder.*/Update(tUser)
    .Set(tUser.LastName, "Malloy")
    .Set(tUser.Version, tUser.Version+1)
    .Set(tUser.ModifiedAt, GetUtcDate())
    .Where(tUser.LastName == "Maloy")
    .Exec(database);

//Writing to console without storing data in memory
await /*SqQueryBuilder.*/Select(tUser.Columns)
    .From(tUser)
    .Query(database, record=>
    {
        Console.Write(tUser.UserId.Read(record) + ",");
        Console.Write(tUser.FirstName.Read(record) + " ");
        Console.Write(tUser.LastName.Read(record) + ",");
        Console.Write(tUser.Version.Read(record) + ",");
        Console.WriteLine(tUser.ModifiedAt.Read(record).ToString("s"));
        return agg;
    });
```

*Actual T-SQL:*

```sql
UPDATE [A0] SET 
    [A0].[LastName]='Malloy',
    [A0].[Version]=[A0].[Version]+1,
    [A0].[ModifiedAt]=GETUTCDATE() 
FROM [dbo].[User] [A0] 
WHERE [A0].[LastName]='Maloy'```
```

*Result:*

```
1,Francois Sturman,1,2020-10-12T11:32:16
2,Allina Freeborne,1,2020-10-12T11:32:16
3,Maye Malloy,2,2020-10-12T11:32:17
```

*Note: In addition to **Update** the library also has **Insert** and **IdentityInsert** helpers (see [Database Data Export/Import](#database-data-export-import) to find an example)*

## Deleting data

Unfortunately, regardless the fact the typo is fixed, we have to say "Good Bye" to May*:

```cs
await /*SqQueryBuilder.*/Delete(tUser)
    .Where(tUser.FirstName.Like("May%"))
    .Output(tUser.UserId)
    .Query(database, (record)=>
    {
        Console.WriteLine("Removed user id: " + tUser.UserId.Read(record));
    });
```

*Actual T-SQL:*

```sql
DELETE [A0] 
OUTPUT DELETED.[UserId] 
FROM [dbo].[User] [A0] 
WHERE [A0].[FirstName] LIKE 'May%'
```

*Result:*

```
Removed user id: 3
```

## More Tables and foreign keys

To create more complex queries we need more than one table. Let's add a couple more:

*dbo.Company*

```cs
public class TableCompany : TableBase
{
    public readonly Int32TableColumn CompanyId;
    public readonly StringTableColumn CompanyName;

    //Audit Columns
    public readonly Int32TableColumn Version;
    public readonly DateTimeTableColumn ModifiedAt;

    public TableCompany() : this(default) { }

    public TableCompany(Alias alias) : base("dbo", "Company", alias)
    {
        this.CompanyId = this.CreateInt32Column(
            nameof(this.CompanyId), ColumnMeta.PrimaryKey().Identity());

        this.CompanyName = this.CreateStringColumn(
            nameof(this.CompanyName), 250);

        this.Version = this.CreateInt32Column("Version",
            ColumnMeta.DefaultValue(0));

        this.ModifiedAt = this.CreateDateTimeColumn("ModifiedAt",
            columnMeta: ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));
    }
}
```

*dbo.Customer*

```cs
public class TableCustomer : TableBase
{
    public Int32TableColumn CustomerId { get; }
    public NullableInt32TableColumn UserId { get; }
    public NullableInt32TableColumn CompanyId { get; }

    public TableCustomer() : this(default) { }

    public TableCustomer(Alias alias) : base("dbo", "Customer", alias)
    {
        this.CustomerId = this.CreateInt32Column(
            nameof(this.CustomerId), ColumnMeta.PrimaryKey().Identity());

        this.UserId = this.CreateNullableInt32Column(
            nameof(this.UserId), 
            ColumnMeta.ForeignKey<TableUser>(u => u.UserId));

        this.CompanyId = this.CreateNullableInt32Column(
            nameof(this.CompanyId), 
            ColumnMeta.ForeignKey<TableCompany>(u => u.CompanyId));

        //Indexes            
        this.AddUniqueIndex(this.UserId, this.CompanyId);
        this.AddUniqueIndex(this.CompanyId, this.UserId);
    }
}
```

Pay attention to the way how the foreign keys are defined:

```cs
ColumnMeta.ForeignKey<TableUser>(u => u.UserId)
```

And indexes:

```cs
this.AddUniqueIndex(this.UserId, this.CompanyId);
this.AddUniqueIndex(this.CompanyId, this.UserId);
```

Since now we have the foreign keys we have to delete and create the table in the specific order:

```cs
var tables = new TableBase[]{ new TableUser() , new TableCompany(), new TableCustomer() };

foreach (var table in tables.Reverse())
{
    await database.Statement(table.Script.DropIfExist());
}
foreach (var table in tables)
{
    await database.Statement(table.Script.Create());
}
```

Now we can insert some companies:

```cs
var tCompany = new TableCompany();

Console.WriteLine("Companies:");

await /*SqQueryBuilder.*/InsertDataInto(tCompany, new[] {"Microsoft", "Google"})
    .MapData(s => s.Set(s.Target.CompanyName, s.Source))
    .AlsoInsert(s => s
        .Set(s.Target.Version, 1)
        .Set(s.Target.ModifiedAt, GetUtcDate()))
    .Output(tCompany.CompanyId, tCompany.CompanyName)
    .Query(database, (r) =>
    {
        Console.WriteLine($"Id: {tCompany.CompanyId.Read(r)}, Name: {tCompany.CompanyName.Read(r)}");
    });
```

and create "Customers":

```cs
var tUser = new TableUser();
var tCompany = new TableCompany();
var tCustomer = new TableCustomer();
var tSubCustomer = new TableCustomer();

//Users
await /*SqQueryBuilder.*/InsertInto(tCustomer, tCustomer.UserId)
    .From(
        Select(tUser.UserId)
            .From(tUser)
            .Where(!Exists(
                SelectOne()
                    .From(tSubCustomer)
                    .Where(tSubCustomer.UserId == tUser.UserId))))
    .Exec(database);

//Companies
await InsertInto(tCustomer, tCustomer.CompanyId)
    .From(
        Select(tCompany.CompanyId)
            .From(tCompany)
            .Where(!Exists(
                SelectOne()
                    .From(tSubCustomer)
                    .Where(tSubCustomer.CompanyId == tCompany.CompanyId))))
    .Exec(database);
```

*Actual T-SQL:*

```sql
INSERT INTO [dbo].[Customer]([UserId]) 
SELECT [A0].[UserId] 
FROM [dbo].[User] [A0] WHERE NOT EXISTS
(
    SELECT 1 
    FROM [dbo].[Customer] [A1] 
    WHERE [A1].[UserId]=[A0].[UserId]
)

INSERT INTO [dbo].[Customer]([CompanyId]) 
SELECT [A0].[CompanyId] 
FROM [dbo].[Company] [A0] 
WHERE NOT EXISTS(
    SELECT 1 FROM [dbo].[Customer] [A1] 
    WHERE [A1].[CompanyId]=[A0].[CompanyId]
)
```

_Note: SqExpress actively uses operator overloads. Therefore, operators >, >=, <, <=, ==, &, |, !, /, +, -, *, and % are overloaded when applied to SqExpress syntax nodes, resulting in new syntax nodes.*

# Data Selection

This chapter moves from straightforward reads to the query patterns that usually define real production workloads.
Here, SqExpress shows its full power: composable joins, derived tables, subqueries, CTEs, analytic functions, and set operators.
The section walks step by step through `Joining Tables`, `Aliasing`, `Derived Tables`, `Subqueries`, `CTE`, `Analytic And Window Functions`, and `Set Operators`.
Each example is designed to keep intent obvious in C# while preserving tight control over the resulting SQL shape.
Use these patterns when query complexity, performance tuning, and long-term maintainability matter.

## Joining Tables

Now we can Join all the tables:

```cs
var tUser = new TableUser();
var tCompany = new TableCompany();
var tCustomer = new TableCustomer();

var cType = CustomColumnFactory.Int16("Type");
var cName = CustomColumnFactory.String("Name");

var customers = await /*SqQueryBuilder.*/Select(
        tCustomer.CustomerId,
        /*SqQueryBuilder.*/Case()
            .When(IsNotNull(tUser.UserId))
            .Then(Cast(Literal(1), SqlType.Int16))
            .When(IsNotNull(tCompany.CompanyId))
            .Then(Cast(Literal(2), SqlType.Int16))
            .Else(Null)
            .As(cType),
        /*SqQueryBuilder.*/Case()
            .When(IsNotNull(tUser.UserId))
            .Then(tUser.FirstName + " " + tUser.LastName)
            .When(IsNotNull(tCompany.CompanyId))
            .Then(tCompany.CompanyName)
            .Else(Null)
            .As(cName)
    )
    .From(tCustomer)
    .LeftJoin(tUser, on: tUser.UserId == tCustomer.UserId)
    .LeftJoin(tCompany, on: tCompany.CompanyId == tCustomer.CompanyId)
    .QueryList(database,
        r => (Id: tCustomer.CustomerId.Read(r), CustomerType: cType.Read(r), Name: cName.Read(r)));

foreach (var customer in customers)
{
    Console.WriteLine($"Id: {customer.Id}, Name: {customer.Name}, Type: {customer.CustomerType}");
}
```

*Actual T-SQL:*

```sql
SELECT 
    [A0].[CustomerId],
    CASE 
        WHEN [A1].[UserId] IS NOT NULL 
        THEN CAST(1 AS smallint) 
        WHEN [A2].[CompanyId] IS NOT NULL 
        THEN CAST(2 AS smallint) 
        ELSE NULL END 
    [Type],
    CASE 
        WHEN [A1].[UserId] IS NOT NULL 
        THEN [A1].[FirstName]+' '+[A1].[LastName] 
        WHEN [A2].[CompanyId] IS NOT NULL 
        THEN [A2].[CompanyName] 
        ELSE NULL END 
    [Name] 
FROM [dbo].[Customer] [A0] 
LEFT JOIN [dbo].[User] [A1] 
    ON [A1].[UserId]=[A0].[UserId] 
LEFT JOIN [dbo].[Company] [A2] 
    ON [A2].[CompanyId]=[A0].[CompanyId]
```

*Result:*

```
Id: 1, Name: Francois Sturman, Type: 1
Id: 2, Name: Allina Freeborne, Type: 1
Id: 3, Name: Microsoft, Type: 2
Id: 4, Name: Google, Type: 2
```

## Aliasing

Every time you create a table object, it is associated by default with an alias that will be used wherever you refer to the table. Each new instance will use a new alias. However you can explicitly specify your own alias or omit it:

```cs
var tUser = new User("USR");
var tUserNoAlias = new User(Alias.Empty);

Select(tUser.UserId).From(tUser);
Select(tUserNoAlias.UserId).From(tUserNoAlias);
```

*Actual T-SQL:*

```sql
--var tUser = new User("USR");
SELECT [USR].[UserId] FROM [dbo].[user] [USR]

--var tUserNoAlias = new User(Alias.Empty);
SELECT [UserId] FROM [dbo].[user]
```

## Derived Tables

The previous query is quite complex so it makes sense to store it as a derived table and reuse it in future:

```cs
public class DerivedTableCustomer : DerivedTableBase
{
    public readonly Int32CustomColumn CustomerId;

    public readonly Int16CustomColumn Type;

    public readonly StringCustomColumn Name;

    public DerivedTableCustomer(Alias alias = default) : base(alias)
    {
        this.CustomerId = this.CreateInt32Column("CustomerId");
        this.Type = this.CreateInt16Column("Type");
        this.Name = this.CreateStringColumn("Name");
    }

    protected override IExprSubQuery CreateQuery()
    {
        var tUser = new TableUser();
        var tCompany = new TableCompany();
        var tCustomer = new TableCustomer();

        return /*SqQueryBuilder.*/Select(
                tCustomer.CustomerId.As(this.CustomerId),
                /*SqQueryBuilder.*/Case()
                    .When(IsNotNull(tUser.UserId))
                    .Then(Cast(Literal(1), SqlType.Int16))
                    .When(IsNotNull(tCompany.CompanyId))
                    .Then(Cast(Literal(2), SqlType.Int16))
                    .Else(Null)
                    .As(this.Type),
                /*SqQueryBuilder.*/Case()
                    .When(IsNotNull(tUser.UserId))
                    .Then(tUser.FirstName + " " + tUser.LastName)
                    .When(IsNotNull(tCompany.CompanyId))
                    .Then(tCompany.CompanyName)
                    .Else(Null)
                    .As(this.Name)
            )
            .From(tCustomer)
            .LeftJoin(tUser, on: tUser.UserId == tCustomer.UserId)
            .LeftJoin(tCompany, on: tCompany.CompanyId == tCustomer.CompanyId)
            .Done();
    }
}
```

and this is how it can be reused:

```cs
var tCustomer = new DerivedTableCustomer("CUST");

var customers = await /*SqQueryBuilder.*/Select(tCustomer.Columns)
    .From(tCustomer)
    .Where(tCustomer.Type == 2 | tCustomer.Name.Like("%Free%"))
    .OrderBy(Desc(tCustomer.Name))
    .OffsetFetch(1, 2)
    .QueryList(database,
        r => (Id: tCustomer.CustomerId.Read(r), CustomerType: tCustomer.Type.Read(r), Name: tCustomer.Name.Read(r)));

foreach (var customer in customers)
{
    Console.WriteLine($"Id: {customer.Id}, Name: {customer.Name}, Type: {customer.CustomerType}");
}

```

*Actual T-SQL:*

```sql
SELECT 
    [CUST].[CustomerId],
    [CUST].[Type],
    [CUST].[Name] 
FROM 
(
    SELECT 
        [A0].[CustomerId] [CustomerId],
        CASE 
        WHEN [A1].[UserId] IS NOT NULL 
        THEN CAST(1 AS smallint) 
        WHEN [A2].[CompanyId] IS NOT NULL 
        THEN CAST(2 AS smallint) 
        ELSE NULL 
        END [Type],
        CASE 
        WHEN [A1].[UserId] IS NOT NULL 
        THEN [A1].[FirstName]+' '+[A1].[LastName] 
        WHEN [A2].[CompanyId] IS NOT NULL 
        THEN [A2].[CompanyName] 
        ELSE NULL END [Name] 
    FROM [dbo].[Customer] [A0] 
    LEFT JOIN [dbo].[User] [A1] 
        ON [A1].[UserId]=[A0].[UserId] 
    LEFT JOIN [dbo].[Company] [A2] 
        ON [A2].[CompanyId]=[A0].[CompanyId]
)[CUST] 
WHERE 
    [CUST].[Type]=2 OR [CUST].[Name] LIKE '%Free%' 
ORDER BY [CUST].[Name] DESC 
OFFSET 1 ROW FETCH NEXT 2 ROW ONLY
```

*Result:*

```
Id: 4, Name: Google, Type: 2
Id: 2, Name: Allina Freeborne, Type: 1
```

## Subqueries

It is not necessary to create a new class when you need a subquery - it can be directly described in an original expression. It is enough just to predefine the aliases for columns and tables:

```cs
var num = CustomColumnFactory.Int32("3");
//Note: "3" (the first value) is for compatibility with MySql
//which does not properly support values constructors

var sum = CustomColumnFactory.Int32("Sum");

var numbers = Values(3, 1, 1, 7, 3, 7, 3, 7, 7, 8).AsColumns(num);
var numbersSubQuery = TableAlias();

var mostFrequentNum = (int) await
    /*SqQueryBuilder.*/SelectTop(1, numbersSubQuery.Column(num))
        .From(
            /*SqQueryBuilder.*/Select(numbers.Column(num), CountOne().As(sum))
                .From(numbers)
                .GroupBy(numbers.Column(num))
                .As(numbersSubQuery)
        )
        .OrderBy(/*SqQueryBuilder.*/Desc(numbersSubQuery.Column(sum)))
        .QueryScalar(database);

Console.WriteLine("The most frequent number: "  + mostFrequentNum);
```

*Actual T-SQL:*

```sql
SELECT 
    TOP 1 [A0].[3] 
FROM 
(
    SELECT [A1].[3],COUNT(1) [Sum] 
    FROM (VALUES (3),(1),(1),(7),(3),(7),(3),(7),(7),(8))[A1]([3]) 
    GROUP BY [A1].[3]
) [A0] 
ORDER BY [A0].[Sum] DESC
```

*Note: In this example you can see how to use **Table Value Constructor***

## CTE

To perform recursive (*actually "incremental"*) requests the library supports CTE (Common Table Expressions).

The typical scenario is traversing some hierarchical data stored in a table, for example the following query will return a tree closure table:

```cs
class CteTreeClosure : CteBase
{
    public CteTreeClosure(Alias alias = default) : base(nameof(CteTreeClosure), alias)
    {
        this.Id = this.CreateInt32Column(nameof(this.Id));
        this.ParentId = this.CreateNullableInt32Column(nameof(this.ParentId));
        this.Depth = this.CreateInt32Column(nameof(this.Depth));
    }

    public Int32CustomColumn Id { get; }

    public NullableInt32CustomColumn ParentId { get; }

    public Int32CustomColumn Depth { get; }

    public override IExprSubQuery CreateQuery()
    {
        var initial = new TreeData();
        var current = new TreeData();

        var previous = new CteTreeClosure();

        return /*SqQueryBuilder.*/Select(initial.Id, initial.ParentId, /*SqQueryBuilder.*/Literal(1).As(this.Depth))
            .From(initial)
            .UnionAll(Select(
                    previous.Id,
                    current.ParentId,
                    (previous.Depth + 1).As(this.Depth))
                .From(current)
                .InnerJoin(previous, on: previous.ParentId == current.Id))
            .Done();
    }
}
...

var result = await /*SqQueryBuilder.*/Select(treeClosure.Id, treeClosure.ParentId, treeClosure.Depth)
    .From(treeClosure)
    .QueryList(context.Database,
        r => (
            Id: treeClosure.Id.Read(r),
            ParentId: treeClosure.ParentId.Read(r),
            Depth: treeClosure.Depth.Read(r)));

```

Working with CTEs in SqExpress is very similar to derived tables - you need to create a class derived from  **CteBase** abstract class, describe columns and implement **CreateQuery** method which will return actual CTE query where the class can be used as a table descriptor (to create recursion if it is required).

The example code will generate the following sql:

```sql
WITH [CteTreeClosure] AS(
        SELECT [A1].[Id],[A1].[ParentId],1 [Depth] 
        FROM [#TreeData] [A1] 
    UNION ALL 
        SELECT [A2].[Id],[A3].[ParentId],[A2].[Depth]+1 [Depth] 
        FROM [#TreeData] [A3] 
        JOIN [CteTreeClosure] [A2] 
            ON [A2].[ParentId]=[A3].[Id]
)
                
SELECT [A0].[Id],[A0].[ParentId],[A0].[Depth] FROM [CteTreeClosure] [A0]
```

*MySql*

```sql
WITH RECURSIVE `CteTreeClosure` AS(
        SELECT `A0`.`Id`,`A0`.`ParentId`,1 `Depth` 
        FROM `TreeData` `A0` 
    UNION ALL 
        SELECT `A1`.`Id`,`A2`.`ParentId`,`A1`.`Depth`+1 `Depth` 
        FROM `TreeData` `A2` 
        JOIN `CteTreeClosure` `A1` 
            ON `A1`.`ParentId`=`A2`.`Id`
) 

SELECT `A3`.`Id`,`A3`.`ParentId`,`A3`.`Depth` FROM `CteTreeClosure` `A3````
```

## Analytic And Window Functions

SqExpress supports common analytic and window functions like **ROW_NUMBER**, **RANK**, **FIRST_VALUE**, **LAST_VALUE** etc.

```cs
var cUserName = CustomColumnFactory.String("Name");
var cNum = CustomColumnFactory.Int64("Num");
var cFirst = CustomColumnFactory.String("First");
var cLast = CustomColumnFactory.String("Last");

var user = new TableUser();

await /*SqQueryBuilder.*/Select(
        (user.FirstName + " " + user.LastName)
        .As(cUserName),
        /*SqQueryBuilder.*/RowNumber()
            /*.OverPartitionBy(some fields)*/
            .OverOrderBy(user.FirstName)
            .As(cNum),
        /*SqQueryBuilder.*/FirstValue(user.FirstName + " " + user.LastName)
            /*.OverPartitionBy(some fields)*/
            .OverOrderBy(user.FirstName)
            .FrameClauseEmpty()
            .As(cFirst),
        /*SqQueryBuilder.*/LastValue(user.FirstName + " " + user.LastName)
            /*.OverPartitionBy(some fields)*/
            .OverOrderBy(user.FirstName)
            .FrameClause(
                FrameBorder.UnboundedPreceding,
                FrameBorder.UnboundedFollowing)
            .As(cLast))
    .From(user)
    .Query(database,
        r => Console.WriteLine(
            $"Num: {cNum.Read(r)}, Name: {cUserName.Read(r)}, " +
            $"First: {cFirst.Read(r)}, Last: {cLast.Read(r)}"));
```

*Actual T-SQL:*

```sql
SELECT 
    [A0].[FirstName]+' '+[A0].[LastName] 
        [Name],
    ROW_NUMBER()OVER(ORDER BY [A0].[FirstName]) 
        [Num],
    FIRST_VALUE([A0].[FirstName]+' '+[A0].[LastName])
        OVER(ORDER BY [A0].[FirstName]) 
        [First],
    LAST_VALUE([A0].[FirstName]+' '+[A0].[LastName])
        OVER(ORDER BY [A0].[FirstName] 
            ROWS BETWEEN 
            UNBOUNDED PRECEDING 
            AND UNBOUNDED FOLLOWING) 
        [Last] 
FROM [dbo].[User] [A0]
```

## Set Operators

The library supports all the SET operators:

```cs
//If you need to repeat one query several times 
// you can store it in a variable
var select1 = /*SqQueryBuilder.*/Select(1);
var select2 = /*SqQueryBuilder.*/Select(2);

var result = await select1
    .Union(select2)
    .UnionAll(select2)
    .Except(select2)
    .Intersect(select1.Union(select2))
    .QueryList(database, r => r.GetInt32(0));

Console.WriteLine("Result Of Set Operators:");
Console.WriteLine(result[0]);
```

Ans actual SQL will be:

```sql
(
    (
        (
            SELECT 1 
            UNION 
            SELECT 2
        ) 
        UNION ALL 
        SELECT 2
    ) 
    EXCEPT 
    SELECT 2
) 
INTERSECT 
(
    SELECT 1 
    UNION 
    SELECT 2
)
```

## Merge

As a bonus, if you use MS SQL Server, you can use **Merge** statement:

```cs
var data = new[]
{
    new {FirstName = "Francois", LastName = "Sturman2"},
    new {FirstName = "Allina", LastName = "Freeborne2"},
    new {FirstName = "Maye", LastName = "Malloy"},
};

var action = CustomColumnFactory.String("Actions");
var inserted = CustomColumnFactory.NullableInt32("Inserted");
var deleted = CustomColumnFactory.NullableInt32("Deleted");

var tableUser = new TableUser();
await /*SqQueryBuilder.*/MergeDataInto(tableUser, data)
    .MapDataKeys(s => s
        .Set(s.Target.FirstName, s.Source.FirstName))
    .MapData(s => s
        .Set(s.Target.LastName, s.Source.LastName))
    .WhenMatchedThenUpdate()
    .AlsoSet(s => s
        .Set(s.Target.Version, s.Target.Version + 1)
        .Set(s.Target.ModifiedAt, GetUtcDate()))
    .WhenNotMatchedByTargetThenInsert()
    .AlsoInsert(s => s
        .Set(s.Target.Version, 1)
        .Set(s.Target.ModifiedAt, GetUtcDate()))
    .Output((t, s, m) => m.Inserted(t.UserId.As(inserted)).Deleted(t.UserId.As(deleted)).Action(action))
    .Done()
    .Query(database,
        (r) =>
        {
            Console.WriteLine($"UserId Inserted: {inserted.Read(r)},UserId Deleted: {deleted.Read(r)} , Action: {action.Read(r)}");
        });
```

*Actual T-SQL:*

```sql
MERGE [dbo].[User] [A0] 
USING (
    VALUES 
    ('Francois','Sturman2'),
    ('Allina','Freeborne2'),
    ('Maye','Malloy'))[A1]([FirstName],[LastName]) 
ON [A0].[FirstName]=[A1].[FirstName] 
WHEN MATCHED 
THEN UPDATE SET [A0].[LastName]=[A1].[LastName],[A0].[Version]=[A0].[Version]+1,[A0].[ModifiedAt]=GETUTCDATE() 
WHEN NOT MATCHED 
THEN INSERT([FirstName],[LastName],[Version],[ModifiedAt]) 
VALUES([A1].[FirstName],[A1].[LastName],1,GETUTCDATE()) 
OUTPUT INSERTED.[UserId] [Inserted],DELETED.[UserId] [Deleted],$ACTION [Actions];
```

*Result:*

```
UserId Inserted: 4,UserId Deleted:  , Action: INSERT
UserId Inserted: 1,UserId Deleted: 1 , Action: UPDATE
UserId Inserted: 2,UserId Deleted: 2 , Action: UPDATE
```

For PostgresSQL or MySQL the library generates polyfills that use a temporary table to store passed data. For example the previous query will be converted into the following statements (OUTPUT is not supported):

*Actual MYSQL:*

```sql
CREATE TEMPORARY TABLE `tmpMergeDataSource`(
    `FirstName` varchar(8) character set utf8,
    `LastName` varchar(10) character set utf8,
    CONSTRAINT PRIMARY KEY (`FirstName`))
;
INSERT INTO `tmpMergeDataSource`(`FirstName`,`LastName`) 
VALUES ('Francois','Sturman2'),('Allina','Freeborne2'),('Maye','Malloy')
;
UPDATE `User` `A0`,`tmpMergeDataSource` `A1` 
SET 
    `A0`.`LastName`=`A1`.`LastName`,
    `A0`.`Version`=`A0`.`Version`+1,
    `A0`.`ModifiedAt`=UTC_TIMESTAMP()
WHERE `A0`.`FirstName`=`A1`.`FirstName`
;
INSERT INTO `User`(`FirstName`,`LastName`,`Version`,`ModifiedAt`) 
SELECT `A1`.`FirstName`,`A1`.`LastName`,1,UTC_TIMESTAMP() 
FROM `tmpMergeDataSource` `A1` 
WHERE NOT EXISTS(SELECT 1 FROM `User` `A0` WHERE `A0`.`FirstName`=`A1`.`FirstName`)
;
DROP TABLE `tmpMergeDataSource`;
```

## Temporary Tables

In some scenarios temporary tables might be very useful and you can create such table as follows:

```cs
public class TempTable : TempTableBase
{
    public TempTable(Alias alias = default) : base("tempTable", alias)
    {
        this.Id = CreateInt32Column(nameof(Id),
            ColumnMeta.PrimaryKey().Identity());

        this.Name = CreateStringColumn(nameof(Name), 255);
    }

    public readonly Int32TableColumn Id;

    public readonly StringTableColumn Name;
}
```

and then use it:

```cs
var tmp = new TempTable();

var tableUser = new TableUser();
var tableCompany = new TableCompany();

await database.Statement(tmp.Script.Create());

//Users
await /*SqQueryBuilder.*/InsertInto(tmp, tmp.Name)
    .From(Select(tableUser.FirstName + " "+ tableUser.LastName)
    .From(tableUser))
    .Exec(database);

//Companies
await /*SqQueryBuilder.*/InsertInto(tmp, tmp.Name)
    .From(Select(tableCompany.CompanyName)
    .From(tableCompany))
    .Exec(database);

await /*SqQueryBuilder.*/Select(tmp.Columns)
    .From(tmp)
    .OrderBy(tmp.Name)
    .Query(database,
        (r) =>
        {
            Console.WriteLine($"Id: {tmp.Id.Read(r)}, Name: {tmp.Name.Read(r)}");
        });

//Dropping the temp table is optional
//It will be automatically removed when
//the connection is closed
await database.Statement(tmp.Script.Drop());
```

The result will be:

```
Id: 2, Name: Allina Freeborne
Id: 1, Name: Francois Sturman
Id: 4, Name: Google
Id: 3, Name: Microsoft
```

## Database Data Export Import

Having a list of table descriptors you can easily export all theirs data into any text format - JSON for example:

```cs
static async Task<string> ToJsonString(ISqDatabase database, TableBase[] tableBases)
{
    using var ms = new MemoryStream();
    using Utf8JsonWriter writer = new Utf8JsonWriter(ms);

    writer.WriteStartObject();
    foreach (var table in tableBases)
    {
        await ReadTableDataIntoJson(writer, database, table);
    }

    writer.WriteEndObject();
    writer.Flush();

    var s = Encoding.UTF8.GetString(ms.ToArray());
    return s;
}

static async Task ReadTableDataIntoJson(Utf8JsonWriter writer, ISqDatabase database, TableBase table)
{
    writer.WriteStartArray(table.FullName.AsExprTableFullName().TableName.Name);

    writer.WriteStartArray();
    foreach (var column in table.Columns)
    {
        writer.WriteStringValue(column.ColumnName.Name);
    }

    writer.WriteEndArray();

    await /*SqQueryBuilder.*/Select(table.Columns)
        .From(table)
        .Query(database,
            r =>
            {
                writer.WriteStartArray();
                foreach (var column in table.Columns)
                {
                    var readAsString = column.ReadAsString(r);
                    writer.WriteStringValue(readAsString);
                }

                writer.WriteEndArray();
            });

    writer.WriteEndArray();
}
```

Result:

```json
{
    "User": [
 ["UserId", "FirstName", "LastName", "Version", "ModifiedAt"], 
 ["1", "Francois", "Sturman2", "2", "2021-10-26T08:07:03.160"], 
 ["2", "Allina", "Freeborne2", "2", "2021-10-26T08:07:03.160"], 
 ["4", "Maye", "Malloy", "1", "2021-10-26T08:07:03.160"]],
    "Company": [
 ["CompanyId", "CompanyName", "Version", "ModifiedAt"], 
 ["1", "Microsoft", "1", "2021-10-26T08:07:03.080"], 
 ["2", "Google", "1", "2021-10-26T08:07:03.080"]],
    "Customer": [
 ["CustomerId", "UserId", "CompanyId"], 
 ["3", null, "1"], 
 ["4", null, "2"], 
 ["1", "1", null], 
 ["2", "2", null]]
}
```

Import from a text format is not difficult as well:

```cs
static async Task InsertTableData(ISqDatabase database, TableBase table, JsonElement element)
{
    var columnsDict = table.Columns.ToDictionary(i => i.ColumnName.Name, i => i);
    var colIndexes = element.EnumerateArray().First().EnumerateArray().Select(c => c.GetString()).ToList();

    var rowsEnumerable = element
        .EnumerateArray()
        .Skip(1)
        .Select(e =>
            e.EnumerateArray()
                .Select((c, i) =>
                    columnsDict[colIndexes[i]]
                        .FromString(c.ValueKind == JsonValueKind.Null ? null : c.GetString()))
                .ToList());

    var insertExpr = /*SqQueryBuilder.*/IdentityInsertInto(table, table.Columns).Values(rowsEnumerable);
    if (!insertExpr.Insert.Source.IsEmpty)
    {
        await insertExpr.Exec(database);
    }
}
```

## Getting and Comparing Database Table Metadata

You can a list of dynamic table descriptors directly from a database using ```GetTables()``` method of ```ISqDatabase``` object. For example, this how you can read a list of all tables with all columns:

```cs
async Task ShowAllTablesWithColumns(ISqDatabase database)
{
    var actualTables = await database.GetTables();
    foreach (var table in actualTables)
    {
        Console.WriteLine(table.FullName.TableName);
        foreach (var tableColumn in table.Columns)
        {
            Console.WriteLine($"   -{tableColumn.ColumnName.Name}:{TSqlExporter.Default.ToSql(tableColumn.SqlType)}");
        }
    }
}
```

You also can compare 2 lists of table to find any kind of differences:

```cs
async Task<bool> CheckDatabaseIsUpdated(ISqDatabase database, IReadOnlyList<TableBase> expectedTableList)
{
    var actualTables = await database.GetTables();

    var comparison = expectedTableList.CompareWith(actualTables);

    bool result = true;

    if (comparison != null)
    {
        if (comparison.ExtraTables.Count > 0)
        {
            Console.WriteLine($"There are {comparison.ExtraTables.Count} extra tables");
        }
        if (comparison.MissedTables.Count > 0)
        {
            result = false;
            Console.WriteLine($"There are {comparison.MissedTables.Count} missed tables");
        }
        if (comparison.DifferentTables.Count > 0)
        {
            result = false;
            Console.WriteLine($"There are {comparison.DifferentTables.Count} different tables");

            foreach (var differentTable in comparison.DifferentTables)
            {
                Console.WriteLine($"Table {differentTable.Table.FullName.TableName}");
                foreach (var extra in differentTable.TableComparison.ExtraColumns)
                {
                    Console.WriteLine($"Extra column: {extra.ColumnName.Name}");
                }
                foreach (var missed in differentTable.TableComparison.MissedColumns)
                {
                    Console.WriteLine($"Extra column: {missed.ColumnName.Name}");
                }
                foreach (var differentColumns in differentTable.TableComparison.DifferentColumns)
                {
                    Console.WriteLine($"Different column: {differentColumns.Column.ColumnName.Name} - {differentColumns.ColumnComparison}");
                }

            }

        }
    }
    return result;
}
```

You cane also create new table dynamic descriptors and modify existing ones:

```cs
var tbl = SqTable.Create(
    "schema",
    "table",
    b => b
        .AppendInt32Column("Id", ColumnMeta.PrimaryKey().Identity())
        .AppendStringColumn("Value", 255, true)
        .AppendBooleanColumn("IsActive", ColumnMeta.DefaultValue(false)),
    i => i
        .AppendIndex(i.Asc("Id"), i.Desc("Value"))
        .AppendIndex(i.Asc("Value"))
);

tbl = tbl.With(
    tbl.FullName.WithSchemaName("schema2").WithTableName("table2"),
    (cols, app) => app
        .AppendColumns(cols.Where(c => c.ColumnName.Name != "IsActive"))
        .AppendDateTimeOffsetColumn("modifyDate"),
    (indexes, app) => app
        .AppendIndexes(indexes.Where(i=>i.Columns.Count > 1))
        .AddUniqueIndex(app.Desc("modifyDate"))
);
```

## Syntax Tree

You can go through an existing syntax tree object and modify if it is required:

```cs
//Var some external filter..
ExprBoolean filter = CustomColumnFactory.Int16("Type") == 2 /*Company*/;

var tableCustomer = new TableCustomer();

var baseSelect = /*SqQueryBuilder.*/Select(tableCustomer.CustomerId)
    .From(tableCustomer)
    .Where(filter)
    .Done();

//Checking that filter has "Type" column
var hasVirtualColumn = filter.SyntaxTree()
    .FirstOrDefault<ExprColumnName>(e => e.Name == "Type") != null;

if (hasVirtualColumn)
{
    baseSelect = (ExprQuerySpecification) baseSelect.SyntaxTree()
        .Modify(e =>
        {
            var result = e;
            //Joining with the sub query
            if (e is TableCustomer table)
            {
                var derivedTable = new DerivedTableCustomer();

                result = new ExprJoinedTable(
                    table,
                    ExprJoinedTable.ExprJoinType.Inner,
                    derivedTable,
                    table.CustomerId == derivedTable.CustomerId);
            }

            return result;
        });
}

await baseSelect!
    .Query(database,
        (r) =>
        {
            Console.WriteLine($"Id: {tableCustomer.CustomerId.Read(r)}");
        });
```

For simpler scenarios, you can use `With...` functions:

```cs
var tUser = new TableUser();

Console.WriteLine("Original expression:");
var expression = SelectTop(1, tUser.FirstName).From(tUser).Done();

await expression.QueryScalar(database);

expression = expression
    .WithTop(null)
    .WithSelectList(tUser.UserId, tUser.FirstName + " " + tUser.LastName)
    .WithWhere(tUser.UserId == 7);

Console.WriteLine("With changed selection list and filter:");
await expression.QueryScalar(database);

var tCustomer = new TableCustomer();
expression = expression
    .WithInnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId);

Console.WriteLine("With joined table");
await expression.QueryScalar(database);
```

For read-only traversal and analysis, `ExprVisitorBase` can be easier than `Modify(...)`:

```cs
public sealed class ColumnNameCollector : ExprVisitorBase
{
    public HashSet<string> ColumnNames { get; } = new HashSet<string>();

    public override void VisitExprColumnName(ExprColumnName expr)
    {
        this.ColumnNames.Add(expr.Name);
        base.VisitExprColumnName(expr); // keep default traversal
    }
}

var collector = new ColumnNameCollector();
baseSelect.Accept(collector);
```

Remarks:
- During visitor callbacks you can inspect `CurrentPath`, `CurrentNode`, and `Depth`.
- If you override a `Visit...` method and still need children traversal, call `base.Visit...(...)`.

*Actual T-SQL:*

```sql
--Original expression:
SELECT TOP 1 
    [A0].[FirstName] 
FROM [dbo].[User] [A0]

--With changed selection list  and filter:
SELECT 
    [A0].[UserId],
    [A0].[FirstName]+' '+[A0].[LastName] 
FROM [dbo].[User] 
    [A0] 
WHERE 
    [A0].[UserId]=7

--With joined table
SELECT 
    [A0].[UserId],
    [A0].[FirstName]+' '+[A0].[LastName] 
FROM [dbo].[User] 
    [A0] 
JOIN [dbo].[Customer] 
    [A1] ON 
    [A1].[UserId]=[A0].[UserId] 
WHERE 
    [A0].[UserId]=7
```

## Serialization to XML

Each expression can be exported to a xml string and then restored back. It can be useful to pass expressions over network:

```cs
var tableUser = new TableUser(Alias.Empty);

var selectExpr = Select(tableUser.FirstName, tableUser.LastName)
    .From(tableUser)
    .Where(tableUser.LastName == "Sturman")
    .Done();

//Exporting
var stringBuilder = new StringBuilder();
using XmlWriter writer = XmlWriter.Create(stringBuilder);
selectExpr.SyntaxTree().ExportToXml(writer);

//Importing
XmlDocument document = new XmlDocument();
document.LoadXml(stringBuilder.ToString());
var restored = (ExprQuerySpecification)ExprDeserializer
    .DeserializeFormXml(document.DocumentElement!);

var result = await restored
    .QueryList(database, r => (tableUser.FirstName.Read(r), tableUser.LastName.Read(r)));

foreach (var name in result)
{
    Console.WriteLine(name);
}
```

This an example of the XML text:

```xml
<Expr typeTag="QuerySpecification">
   <SelectList>
      <SelectList0 typeTag="Column">
         <ColumnName typeTag="ColumnName">
            <Name>FirstName</Name>
         </ColumnName>
      </SelectList0>
      <SelectList1 typeTag="Column">
         <ColumnName typeTag="ColumnName">
            <Name>LastName</Name>
         </ColumnName>
      </SelectList1>
   </SelectList>
   <From typeTag="Table">
      <FullName typeTag="TableFullName">
         <DbSchema typeTag="DbSchema">
            <Schema typeTag="SchemaName">
               <Name>dbo</Name>
            </Schema>
         </DbSchema>
         <TableName typeTag="TableName">
            <Name>User</Name>
         </TableName>
      </FullName>
   </From>
   <Where typeTag="BooleanEq">
      <Left typeTag="Column">
         <ColumnName typeTag="ColumnName">
            <Name>LastName</Name>
         </ColumnName>
      </Left>
      <Right typeTag="StringLiteral">
         <Value>Sturman</Value>
      </Right>
   </Where>
   <Distinct>false</Distinct>
</Expr>
```

## Serialization to JSON

The similar functionality exists for JSON (.Net Core 3.1+)

```cs
var tableUser = new TableUser(Alias.Empty);

var selectExpr = Select(tableUser.FirstName, tableUser.LastName)
    .From(tableUser)
    .Where(tableUser.LastName == "Sturman")
    .Done();

//Exporting
var memoryStream = new MemoryStream();
var jsonWriter = new Utf8JsonWriter(memoryStream);
selectExpr.SyntaxTree().ExportToJson(jsonWriter);

string json = Encoding.UTF8.GetString(memoryStream.ToArray());

//Importing
var restored = (ExprQuerySpecification)ExprDeserializer
    .DeserializeFormJson(JsonDocument.Parse(json).RootElement);

var result = await restored
    .QueryList(database, r => (tableUser.FirstName.Read(r), tableUser.LastName.Read(r)));

foreach (var name in result)
{
    Console.WriteLine(name);
}
```

This an example of the JSON text:

```json
{
   "$type":"QuerySpecification",
   "SelectList":[
      {
         "$type":"Column",
         "ColumnName":{
            "$type":"ColumnName",
            "Name":"FirstName"
         }
      },
      {
         "$type":"Column",
         "ColumnName":{
            "$type":"ColumnName",
            "Name":"LastName"
         }
      }
   ],
   "From":{
      "$type":"Table",
      "FullName":{
         "$type":"TableFullName",
         "DbSchema":{
            "$type":"DbSchema",
            "Schema":{
               "$type":"SchemaName",
               "Name":"dbo"
            }
         },
         "TableName":{
            "$type":"TableName",
            "Name":"User"
         }
      }
   },
   "Where":{
      "$type":"BooleanEq",
      "Left":{
         "$type":"Column",
         "ColumnName":{
            "$type":"ColumnName",
            "Name":"LastName"
         }
      },
      "Right":{
         "$type":"StringLiteral",
         "Value":"Sturman"
      }
   },
   "Distinct":false
}
```

## Serialization to Plain List

Also an expression can be exported into a list of plain entities. It might be useful if you want to store some expressions (e.g. "Favorites Filters") in a plain structure:

```cs
var tableUser = new TableUser(Alias.Empty);

ExprBoolean filter1 = tableUser.LastName == "Sturman";
ExprBoolean filter2 = tableUser.LastName == "Freeborne";

var tableFavoriteFilter = new TableFavoriteFilter();
var tableFavoriteFilterItem = new TableFavoriteFilterItem();

var filterIds = await InsertDataInto(tableFavoriteFilter, new[] {"Filter 1", "Filter 2"})
    .MapData(s => s.Set(s.Target.Name, s.Source))
    .Output(tableFavoriteFilter.FavoriteFilterId)
    .QueryList(database, r => tableFavoriteFilterItem.FavoriteFilterId.Read(r));

var filter1Items = 
    filter1.SyntaxTree().ExportToPlainList((i, id, index, b, s, value) =>
    FilterPlainItem.Create(filterIds[0], i, id, index, b, s, value));

var filter2Items = 
    filter2.SyntaxTree().ExportToPlainList((i, id, index, b, s, value) =>
    FilterPlainItem.Create(filterIds[1], i, id, index, b, s, value));

await /*SqQueryBuilder.*/InsertDataInto(tableFavoriteFilterItem, filter1Items.Concat(filter2Items))
    .MapData(s => s
        .Set(s.Target.FavoriteFilterId, s.Source.FavoriteFilterId)
        .Set(s.Target.Id, s.Source.Id)
        .Set(s.Target.ParentId, s.Source.ParentId)
        .Set(s.Target.IsTypeTag, s.Source.IsTypeTag)
        .Set(s.Target.ArrayIndex, s.Source.ArrayIndex)
        .Set(s.Target.Tag, s.Source.Tag)
        .Set(s.Target.Value, s.Source.Value)
    )
    .Exec(database);

//Restoring
var restoredFilterItems = await /*SqQueryBuilder.*/Select(tableFavoriteFilterItem.Columns)
    .From(tableFavoriteFilterItem)
    .Where(tableFavoriteFilterItem.FavoriteFilterId.In(filterIds))
    .QueryList(
        database,
        r => new FilterPlainItem(
        favoriteFilterId: tableFavoriteFilterItem.FavoriteFilterId.Read(r),
        id: tableFavoriteFilterItem.Id.Read(r),
        parentId: tableFavoriteFilterItem.ParentId.Read(r),
        isTypeTag: tableFavoriteFilterItem.IsTypeTag.Read(r),
        arrayIndex: tableFavoriteFilterItem.ArrayIndex.Read(r),
        tag: tableFavoriteFilterItem.Tag.Read(r),
        value: tableFavoriteFilterItem.Value.Read(r)));

var restoredFilter1 = (ExprBoolean)ExprDeserializer
    .DeserializeFormPlainList(restoredFilterItems.Where(fi =>
        fi.FavoriteFilterId == filterIds[0]));

var restoredFilter2 = (ExprBoolean)ExprDeserializer
    .DeserializeFormPlainList(restoredFilterItems.Where(fi =>
        fi.FavoriteFilterId == filterIds[1]));

Console.WriteLine("Filter 1");
await /*SqQueryBuilder.*/Select(tableUser.FirstName, tableUser.LastName)
    .From(tableUser)
    .Where(restoredFilter1)
    .Query(database,
        (r) =>
        {
            Console.WriteLine($"{tableUser.FirstName.Read(r)} {tableUser.LastName.Read(r)}");
        });

Console.WriteLine("Filter 2");
await /*SqQueryBuilder.*/Select(tableUser.FirstName, tableUser.LastName)
    .From(tableUser)
    .Where(restoredFilter2)
    .Query(database,
        (r) =>
        {
            Console.WriteLine($"{tableUser.FirstName.Read(r)} {tableUser.LastName.Read(r)}");
        });
```

## Retrieving Database Table Metadata

The **ISqDatabase** interface includes a method called **GetTables()** that retrieves all table descriptors from a database defined in the connection string:

```cs
ISqDatabase database = ...;

var allTables = await database.GetTables();

foreach (var table in allTables)
{
    Console.WriteLine($"{table.FullName.TableName}");
    foreach (var column in table.Columns)
    {
        Console.WriteLine(
            $"   *{column.ColumnName.Name} {column.SqlType.ToSql(TSqlExporter.Default)}");
    }
}

```

Result:

```
User
   *UserId int
   *FirstName [nvarchar](255)
   *LastName [nvarchar](255)
   *Version int
   *ModifiedAt datetime
Customer
   *CustomerId int
   *UserId int
   *CompanyId int

etc...
```

*Note: The list of tables is sorted in such a way that dependent tables appear last. Therefore, if the list is reversed, tables can be safely deleted:*

```cs
var allTables = await database.GetTables();

foreach (var table in allTables.Reverse())
{
    await database.Statement(table.Script.Drop());
}
```

The list of tables can be compared with each other:

```cs
var declaredTables = AllTables.BuildAllTableList(SqlDialect.TSql);
var actualTables = await database.GetTables();

var comparison = declaredTables.CompareWith(actualTables);

if (comparison != null)
{
    if (comparison.ExtraTables.Count > 0)
    {
        Console.WriteLine($"There are {comparison.ExtraTables.Count} extra tables");
    }
    if (comparison.MissedTables.Count > 0)
    {
        Console.WriteLine($"There are {comparison.MissedTables.Count} missed tables");
    }
    if (comparison.DifferentTables.Count > 0)
    {
        Console.WriteLine($"There are {comparison.MissedTables.Count} different tables");
    }
}
```

## Table Descriptors Scaffolding

**SqExpress** comes with the code-gen utility (it is located in the nuget package cache). It can read metadata form a database and create table descriptor classes in your code. It requires .Net Core 3.1+

```Package Manager Console```

```
SYNTAX
    Gen-Tables [-DbType] {mssql | mysql | pgsql} [-ConnectionString] <string> [-OutputDir <string>] [-TableClassPrefix <string>] [-Namespace <string>]
```

```GenerateTables.cmd```

```cmd
@echo off
set root=%userprofile%\.nuget\packages\sqexpress

for /F "tokens=*" %%a in ('dir "%root%" /b /a:d /o:n') do set "lib=%root%\%%a"

set lib=%lib%\tools\codegen\SqExpress.CodeGenUtil.dll

dotnet "%lib%" gentables mssql "MyConnectionString" --table-class-prefix "Tbl" -o ".\Tables" -n "MyCompany.MyProject.Tables"
```

```GenerateTables.sh```

```sh
#!/bin/bash

lib=~/.nuget/packages/sqexpress/$(ls ~/.nuget/packages/sqexpress -r|head -n 1)/tools/codegen/SqExpress.CodeGenUtil.dll

dotnet $lib gentables mssql "MyConnectionString" --table-class-prefix "Tbl" -o "./Tables" -n "MyCompany.MyProject.Tables"
```

It uses Roslyn compiler so it does not overwrite existing files - it patched it with actual columns. All kind of changes like attributes, namespaces, interfaces will remain after next runs.

## DTOs Scaffolding

You can add special attributes to column properties in table descriptors to provide information to the code-gen util to create (update) DTO classes with mappings:

```cs
public class TableUser : TableBase
{
    [SqModel("UserName", PropertyName = "Id")]
    public Int32TableColumn UserId { get; }

    [SqModel("UserName")]
    public StringTableColumn FirstName { get; }

    [SqModel("UserName")]
    public StringTableColumn LastName { get; }

    //Audit Columns
    [SqModel("AuditData")]
    public Int32TableColumn Version { get; }

    [SqModel("AuditData")]
    public DateTimeTableColumn ModifiedAt { get; }

    public TableUser(Alias alias) : base("dbo", "User", alias)
    {
        ...
    }
}
```

To run the code-gen util before a project building, just define the following property in the project file:

```
<Project ..,>
  <PropertyGroup>
    ...
    <SqModelGenEnable>true</SqModelGenEnable>
    ...
  </PropertyGroup>
```

The list of all code-generation parameters can be found here: [SqExpress.props](https://github.com/0x1000000/SqExpress/blob/main/SqExpress/SqExpress.props).

The code generation tool can also be run from the command line:

```Package Manager Console```

```
SYNTAX
    Gen-Models [-InputDir <string>] [-OutputDir <string>] [-Namespace <string>] [-NoRwClasses] [-NullRefTypes] [-CleanOutput] [-ModelType {ImmutableClass | Record}]  [<CommonParameters>]
```

```GenerateModel.cmd```

```cmd
@echo off
set root=%userprofile%\.nuget\packages\sqexpress

for /F "tokens=*" %%a in ('dir "%root%" /b /a:d /o:n') do set "lib=%root%\%%a"

set lib=%lib%\tools\codegen\SqExpress.CodeGenUtil.dll

dotnet "%lib%" genmodels -i "." -o ".\Models" -n "SqExpress.GetStarted.Models" --null-ref-types
```

```generate-model.sh```

```
#!/bin/bash
lib=~/.nuget/packages/sqexpress/$(ls ~/.nuget/packages/sqexpress -r|head -n 1)/tools/codegen/SqExpress.CodeGenUtil.dll
dotnet $lib genmodels -i "." -o "./Models" -n "SqExpress.GetStarted.Models"
```

The result will be the following classes:

```UserName.cs```

```cs
public class UserName
{
    public UserName(int id, string firstName, string lastName)
    {
        this.Id = id;
        this.FirstName = firstName;
        this.LastName = lastName;
    }

    public static UserName Read(ISqDataRecordReader record, TableUser table)
    {
        return new UserName(id: table.UserId.Read(record), firstName: table.FirstName.Read(record), lastName: table.LastName.Read(record));
    }

    public int Id { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public static TableColumn[] GetColumns(TableUser table)
    {
        return new TableColumn[]{table.UserId, table.FirstName, table.LastName};
    }

    public static IRecordSetterNext GetMapping(IDataMapSetter<TableUser, UserName> s)
    {
        return s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName);
    }

    public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableUser, UserName> s)
    {
        return s.Set(s.Target.UserId, s.Source.Id);
    }

    public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableUser, UserName> s)
    {
        return s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName);
    }

    public UserName WithId(int id)
    {
        return new UserName(id: id, firstName: this.FirstName, lastName: this.LastName);
    }

    public UserName WithFirstName(string firstName)
    {
        return new UserName(id: this.Id, firstName: firstName, lastName: this.LastName);
    }

    public UserName WithLastName(string lastName)
    {
        return new UserName(id: this.Id, firstName: this.FirstName, lastName: lastName);
    }
}
```

and [```AuditData.cs```](https://github.com/0x1000000/SqExpress/blob/main/SqExpress.GetStarted/Models/AuditData.cs)

You can use them as follows:

```cs
var tUser = new TableUser();

var users = await Select(UserName.GetColumns(tUser))
    .From(tUser)
    .QueryList(database, r => UserName.Read(r, tUser));

foreach (var userName in users)
{
    Console.WriteLine($"{userName.Id} {userName.FirstName} {userName.LastName}");
}
```

*Note: **SqModel** attribute can be also used for temporary and derived table descriptors.*

## Model Selection

The library contains a fluent api that helps selecting tuples of models inner or left joined.

```
SqModelSelectBuilder
    .Select(Model1.GetReader())
    .InnerJoin(
        Model2.GetReader(), 
        on: t=> t.Table.Id1 == t.JoinedTable1.Id1)
    .InnerJoin(
        Model3.GetReader(), 
        on: t=> t.JoinedTable2.Id2 == t.JoinedTable1.Id2)
    ...
    .InnerJoin(
        ModelN.GetReader(), 
        on: t=> t.JoinedTable(N-1).Id(N-1) == t.JoinedTable(N-2).Id(N-1)))
    .LeftJoin(
        Model(N+1).GetReader(), 
        on: t=> t.JoinedTableN.IdN == t.JoinedTable(N-1).IdN))
    ...
    .Get(
        filter: t=> <Boolean Expression>,
        order: t=><Order Expression>,
        tuple=> <Result Mapping>)
    .QueryList(database);

    ... or
    .Find(
        offset, pageSize,
        filter: t=> <Boolean Expression>,
        order: t=><Order Expression>,
        tuple=> <Result Mapping>)
    .QueryPage(database);
```

Example:

```cs
var page = await SqModelSelectBuilder
    .Select(ModelEmptyReader.Get<TableCustomer>())
    .LeftJoin(
        UserName.GetReader(), 
        on: t => t.Table.UserId == t.JoinedTable1.UserId)
    .LeftJoin(
        CompanyName.GetReader(), 
        on: t => t.Table.CompanyId == t.JoinedTable2.CompanyId)
    .Find(0,10,
        filter: null,
        order: t => Asc(
            IsNull(
                t.JoinedTable1.FirstName + t.JoinedTable1.LastName,
                t.JoinedTable2.CompanyName)
            ),
        r => (r.JoinedModel1 != null 
                ? r.JoinedModel1.FirstName + " "+ r.JoinedModel1.LastName 
                : null) 
            ??
            r.JoinedModel2?.Name ?? "Unknown")
    .QueryPage(database);

foreach (var name in page.Items)
{
    Console.WriteLine(name);
}

```

## Using in ASP.NET

There is a demo ASP.NET project that shows how [SqExpress](https://github.com/0x1000000/SqGoods/tree/main) can be used in a real web app.

The ideas:

1. Each API request uses only one SQL connection which is stored in [a connection storage](https://github.com/0x1000000/SqGoods/blob/main/SqGoods.DomainLogic/DataAccess/MsSqlConnectionStorage.cs);
2. The connection storage [can create an instance of SqDatabase](https://github.com/0x1000000/SqGoods/blob/main/SqGoods.DomainLogic/DataAccess/MsSqlConnectionStorage.cs#L18);
3. The connection storage and SqDatabase [have "Scoped" lifecycle](https://github.com/0x1000000/SqGoods/blob/main/SqGoods.DomainLogic/DomainLogicRegistration.cs#L17);
4. SqDatabase is used in [entity repositories that are responsible for "Domain Logic"](https://github.com/0x1000000/SqGoods/blob/main/SqGoods.DomainLogic/Repositories/SgCategoryRepository.cs).

## PostgreSQL

You can run all the scenarios using Postgres SQL (of course the actual sql will be different):

```Cs
DbCommand NpgsqlCommandFactory(NpgsqlConnection connection, string sqlText)
{
    return new NpgsqlCommand(sqlText, connection);
}

const string connectionString = 
    "Host=localhost;Port=5432;Username=postgres;Password=test;Database=test";

using (var connection = new NpgsqlConnection(connectionString))
{
    using (var database = new SqDatabase<NpgsqlConnection>(
        connection: connection,
        commandFactory: NpgsqlCommandFactory,
        sqlExporter: new PgSqlExporter(builderOptions: SqlBuilderOptions.Default
            .WithSchemaMap(schemaMap: new[] {
                new SchemaMap(@from: "dbo", to: "public")}))))
    {
        ...
    }
}
```

*Note: You need to add **Npgsql** package to your project.*

## MySQL

You can also run all the scenarios using MySQL:

```Cs
DbCommand MySqlCommandFactory(MySqlConnection connection, string sqlText)
{
    return new MySqlCommand(sqlText, connection);
}

const string connectionString = 
    "server=127.0.0.1;uid=test;pwd=test;database=test";

using (var connection = new MySqlConnection(connectionString))
{
    using (var database = new SqDatabase<MySqlConnection>(
        connection: connection,
        commandFactory: MySqlCommandFactory,
        sqlExporter: new MySqlExporter(
            builderOptions: SqlBuilderOptions.Default)))
    {
        ...
    }
}
```

*Note: You need to add **MySql.Data** or **MySqlConnector** package to your project.*

## AutoMapper

Since the DAL works on top of ADO.NET, you can use AutoMapper (if you like it):

```cs
var mapper = new Mapper(new MapperConfiguration(cfg =>
{
    cfg.AddDataReaderMapping();
    var map = cfg.CreateMap<IDataRecord, AllColumnTypesDto>();

    if (context.IsPostgresSql)
    {
        map
            .ForMember(nameof(table.ColByte), c => c.Ignore())
            .ForMember(nameof(table.ColNullableByte), c => c.Ignore());
    }
}));

var result = await Select(table.Columns)
    .From(table)
    .QueryList(context.Database, r => mapper.Map<IDataRecord, AllColumnTypesDto>(r));
```

[(taken from "Test/SqExpress.IntTest/Scenarios/ScAllColumnTypes.cs")](https://github.com/0x1000000/SqExpress/blob/main/Test/SqExpress.IntTest/Scenarios/ScAllColumnTypes.cs#L26)
