# Database-First Table Generation

Use database-first generation when the physical database schema is the source of truth.

Supported databases:

| Database | Mode |
|---|---|
| Microsoft SQL Server | `mssql` |
| MySQL or MariaDB | `mysql` |
| PostgreSQL | `pgsql` |

## Quick Start

Run `Gen-Tables` in Visual Studio's Package Manager Console:

```powershell
Gen-Tables mssql `
  -ConnectionString "Server=(local);Database=AppDb;Integrated Security=True;TrustServerCertificate=True" `
  -UseTableDeclarationAttributes
```

The command connects to the database, reads its schema, and generates strongly typed table descriptors.

With no additional parameters, files are written to `Tables`, relative to the selected project directory. Attribute-based declarations are requested explicitly because they are the recommended output.

## Other Databases

MySQL or MariaDB:

```powershell
Gen-Tables mysql `
  -ConnectionString "<connection-string>" `
  -UseTableDeclarationAttributes
```

PostgreSQL:

```powershell
Gen-Tables pgsql `
  -ConnectionString "<connection-string>" `
  -UseTableDeclarationAttributes
```

## Generated Output

Attribute-based generation produces compact partial classes:

```csharp
using SqExpress.TableDeclarationAttributes;

[TableDescriptor("app", "Customer")]
public partial class TableCustomer
{
    [Int32Column("CustomerId", PrimaryKey = true, Identity = true)]
    public Int32TableColumn CustomerId { get; set; } = null!;

    [StringColumn("Name", MaxLength = 200, IsUnicode = true)]
    public StringTableColumn Name { get; set; } = null!;
}
```

The SqExpress source generator supplies the `TableBase` implementation. See the [Table Description Reference](table_description.md) for all declaration attributes.

Without `-UseTableDeclarationAttributes`, `Gen-Tables` generates complete `TableBase` classes. This remains supported for existing projects.

## Common Examples

Choose the output directory, namespace, and class prefix:

```powershell
Gen-Tables mssql `
  -ConnectionString "<connection-string>" `
  -OutputDir Tables `
  -Namespace MyApp.Data.Tables `
  -TableClassPrefix Table `
  -UseTableDeclarationAttributes
```

`OutputDir` is relative to the selected project directory unless an absolute path is supplied.

Organize tables by database schema:

```powershell
Gen-Tables mssql `
  -ConnectionString "<connection-string>" `
  -SplitTablesBySchema `
  -UseTableDeclarationAttributes
```

Remove descriptors that no longer exist in the database:

```powershell
Gen-Tables mssql `
  -ConnectionString "<connection-string>" `
  -CleanOutput `
  -UseTableDeclarationAttributes
```

Skip unsupported database column types:

```powershell
Gen-Tables mssql `
  -ConnectionString "<connection-string>" `
  -SkipUnknownColumnTypes `
  -UseTableDeclarationAttributes
```

Review every reported omission because the generated descriptor will not represent the complete table.

Filter tables by physical schema and table name:

```powershell
Gen-Tables mssql `
  -ConnectionString "<connection-string>" `
  -Include "sales.*", "Customer?" `
  -Exclude "*.OrderArchive*" `
  -UseTableDeclarationAttributes
```

Patterns containing `.` match the complete `schema.table` name. Patterns without `.` match table names in every schema. Matching is case-insensitive; `*` matches zero or more characters and `?` matches exactly one character. When includes are supplied, a table must match at least one include. Excludes are applied afterward and always win.

Foreign-key metadata pointing to a filtered-out table is omitted. With `-CleanOutput`, descriptor files for filtered-out tables are removed, so use cleanup only when the output directory is generator-owned.

## Project Defaults

Set reusable `Gen-Tables` defaults in the selected project:

