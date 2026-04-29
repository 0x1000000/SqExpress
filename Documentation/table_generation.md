# Table Generation Reference

This document describes SqExpress table descriptor generation from existing database metadata or from an Entity Framework Core `DbContext`.

Generated table descriptors can be emitted either as direct `TableBase` classes or as attribute-based partial declarations consumed by the SqExpress source generator. Attribute-based declarations are the recommended output. For the full attribute reference, see [Table Description Reference](table_description.md).

## Package Manager Console

The Visual Studio Package Manager Console entry point is `Gen-Tables`.

```powershell
Gen-Tables mssql -ConnectionString "<connection-string>"
Gen-Tables mysql -ConnectionString "<connection-string>"
Gen-Tables pgsql -ConnectionString "<connection-string>"
Gen-Tables ef [[-Project] <project-name-or-path>] [-DbContext <type-name>]
```

Common options:

```powershell
[-OutputDir <path>]
[-TableClassPrefix <prefix>]
[-Namespace <namespace>]
[-UseTableDeclarationAttributes]
[-SkipUnknownColumnTypes]
```

### Database Modes

`mssql`, `mysql`, and `pgsql` connect to a real database and read database metadata from that connection.

Example:

```powershell
Gen-Tables mssql `
  -ConnectionString "Server=(local);Database=AppDb;Integrated Security=True;TrustServerCertificate=True" `
  -OutputDir Tables `
  -Namespace MyApp.Tables `
  -TableClassPrefix Table `
  -UseTableDeclarationAttributes
```

### EF Mode

`ef` mode creates a target EF Core `DbContext` and reads its public EF relational model metadata. It does not open a database connection and does not read physical database metadata.

Example using the selected Package Manager Console project:

```powershell
Gen-Tables ef -UseTableDeclarationAttributes
```

Example using another project:

```powershell
Gen-Tables ef ".\Data\MyApp.Data.csproj" -DbContext AppDbContext -UseTableDeclarationAttributes
```

When `Project` is supplied, it can be:

- a Visual Studio project name, when discoverable by Package Manager Console;
- a `.csproj` path;
- a path relative to the selected Package Manager Console project directory.

When `Project` is omitted, the selected Package Manager Console project is used.

### DbContext Resolution

EF mode resolves the context in this order:

1. If `-DbContext` is supplied, use the matching context type or its matching design-time factory.
2. Otherwise, if there is exactly one public `IDesignTimeDbContextFactory<TContext>`, use it.
3. Otherwise, if there is exactly one `DbContext` type with a parameterless constructor, instantiate it.
4. Otherwise, fail with a clear message asking for `-DbContext` or a design-time factory.

Only public types and public methods/properties are used. SqExpress does not use private or internal EF APIs for metadata extraction.

### EF Provider Support

The first EF provider supported by `Gen-Tables ef` is SQL Server. The generated descriptors reflect EF's configured relational mapping: schemas, table names, columns, keys, indexes, defaults, and supported SQL Server column types.

Unsupported or unmapped EF metadata is reported during generation. Use `-SkipUnknownColumnTypes` only when you intentionally want generation to continue without unsupported columns.

## Generated Attribute Declarations

Use `-UseTableDeclarationAttributes` to generate partial classes decorated with SqExpress declaration attributes:

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

The SqExpress source generator then produces the corresponding `TableBase` implementation. See [Table Description Reference](table_description.md) for all supported table, column, index, default value, and model attributes.

## Direct CLI

The underlying code-generation utility can be called directly:

```powershell
dotnet "<path-to-package>\tools\codegen\SqExpress.CodeGenUtil.dll" gentables mssql "<connection-string>" --use-table-declaration-attributes
dotnet "<path-to-package>\tools\codegen\SqExpress.CodeGenUtil.dll" gentables ef "<project-or-assembly-path>" --db-context AppDbContext --use-table-declaration-attributes
```

The Package Manager Console function is usually preferred because it resolves the selected project and package tool path for you.

## Project Attributes and MSBuild Properties

Projects can run EF table generation during build by setting `SqEfTablesGenEnable` to `true`.

When SqExpress is referenced as a NuGet package, `SqExpress.props` and `SqExpress.targets` are imported automatically by MSBuild.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <SqEfTablesGenEnable>true</SqEfTablesGenEnable>
    <SqEfTablesGenOutput>Tables</SqEfTablesGenOutput>
    <SqEfTablesGenNamespace>MyApp.Tables</SqEfTablesGenNamespace>
    <SqEfTablesGenTableClassPrefix>Table</SqEfTablesGenTableClassPrefix>
    <SqEfTablesGenDbContext>AppDbContext</SqEfTablesGenDbContext>
    <SqEfTablesGenUseTableDeclarationAttributes>true</SqEfTablesGenUseTableDeclarationAttributes>
  </PropertyGroup>
</Project>
```

