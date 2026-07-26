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

        /// <summary>Initializes a reusable CTE descriptor whose query and typed result columns are supplied by the derived class.</summary>
        /// <param name="name">The CTE name emitted in the <c>WITH</c> clause.</param>
        /// <param name="alias">The qualifier used for references to the CTE; by default the CTE name is used.</param>
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

        /// <summary>Registers an alias-qualified, non-nullable Boolean reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected BooleanCustomColumn CreateBooleanColumn(string name)
        {
            var result = new BooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable Boolean reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableBooleanCustomColumn CreateNullableBooleanColumn(string name)
        {
            var result = new NullableBooleanCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable byte reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected ByteCustomColumn CreateByteColumn(string name)
        {
            var result = new ByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable byte reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableByteCustomColumn CreateNullableByteColumn(string name)
        {
            var result = new NullableByteCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable binary reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected ByteArrayCustomColumn CreateByteArrayColumn(string name)
        {
            var result = new ByteArrayCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable binary reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableByteArrayCustomColumn CreateNullableByteArrayColumn(string name)
        {
            var result = new NullableByteArrayCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable 16-bit integer reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected Int16CustomColumn CreateInt16Column(string name)
        {
            var result = new Int16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable 16-bit integer reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableInt16CustomColumn CreateNullableInt16Column(string name)
        {
            var result = new NullableInt16CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable 32-bit integer reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected Int32CustomColumn CreateInt32Column(string name)
        {
            var result = new Int32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable 32-bit integer reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableInt32CustomColumn CreateNullableInt32Column(string name)
        {
            var result = new NullableInt32CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable 64-bit integer reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected Int64CustomColumn CreateInt64Column(string name)
        {
            var result = new Int64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable 64-bit integer reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableInt64CustomColumn CreateNullableInt64Column(string name)
        {
            var result = new NullableInt64CustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable decimal reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected DecimalCustomColumn CreateDecimalColumn(string name)
        {
            var result = new DecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable decimal reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableDecimalCustomColumn CreateNullableDecimalColumn(string name)
        {
            var result = new NullableDecimalCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable double-precision reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected DoubleCustomColumn CreateDoubleColumn(string name)
        {
            var result = new DoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable double-precision reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableDoubleCustomColumn CreateNullableDoubleColumn(string name)
        {
            var result = new NullableDoubleCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable date/time reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected DateTimeCustomColumn CreateDateTimeColumn(string name)
        {
            var result = new DateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable date/time reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableDateTimeCustomColumn CreateNullableDateTimeColumn(string name)
        {
            var result = new NullableDateTimeCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable date-time-offset reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected DateTimeOffsetCustomColumn CreateDateTimeOffsetColumn(string name)
        {
            var result = new DateTimeOffsetCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable date-time-offset reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableDateTimeOffsetCustomColumn CreateNullableDateTimeOffsetColumn(string name)
        {
            var result = new NullableDateTimeOffsetCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable GUID reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected GuidCustomColumn CreateGuidColumn(string name)
        {
            var result = new GuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, nullable GUID reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
        protected NullableGuidCustomColumn CreateNullableGuidColumn(string name)
        {
            var result = new NullableGuidCustomColumn(name, this.Alias);
            this._columns.Add(result);
            return result;
        }

        /// <summary>Registers an alias-qualified, non-nullable string reference in the CTE result shape.</summary>
        /// <param name="name">The output name produced by the CTE query.</param>
        /// <returns>A typed column reference for use by consumers of the CTE.</returns>
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
