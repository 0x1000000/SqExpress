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