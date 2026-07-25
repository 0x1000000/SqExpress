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
| `OutputDir` | no | project property `SqTablseGenOutput`; otherwise `Tables` | Directory for generated `.cs` files. A relative path is resolved from the selected project directory. |
| `TableClassPrefix` | no | project property `SqTablseGenTableClassPrefix`; otherwise tool default `Table` | Prefix for generated descriptor class names. |
| `Namespace` | no | project property `SqTablseGenNamespace`; otherwise selected project name plus output path | Base namespace for generated descriptors. |
| `UseTableDeclarationAttributes` | no | `false` | Generates attribute-based partial declarations. |
| `SkipUnknownColumnTypes` | no | `false` | Omits unsupported columns instead of failing. |
| `SplitTablesBySchema` | no | `false` | Creates schema-specific folders and namespace segments. |
| `CleanOutput` | no | project property `SqTablseGenCleanOutput`; otherwise `false` | Removes obsolete recognized descriptors after successful generation. |

The `SqTablse...` spelling is retained for backward compatibility.

## Pure CLI

For scripts, CI, or environments without Visual Studio Package Manager Console, invoke the utility included in the NuGet package:

```powershell
dotnet "<package>\tools\codegen\SqExpress.CodeGenUtil.dll" `
  gentables mssql "<connection-string>" `
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

Boolean CLI options are switches: include the option to enable it and omit the option to keep its default value.

For generation from an EF Core model, see [Entity Framework Table Generation](ef_table_generation.md).