Source-tree integration tests or custom package layouts may import `SqExpress.props` and `SqExpress.targets` explicitly and may override `SqExpressCodeGenPath` to point to a locally built `SqExpress.CodeGenUtil.dll`.

### EF Table Generation Properties

| Property | Default | Description |
|---|---|---|
| `SqEfTablesGenEnable` | `false` | Enables EF table generation during build. |
| `SqEfTablesGenProject` | empty | EF project or assembly source. Empty means the current project output. |
| `SqEfTablesGenOutput` | `Tables` | Directory for generated `.cs` files. |
| `SqEfTablesGenNamespace` | `$(MSBuildProjectName).Tables` | Namespace for generated files. |
| `SqEfTablesGenTableClassPrefix` | empty | Optional generated table class prefix. |
| `SqEfTablesGenDbContext` | empty | Optional context type name. Use when context resolution is ambiguous. |
| `SqEfTablesGenUseTableDeclarationAttributes` | `true` | Generates attribute-based partial declarations when `true`. |
| `SqEfTablesGenSkipUnknownColumnTypes` | `true` | Skips unsupported EF column types when `true`; fails on them when `false`. |
| `SqExpressCodeGenPath` | package tool path | Path to `SqExpress.CodeGenUtil.dll`. Normally set by SqExpress props. |

`SqEfTablesGenProject` can point to a different project or assembly. When it is empty, the target uses the current project output and performs an inner build with EF generation disabled so the project can be built before descriptors are generated.

`SqExpressCodeGenPath` normally should not be set by application projects. It is useful only for source-tree integration tests or unusual build layouts where the packaged `tools/codegen` path is not available.

### Same-Project Generation Notes

Same-project EF generation has an unavoidable bootstrap step:

1. Build the project without EF table generation.
2. Load the built assembly and read EF relational metadata.
3. Generate table declaration files.
4. Compile the final project with generated descriptors included.

Generated descriptors should not be required by the `DbContext` itself. Keep database setup and context configuration independent from generated table classes.

## Legacy Table Generation Properties

The Package Manager Console `Gen-Tables` function also reads the existing table generation defaults:

| Property | Default | Description |
|---|---|---|
| `SqTablseGenOutput` | `Tables` | Default output directory for `Gen-Tables`. |
| `SqTablseGenNamespace` | `$(MSBuildProjectName).Tables` | Default generated namespace for `Gen-Tables`. |
| `SqTablseGenTableClassPrefix` | empty | Default generated table class prefix for `Gen-Tables`. |

The property names keep the existing `SqTablse...` spelling for backward compatibility.

## Recommended Setup

For new projects:

1. Prefer `Gen-Tables ef` when EF Core is the source of truth for relational mapping.
2. Use `UseTableDeclarationAttributes` or `SqEfTablesGenUseTableDeclarationAttributes=true`.
3. Add `SqEfTablesGenDbContext` when more than one context exists.
4. Keep generated files in a dedicated `Tables` folder and namespace.
5. Use a public `IDesignTimeDbContextFactory<TContext>` for contexts that cannot be created with a parameterless constructor.
