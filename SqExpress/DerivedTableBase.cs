using System.Collections.Generic;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Utils;

namespace SqExpress
{
    public abstract class DerivedTableBase : ExprDerivedTable
    {
        private ExprDerivedTableQuery? _table;

        private readonly List<ExprColumn> _columns = new List<ExprColumn>();

        public IReadOnlyList<ExprColumn> Columns => this._columns;

        protected DerivedTableBase(Alias alias = default) : base(BuildAlias(alias))
        {
        }

        private static ExprTableAlias BuildAlias(Alias alias)
        {
            var a = alias.BuildAliasExpression();
            if (a == null)
            {
                throw new SqExpressException("Derived table alias cannot be empty");
            }
            return new ExprTableAlias(a);
        }

        protected abstract IExprSubQuery CreateQuery();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
        {
            this._table ??=
                new ExprDerivedTableQuery(this.CreateQuery(), this.Alias, this.Columns.SelectToReadOnlyList(i => i.ColumnName));
            return this._table.Accept(visitor);
        }

        internal T RegisterColumn<T>(T otherColumn) where T: ExprColumn
        {
            this._columns.Add(otherColumn);
            return otherColumn;
        }

        protected BooleanCustomColumn CreateBooleanColumn(string name)
        {
            var result = new BooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableBooleanCustomColumn CreateNullableBooleanColumn(string name)
        {
            var result = new NullableBooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected ByteCustomColumn CreateByteColumn(string name)
        {
            var result = new ByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableByteCustomColumn CreateNullableByteColumn(string name)
        {
            var result = new NullableByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected Int16CustomColumn CreateInt16Column(string name)
        {
            var result = new Int16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableInt16CustomColumn CreateNullableInt16Column(string name)
        {
            var result = new NullableInt16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected Int32CustomColumn CreateInt32Column(string name)
        {
            var result = new Int32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableInt32CustomColumn CreateNullableInt32Column(string name)
        {
            var result = new NullableInt32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected Int64CustomColumn CreateInt64Column(string name)
        {
            var result = new Int64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableInt64CustomColumn CreateNullableInt64Column(string name)
        {
            var result = new NullableInt64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected DecimalCustomColumn CreateDecimalColumn(string name)
        {
            var result = new DecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableDecimalCustomColumn CreateNullableDecimalColumn(string name)
        {
            var result = new NullableDecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected DoubleCustomColumn CreateDoubleColumn(string name)
        {
            var result = new DoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableDoubleCustomColumn CreateNullableDoubleColumn(string name)
        {
            var result = new NullableDoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected DateTimeCustomColumn CreateDateTimeColumn(string name)
        {
            var result = new DateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableDateTimeCustomColumn CreateNullableDateTimeColumn(string name)
        {
            var result = new NullableDateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected GuidCustomColumn CreateGuidColumn(string name)
        {
            var result = new GuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableGuidCustomColumn CreateNullableGuidColumn(string name)
        {
            var result = new NullableGuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected StringCustomColumn CreateStringColumn(string name)
        {
            var result = new StringCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }
    }
}