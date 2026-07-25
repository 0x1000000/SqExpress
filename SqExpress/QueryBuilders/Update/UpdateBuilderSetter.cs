using System;
using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.Update
{
    /// <summary>Allows additional assignments and selects the row source or filter for an <c>UPDATE</c> statement.</summary>
    public readonly struct UpdateBuilderSetter : IUpdateSetter<UpdateBuilderSetter, ExprColumn>
    {
        private readonly ExprTable _target;

        private readonly List<ExprColumnSetClause> _sets;

        internal UpdateBuilderSetter(ExprTable target, List<ExprColumnSetClause> sets)
        {
            this._target = target;
            this._sets = sets;
        }

        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, IExprAssigning value)
        {
            this._sets.Add(new ExprColumnSetClause(col, value));
            return new UpdateBuilderSetter(this._target, this._sets);
        }

        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, int? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, int value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, string value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, Guid? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, Guid value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, DateTime? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, DateTime value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, DateTimeOffset? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, DateTimeOffset value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, bool? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, bool value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, byte? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, byte value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, short? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, short value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, long? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, long value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, decimal? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, decimal value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, double? value) => this.Set(col, SqQueryBuilder.Literal(value));
        /// <inheritdoc/>
        public UpdateBuilderSetter Set(ExprColumn col, double value) => this.Set(col, SqQueryBuilder.Literal(value));


        /// <summary>Adds a source table expression used by assignments, joins, or the row filter.</summary>
        public UpdateBuilderFinal From(IExprTableSource source) =>
            new UpdateBuilderFinal(this._target, this._sets, source);

        /// <summary>Completes an update without a <c>WHERE</c> predicate, affecting all target rows.</summary>
        public ExprUpdate All() => new ExprUpdate(this._target, this._sets, null, null);

        /// <summary>Completes an update with the supplied row predicate.</summary>
        /// <remarks>A <see langword="null"/> condition is equivalent to an unfiltered update.</remarks>
        public ExprUpdate Where(ExprBoolean? condition) 
            => new ExprUpdate(this._target, this._sets, null, condition);
    }
}