```xml
<PropertyGroup>
  <SqTablesGenOutput>Tables</SqTablesGenOutput>
  <SqTablesGenNamespace>$(MSBuildProjectName).Tables</SqTablesGenNamespace>
  <SqTablesGenTableClassPrefix>Table</SqTablesGenTableClassPrefix>
  <SqTablesGenUseTableDeclarationAttributes>true</SqTablesGenUseTableDeclarationAttributes>
  <SqTablesGenSkipUnknownColumnTypes>false</SqTablesGenSkipUnknownColumnTypes>
  <SqTablesGenSplitTablesBySchema>true</SqTablesGenSplitTablesBySchema>
  <SqTablesGenCleanOutput>false</SqTablesGenCleanOutput>
  <SqTablesGenInclude>sales.*;Customer?</SqTablesGenInclude>
  <SqTablesGenExclude>*.Archive*</SqTablesGenExclude>
</PropertyGroup>
```

When `Gen-Tables` is called without a corresponding parameter, a stored project property overrides the command's built-in default. An explicitly supplied `Gen-Tables` parameter has higher priority and overrides the stored property.

### All Stored Table-Generation Properties

| Project property | `Gen-Tables` parameter | Default when unset | Description |
|---|---|---|---|
| `SqTablesGenOutput` | `OutputDir` | `Tables` | Output directory for generated `.cs` files. |
| `SqTablesGenNamespace` | `Namespace` | selected project name plus output path | Base namespace for generated descriptors. |
| `SqTablesGenTableClassPrefix` | `TableClassPrefix` | `Table` | Prefix for generated descriptor class names. |
| `SqTablesGenUseTableDeclarationAttributes` | `UseTableDeclarationAttributes` | `false` | Generate attribute-based partial declarations instead of complete `TableBase` classes. |
| `SqTablesGenSkipUnknownColumnTypes` | `SkipUnknownColumnTypes` | `false` | Omit unsupported columns instead of failing generation. |
| `SqTablesGenSplitTablesBySchema` | `SplitTablesBySchema` | `false` | Create schema-specific output folders and namespace segments. |
| `SqTablesGenCleanOutput` | `CleanOutput` | `false` | Remove obsolete recognized descriptors after successful generation. |
| `SqTablesGenInclude` | `Include` | all tables | Semicolon-separated, case-insensitive include patterns supporting `*` and `?`. |
| `SqTablesGenExclude` | `Exclude` | none | Semicolon-separated exclude patterns applied after includes. Exclusions always win. |

For example, if `SqTablesGenSplitTablesBySchema` is `true`, calling `Gen-Tables` without `-SplitTablesBySchema` enables schema splitting. Passing `-SplitTablesBySchema:$false` explicitly disables it for that invocation.

