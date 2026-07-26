using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Delete
{
    /// <summary>Builds a delete directly against a target table or through a joined source.</summary>
    public readonly struct DeleteBuilder
    {
        private readonly ExprTable _target;

        /// <summary>Initializes a delete builder for a target table.</summary>
        /// <remarks>Most callers should start with <see cref="SqQueryBuilder.Delete(ExprTable)"/>.</remarks>
        /// <param name="target">The table whose rows may be deleted.</param>
        public DeleteBuilder(ExprTable target)
        {
            this._target = target;
        }

        /// <summary>Explicitly completes an unfiltered delete that affects every target row.</summary>
        /// <returns>The completed delete syntax tree.</returns>
        public ExprDelete All()
        {
            return new ExprDelete(target: this._target, source: null, filter: null);
        }

        /// <summary>Completes a delete with the supplied target-row predicate.</summary>
        /// <remarks>A null predicate is equivalent to an unfiltered delete.</remarks>
        /// <param name="filter">The SQL predicate selecting rows to delete.</param>
        /// <returns>The completed delete syntax tree.</returns>
        public ExprDelete Where(ExprBoolean? filter)
        {
            return new ExprDelete(target: this._target, source: null, filter: filter);
        }

        /// <summary>Adds a source table expression used by joins or the delete predicate.</summary>
        /// <param name="source">The initial table, derived table, CTE, or joined source.</param>
        /// <returns>The joined-delete stage.</returns>
        public DeleteFromBuilder From(IExprTableSource source)
        {
            return new DeleteFromBuilder(target: this._target, source: source);
        }
    }
}
