# SqExpress
![Logo](https://github.com/0x1000000/SqExpress/blob/main/SqExpress/Icon.png)

The library provides a generic sql syntax tree with export to MS t-SQL, PostgreSQL and MySQL text.

It also provides a set of builders and operators which will help you building complex Sql expressions.

It does not use LINQ and your C# code will be close to real SQL as much as possible, so it can be used when you need the full SQL flexibility to create efficient Db requests.

It is delivered with a simple but efficient data access mechanism which warps ADO.Net DbConnection and can be used with MS SQL Client or Npgsql or MySQL Connector.

It can be used together with the “Code First” concept when you declare SQL tables as C# classes with possibility to generate recreation scripts for a target platform (MS SQL or PostgreSQL or MySQL)

This an article that explains the library principles: ["Syntax Tree and Alternative to LINQ in Interaction with SQL Databases"](https://itnext.io/syntax-tree-and-alternative-to-linq-in-interaction-with-sql-databases-656b78fe00dc?source=friends_link&sk=f5f0587c08166d8824b96b48fe2cf33c)

# Content
1. [Get Started](#get-started)
2. [Recreating Table](#recreating-table)
3. [Inserting Data](#inserting-data)
4. [Selecting Data](#selecting-data)
5. [Updating Data](#updating-data)
6. [Deleting Data](#deleting-data)
7. [More Tables and foreign keys](#more-tables-and-foreign-keys)
8. [Joining Tables](#joining-tables)
9. [Aliasing](#aliasing)
10. [Derived Tables](#derived-tables)
11. [Subquries](#subquries)
12. [Analytic And Window Functions](#analytic-and-window-functions)
13. [Set Operators](#set-operators)
14. [PostgreSQL](#postgreSQL)
15. [MySQL](#mysql)
16. [Merge](#merge)
17. [Temporary Tables](#temporary-tables)
18. [Syntax Tree](#syntax-tree)
19. [Serialization to XML](#serialization-to-xml)
20. [Serialization to Plain List](#serialization-to-plain-list)
21. [Auto-Mapper](#auto-mapper)

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
Now let's get rid of the necessity in writing __"SqQueryBuilder."__:
```cs
using static SqExpress.SqQueryBuilder;
...
    var query = Select("Hello World!").Done();

    Console.WriteLine(TSqlExporter.Default.ToSql(query));

```
The result will be:
```
SELECT 'Hello World!'
```
## (Re)Creating Table 
Ok, let's try to select some data from a real table, but first we need to describe the table:
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
Now it is the time to insert some date in the table:
```cs
...
var data = new[]
{
    new {FirstName = "Francois", LastName = "Sturman"},
    new {FirstName = "Allina", LastName = "Freeborne"},
    new {FirstName = "Maye", LastName = "Maloy"},
};

await InsertDataInto(tUser, data)
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
var selectResult = await Select(tUser.UserId, tUser.FirstName, tUser.LastName)
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
await Update(tUser)
    .Set(tUser.LastName, "Malloy")
    .Set(tUser.Version, tUser.Version+1)
    .Set(tUser.ModifiedAt, GetUtcDate())
    .Where(tUser.LastName == "Maloy")
    .Exec(database);

//Writing to console without storing data in memory
await Select(tUser.Columns)
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
## Deleting data
Unfortunately, regardless the fact the typo is fixed, we have to say "Good Bye" to May*:
```cs
await Delete(tUser)
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
To crete more complex queries we need more than one table. Let's add a couple more:

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

await InsertDataInto(tCompany, new[] {"Microsoft", "Google"})
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
await InsertInto(tCustomer, tCustomer.UserId)
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
## Joining Tables
Now we can Join all the tables:
```cs
var tUser = new TableUser();
var tCompany = new TableCompany();
var tCustomer = new TableCustomer();

var cType = CustomColumnFactory.Int16("Type");
var cName = CustomColumnFactory.String("Name");

var customers = await Select(
        tCustomer.CustomerId,
        Case()
            .When(IsNotNull(tUser.UserId))
            .Then(Cast(Literal(1), SqlType.Int16))
            .When(IsNotNull(tCompany.CompanyId))
            .Then(Cast(Literal(2), SqlType.Int16))
            .Else(Null)
            .As(cType),
        Case()
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

        return Select(
                tCustomer.CustomerId.As(this.CustomerId),
                Case()
                    .When(IsNotNull(tUser.UserId))
                    .Then(Cast(Literal(1), SqlType.Int16))
                    .When(IsNotNull(tCompany.CompanyId))
                    .Then(Cast(Literal(2), SqlType.Int16))
                    .Else(Null)
                    .As(this.Type),
                Case()
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

var customers = await Select(tCustomer.Columns)
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
## Subquries
It is not necessary to create a new class when you need a subquery - it can be directly described in an original expression. It is enough just to predefine the aliases for columns and tables:
```cs
var num = CustomColumnFactory.Int32("3");
//Note: "3" (the first value) is for compatibility with MySql
//which does not properly support values constructors

var sum = CustomColumnFactory.Int32("Sum");

var numbers = Values(3, 1, 1, 7, 3, 7, 3, 7, 7, 8).AsColumns(num);
var numbersSubQuery = TableAlias();

var mostFrequentNum = (int) await
    SelectTop(1, numbersSubQuery.Column(num))
        .From(
            Select(numbers.Column(num), CountOne().As(sum))
                .From(numbers)
                .GroupBy(numbers.Column(num))
                .As(numbersSubQuery)
        )
        .OrderBy(Desc(numbersSubQuery.Column(sum)))
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
## Analytic And Window Functions
SqExpress supports common analytic and window functions like **ROW_NUMBER**, **RANK**, **FIRST_VALUE**, **LAST_VALUE** etc.
```cs
var cUserName = CustomColumnFactory.String("Name");
var cNum = CustomColumnFactory.Int64("Num");
var cFirst = CustomColumnFactory.String("First");
var cLast = CustomColumnFactory.String("Last");

var user = new TableUser();

await Select(
        (user.FirstName + " " + user.LastName)
        .As(cUserName),
        RowNumber()
            /*.OverPartitionBy(some fields)*/
            .OverOrderBy(user.FirstName)
            .As(cNum),
        FirstValue(user.FirstName + " " + user.LastName)
            /*.OverPartitionBy(some fields)*/
            .OverOrderBy(user.FirstName)
            .FrameClauseEmpty()
            .As(cFirst),
        LastValue(user.FirstName + " " + user.LastName)
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
var select1 = Select(1);
var select2 = Select(2);

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
You also can run all the scenarios using My SQL:
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
await MergeDataInto(tableUser, data)
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
await InsertInto(tmp, tmp.Name)
    .From(Select(tableUser.FirstName + " "+ tableUser.LastName)
    .From(tableUser))
    .Exec(database);

//Companies
await InsertInto(tmp, tmp.Name)
    .From(Select(tableCompany.CompanyName)
    .From(tableCompany))
    .Exec(database);

await Select(tmp.Columns)
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

## Syntax Tree
You can go through an existing syntax tree object and modify if it is required:
```cs
//Var some external filter..
ExprBoolean filter = CustomColumnFactory.Int16("Type") == 2 /*Company*/;

var tableCustomer = new TableCustomer();

var baseSelect = Select(tableCustomer.CustomerId)
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
For simpler scenarios you can just use “With…” functions:
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

await InsertDataInto(tableFavoriteFilterItem, filter1Items.Concat(filter2Items))
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
var restoredFilterItems = await Select(tableFavoriteFilterItem.Columns)
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
await Select(tableUser.FirstName, tableUser.LastName)
    .From(tableUser)
    .Where(restoredFilter1)
    .Query(database,
        (r) =>
        {
            Console.WriteLine($"{tableUser.FirstName.Read(r)} {tableUser.LastName.Read(r)}");
        });

Console.WriteLine("Filter 2");
await Select(tableUser.FirstName, tableUser.LastName)
    .From(tableUser)
    .Where(restoredFilter2)
    .Query(database,
        (r) =>
        {
            Console.WriteLine($"{tableUser.FirstName.Read(r)} {tableUser.LastName.Read(r)}");
        });
```
## Auto-Mapper
Since the DAL works on top the ADO you can use Auto-Mapper (if you like it):
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