using System.Collections.Generic;
using SqExpress.Meta;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Utils;

namespace SqExpress
{
    /// <summary>Base class for strongly typed database table descriptors.</summary>
    /// <remarks>
    /// Derived descriptors normally expose columns as properties initialized with the protected
    /// <c>Create...Column</c> helpers. Created columns and indexes are registered automatically for metadata,
    /// script generation, binding, and database-first workflows.
    /// </remarks>
    public class TableBase : ExprTable
    {
        /// <summary>Initializes a table descriptor that can be used both in queries and as schema metadata.</summary>
        /// <param name="schema">The schema name, or <see langword="null"/> for an unqualified table name.</param>
        /// <param name="name">The physical database table name.</param>
        /// <param name="alias">An optional query alias; the default leaves the table unaliased.</param>
        public TableBase(string? schema, string name, Alias alias = default)
            : base(new ExprTableFullName(schema != null ? new ExprDbSchema(null, new ExprSchemaName(schema)) : null,
                    new ExprTableName(name)),
                BuildTableAlias(alias))
        {
        }

        /// <summary>Initializes a descriptor for a table qualified by schema and, optionally, database.</summary>
        /// <param name="databaseName">The database/catalog name, or <see langword="null"/> to omit that qualifier.</param>
        /// <param name="schema">The required schema name.</param>
        /// <param name="name">The physical table name.</param>
        /// <param name="alias">An optional query alias.</param>
        public TableBase(string? databaseName, string schema, string name, Alias alias = default)
            : base(new ExprTableFullName(
                    new ExprDbSchema(databaseName != null ? new ExprDatabaseName(databaseName) : null, new ExprSchemaName(schema)),
                    new ExprTableName(name)),
                BuildTableAlias(alias))
        {
        }

        /// <summary>Initializes an extensibility-oriented descriptor from prebuilt name and alias syntax.</summary>
        /// <param name="fullName">The complete syntax-level table name.</param>
        /// <param name="alias">The optional query alias.</param>
        protected internal TableBase(IExprTableFullName fullName, ExprTableAlias? alias) : base(fullName, alias)
        {
        }

        private readonly List<TableColumn> _columns = new();
        
        private readonly List<IndexMeta> _indexes = new();

        /// <summary>Gets the columns registered by this descriptor.</summary>
        public IReadOnlyList<TableColumn> Columns => this._columns;

        /// <summary>Gets the indexes registered by this descriptor.</summary>
        public IReadOnlyList<IndexMeta> Indexes => this._indexes;

        /// <summary>Gets the schema-script builder for this table descriptor.</summary>
        public TableBaseScript Script => new(this);

        /// <summary>Adds preconstructed columns to the descriptor's metadata in enumeration order.</summary>
        /// <param name="columns">Columns whose table ownership and source qualification are already configured.</param>
        protected internal void AddColumns(IEnumerable<TableColumn> columns)
        {
            this._columns.AddRange(columns);
        }

        /// <summary>Adds preconstructed indexes to the descriptor's schema metadata.</summary>
        /// <param name="indexes">Index definitions to register in enumeration order.</param>
        protected internal void AddIndexes(IEnumerable<IndexMeta> indexes)
        {
            this._indexes.AddRange(indexes);
        }

