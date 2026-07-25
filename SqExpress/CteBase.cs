using System.Collections.Generic;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Utils;

namespace SqExpress
{
    /// <summary>Base class for strongly typed common table expressions.</summary>
    /// <remarks>
    /// Derived classes define the CTE query through the inherited query contract and expose result columns created
    /// with the protected column helpers. The query is created lazily when the CTE enters a syntax-tree operation.
    /// </remarks>
    public abstract class CteBase : ExprCte
    {
        private ExprCteQuery? _query;

        private readonly List<ExprColumn> _columns = new List<ExprColumn>();

        /// <summary>Gets the result columns registered by this CTE descriptor.</summary>
        public IReadOnlyList<ExprColumn> Columns => this._columns;

        /// <summary>Initializes a named CTE descriptor with an optional alias.</summary>
        protected CteBase(string name, Alias alias = default) : base(name, BuildAlias(alias, name))
        {
        }

        private static ExprTableAlias? BuildAlias(Alias alias, string name)
        {
            var a = alias.BuildAliasExpression();
            return a == null ? null : new ExprTableAlias(a);
        }

        /// <inheritdoc/>
        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            this._query ??= new CteOriginalRef(this.Name, this.Alias, this.CreateQuery(), this);
            return this._query.Accept(visitor, arg);
        }

        /// <summary>Creates and registers a non-nullable Boolean result column.</summary>
        protected BooleanCustomColumn CreateBooleanColumn(string name)
        {
            var result = new BooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable Boolean result column.</summary>
        protected NullableBooleanCustomColumn CreateNullableBooleanColumn(string name)
        {
            var result = new NullableBooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable byte result column.</summary>
        protected ByteCustomColumn CreateByteColumn(string name)
        {
            var result = new ByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable byte result column.</summary>
        protected NullableByteCustomColumn CreateNullableByteColumn(string name)
        {
            var result = new NullableByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable binary result column.</summary>
        protected ByteArrayCustomColumn CreateByteArrayColumn(string name)
        {
            var result = new ByteArrayCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable binary result column.</summary>
        protected NullableByteArrayCustomColumn CreateNullableByteArrayColumn(string name)
        {
            var result = new NullableByteArrayCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable 16-bit integer result column.</summary>
        protected Int16CustomColumn CreateInt16Column(string name)
        {
            var result = new Int16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable 16-bit integer result column.</summary>
        protected NullableInt16CustomColumn CreateNullableInt16Column(string name)
        {
            var result = new NullableInt16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable 32-bit integer result column.</summary>
        protected Int32CustomColumn CreateInt32Column(string name)
        {
            var result = new Int32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable 32-bit integer result column.</summary>
        protected NullableInt32CustomColumn CreateNullableInt32Column(string name)
        {
            var result = new NullableInt32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable 64-bit integer result column.</summary>
        protected Int64CustomColumn CreateInt64Column(string name)
        {
            var result = new Int64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable 64-bit integer result column.</summary>
        protected NullableInt64CustomColumn CreateNullableInt64Column(string name)
        {
            var result = new NullableInt64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable decimal result column.</summary>
        protected DecimalCustomColumn CreateDecimalColumn(string name)
        {
            var result = new DecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable decimal result column.</summary>
        protected NullableDecimalCustomColumn CreateNullableDecimalColumn(string name)
        {
            var result = new NullableDecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable double-precision result column.</summary>
        protected DoubleCustomColumn CreateDoubleColumn(string name)
        {
            var result = new DoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable double-precision result column.</summary>
        protected NullableDoubleCustomColumn CreateNullableDoubleColumn(string name)
        {
            var result = new NullableDoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable date/time result column.</summary>
        protected DateTimeCustomColumn CreateDateTimeColumn(string name)
        {
            var result = new DateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable date/time result column.</summary>
        protected NullableDateTimeCustomColumn CreateNullableDateTimeColumn(string name)
        {
            var result = new NullableDateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable date-time-offset result column.</summary>
        protected DateTimeOffsetCustomColumn CreateDateTimeOffsetColumn(string name)
        {
            var result = new DateTimeOffsetCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable date-time-offset result column.</summary>
        protected NullableDateTimeOffsetCustomColumn CreateNullableDateTimeOffsetColumn(string name)
        {
            var result = new NullableDateTimeOffsetCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable GUID result column.</summary>
        protected GuidCustomColumn CreateGuidColumn(string name)
        {
            var result = new GuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a nullable GUID result column.</summary>
        protected NullableGuidCustomColumn CreateNullableGuidColumn(string name)
        {
            var result = new NullableGuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Creates and registers a non-nullable string result column.</summary>
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
