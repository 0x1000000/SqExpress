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
        /// <summary>Initializes a table descriptor with an optional schema and alias.</summary>
        public TableBase(string? schema, string name, Alias alias = default)
            : base(new ExprTableFullName(schema != null ? new ExprDbSchema(null, new ExprSchemaName(schema)) : null,
                    new ExprTableName(name)),
                BuildTableAlias(alias))
        {
        }

        /// <summary>Initializes a database- and schema-qualified table descriptor.</summary>
        public TableBase(string? databaseName, string schema, string name, Alias alias = default)
            : base(new ExprTableFullName(
                    new ExprDbSchema(databaseName != null ? new ExprDatabaseName(databaseName) : null, new ExprSchemaName(schema)),
                    new ExprTableName(name)),
                BuildTableAlias(alias))
        {
        }

        /// <summary>Initializes a table descriptor from syntax-level name and alias expressions.</summary>
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

        /// <summary>Registers multiple preconstructed columns with this descriptor.</summary>
        protected internal void AddColumns(IEnumerable<TableColumn> columns)
        {
            this._columns.AddRange(columns);
        }

        /// <summary>Registers multiple preconstructed indexes with this descriptor.</summary>
        protected internal void AddIndexes(IEnumerable<IndexMeta> indexes)
        {
            this._indexes.AddRange(indexes);
        }

        /// <summary>Creates and registers a non-nullable Boolean column.</summary>
        protected BooleanTableColumn CreateBooleanColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new BooleanTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable Boolean column.</summary>
        protected NullableBooleanTableColumn CreateNullableBooleanColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableBooleanTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable byte column.</summary>
        protected ByteTableColumn CreateByteColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new ByteTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable byte column.</summary>
        protected NullableByteTableColumn CreateNullableByteColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a variable-size, non-nullable binary column.</summary>
        protected ByteArrayTableColumn CreateByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
        {
            var result = new ByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a variable-size, nullable binary column.</summary>
        protected NullableByteArrayTableColumn CreateNullableByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a fixed-size, non-nullable binary column.</summary>
        protected ByteArrayTableColumn CreateFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
        {
            var result = new ByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a fixed-size, nullable binary column.</summary>
        protected NullableByteArrayTableColumn CreateNullableFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable 16-bit integer column.</summary>
        protected Int16TableColumn CreateInt16Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int16TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable 16-bit integer column.</summary>
        protected NullableInt16TableColumn CreateNullableInt16Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt16TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable 32-bit integer column.</summary>
        protected Int32TableColumn CreateInt32Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int32TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable 32-bit integer column.</summary>
        protected NullableInt32TableColumn CreateNullableInt32Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt32TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable 64-bit integer column.</summary>
        protected Int64TableColumn CreateInt64Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int64TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable 64-bit integer column.</summary>
        protected NullableInt64TableColumn CreateNullableInt64Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt64TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable decimal column.</summary>
        protected DecimalTableColumn CreateDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null,ColumnMeta? columnMeta = null)
        {
            var result = new DecimalTableColumn(this.Alias, new ExprColumnName(name), this, decimalPrecisionScale, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable decimal column.</summary>
        protected NullableDecimalTableColumn CreateNullableDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDecimalTableColumn(this.Alias, new ExprColumnName(name), this, decimalPrecisionScale, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable double-precision column.</summary>
        protected DoubleTableColumn CreateDoubleColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new DoubleTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable double-precision column.</summary>
        protected NullableDoubleTableColumn CreateNullableDoubleColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDoubleTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable date or date-time column.</summary>
        protected DateTimeTableColumn CreateDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
        {
            var result = new DateTimeTableColumn(this.Alias, new ExprColumnName(name), this, isDate, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable date or date-time column.</summary>
        protected NullableDateTimeTableColumn CreateNullableDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDateTimeTableColumn(this.Alias, new ExprColumnName(name), this, isDate, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable date-time-offset column.</summary>
        protected DateTimeOffsetTableColumn CreateDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new DateTimeOffsetTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable date-time-offset column.</summary>
        protected NullableDateTimeOffsetTableColumn CreateNullableDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDateTimeOffsetTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable GUID column.</summary>
        protected GuidTableColumn CreateGuidColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new GuidTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable GUID column.</summary>
        protected NullableGuidTableColumn CreateNullableGuidColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableGuidTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a variable-size, non-nullable string column.</summary>
        protected StringTableColumn CreateStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeString(size, isUnicode, isText), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a variable-size, nullable string column.</summary>
        protected NullableStringTableColumn CreateNullableStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeString(size, isUnicode, isText), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a fixed-size, nullable string column.</summary>
        protected NullableStringTableColumn CreateNullableFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeString(size, isUnicode), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a fixed-size, non-nullable string column.</summary>
        protected StringTableColumn CreateFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeString(size, isUnicode), columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable XML column represented by a string column.</summary>
        protected NullableStringTableColumn CreateNullableXmlColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, ExprTypeXml.Instance, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable XML column represented by a string column.</summary>
        protected StringTableColumn CreateXmlColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, ExprTypeXml.Instance, columnMeta);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Converts a public alias value into the syntax-level table alias representation.</summary>
        protected internal static ExprTableAlias? BuildTableAlias(Alias alias)
        {
            var a = alias.BuildAliasExpression();

            if (a != null)
            {
                return new ExprTableAlias(a);
            }

            return null;
        }

        /// <summary>Registers an unnamed non-unique, non-clustered index.</summary>
        protected void AddIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, false));
        /// <summary>Registers a named non-unique, non-clustered index.</summary>
        protected void AddIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, false));
        
        /// <summary>Registers an unnamed unique, non-clustered index.</summary>
        protected void AddUniqueIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, false));
        /// <summary>Registers a named unique, non-clustered index.</summary>
        protected void AddUniqueIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, true, false));
        
        /// <summary>Registers an unnamed non-unique clustered index.</summary>
        protected void AddClusteredIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, true));
        /// <summary>Registers a named non-unique clustered index.</summary>
        protected void AddClusteredIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, true));
        
        /// <summary>Registers an unnamed unique clustered index.</summary>
        protected void AddUniqueClusteredIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, true));
        /// <summary>Registers a named unique clustered index.</summary>
        protected void AddUniqueClusteredIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, true, true));

        private static IndexMetaColumn[] AssertIndexColumnsNotEmpty(IndexMetaColumn[] columns)
        {
            columns.AssertNotEmpty("Table index has to contain at least one column");
            return columns;
        }
    }
}
