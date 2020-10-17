using System.Collections.Generic;
using SqExpress.Meta;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

namespace SqExpress
{
    public class TableBase : ExprTable
    {
        public TableBase(string schema, string name, Alias alias = default) 
            : base(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName(schema)), new ExprTableName(name)), BuildTableAlias(alias))
        {
        }

        public TableBase(string databaseName, string schema, string name, Alias alias = default) 
            : base(new ExprTableFullName(new ExprDbSchema(new ExprDatabaseName(databaseName), new ExprSchemaName(schema)), new ExprTableName(name)), BuildTableAlias(alias))
        {
        }

        private readonly List<TableColumn> _columns = new List<TableColumn>();

        public IReadOnlyList<TableColumn> Columns => this._columns;

        public TableBaseScript Script => new TableBaseScript(this);

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

        private static ExprTableAlias? BuildTableAlias(Alias alias)
        {
            var a = alias.BuildAliasExpression();

            if (a != null)
            {
                return new ExprTableAlias(a);
            }

            return null;
        }
    }
}