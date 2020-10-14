# SqExpress
![Logo](https://github.com/0x1000000/SqExpress/blob/main/SqExpress/Icon.png)

The library provides a generic sql syntax tree with export to MS t-SQL and Postgres SQL text.

It also provides a set of builders and operators which will help you building complex Sql expressions.

It does not use LINQ and your C# code will be close to real SQL as much as possible, so it can be used when you need the full SQL flexibility to create efficient Db requests.

It is delivered with a simple but efficient data access mechanism which warps ADO.Net DbConnection and can be used with MS SQL Client or Npgsql.

It can be used together with the “Code First” concept when you declare SQL tables as C# classes with possibility to generate recreation scripts for a target platform (MS SQL or Postgres SQL)

# Content
1. [Get Started](#get-started)
2. [Recreating Table](#recreating-table)
3. [Inserting Data](#inserting-data)
4. [Selecting Data](#selecting-data)
5. [Updating Data](#updating-data)
6. [Deleting Data](#deleting-data)
7. [More Tables and foreign keys](#more-tables-and-foreign-keys)
8. [Joining Tables](#joining-tables)
9. [Derived Table](#derived-table)
10. [Postgres Sql](#postgres-sql)
11. [Merge](#merge)

# Get Started

Add a reference to the library: 
```
Install-Package SqExpress -Version 0.0.3.1
```
and start with "Hello World":
```cs
static void Main()
{
    var query = SqQueryBuilder
        .Select(SqQueryBuilder.Literal("Hello World!")).Done();

    Console.WriteLine(TSqlExporter.Default.ToSql(query));
}
```
Now let's get rid of the necessity in writing __"SqQueryBuilder."__:
```cs
using static SqExpress.SqQueryBuilder;
...
    var query = Select(Literal("Hello World!")).Done();

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

        this.Version = this.CreateInt32Column("Version");
        this.ModifiedAt = this.CreateDateTimeColumn("ModifiedAt");
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
    DROP TABLE [dbo].[User]
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
    .Query(database, (object)null, (agg, record)=>
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
    .Query(database, (object)null, (agg, record)=>
    {
        Console.WriteLine("Removed user id: " + tUser.UserId.Read(record));
        return agg;
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

        this.Version = this.CreateInt32Column("Version");
        this.ModifiedAt = this.CreateDateTimeColumn("ModifiedAt");
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
    }
}
```
Pay attention to the way how the foreign keys are defined:
```cs
ColumnMeta.ForeignKey<TableUser>(u => u.UserId)
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
    .Query(database, (object) null, (agg,r) =>
    {
        Console.WriteLine($"Id: {tCompany.CompanyId.Read(r)}, Name: {tCompany.CompanyName.Read(r)}");
        return null;
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
## Derived Table

This query is quite complex so it makes sense to store it as a derived table and reuse it in future:
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
## Postgres SQL
You can run all the scenarios using Postgres SQL (of course the actual sql will be different):
```
DbCommand NpgsqlCommandFactory(NpgsqlConnection connection, string sqlText)
{
    return new NpgsqlCommand(sqlText, connection);
}

using (var connection = new NpgsqlConnection("Host=localhost;Port=5432;Username=postgres;Password=test;Database=test"))
{
    using (var database = new SqDatabase<NpgsqlConnection>(
        connection: connection,
        commandFactory: NpgsqlCommandFactory,
        sqlExporter: new PgSqlExporter(builderOptions: SqlBuilderOptions.Default
            .WithSchemaMap(schemaMap: new[] {new SchemaMap(@from: "dbo", to: "public")}))))
    {
        ...
    }
}
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
        (object) null,
        (agg, r) =>
        {
            Console.WriteLine($"UserId Inserted: {inserted.Read(r)},UserId Deleted: {deleted.Read(r)} , Action: {action.Read(r)}");
            return agg;
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
