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

        /// <summary>Initializes a reusable descriptor for a parenthesized subquery in a <c>FROM</c> or join clause.</summary>
        /// <param name="alias">The required qualifier for projected columns; the default requests an automatically generated alias.</param>
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

        /// <summary>Builds the subquery whose projection must correspond to the columns registered by the descriptor.</summary>
        /// <returns>The completed subquery placed inside the derived-table expression.</returns>
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

        /// <summary>Registers an alias-qualified, non-nullable Boolean reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected BooleanCustomColumn CreateBooleanColumn(string name)
        {
            var result = new BooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable Boolean reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableBooleanCustomColumn CreateNullableBooleanColumn(string name)
        {
            var result = new NullableBooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable byte reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected ByteCustomColumn CreateByteColumn(string name)
        {
            var result = new ByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable byte reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableByteCustomColumn CreateNullableByteColumn(string name)
        {
            var result = new NullableByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable binary reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected ByteArrayCustomColumn CreateByteArrayColumn(string name)
        {
            var result = new ByteArrayCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable binary reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableByteArrayCustomColumn CreateNullableByteArrayColumn(string name)
        {
            var result = new NullableByteArrayCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable 16-bit integer reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected Int16CustomColumn CreateInt16Column(string name)
        {
            var result = new Int16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable 16-bit integer reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableInt16CustomColumn CreateNullableInt16Column(string name)
        {
            var result = new NullableInt16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable 32-bit integer reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected Int32CustomColumn CreateInt32Column(string name)
        {
            var result = new Int32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable 32-bit integer reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableInt32CustomColumn CreateNullableInt32Column(string name)
        {
            var result = new NullableInt32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable 64-bit integer reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected Int64CustomColumn CreateInt64Column(string name)
        {
            var result = new Int64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable 64-bit integer reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableInt64CustomColumn CreateNullableInt64Column(string name)
        {
            var result = new NullableInt64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable decimal reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected DecimalCustomColumn CreateDecimalColumn(string name)
        {
            var result = new DecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable decimal reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableDecimalCustomColumn CreateNullableDecimalColumn(string name)
        {
            var result = new NullableDecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable double-precision reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected DoubleCustomColumn CreateDoubleColumn(string name)
        {
            var result = new DoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable double-precision reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableDoubleCustomColumn CreateNullableDoubleColumn(string name)
        {
            var result = new NullableDoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable date/time reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected DateTimeCustomColumn CreateDateTimeColumn(string name)
        {
            var result = new DateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable date/time reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableDateTimeCustomColumn CreateNullableDateTimeColumn(string name)
        {
            var result = new NullableDateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable date-time-offset reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected DateTimeOffsetCustomColumn CreateDateTimeOffsetColumn(string name)
        {
            var result = new DateTimeOffsetCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable date-time-offset reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableDateTimeOffsetCustomColumn CreateNullableDateTimeOffsetColumn(string name)
        {
            var result = new NullableDateTimeOffsetCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable GUID reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected GuidCustomColumn CreateGuidColumn(string name)
        {
            var result = new GuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable GUID reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableGuidCustomColumn CreateNullableGuidColumn(string name)
        {
            var result = new NullableGuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable string reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected StringCustomColumn CreateStringColumn(string name)
        {
            var result = new StringCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable string reference in the derived table's projected shape.</summary>
        /// <param name="name">The output name produced by <see cref="CreateQuery"/>.</param>
        /// <returns>A typed column reference for queries consuming this derived table.</returns>
        protected NullableStringCustomColumn CreateNullableStringColumn(string name)
        {
            var result = new NullableStringCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }
    }
}
