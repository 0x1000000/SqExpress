# Entity Framework Table Generation

Use EF table generation when an Entity Framework Core relational model is the source of truth.

The recommended workflow uses MSBuild project properties. Generation then runs automatically during a normal build.

## Table of Contents

- [Quick Start](#quick-start)
- [All EF Generation Project Properties](#all-ef-generation-project-properties)
- [Selecting a `DbContext`](#selecting-a-dbcontext)
- [Multi-Targeted Projects](#multi-targeted-projects)
- [Output and Naming](#output-and-naming)
- [Attribute-Based Declarations](#attribute-based-declarations)
- [Schema Folders](#schema-folders)
- [Cleaning Obsolete Descriptors](#cleaning-obsolete-descriptors)
- [Filtering Tables](#filtering-tables)
- [Unsupported Column Types](#unsupported-column-types)
- [Command-Line Interface](#command-line-interface)
  - [Reusable project scripts](#reusable-project-scripts)
    - [Windows command script](#windows-command-script)
    - [Unix shell script](#unix-shell-script)
  - [All EF CLI arguments and options](#all-ef-cli-arguments-and-options)
- [Package Manager Console](#package-manager-console)
  - [All `Gen-Tables ef` parameters](#all-gen-tables-ef-parameters)

## Quick Start

Add the SqExpress package to the project containing the `DbContext`, then add:

```xml
<PropertyGroup>
  <SqEfTablesGenEnable>true</SqEfTablesGenEnable>
</PropertyGroup>
```

That single property is enough to start table-descriptor generation:

```powershell
dotnet build
```

With no other configuration, SqExpress:

- reads the EF model from the current project;
- uses the current target framework;
- automatically selects the `DbContext` when it is unambiguous;
- writes generated files to `Tables`, relative to the project directory;
- uses namespace `$(MSBuildProjectName).Tables`;
- uses the effective class prefix `Table`;
- generates attribute-based table declarations;
- skips unsupported column types;
- leaves obsolete descriptors in place.

EF generation currently supports models configured with `Microsoft.EntityFrameworkCore.SqlServer`. It reads the configured relational model and does not require a reachable database.

## All EF Generation Project Properties

EF-specific properties select and activate the EF metadata source. Shared `SqTablesGen*` properties control the generated output and are also used by `Gen-Tables`.

The following block shows the generation properties together. It is a reference, not a recommended block to copy in full. In an application project, specify only `SqEfTablesGenEnable` and the defaults that need to be changed.

```xml
<PropertyGroup>
  <SqEfTablesGenEnable>true</SqEfTablesGenEnable>
  <SqEfTablesGenProject></SqEfTablesGenProject>
  <SqTablesGenOutput>Tables</SqTablesGenOutput>
  <SqTablesGenNamespace>$(MSBuildProjectName).Tables</SqTablesGenNamespace>
  <SqTablesGenTableClassPrefix></SqTablesGenTableClassPrefix>
  <SqEfTablesGenDbContext></SqEfTablesGenDbContext>
  <SqEfTablesGenFramework></SqEfTablesGenFramework>
  <SqTablesGenUseTableDeclarationAttributes>true</SqTablesGenUseTableDeclarationAttributes>
  <SqTablesGenSkipUnknownColumnTypes>true</SqTablesGenSkipUnknownColumnTypes>
  <SqTablesGenSplitTablesBySchema>false</SqTablesGenSplitTablesBySchema>
  <SqTablesGenCleanOutput>false</SqTablesGenCleanOutput>
  <SqTablesGenInclude></SqTablesGenInclude>
  <SqTablesGenExclude></SqTablesGenExclude>
</PropertyGroup>
```

| Property | Default value | What it controls | When to change it |
|---|---|---|---|
| `SqEfTablesGenEnable` | `false` | Whether EF table generation runs before the project build. | Set it to `true` in every project where descriptors should be generated from EF. Leave it `false` to disable generation temporarily or permanently. |
| `SqEfTablesGenProject` | empty | The project containing the EF model. Empty means the current project (`$(MSBuildProjectFullPath)`). | Set it when generation is configured in one project but the `DbContext` and EF model live in another `.csproj`. Do not set it for the usual same-project setup. |
| `SqTablesGenOutput` | `Tables` | The directory for generated `.cs` files. A relative path is resolved from the project directory. | Change it to follow the project's source layout, to keep multiple generated sets separate, or to place generated files in a dedicated subtree. |
| `SqTablesGenNamespace` | `$(MSBuildProjectName).Tables` | The base namespace of generated descriptors. Using `$(MSBuildProjectName)` as the root is recommended. | Change the suffix when generated files belong in another namespace. Replace the root only when the project's established root namespace intentionally differs from its MSBuild project name. |
| `SqTablesGenTableClassPrefix` | empty (effective tool default: `Table`) | The prefix added to generated descriptor class names. | Set it when the project uses a different naming convention or needs to avoid collisions with entity classes. For example, `DbTable` produces `DbTableCustomer`. Leave it empty to produce names such as `TableCustomer`. |
| `SqEfTablesGenDbContext` | empty | The context type used as the metadata source. Empty enables automatic selection. | Set it when the project contains multiple `DbContext` types, multiple design-time factories, or automatic selection reports ambiguity. |
| `SqEfTablesGenFramework` | empty | The target framework used to evaluate the EF project. Empty uses the current `$(TargetFramework)`. | Set it when invoking generation for a multi-targeted EF project outside its normal target build, or when one particular framework must always be used. |
| `SqTablesGenUseTableDeclarationAttributes` | `true` | Whether output is compact attribute-based partial declarations instead of complete `TableBase` classes. | Usually leave it `true`. Set it to `false` only for an existing workflow that requires directly generated `TableBase` implementations. |
| `SqTablesGenSkipUnknownColumnTypes` | `true` | Whether unsupported EF column types are reported and omitted instead of failing generation. | Set it to `false` when a partial descriptor is unsafe and every mapped column must be supported. Leave it `true` when unsupported columns may intentionally be excluded after reviewing diagnostics. |
| `SqTablesGenSplitTablesBySchema` | `false` | Whether database schemas become output subdirectories and namespace segments. | Set it to `true` when the model uses multiple schemas, especially when schemas contain tables with the same name or the project is organized by schema. |
| `SqTablesGenCleanOutput` | `false` | Whether obsolete recognized descriptors are removed after successful generation. | Set it to `true` when the output directory is generator-owned and should exactly follow the EF model. Leave it `false` if that directory also contains manually maintained descriptors. |
| `SqTablesGenInclude` | empty | Semicolon-separated, case-insensitive table patterns to include. Empty includes every table. | Set it when only part of the EF relational model should produce descriptors. Use `*` and `?`, and qualify with `schema.` when needed. |
| `SqTablesGenExclude` | empty | Semicolon-separated table patterns to exclude after includes. | Set it for archive, migration, or other tables that must never be generated. Excludes always win. |
| `SqExpressCodeGenPath` | packaged tool path | The location of `SqExpress.CodeGenUtil.dll`. The package sets it automatically. | Change it only for source-tree development, custom package layouts, or controlled builds that intentionally use another code-generator binary. Application projects normally should not set it. |

The older misspelled `SqTablseGen*` names remain supported for shared settings. `SqTablesGen*` takes precedence when both forms are set. Explicit `Gen-Tables` parameters take precedence over either project property.

## Selecting a `DbContext`

Leave `SqEfTablesGenDbContext` empty when the project has one unambiguous context. Specify it when the project contains multiple contexts:

```xml
<SqEfTablesGenDbContext>AppDbContext</SqEfTablesGenDbContext>
```

The context must be available through either:

- a public `IDesignTimeDbContextFactory<TContext>`; or
- a public parameterless constructor.

If SqExpress cannot select or create the context, the build stops with a diagnostic instead of guessing.

## Multi-Targeted Projects

Leaving `SqEfTablesGenFramework` empty uses the current `$(TargetFramework)` during a normal build.

Set it explicitly when generation should always use one framework:

```xml
<SqEfTablesGenFramework>net8.0</SqEfTablesGenFramework>
```

## Output and Naming

Use `$(MSBuildProjectName)` as the namespace root so the generated namespace follows project renames and the same configuration can be reused across projects:

```xml
<SqTablesGenOutput>Tables</SqTablesGenOutput>
<SqTablesGenNamespace>$(MSBuildProjectName).Tables</SqTablesGenNamespace>
<SqTablesGenTableClassPrefix>Table</SqTablesGenTableClassPrefix>
```

With a project named `MyApp.Data`, this generates `TableCustomer` in namespace `MyApp.Data.Tables`.

`SqTablesGenOutput` is relative to the project directory unless an absolute path is supplied.

Keep generated descriptors in a dedicated directory so generated code is easy to identify and cleanup has a clear scope.

## Attribute-Based Declarations

Attribute-based declarations are enabled by default and are recommended:

```xml
<SqTablesGenUseTableDeclarationAttributes>true</SqTablesGenUseTableDeclarationAttributes>
```

Example output:

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

See the [Table Description Reference](table_description.md) for all declaration attributes.

Set the property to `false` only when complete `TableBase` classes are required:

```xml
<SqTablesGenUseTableDeclarationAttributes>false</SqTablesGenUseTableDeclarationAttributes>
```

## Schema Folders

Enable schema-specific folders and namespaces with:

```xml
<SqTablesGenSplitTablesBySchema>true</SqTablesGenSplitTablesBySchema>
```

For example, schema `sales-data` becomes directory `Tables/SalesData` and namespace segment `SalesData`. Tables without a schema use `Default`.

`AllTables.cs` remains in the root output directory. Generation fails when normalized schemas would produce colliding paths or type names.

## Cleaning Obsolete Descriptors

To make the generated directory mirror the current EF model:

```xml
<SqTablesGenCleanOutput>true</SqTablesGenCleanOutput>
```

Cleanup runs only after metadata is read successfully. A file is deleted only when no other type declarations remain.

Leave cleanup disabled when the directory contains descriptors maintained by hand.

## Filtering Tables

Generate only selected physical tables with include and exclude patterns:

```xml
<SqTablesGenInclude>sales.*;Customer?</SqTablesGenInclude>
<SqTablesGenExclude>*.OrderArchive*</SqTablesGenExclude>
```

A pattern containing `.` matches the complete `schema.table` name; an unqualified pattern matches table names in every schema. Matching is case-insensitive. `*` matches zero or more characters and `?` matches exactly one character. A table must match an include when any includes are configured, then exclusions are applied and always win.

Foreign-key metadata pointing to a filtered-out table is omitted. When `SqTablesGenCleanOutput` is enabled, files belonging to filtered-out tables are removed.

## Unsupported Column Types

Unsupported columns are reported and omitted by default:

```xml
<SqTablesGenSkipUnknownColumnTypes>true</SqTablesGenSkipUnknownColumnTypes>
```

Review these diagnostics because the generated descriptor will not represent the complete table.

For strict generation:

```xml
<SqTablesGenSkipUnknownColumnTypes>false</SqTablesGenSkipUnknownColumnTypes>
```

## Command-Line Interface

For scripts, CI, or environments without Visual Studio Package Manager Console, invoke the utility included in the SqExpress NuGet package directly:

```powershell
dotnet "<package>\tools\codegen\SqExpress.CodeGenUtil.dll" `
  gentables ef ".\Data\MyApp.Data.csproj" `
  --include "sales.*;Customer?" `
  --exclude "*.OrderArchive*" `
  --use-table-declaration-attributes
```

Replace `<package>` with the path to the installed SqExpress package version. The first positional argument after `gentables` must be `ef`, followed by the path to the EF project.

For the CLI, a relative `--output-dir` path is resolved from the process's current working directory.

### Reusable project scripts

To avoid hard-coding a package version, add a helper script to the project that discovers the latest installed SqExpress package before running the generator. Keeping the script in the project repository makes the generation command easy to reuse locally and in CI.

### Windows command script

`GenerateTables.cmd`:

```cmd
@echo off
set root=%userprofile%\.nuget\packages\sqexpress

for /F "tokens=*" %%a in ('dir "%root%" /b /a:d /o:n') do set "lib=%root%\%%a"

set lib=%lib%\tools\codegen\SqExpress.CodeGenUtil.dll

dotnet "%lib%" gentables ef ".\Data\MyApp.Data.csproj" --db-context "AppDbContext" --framework "net8.0" --use-table-declaration-attributes
```

### Unix shell script

`GenerateTables.sh`:

```sh
#!/bin/bash

lib=~/.nuget/packages/sqexpress/$(ls ~/.nuget/packages/sqexpress -r|head -n 1)/tools/codegen/SqExpress.CodeGenUtil.dll

dotnet $lib gentables ef "./Data/MyApp.Data.csproj" --db-context "AppDbContext" --framework "net8.0" --use-table-declaration-attributes
```

Both scripts use the README's version-discovery approach to select the latest installed SqExpress package.

### All EF CLI Arguments and Options

| Argument or option | Required | Default | Description |
|---|---:|---|---|
| `CONNECTION_TYPE` | yes | none | Must be `ef`. |
| `EF_PROJECT` | yes | none | Path to the EF `.csproj`. |
| `--table-class-prefix <value>` | no | `Table` | Prefix for generated descriptor class names. |
| `-o`, `--output-dir <path>` | no | current working directory | Directory for generated files. A relative path is resolved from the current working directory. |
| `-n`, `--namespace <value>` | no | `MyCompany.MyApp.Tables` | Base namespace for generated descriptors. |
| `-v`, `--verbosity <value>` | no | `minimal` | Logging level: `quiet`, `minimal`, `normal`, or `detailed`. |
| `--use-table-declaration-attributes` | no | `false` | Generates attribute-based partial declarations. |
| `--skip-unknown-column-types` | no | `false` | Omits unsupported columns instead of failing. |
| `--db-context <type>` | no | automatic | Context type name. |
| `--framework <tfm>` | no | automatic | Target framework. Required when it cannot be inferred unambiguously. |
| `--split-tables-by-schema` | no | `false` | Creates schema-specific folders and namespaces. |
| `--clean-output` | no | `false` | Removes obsolete recognized descriptors after successful generation. |
| `--include <patterns>` | no | all tables | Semicolon-separated, case-insensitive include patterns supporting `*` and `?`. |
| `--exclude <patterns>` | no | none | Semicolon-separated exclude patterns, applied after includes. |

Boolean CLI options are switches: include the option to enable it and omit the option to retain its default.
Quote filter lists so the shell does not expand wildcard characters. Supply multiple patterns as a semicolon-separated list.

## Package Manager Console

MSBuild properties are the recommended EF workflow. For one-time generation, Visual Studio Package Manager Console also supports:

```powershell
Gen-Tables ef -UseTableDeclarationAttributes
```

To select another project, context, or framework:

```powershell
Gen-Tables ef ".\Data\MyApp.Data.csproj" `
  -DbContext AppDbContext `
  -Framework net8.0 `
  -UseTableDeclarationAttributes
```

The project argument can be a Visual Studio project name or `.csproj` path. When omitted, the selected Package Manager Console project is used.

### All `Gen-Tables ef` Parameters

| Parameter | Required | Default | Description |
|---|---:|---|---|
| `DbType` | yes | none | Must be `ef`. First positional argument. |
| `Project` | no | project property `SqEfTablesGenProject`; otherwise selected PMC project | EF project name or `.csproj` path. Second positional argument. |
| `DbContext` | no | project property `SqEfTablesGenDbContext`; otherwise automatic | Context type name. |
| `Framework` | no | project property `SqEfTablesGenFramework`; otherwise automatic | Target framework. |
| `OutputDir` | no | project property `SqTablesGenOutput`; otherwise `Tables` | Directory for generated files. A relative path is resolved from the selected project directory. |
| `TableClassPrefix` | no | project property `SqTablesGenTableClassPrefix`; otherwise tool default `Table` | Generated class-name prefix. |
| `Namespace` | no | project property `SqTablesGenNamespace`; otherwise selected project name plus output path | Generated base namespace. |
| `UseTableDeclarationAttributes` | no | project property `SqTablesGenUseTableDeclarationAttributes`; otherwise `false` | Generates attribute-based partial declarations. |
| `SkipUnknownColumnTypes` | no | project property `SqTablesGenSkipUnknownColumnTypes`; otherwise `false` | Omits unsupported columns instead of failing. |
| `SplitTablesBySchema` | no | project property `SqTablesGenSplitTablesBySchema`; otherwise `false` | Creates schema-specific folders and namespaces. |
| `CleanOutput` | no | project property `SqTablesGenCleanOutput`; otherwise `false` | Removes obsolete recognized descriptors. |
| `Include` | no | project property `SqTablesGenInclude`; otherwise all tables | One or more wildcard include patterns. |
| `Exclude` | no | project property `SqTablesGenExclude`; otherwise none | One or more wildcard exclude patterns; exclusions win. |

The older `SqTablseGen*` spelling is retained as a lower-priority fallback.

For EF-specific settings, an explicit `Project`, `DbContext`, or `Framework` argument overrides its corresponding `SqEfTablesGen*` project property. When `Project` is omitted and `SqEfTablesGenProject` is empty, the selected Package Manager Console project is used.

For generation from a live database, see [Database-First Table Generation](database_first_table_generation.md).
