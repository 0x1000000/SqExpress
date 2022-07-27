using System.Collections.Generic;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Utils;

namespace SqExpress
{
    public abstract class CteBase : ExprCte
    {
        private ExprCteQuery? _query;

        private readonly List<ExprColumn> _columns = new List<ExprColumn>();

        public IReadOnlyList<ExprColumn> Columns => this._columns;

        protected CteBase(string name, Alias alias = default) : base(name, BuildAlias(alias, name))
        {
        }

        private static ExprTableAlias? BuildAlias(Alias alias, string name)
        {
            var a = alias.BuildAliasExpression();
            return a == null ? null : new ExprTableAlias(a);
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            this._query ??= new CteOriginalRef(this.Name, this.Alias, this.CreateQuery(), this);
            return this._query.Accept(visitor, arg);
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

        protected ByteArrayCustomColumn CreateByteArrayColumn(string name)
        {
            var result = new ByteArrayCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableByteArrayCustomColumn CreateNullableByteArrayColumn(string name)
        {
            var result = new NullableByteArrayCustomColumn(name, this.Alias);
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

        protected DateTimeOffsetCustomColumn CreateDateTimeOffsetColumn(string name)
        {
            var result = new DateTimeOffsetCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        protected NullableDateTimeOffsetCustomColumn CreateNullableDateTimeOffsetColumn(string name)
        {
            var result = new NullableDateTimeOffsetCustomColumn(name, this.Alias);
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

    //It is required for proper syntax tree modification
    internal class CteOriginalRef : ExprCteQuery
    {
        public CteBase Original { get; }

        public CteOriginalRef(string name, ExprTableAlias? alias, IExprSubQuery query, CteBase original) : base(name, alias, query)
        {
            this.Original = original;
        }
    }
}