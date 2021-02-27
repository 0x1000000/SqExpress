# 0.1.0.0
### New Features
- .Net Framework 4.5
- Binary Column Type
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