        /// <summary>Creates a non-nullable Boolean column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional key, default, identity, or foreign-key metadata.</param>
        /// <returns>A typed column bound to this table and its alias.</returns>
        protected BooleanTableColumn CreateBooleanColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new BooleanTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable Boolean column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional key, default, identity, or foreign-key metadata.</param>
        /// <returns>A typed nullable column bound to this table and its alias.</returns>
        protected NullableBooleanTableColumn CreateNullableBooleanColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableBooleanTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable byte column using the target dialect's compatible type and registers its metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed column bound to this table and its alias.</returns>
        protected ByteTableColumn CreateByteColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new ByteTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable byte column using the target dialect's compatible type and registers its metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable column bound to this table and its alias.</returns>
        protected NullableByteTableColumn CreateNullableByteColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable variable-length binary column and registers its schema metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">Maximum byte count, or <see langword="null"/> for the dialect's unsized/default form.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed binary column bound to this table and its alias.</returns>
        protected ByteArrayTableColumn CreateByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
        {
            var result = new ByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable variable-length binary column and registers its schema metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">Maximum byte count, or <see langword="null"/> for the dialect's unsized/default form.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable binary column bound to this table and its alias.</returns>
        protected NullableByteArrayTableColumn CreateNullableByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable fixed-length binary column and registers its schema metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">The required number of bytes.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed binary column bound to this table and its alias.</returns>
        protected ByteArrayTableColumn CreateFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
        {
            var result = new ByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable fixed-length binary column and registers its schema metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">The required number of bytes when non-null.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable binary column bound to this table and its alias.</returns>
        protected NullableByteArrayTableColumn CreateNullableFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable 16-bit integer column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed column bound to this table and its alias.</returns>
        protected Int16TableColumn CreateInt16Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int16TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable 16-bit integer column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable column bound to this table and its alias.</returns>
        protected NullableInt16TableColumn CreateNullableInt16Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt16TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable 32-bit integer column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed column bound to this table and its alias.</returns>
        protected Int32TableColumn CreateInt32Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int32TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable 32-bit integer column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable column bound to this table and its alias.</returns>
        protected NullableInt32TableColumn CreateNullableInt32Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt32TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable 64-bit integer column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed column bound to this table and its alias.</returns>
        protected Int64TableColumn CreateInt64Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int64TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable 64-bit integer column and registers it for querying and schema generation.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable column bound to this table and its alias.</returns>
        protected NullableInt64TableColumn CreateNullableInt64Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt64TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable exact-numeric column with optional precision/scale metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="decimalPrecisionScale">Optional total precision and fractional scale; <see langword="null"/> uses the dialect default.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed decimal column bound to this table and its alias.</returns>
        protected DecimalTableColumn CreateDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null,ColumnMeta? columnMeta = null)
        {
            var result = new DecimalTableColumn(this.Alias, new ExprColumnName(name), this, decimalPrecisionScale, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable exact-numeric column with optional precision/scale metadata.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="decimalPrecisionScale">Optional total precision and fractional scale; <see langword="null"/> uses the dialect default.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable decimal column bound to this table and its alias.</returns>
        protected NullableDecimalTableColumn CreateNullableDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDecimalTableColumn(this.Alias, new ExprColumnName(name), this, decimalPrecisionScale, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable approximate-numeric column using the dialect's double-precision type.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed double column bound to this table and its alias.</returns>
        protected DoubleTableColumn CreateDoubleColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new DoubleTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable approximate-numeric column using the dialect's double-precision type.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable double column bound to this table and its alias.</returns>
        protected NullableDoubleTableColumn CreateNullableDoubleColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDoubleTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable date-only or date-and-time column in the target dialect.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="isDate"><see langword="true"/> for date-only storage; otherwise date and time.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed temporal column bound to this table and its alias.</returns>
        protected DateTimeTableColumn CreateDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
        {
            var result = new DateTimeTableColumn(this.Alias, new ExprColumnName(name), this, isDate, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable date-only or date-and-time column in the target dialect.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="isDate"><see langword="true"/> for date-only storage; otherwise date and time.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable temporal column bound to this table and its alias.</returns>
        protected NullableDateTimeTableColumn CreateNullableDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDateTimeTableColumn(this.Alias, new ExprColumnName(name), this, isDate, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable offset-aware temporal column using the dialect's compatible type.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed date-time-offset column bound to this table and its alias.</returns>
        protected DateTimeOffsetTableColumn CreateDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new DateTimeOffsetTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable offset-aware temporal column using the dialect's compatible type.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable date-time-offset column bound to this table and its alias.</returns>
        protected NullableDateTimeOffsetTableColumn CreateNullableDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDateTimeOffsetTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable GUID/UUID column using the target dialect's native compatible type.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed GUID column bound to this table and its alias.</returns>
        protected GuidTableColumn CreateGuidColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new GuidTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable GUID/UUID column using the target dialect's native compatible type.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable GUID column bound to this table and its alias.</returns>
        protected NullableGuidTableColumn CreateNullableGuidColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableGuidTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable variable-length character column with dialect-aware Unicode and large-text options.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">Maximum character count, or <see langword="null"/> for the dialect's unsized/default form.</param>
        /// <param name="isUnicode">Whether to request Unicode-capable storage when the dialect distinguishes it.</param>
        /// <param name="isText">Whether to request a large-text type instead of a regular varying character type.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed string column bound to this table and its alias.</returns>
        protected StringTableColumn CreateStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeString(size, isUnicode, isText), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable variable-length character column with dialect-aware Unicode and large-text options.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">Maximum character count, or <see langword="null"/> for the dialect's unsized/default form.</param>
        /// <param name="isUnicode">Whether to request Unicode-capable storage when the dialect distinguishes it.</param>
        /// <param name="isText">Whether to request a large-text type instead of a regular varying character type.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable string column bound to this table and its alias.</returns>
        protected NullableStringTableColumn CreateNullableStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeString(size, isUnicode, isText), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable fixed-length character column whose type is rendered for the target dialect.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">The fixed character count when non-null.</param>
        /// <param name="isUnicode">Whether to request Unicode-capable storage when supported separately.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed nullable string column bound to this table and its alias.</returns>
        protected NullableStringTableColumn CreateNullableFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeString(size, isUnicode), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable fixed-length character column whose type is rendered for the target dialect.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="size">The fixed character count.</param>
        /// <param name="isUnicode">Whether to request Unicode-capable storage when supported separately.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A typed string column bound to this table and its alias.</returns>
        protected StringTableColumn CreateFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeString(size, isUnicode), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a nullable XML-typed column exposed through SqExpress's string-column API.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A nullable string-compatible column whose SQL type is XML.</returns>
        protected NullableStringTableColumn CreateNullableXmlColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, ExprTypeXml.Instance, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates a non-nullable XML-typed column exposed through SqExpress's string-column API.</summary>
        /// <param name="name">The physical column name.</param>
        /// <param name="columnMeta">Optional schema metadata.</param>
        /// <returns>A string-compatible column whose SQL type is XML.</returns>
        protected StringTableColumn CreateXmlColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, ExprTypeXml.Instance, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Converts the public alias abstraction to the AST representation used by table sources.</summary>
        /// <param name="alias">The explicit, automatic, or empty alias value.</param>
        /// <returns>A syntax alias, or <see langword="null"/> when the table should remain unaliased.</returns>
        protected internal static ExprTableAlias? BuildTableAlias(Alias alias)
        {
            var a = alias.BuildAliasExpression();

            if (a != null)
            {
                return new ExprTableAlias(a);
            }

            return null;
        }

        /// <summary>Registers an unnamed, non-unique, non-clustered index for schema generation and comparison.</summary>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, false));
        /// <summary>Registers a named, non-unique, non-clustered index for schema generation and comparison.</summary>
        /// <param name="name">The physical index name.</param>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, false));
        
        /// <summary>Registers an unnamed unique, non-clustered index for schema generation and comparison.</summary>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddUniqueIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, false));
        /// <summary>Registers a named unique, non-clustered index for schema generation and comparison.</summary>
        /// <param name="name">The physical index name.</param>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddUniqueIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, true, false));
        
        /// <summary>Registers an unnamed, non-unique clustered index for dialects that support clustered indexes.</summary>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddClusteredIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, true));
        /// <summary>Registers a named, non-unique clustered index for dialects that support clustered indexes.</summary>
        /// <param name="name">The physical index name.</param>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddClusteredIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, true));
        
        /// <summary>Registers an unnamed unique clustered index for dialects that support clustered indexes.</summary>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddUniqueClusteredIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, true));
        /// <summary>Registers a named unique clustered index for dialects that support clustered indexes.</summary>
        /// <param name="name">The physical index name.</param>
        /// <param name="columns">One or more indexed columns, including their sort directions.</param>
        protected void AddUniqueClusteredIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, true, true));

        private static IndexMetaColumn[] AssertIndexColumnsNotEmpty(IndexMetaColumn[] columns)
        {
            columns.AssertNotEmpty("Table index has to contain at least one column");
            return columns;
        }
    }
}
