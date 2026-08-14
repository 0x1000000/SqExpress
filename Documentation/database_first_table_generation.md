# Database-First Table Generation

Use database-first generation when the physical database schema is the source of truth. SqExpress connects to the database, reads its schema, and generates strongly typed table descriptors.

Supported databases:

| Database | Connection type |
|---|---|
| Microsoft SQL Server | `mssql` |
| MySQL or MariaDB | `mysql` |
| PostgreSQL | `pgsql` |

## Table of Contents

- [Quick Start](#quick-start)
  - [Windows command script](#windows-command-script)
  - [Unix shell script](#unix-shell-script)
  - [Package Manager Console](#package-manager-console)
- [Command-Line Interface Reference](#command-line-interface-reference)
  - [All CLI arguments and options](#all-cli-arguments-and-options)
- [`Gen-Tables` Package Manager Console Reference](#gen-tables-package-manager-console-reference)
  - [All `Gen-Tables` parameters](#all-gen-tables-parameters)
  - [Project file properties and defaults](#project-file-properties-and-defaults)
- [Generated Output](#generated-output)
- [Database Providers](#database-providers)
- [Output Location and Naming](#output-location-and-naming)
- [Splitting Tables by Schema](#splitting-tables-by-schema)
- [Filtering Tables](#filtering-tables)
- [Cleaning Obsolete Descriptors](#cleaning-obsolete-descriptors)
- [Unsupported Column Types](#unsupported-column-types)

## Quick Start

The command-line utility is included in the SqExpress NuGet package. The following helper scripts discover the latest installed package version automatically. Store the appropriate script in your project repository so developers and CI use the same repeatable generation command.

### Windows command script

Create `GenerateTables.cmd` in the project directory:

```cmd
@echo off
set root=%userprofile%\.nuget\packages\sqexpress

for /F "tokens=*" %%a in ('dir "%root%" /b /a:d /o:n') do set "lib=%root%\%%a"

set lib=%lib%\tools\codegen\SqExpress.CodeGenUtil.dll

dotnet "%lib%" gentables mssql "MyConnectionString" --table-class-prefix "Tbl" -o ".\Tables" -n "MyCompany.MyProject.Tables" --use-table-declaration-attributes
```

### Unix shell script

Create `GenerateTables.sh` in the project directory:

```sh
#!/bin/bash

lib=~/.nuget/packages/sqexpress/$(ls ~/.nuget/packages/sqexpress -r|head -n 1)/tools/codegen/SqExpress.CodeGenUtil.dll

dotnet $lib gentables mssql "MyConnectionString" --table-class-prefix "Tbl" -o "./Tables" -n "MyCompany.MyProject.Tables" --use-table-declaration-attributes
```

### Package Manager Console

In Visual Studio, run the equivalent `Gen-Tables` command:

```powershell
Gen-Tables mssql `
  -ConnectionString "Server=(local);Database=AppDb;Integrated Security=True;TrustServerCertificate=True" `
  -UseTableDeclarationAttributes
```

With no additional parameters, Package Manager Console writes files to `Tables`, relative to the selected project directory. Attribute-based declarations are requested explicitly because they are the recommended output.

## Command-Line Interface Reference

For scripts, CI, or environments without Visual Studio Package Manager Console, invoke the packaged utility directly:

```powershell
dotnet "<package>\tools\codegen\SqExpress.CodeGenUtil.dll" `
  gentables mssql "<connection-string>" `
  --include "sales.*;Customer?" `
  --exclude "*.OrderArchive*" `
  --use-table-declaration-attributes
```

Replace `<package>` with the installed SqExpress package directory. The first two positional values are the connection type and connection string. A relative `--output-dir` path is resolved from the process's current working directory.

### All CLI arguments and options

| Argument or option | Required | Default | Description |
|---|---:|---|---|
| `CONNECTION_TYPE` | yes | none | `mssql`, `mysql`, or `pgsql`. |
| `CONNECTION_STRING` | yes | none | Database connection string. |
| `--table-class-prefix <value>` | no | `Table` | Prefix for generated descriptor class names. |
| `-o`, `--output-dir <path>` | no | current working directory | Directory for generated `.cs` files. Relative paths use the process's current working directory. |
| `-n`, `--namespace <value>` | no | `MyCompany.MyApp.Tables` | Base namespace for generated descriptors. |
| `-v`, `--verbosity <value>` | no | `minimal` | `quiet`, `minimal`, `normal`, or `detailed`. |
| `--use-table-declaration-attributes` | no | `false` | Generate attribute-based partial declarations instead of complete `TableBase` classes. |
| `--skip-unknown-column-types` | no | `false` | Omit unsupported columns instead of failing generation. |
| `--split-tables-by-schema` | no | `false` | Create schema-specific folders and namespace segments. |
| `--clean-output` | no | `false` | Remove obsolete recognized descriptors after successful generation. |
| `--include <patterns>` | no | all tables | Semicolon-separated, case-insensitive include patterns supporting `*` and `?`. |
| `--exclude <patterns>` | no | none | Semicolon-separated exclude patterns applied after includes. Exclusions win. |

Boolean options are switches: include an option to enable it and omit it to keep the default. Quote filter lists so the shell does not expand wildcard characters.

## `Gen-Tables` Package Manager Console Reference

General syntax:

```powershell
Gen-Tables -DbType {mssql | mysql | pgsql} `
  -ConnectionString <string> `
  [-OutputDir <string>] `
  [-TableClassPrefix <string>] `
  [-Namespace <string>] `
  [-Verbosity {Quiet | Minimal | Normal | Detailed}] `
  [-UseTableDeclarationAttributes] `
  [-SkipUnknownColumnTypes] `
  [-SplitTablesBySchema] `
  [-CleanOutput] `
  [-Include <string[]>] `
  [-Exclude <string[]>]
```

`DbType` and `ConnectionString` may also be supplied positionally, as shown in the Quick Start.

### All `Gen-Tables` parameters

| Parameter | Required | Default | Description |
|---|---:|---|---|
| `DbType` | yes | none | `mssql`, `mysql`, or `pgsql`. First positional argument. |
| `ConnectionString` | yes | none | Database connection string. Second positional argument or `-ConnectionString`. |
| `OutputDir` | no | project property `SqTablesGenOutput`; otherwise `Tables` | Output directory. Relative paths use the selected project directory. |
| `TableClassPrefix` | no | project property `SqTablesGenTableClassPrefix`; otherwise `Table` | Prefix for generated descriptor class names. |
| `Namespace` | no | project property `SqTablesGenNamespace`; otherwise selected project name plus output path | Base namespace for generated descriptors. |
| `Verbosity` | no | `Minimal` | `Quiet`, `Minimal`, `Normal`, or `Detailed`. |
| `UseTableDeclarationAttributes` | no | project property `SqTablesGenUseTableDeclarationAttributes`; otherwise `false` | Generate attribute-based partial declarations. |
| `SkipUnknownColumnTypes` | no | project property `SqTablesGenSkipUnknownColumnTypes`; otherwise `false` | Omit unsupported columns instead of failing. |
| `SplitTablesBySchema` | no | project property `SqTablesGenSplitTablesBySchema`; otherwise `false` | Create schema-specific folders and namespaces. |
| `CleanOutput` | no | project property `SqTablesGenCleanOutput`; otherwise `false` | Remove obsolete recognized descriptors. |
| `Include` | no | project property `SqTablesGenInclude`; otherwise all tables | One or more case-insensitive table patterns. |
| `Exclude` | no | project property `SqTablesGenExclude`; otherwise none | Patterns applied after includes. Exclusions win. |

An explicit `Gen-Tables` parameter overrides the corresponding project property.

### Project file properties and defaults

Store reusable Package Manager Console defaults in the selected `.csproj`:

```xml
<PropertyGroup>
  <SqTablesGenOutput>Tables</SqTablesGenOutput>
  <SqTablesGenNamespace>$(MSBuildProjectName).Tables</SqTablesGenNamespace>
  <SqTablesGenTableClassPrefix>Table</SqTablesGenTableClassPrefix>
  <SqTablesGenUseTableDeclarationAttributes>true</SqTablesGenUseTableDeclarationAttributes>
  <SqTablesGenSkipUnknownColumnTypes>false</SqTablesGenSkipUnknownColumnTypes>
  <SqTablesGenSplitTablesBySchema>false</SqTablesGenSplitTablesBySchema>
  <SqTablesGenCleanOutput>false</SqTablesGenCleanOutput>
  <SqTablesGenInclude></SqTablesGenInclude>
  <SqTablesGenExclude></SqTablesGenExclude>
</PropertyGroup>
```

| Project property | Default when unset | Description |
|---|---|---|
| `SqTablesGenOutput` | `Tables` | Output directory for generated `.cs` files. |
| `SqTablesGenNamespace` | selected project name plus output path | Base namespace for generated descriptors. |
| `SqTablesGenTableClassPrefix` | `Table` | Prefix for generated descriptor class names. |
| `SqTablesGenUseTableDeclarationAttributes` | `false` | Generate attribute-based declarations instead of complete `TableBase` classes. |
| `SqTablesGenSkipUnknownColumnTypes` | `false` | Omit unsupported columns instead of failing. |
| `SqTablesGenSplitTablesBySchema` | `false` | Create schema-specific folders and namespaces. |
| `SqTablesGenCleanOutput` | `false` | Remove obsolete recognized descriptors. |
| `SqTablesGenInclude` | all tables | Semicolon-separated include patterns. |
| `SqTablesGenExclude` | none | Semicolon-separated exclude patterns applied after includes. |

The precedence order is: explicit `Gen-Tables` parameter, `SqTablesGen*` property, older misspelled `SqTablseGen*` property, then the documented default. For example, when `SqTablesGenSplitTablesBySchema` is `true`, `Gen-Tables` enables schema splitting unless the invocation explicitly supplies `-SplitTablesBySchema:$false`.

## Generated Output

The recommended attribute-based output consists of compact partial classes:

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

Without `--use-table-declaration-attributes` or `-UseTableDeclarationAttributes`, the generator creates complete `TableBase` classes. This remains supported for existing projects.

## Database Providers

Change the connection type for the target database.

Package Manager Console examples:

```powershell
Gen-Tables mysql -ConnectionString "<connection-string>" -UseTableDeclarationAttributes
Gen-Tables pgsql -ConnectionString "<connection-string>" -UseTableDeclarationAttributes
```

The CLI uses the same `mysql` and `pgsql` connection-type values.

## Output Location and Naming

CLI example:

```powershell
dotnet "<package>\tools\codegen\SqExpress.CodeGenUtil.dll" `
  gentables mssql "<connection-string>" `
  --output-dir ".\Tables" `
  --namespace "MyApp.Data.Tables" `
  --table-class-prefix "Table" `
  --use-table-declaration-attributes
```

Package Manager Console uses `-OutputDir`, `-Namespace`, and `-TableClassPrefix`. CLI relative paths use the current working directory; Package Manager Console relative paths use the selected project directory.

## Splitting Tables by Schema

Enable schema-specific directories and namespace segments:

```powershell
# CLI
dotnet "<package>\tools\codegen\SqExpress.CodeGenUtil.dll" gentables mssql "<connection-string>" --split-tables-by-schema

# Package Manager Console
Gen-Tables mssql -ConnectionString "<connection-string>" -SplitTablesBySchema
```

For example, schema `sales-data` becomes directory `Tables/SalesData` and namespace segment `SalesData`. Tables without a schema use `Default`. `AllTables.cs` remains in the root output directory.

Generation fails when normalized schemas would produce colliding paths or type names; it does not silently overwrite one table with another.

## Filtering Tables

Filter by physical schema and table name:

```powershell
# CLI
dotnet "<package>\tools\codegen\SqExpress.CodeGenUtil.dll" `
  gentables mssql "<connection-string>" `
  --include "sales.*;Customer?" `
  --exclude "*.OrderArchive*"

# Package Manager Console
Gen-Tables mssql `
  -ConnectionString "<connection-string>" `
  -Include "sales.*", "Customer?" `
  -Exclude "*.OrderArchive*"
```

A qualified pattern matches the complete `schema.table` name; an unqualified pattern matches table names in every schema. Matching is case-insensitive. `*` matches zero or more characters and `?` matches one character. A table must match an include when includes are configured, after which exclusions are applied and always win.

Foreign-key metadata pointing to a filtered-out table is omitted. When cleanup is enabled, files belonging to filtered-out tables are removed.

## Cleaning Obsolete Descriptors

Use `--clean-output` in the CLI or `-CleanOutput` in Package Manager Console to make the generated directory mirror the latest database metadata.

Cleanup runs only after metadata is read successfully. It recognizes direct descriptors and attribute-based declarations. A file is deleted only when no other type declarations remain.

Keep cleanup disabled when the output directory contains table descriptors maintained by hand. Enable it only when the directory is generator-owned.

## Unsupported Column Types

By default, an unsupported database column type stops generation. Use `--skip-unknown-column-types` in the CLI or `-SkipUnknownColumnTypes` in Package Manager Console to report and omit unsupported columns instead.

Review every reported omission because the generated descriptor will not represent the complete table.

For generation from an EF Core model, see [Entity Framework Table Generation](ef_table_generation.md).
