using System.Collections.Generic;
using SqExpress.Meta;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Utils;

namespace SqExpress
{
    public class TableBase : ExprTable
    {
        public TableBase(string? schema, string name, Alias alias = default)
            : base(new ExprTableFullName(schema != null ? new ExprDbSchema(null, new ExprSchemaName(schema)) : null,
                    new ExprTableName(name)),
                BuildTableAlias(alias))
        {
        }

        public TableBase(string? databaseName, string schema, string name, Alias alias = default)
            : base(new ExprTableFullName(
                    new ExprDbSchema(databaseName != null ? new ExprDatabaseName(databaseName) : null, new ExprSchemaName(schema)),
                    new ExprTableName(name)),
                BuildTableAlias(alias))
        {
        }

        protected internal TableBase(IExprTableFullName fullName, ExprTableAlias? alias) : base(fullName, alias)
        {
        }

        private readonly List<TableColumn> _columns = new List<TableColumn>();
        
        private readonly List<IndexMeta> _indexes = new List<IndexMeta>();

        public IReadOnlyList<TableColumn> Columns => this._columns;

        public IReadOnlyList<IndexMeta> Indexes => this._indexes;

        public TableBaseScript Script => new TableBaseScript(this);

        protected internal void AddColumns(IEnumerable<TableColumn> columns)
        {
            this._columns.AddRange(columns);
        }

        protected internal void AddIndexes(IEnumerable<IndexMeta> indexes)
        {
            this._indexes.AddRange(indexes);
        }

        protected BooleanTableColumn CreateBooleanColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new BooleanTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableBooleanTableColumn CreateNullableBooleanColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableBooleanTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected ByteTableColumn CreateByteColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new ByteTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableByteTableColumn CreateNullableByteColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected ByteArrayTableColumn CreateByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
        {
            var result = new ByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableByteArrayTableColumn CreateNullableByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected ByteArrayTableColumn CreateFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
        {
            var result = new ByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableByteArrayTableColumn CreateNullableFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
        {
            var result = new NullableByteArrayTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeByteArray(size), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected Int16TableColumn CreateInt16Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int16TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableInt16TableColumn CreateNullableInt16Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt16TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected Int32TableColumn CreateInt32Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int32TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableInt32TableColumn CreateNullableInt32Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt32TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected Int64TableColumn CreateInt64Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new Int64TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableInt64TableColumn CreateNullableInt64Column(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableInt64TableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected DecimalTableColumn CreateDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null,ColumnMeta? columnMeta = null)
        {
            var result = new DecimalTableColumn(this.Alias, new ExprColumnName(name), this, decimalPrecisionScale, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableDecimalTableColumn CreateNullableDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDecimalTableColumn(this.Alias, new ExprColumnName(name), this, decimalPrecisionScale, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected DoubleTableColumn CreateDoubleColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new DoubleTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableDoubleTableColumn CreateNullableDoubleColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDoubleTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected DateTimeTableColumn CreateDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
        {
            var result = new DateTimeTableColumn(this.Alias, new ExprColumnName(name), this, isDate, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableDateTimeTableColumn CreateNullableDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDateTimeTableColumn(this.Alias, new ExprColumnName(name), this, isDate, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected DateTimeOffsetTableColumn CreateDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new DateTimeOffsetTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableDateTimeOffsetTableColumn CreateNullableDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableDateTimeOffsetTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected GuidTableColumn CreateGuidColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new GuidTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableGuidTableColumn CreateNullableGuidColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableGuidTableColumn(this.Alias, new ExprColumnName(name), this, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected StringTableColumn CreateStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeString(size, isUnicode, isText), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableStringTableColumn CreateNullableStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeString(size, isUnicode, isText), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableStringTableColumn CreateNullableFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeString(size, isUnicode), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected StringTableColumn CreateFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, new ExprTypeFixSizeString(size, isUnicode), columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected NullableStringTableColumn CreateNullableXmlColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new NullableStringTableColumn(this.Alias, new ExprColumnName(name), this, ExprTypeXml.Instance, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected StringTableColumn CreateXmlColumn(string name, ColumnMeta? columnMeta = null)
        {
            var result = new StringTableColumn(this.Alias, new ExprColumnName(name), this, ExprTypeXml.Instance, columnMeta);
            this._columns.Add(result);
            return result;
        }

        protected internal static ExprTableAlias? BuildTableAlias(Alias alias)
        {
            var a = alias.BuildAliasExpression();

            if (a != null)
            {
                return new ExprTableAlias(a);
            }

            return null;
        }

        protected void AddIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, false));
        protected void AddIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, false));
        
        protected void AddUniqueIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, false));
        protected void AddUniqueIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(columns, name, true, false));
        
        protected void AddClusteredIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, true));
        protected void AddClusteredIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, true));
        
        protected void AddUniqueClusteredIndex(params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, true));
        protected void AddUniqueClusteredIndex(string name, params IndexMetaColumn[] columns) => this._indexes.Add(new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, true, true));

        private static IndexMetaColumn[] AssertIndexColumnsNotEmpty(IndexMetaColumn[] columns)
        {
            columns.AssertNotEmpty("Table index has to contain at least one column");
            return columns;
        }
    }
}