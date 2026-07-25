using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.QueryBuilders.Merge
{
    /// <summary>Builds the items returned by a merge <c>OUTPUT</c> clause.</summary>
    /// <typeparam name="TNext">The next output-builder stage.</typeparam>
    public interface IOutputSetter<out TNext>
    {
        /// <summary>Returns the post-merge value of a target column.</summary>
        TNext Inserted(ExprColumn column);
        /// <summary>Returns and aliases the post-merge value of a target column.</summary>
        TNext Inserted(ExprAliasedColumn column);

        /// <summary>Returns the pre-merge value of a target column.</summary>
        TNext Deleted(ExprColumn column);
        /// <summary>Returns and aliases the pre-merge value of a target column.</summary>
        TNext Deleted(ExprAliasedColumn column);

        /// <summary>Returns a column from the merge statement's visible row sources.</summary>
        TNext Column(ExprColumn column);
        /// <summary>Returns and aliases a column from the merge statement's visible row sources.</summary>
        TNext Column(ExprAliasedColumn column);

        /// <summary>Returns the database action indicator, optionally under an alias.</summary>
        TNext Action(ExprColumnAlias? alias = null);
    }

    /// <summary>Allows additional items to be appended to a mapped merge output clause.</summary>
    public interface IOutputSetterNext : IOutputSetter<IOutputSetterNext>
    {
    }
}
