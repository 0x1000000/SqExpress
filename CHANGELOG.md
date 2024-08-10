# 1.1.0
### New Features
- Dynamic table metadata creation/modification with ```SqTable``` class
### Bugfix
- It adds conversion from **float** to **double** when reading from column with **real** type.
- **varbinary** column explicit length now limited with 8000. Leave the length unspecified to get **max** length 
# 1.0.0
### New Features
- Retrieving of database tables metadata + comparison
- **MERGE** expression builder
- Table Value Functions
- All aggregate functions now can be used with ```OVER(...)```
- SqDatabase can query IAsyncEnumerable&lt;ISqRecord&gt; _(for .Net 6)_
- SqDatabase supports async version for all I/O operation _(for .Net 6)_
- ```expr.SyntaxTree().ModifyDescendants(modifier)``` new method that returns object of the same type.
- arguments of **OR** and **AND** operators now can be nullable _(for .Net 6)_
- **DateDiff** function + polyfills for Postgresql and MySql
- ```GetParentTables()``` for TableBase()

### Breaking Changes
- Values Constructor in MYSQL now is implemented through **UNION ALL** 
- .Net Framework is not supported anymore, however, .Net Standard 2.0 is still supported
- .Net less than 6 now use .Net Standard 2.0 which does not have some functions
- SyntaxTreeActions structure now has a generic parameter

# 0.4.1.0
### New Features
- Bitwise operators (**Warning!** C# operator precedence is used)

# 0.4.0.1
### Bugfix
- Infinite loop when creating tables with foreign keys from multiple threads

# 0.4.0.0
### New Features
- CTE (see [readme.md](https://github.com/0x1000000/SqExpress#cte))
- ```SqQueryBuilder.ValueQuery(...)``` - It allows using a sub-query as a value (e.g. in boolean expressions or a column)
- Powershell commandlets **Gen-Tables** and  **Gen-Models** in Package Manager Console
- Syntax Tree **WalkThrough** method has a new overload with a link to parent node
- SqModels now have new methods: **GetColumnsWithPrefix**, **ReadWithPrefix**, **IsNull**, **IsNullWithPrefix**

### Breaking Changes

- **Concat** helper was renamed to **Combine** (to avoid a conflict with Linq)

# 0.3.3.0
### New Features
- Support of DateTimeOffset
- ```InsertInto(t, c1, c2, ...).Values(v1,v2,..).Values(v1,v2,..)...DoneWithValues()```
### Bugfix
- StackOverflow on recursive foreign keys

# 0.3.2.0
### New Features
- Cancellation Token for all db calls
- Code-gen can be used with .Net 5 and 6 runtimes

# 0.3.1.0
### New Features
- the DTO code-generator now has a parameter that allows generating C# records: ```--model-type ImmutableClass|Record``` or ```<SqModelGenType>ImmutableClass|Record</SqModelGenType>```;
- "CheckExistenceBy" in the Insert data builder what adds WHERE EXISTS(...) to a Insert source to avoid duplicates inserting;
- ExistsIn&lt;TTable&gt;(..predicate..) ... - Helper that returns boolean expression ```EXISTS(SELECT 1 FROM TTable WHERE ..Predicate..)```
- QueryPage extension for OffsetFetch queries.
### Bugfix
- When some column(s) in values constructor contains only nulls, sqexpress now adds an explicit type cast for the first cell e.g. ```CAST(NULL as int)```
- **MergeDataInto** now allows only keys mapping unless **WhenMatchThenUpdate** is defined (without additional updates)
- Derived tables now can have 0 declared columns

# 0.3.0.0
### New Features
- "ReadAsString(recordReader:ISqDataRecordReader): string" and "FromString(value: string?): ExprLiteral" are added to TableColumn. They allow performing mass export/import of database data.
- Lt Gt operators overload between columns
- ColumnMeta is public now
- Identity Insert
- SqModelSelectBuilder - Fluent API that allows quickly get a tuple of DTO SqModels from tables join
- Column method "Read" now has an overload that receives an ordinal index
- "ReadOrdinal" was added to ISqModelReader
- ThenBy for ExprOrderItem andExprOrderBy
- ISqDatabase has a new overload for Query which receives an asynchronous record handler
- Build targets to run model code-generation
- new option "--clean-output" for the model code-generation tool

### Breaking Changes
- ISqModelDerivedReaderReader was removed. ISqModelReader was slightly changed;
- CodeGenUtil - typo fixed in parameter "-v quiet" 

# 0.2.0.0
### New Features
- "MERGE" polyfill for PostgreSql and MYSQL
- ISqModelReader;ISqModelUpdater (--rw-classes key in the code-generation utility)
- SqModel for temporary and derived tables
- Keep class modifiers in the code generation
- New expression modifiers: AddOrderBy, AddOffsetFetch, JoinAsAnd, JoinAsOr
- New query extension: QueryDictionary
- ISqDatabase.BeginTransactionOrUseExisting(out bool isNewTransaction);
- SyntaxTree().Descendants(); SyntaxTree().DescendantsAndSelf();
- UpdateData builder
### Bug Fix
- ISqDatabase.BeginTransaction now does not throw an exception if connection was not opened (it is automatically opened on the first request)
- Duplicates in model code-generation when one model is used for several tables
### Breaking Changes
- Some new methods were added in some public interfaces, so if you have own implementations then they need to be extended.


# 0.1.0.0
### New Features
- Code-generation utility
  - Table descriptors scaffolding
  - DTO models generation
- .Net Framework 4.5
- Binary Column Type
- Fixed Size String Column Type
- Xml Column Type
- Export/Import to JSON (for .Net Core 3.1+)
- ISqDatabase.BeginTransaction()
### Bugfix
- Several Foreign Keys for one table column

### Breaking Changes
- Some public interfaces have new methods to be implemented
- ValueTuples are not used anymore (replaced with explicit structures to compatibility with .Net Framework 4.5)
# 0.0.7.1
### New Features
- Cast does not require "Literal" for literals.
### Bugfix
- Explicit database name for tables in MySql
# 0.0.7.0
### New Features
- My SQL
- All Columns .*
- Values Constructor as Sub-query
- Modulo operator
- Query Handler
- Analytic Functions
- Sub-queries without derived tables 
- Post "Done" modifications### Bugfix
- Explicit database name for tables in MySql</PackageReleaseNotes>
### Bugfix
- '+' instead of '-' in arithmetic expressions
- Double “LIMIT” with “ORDER BY”</PackageReleaseNotes>
# 0.0.6.0
### New Features
- Table descriptors support indexes declarations
- Table descriptors support default values for columns
- Unsafe value expression
# 0.0.5.0
### New Features
- “Where” now accepts nulls
- expr.SyntaxTree().WalkThrough()
- expr.SyntaxTree().Find()
- expr.SyntaxTree().Modify
- ExprDeserializer.Deserialize()
- Database name can be specified in table names
- Temp Tables
- Top -> Limit in PostgreSQL
- DateAdd
- Export/Import to/from PlainList
- Export/Import to/from Xml
- Custom scalar functions
### Braking Changes:
- SqlBuilderBase is now internal
- IExprVisitor changed
