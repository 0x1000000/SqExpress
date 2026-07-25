using System.Collections.Generic;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Utils;

namespace SqExpress
{
    /// <summary>Base class for strongly typed derived-table descriptors backed by a subquery.</summary>
    /// <remarks>
    /// Derived classes implement <see cref="CreateQuery"/> and expose result columns created with the protected
    /// column helpers. A non-empty alias is required because derived-table columns must be qualified.
    /// </remarks>
    public abstract class DerivedTableBase : ExprDerivedTable
    {
        private ExprDerivedTableQuery? _table;

        private readonly List<ExprColumn> _columns = new List<ExprColumn>();

        /// <summary>Gets the result columns registered by this derived-table descriptor.</summary>
        public IReadOnlyList<ExprColumn> Columns => this._columns;

        /// <summary>Initializes a derived-table descriptor with an explicit or automatically generated alias.</summary>
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

        /// <summary>Creates the subquery represented by this derived-table descriptor.</summary>
        protected abstract IExprSubQuery CreateQuery();

        /// <inheritdoc/>
        public override IReadOnlyList<IExprSelecting> ExtractSelecting()
        {
            return this.Columns;
        }

        /// <inheritdoc/>
        public override IExprSubQuery CreateSubQuery()
        {
            return this.CreateQuery();
        }

        /// <inheritdoc/>
        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            this._table ??=
                new DerivedTableQueryWithOriginalRef(this.CreateQuery(), this.Alias, this.Columns.SelectToReadOnlyList(i => i.ColumnName), this);
            return this._table.Accept(visitor, arg);
        }

        internal T RegisterColumn<T>(T otherColumn) where T: ExprColumn
        {
            this._columns.Add(otherColumn);
            return otherColumn;
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

        /// <summary>Creates and registers a nullable string result column.</summary>
        protected NullableStringCustomColumn CreateNullableStringColumn(string name)
        {
            var result = new NullableStringCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }
    }
}
