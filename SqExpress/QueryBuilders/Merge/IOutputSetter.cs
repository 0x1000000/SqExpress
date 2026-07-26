using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.QueryBuilders.Merge
{
    /// <summary>Builds the items returned by a merge <c>OUTPUT</c> clause.</summary>
    /// <typeparam name="TNext">The next output-builder stage.</typeparam>
    public interface IOutputSetter<out TNext>
    {
        /// <summary>Returns the post-merge value of a target column.</summary>
        /// <param name="column">The target column read after the merge action.</param>
        /// <returns>The next output-builder stage.</returns>
        TNext Inserted(ExprColumn column);
        /// <summary>Returns and aliases the post-merge value of a target column.</summary>
        /// <param name="column">The target column and desired result-set alias.</param>
        /// <returns>The next output-builder stage.</returns>
        TNext Inserted(ExprAliasedColumn column);

        /// <summary>Returns the pre-merge value of a target column.</summary>
        /// <param name="column">The target column read before update or deletion.</param>
        /// <returns>The next output-builder stage.</returns>
        TNext Deleted(ExprColumn column);
        /// <summary>Returns and aliases the pre-merge value of a target column.</summary>
        /// <param name="column">The pre-action target column and desired result-set alias.</param>
        /// <returns>The next output-builder stage.</returns>
        TNext Deleted(ExprAliasedColumn column);

        /// <summary>Returns a column from the merge statement's visible row sources.</summary>
        /// <param name="column">A qualified target or source column visible to the output clause.</param>
        /// <returns>The next output-builder stage.</returns>
        TNext Column(ExprColumn column);
        /// <summary>Returns and aliases a column from the merge statement's visible row sources.</summary>
        /// <param name="column">The visible column and desired result-set alias.</param>
        /// <returns>The next output-builder stage.</returns>
        TNext Column(ExprAliasedColumn column);

        /// <summary>Returns the database action indicator, optionally under an alias.</summary>
        /// <param name="alias">An optional result column name for the insert/update/delete action indicator.</param>
        /// <returns>The next output-builder stage.</returns>
        TNext Action(ExprColumnAlias? alias = null);
    }

    /// <summary>Allows additional items to be appended to a mapped merge output clause.</summary>
    public interface IOutputSetterNext : IOutputSetter<IOutputSetterNext>
    {
    }
}