The released, misspelled `SqTablseGen*` equivalents remain supported as lower-priority fallbacks. The complete precedence order is: explicit `Gen-Tables` parameter, `SqTablesGen*`, `SqTablseGen*`, then the default shown above. See [Table Descriptor Generation](table_generation.md#shared-project-properties) for the shared property overview.

## Schema Folders

`-SplitTablesBySchema` creates a folder and namespace segment for each schema.

For example, schema `sales-data` becomes:

- directory `Tables/SalesData`;
- namespace `MyApp.Tables.SalesData`.

Tables without a schema use `Default`. `AllTables.cs` remains in the root output directory.

Generation fails when normalized schemas would produce colliding paths or type names. It does not silently overwrite one table with another.

## Cleaning Obsolete Descriptors

`-CleanOutput` makes the generated directory mirror the latest database metadata.

Cleanup runs only after metadata is read successfully. It recognizes direct descriptors and attribute-based declarations. A file is deleted only when no other type declarations remain.

Keep cleanup disabled when the output directory contains table descriptors maintained by hand.

## All `Gen-Tables` Database-First Parameters

| Parameter | Required | Default | Description |
|---|---:|---|---|
| `DbType` | yes | none | `mssql`, `mysql`, or `pgsql`. First positional argument. |
| `ConnectionString` | yes | none | Database connection string. Can be supplied as the second positional argument or with `-ConnectionString`. |
| `OutputDir` | no | project property `SqTablesGenOutput`; otherwise `Tables` | Directory for generated `.cs` files. A relative path is resolved from the selected project directory. |
| `TableClassPrefix` | no | project property `SqTablesGenTableClassPrefix`; otherwise tool default `Table` | Prefix for generated descriptor class names. |
| `Namespace` | no | project property `SqTablesGenNamespace`; otherwise selected project name plus output path | Base namespace for generated descriptors. |
| `UseTableDeclarationAttributes` | no | project property `SqTablesGenUseTableDeclarationAttributes`; otherwise `false` | Generates attribute-based partial declarations. |
| `SkipUnknownColumnTypes` | no | project property `SqTablesGenSkipUnknownColumnTypes`; otherwise `false` | Omits unsupported columns instead of failing. |
| `SplitTablesBySchema` | no | project property `SqTablesGenSplitTablesBySchema`; otherwise `false` | Creates schema-specific folders and namespace segments. |
| `CleanOutput` | no | project property `SqTablesGenCleanOutput`; otherwise `false` | Removes obsolete recognized descriptors after successful generation. |
| `Include` | no | project property `SqTablesGenInclude`; otherwise all tables | One or more case-insensitive table patterns. Use `*` and `?`; qualify with `schema.` to restrict a pattern to a schema. |
| `Exclude` | no | project property `SqTablesGenExclude`; otherwise none | One or more table patterns applied after includes. Excludes always win. |

The older misspelled `SqTablseGen*` names remain supported. `SqTablesGen*` takes precedence when both forms are set, and an explicit `Gen-Tables` parameter takes precedence over either property.

## Pure CLI

For scripts, CI, or environments without Visual Studio Package Manager Console, invoke the utility included in the NuGet package:

```powershell
dotnet "<package>\tools\codegen\SqExpress.CodeGenUtil.dll" `
  gentables mssql "<connection-string>" `
  --include "sales.*;Customer?" `
  --exclude "*.OrderArchive*" `
  --use-table-declaration-attributes
```

For the pure CLI, a relative `--output-dir` path is resolved from the process's current working directory.

### All Database-First CLI Arguments and Options

| Argument or option | Required | Default | Description |
|---|---:|---|---|
| `CONNECTION_TYPE` | yes | none | `mssql`, `mysql`, or `pgsql`. |
| `CONNECTION_STRING` | yes | none | Database connection string. |
| `--table-class-prefix <value>` | no | `Table` | Prefix for generated descriptor class names. |
| `-o`, `--output-dir <path>` | no | current working directory | Directory for generated `.cs` files. A relative path is resolved from the current working directory. |
| `-n`, `--namespace <value>` | no | `MyCompany.MyApp.Tables` | Base namespace for generated descriptors. |
| `-v`, `--verbosity <value>` | no | `minimal` | Logging level: `quiet`, `minimal`, `normal`, or `detailed`. |
| `--use-table-declaration-attributes` | no | `false` | Generates attribute-based partial declarations. |
| `--skip-unknown-column-types` | no | `false` | Omits unsupported columns instead of failing. |
| `--split-tables-by-schema` | no | `false` | Creates schema-specific folders and namespace segments. |
| `--clean-output` | no | `false` | Removes obsolete recognized descriptors after successful generation. |
| `--include <patterns>` | no | all tables | Semicolon-separated case-insensitive include patterns supporting `*` and `?`. |
| `--exclude <patterns>` | no | none | Semicolon-separated exclude patterns, applied after includes. |

Boolean CLI options are switches: include the option to enable it and omit the option to keep its default value.
Quote filter lists so the shell does not expand wildcard characters. Supply multiple patterns as a semicolon-separated list.

For generation from an EF Core model, see [Entity Framework Table Generation](ef_table_generation.md).
