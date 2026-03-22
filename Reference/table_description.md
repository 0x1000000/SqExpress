# Table Description Reference

This document describes the attribute-based table declaration API in the namespace:

```cs
using SqExpress.TableDecalationAttributes;
```

These attributes are consumed by the built-in source generator and produce `TableBase` or `TempTableBase` implementations at compile time.

## Attribute Index

- [`TableDescriptor`](#tabledescriptor)
- [`TempTableDescriptor`](#temptabledescriptor)
- [`BooleanColumn`](#boolean)
- [`NullableBooleanColumn`](#boolean)
- [`ByteColumn`](#byte--binary)
- [`NullableByteColumn`](#byte--binary)
- [`ByteArrayColumn`](#byte--binary)
- [`NullableByteArrayColumn`](#byte--binary)
- [`Int16Column`](#integer)
- [`NullableInt16Column`](#integer)
- [`Int32Column`](#integer)
- [`NullableInt32Column`](#integer)
- [`Int64Column`](#integer)
- [`NullableInt64Column`](#integer)
- [`DoubleColumn`](#floating-point--decimal)
- [`NullableDoubleColumn`](#floating-point--decimal)
- [`DecimalColumn`](#floating-point--decimal)
- [`NullableDecimalColumn`](#floating-point--decimal)
- [`DateTimeColumn`](#date--time)
- [`NullableDateTimeColumn`](#date--time)
- [`DateTimeOffsetColumn`](#date--time)
- [`NullableDateTimeOffsetColumn`](#date--time)
- [`GuidColumn`](#other-scalar-types)
- [`NullableGuidColumn`](#other-scalar-types)
- [`StringColumn`](#other-scalar-types)
- [`NullableStringColumn`](#other-scalar-types)
- [`XmlColumn`](#other-scalar-types)
- [`NullableXmlColumn`](#other-scalar-types)
- [`Index`](#index)

## Class-Level Descriptor Attributes

Only one descriptor attribute may be used on the same class.

### `TableDescriptor`

Declares a normal table descriptor.

Supported constructor shapes:

```cs
[TableDescriptor("User")]
[TableDescriptor("dbo", "User")]
[TableDescriptor("MyDb", "dbo", "User")]
```

Properties:

| Name | Type | Description |
|---|---|---|
| `DatabaseName` | `string?` | Optional database name. Only used with the 3-argument constructor. |
| `Schema` | `string?` | Optional schema name. |
| `Name` | `string` | SQL table name. |
| `SqModel` | `string?` | Declares a generated DTO model that should include all declared columns from this table. |

Generated base type:

```cs
TableBase
```

### `TempTableDescriptor`

Declares a temporary table descriptor.

Supported constructor shape:

```cs
[TempTableDescriptor("tmpUser")]
[TempTableDescriptor("#tmpUser")]
```

Properties:

| Name | Type | Description |
|---|---|---|
| `Name` | `string` | Temp table name passed to `TempTableBase`. |
| `SqModel` | `string?` | Declares a generated DTO model that should include all declared columns from this temp table. |

Generated base type:

```cs
TempTableBase
```

## Shared Column Properties

All column attributes inherit from `TableColumnAttributeBase` and support these named properties:

| Property | Type | Description |
|---|---|---|
| `PropertyName` | `string?` | Overrides the generated C# property name. |
| `Pk` | `bool` | Marks the column as part of the primary key. |
| `Identity` | `bool` | Marks the column as identity/auto-increment. |
| `FkSchema` | `string?` | Foreign key target schema. |
| `FkDatabase` | `string?` | Foreign key target database. |
| `FkTable` | `string?` | Foreign key target table name. |
| `FkColumn` | `string?` | Foreign key target column name. |
| `DefaultValue` | `string?` | Default value text. Parsed according to the column type. |
| `SqModels` | `string?` | Comma-separated DTO model list. Each entry is `ModelName` or `ModelName.PropertyName`. |
| `SqModelCast` | `Type?` | Optional CLR cast applied when reading this column into all generated DTO models that include it. |

Example:

```cs
[Int32Column("UserId", Pk = true, Identity = true)]
[NullableInt32Column("CompanyId", FkTable = "Company", FkColumn = "CompanyId")]
[StringColumn("FirstName", SqModels = "UserDto,UserName")]
[Int32Column("UserId", SqModels = "UserDto.Id", SqModelCast = typeof(long))]
```

## SqModel Metadata

Attribute-based table declarations can also drive DTO generation through the built-in source generator.

Class-level:

- `SqModel = "UserDto"` on `TableDescriptor` or `TempTableDescriptor` includes every declared column in the generated model.

Column-level:

- `SqModels = "UserDto"` adds the column to model `UserDto`.
- `SqModels = "UserDto.Id"` adds the column to model `UserDto` and renames the generated DTO property to `Id`.
- multiple entries are comma-separated, for example `SqModels = "UserDto,AuditUserDto.Id"`.
- if a class-level `SqModel` and a column-level `SqModels` entry refer to the same model, the membership is deduplicated.
- `SqModelCast = typeof(SomeType)` applies the same cast to all generated models that include that column.

The analyzer/source-generator path respects:

- `SqModelGenNamespace`
- `SqModelGenType`

Current default:

- `SqModelGenType` defaults to `Record`
- `ImmutableClass` is still supported for backward compatibility

## Specialized Column Properties

### String Columns

Available on `StringColumn` and `NullableStringColumn`.

| Property | Type | Description |
|---|---|---|
| `Unicode` | `bool` | Generates a Unicode string column. |
| `MaxLength` | `int` | Maximum length. Use `-1` or omit for unspecified length. |
| `FixedLength` | `bool` | Generates fixed-size string columns. |
| `Text` | `bool` | Marks the string as text-style storage when supported. |

### Byte Array Columns

Available on `ByteArrayColumn` and `NullableByteArrayColumn`.

| Property | Type | Description |
|---|---|---|
| `MaxLength` | `int` | Maximum binary length. Use `-1` or omit for unspecified length. |
| `FixedLength` | `bool` | Generates fixed-size binary columns. |

### Decimal Columns

Available on `DecimalColumn` and `NullableDecimalColumn`.

| Property | Type | Description |
|---|---|---|
| `Precision` | `int` | Decimal precision. |
| `Scale` | `int` | Decimal scale. |

### DateTime Columns

Available on `DateTimeColumn` and `NullableDateTimeColumn`.

| Property | Type | Description |
|---|---|---|
| `IsDate` | `bool` | Generates a date-only column where supported. |

## Concrete Column Attributes

### Boolean

- `BooleanColumn`
- `NullableBooleanColumn`

### Byte / Binary

- `ByteColumn`
- `NullableByteColumn`
- `ByteArrayColumn`
- `NullableByteArrayColumn`

### Integer

- `Int16Column`
- `NullableInt16Column`
- `Int32Column`
- `NullableInt32Column`
- `Int64Column`
- `NullableInt64Column`

### Floating Point / Decimal

- `DoubleColumn`
- `NullableDoubleColumn`
- `DecimalColumn`
- `NullableDecimalColumn`

### Date / Time

- `DateTimeColumn`
- `NullableDateTimeColumn`
- `DateTimeOffsetColumn`
- `NullableDateTimeOffsetColumn`

### Other Scalar Types

- `GuidColumn`
- `NullableGuidColumn`
- `StringColumn`
- `NullableStringColumn`
- `XmlColumn`
- `NullableXmlColumn`

## `Index`

Declares an index on the generated descriptor.

Constructor:

```cs
[Index("Name")]
[Index("LastName", "FirstName")]
```

Properties:

| Property | Type | Description |
|---|---|---|
| `Columns` | `string[]` | Indexed SQL column names. Provided by the constructor. |
| `Name` | `string?` | Optional index name override. |
| `Unique` | `bool` | Marks the index as unique. |
| `Clustered` | `bool` | Marks the index as clustered where supported. |
| `DescendingColumns` | `string[]?` | Columns that should be descending. They must also appear in `Columns`. |

Example:

```cs
[Index("LastName", "FirstName", Unique = true, DescendingColumns = new[] { "LastName" })]
```

## Supported `DefaultValue` Tokens

`DefaultValue` is parsed according to the target column type.

Supported predefined tokens:

| Token | Description | Allowed On |
|---|---|---|
| `$null` | Generates `SqQueryBuilder.Null` | Nullable columns |
| `$utcNow` | Generates `SqQueryBuilder.GetUtcDate()` | `DateTime`, `NullableDateTime`, `DateTimeOffset`, `NullableDateTimeOffset` |
| `$now` | Generates `SqQueryBuilder.GetDate()` | `DateTime`, `NullableDateTime`, `DateTimeOffset`, `NullableDateTimeOffset` |

Other values are parsed from text based on the column type:

- `bool`: `true`, `false`, `0`, `1`
- integer types: numeric text
- `decimal`, `double`: invariant numeric text
- `Guid`: GUID text
- `DateTime`, `DateTimeOffset`: parseable date/time text
- `string`, `xml`: raw string value

## Example: Normal Table

```cs
using SqExpress.TableDecalationAttributes;

[TableDescriptor("dbo", "User")]
[Int32Column("UserId", Pk = true, Identity = true)]
[StringColumn("FirstName", Unicode = true, MaxLength = 255)]
[StringColumn("LastName", Unicode = true, MaxLength = 255)]
[Int32Column("Version", DefaultValue = "0")]
[DateTimeColumn("ModifiedAt", DefaultValue = "$utcNow")]
[Index("FirstName")]
[Index("LastName")]
public partial class TableUser
{
}
```

## Example: Temp Table

```cs
using SqExpress.TableDecalationAttributes;

[TempTableDescriptor("#TmpUser")]
[Int32Column("UserId", Pk = true)]
[StringColumn("Name", MaxLength = 255)]
[Index("Name")]
public partial class TmpUser
{
}
```
