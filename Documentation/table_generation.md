# Table Descriptor Generation

SqExpress supports two independent table-generation workflows. Choose the guide that matches the source of truth for your schema:

1. [Database-First Table Generation](database_first_table_generation.md)  
   Generate descriptors by connecting directly to Microsoft SQL Server, MySQL/MariaDB, or PostgreSQL.

2. [Entity Framework Table Generation](ef_table_generation.md)  
   Generate descriptors automatically from an EF Core SQL Server model during `dotnet build`.

For new projects, attribute-based table declarations are recommended in both workflows. See the [Table Description Reference](table_description.md) for the available declaration attributes.

## Shared Project Properties

Both workflows use the correctly spelled `SqTablesGen*` family for output behavior. For example:

```xml
<PropertyGroup>
  <SqTablesGenOutput>Tables</SqTablesGenOutput>
  <SqTablesGenNamespace>$(MSBuildProjectName).Tables</SqTablesGenNamespace>
  <SqTablesGenTableClassPrefix>Table</SqTablesGenTableClassPrefix>
  <SqTablesGenUseTableDeclarationAttributes>true</SqTablesGenUseTableDeclarationAttributes>
  <SqTablesGenSkipUnknownColumnTypes>false</SqTablesGenSkipUnknownColumnTypes>
  <SqTablesGenSplitTablesBySchema>true</SqTablesGenSplitTablesBySchema>
  <SqTablesGenCleanOutput>true</SqTablesGenCleanOutput>
  <SqTablesGenInclude>sales.*;Customer?</SqTablesGenInclude>
  <SqTablesGenExclude>*.Archive*</SqTablesGenExclude>
</PropertyGroup>
```

These settings provide defaults for `Gen-Tables` and configure automatic EF generation. EF activation and metadata-source selection continue to use `SqEfTablesGenEnable`, `SqEfTablesGenProject`, `SqEfTablesGenDbContext`, and `SqEfTablesGenFramework`.

The released, misspelled `SqTablseGen*` equivalents remain supported as fallbacks. Precedence is: explicit `Gen-Tables` parameter, `SqTablesGen*`, `SqTablseGen*`, then the tool or workflow default.
