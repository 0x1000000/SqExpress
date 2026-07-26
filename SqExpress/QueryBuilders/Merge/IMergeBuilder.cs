using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Merge;

/// <summary>Requires the join condition that matches target rows to source rows.</summary>
public interface IMergeBuilderCondition
{
    /// <summary>Sets the target-to-source match condition and opens the <c>WHEN</c> clause stages.</summary>
    /// <param name="on">The predicate relating target and source rows, normally by key columns.</param>
    /// <returns>The stage offering matched and unmatched actions.</returns>
    IMergeMatchedBuilder On(ExprBoolean on);
}

/// <summary>Offers actions for source rows that match target rows.</summary>
public interface IMergeMatchedBuilder : IMergeNotMatchedByTargetBuilder
{
    /// <summary>Starts a matched action restricted by an additional predicate.</summary>
    /// <param name="filter">An additional condition evaluated only for rows satisfying the main merge condition.</param>
    /// <returns>The stage that selects update or delete.</returns>
    IMergeMatchedThenBuilder WhenMatchedAnd(ExprBoolean filter);

    /// <summary>Starts an action for every target/source pair satisfying the main merge condition.</summary>
    /// <returns>The stage that selects update or delete.</returns>
    IMergeMatchedThenBuilder WhenMatched();
}

/// <summary>Selects the action applied to matched target and source rows.</summary>
public interface IMergeMatchedThenBuilder
{
    /// <summary>Chooses update for matched rows and requires its first target-column assignment.</summary>
    /// <returns>The first-assignment stage.</returns>
    IMergeMatchedThenFirstUpdateBuilder ThenUpdate();

    /// <summary>Chooses deletion of matched target rows and advances to unmatched actions.</summary>
    /// <returns>The stage offering not-matched-by-target actions.</returns>
    IMergeNotMatchedByTargetBuilder ThenDelete();
}

/// <summary>Requires the first assignment of a matched-row update.</summary>
public interface IMergeMatchedThenFirstUpdateBuilder : IUpdateSetter<IMergeMatchedThenUpdateBuilder, ExprColumn>
{

}

/// <summary>Allows more matched-row assignments or advancement to the next merge clause.</summary>
public interface IMergeMatchedThenUpdateBuilder : IMergeMatchedThenFirstUpdateBuilder, IMergeNotMatchedByTargetBuilder
{

}

/// <summary>Offers actions for source rows that have no matching target row.</summary>
public interface IMergeNotMatchedByTargetBuilder : IMergeNotMatchedBySourceBuilder
{
    /// <summary>Starts an insert action restricted by an additional source-row predicate.</summary>
    /// <param name="filter">An additional condition evaluated for source rows absent from the target.</param>
    /// <returns>The stage that selects explicit or default-values insertion.</returns>
    IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTargetAnd(ExprBoolean filter);

    /// <summary>Starts insertion for every source row that has no matching target row.</summary>
    /// <returns>The stage that selects explicit or default-values insertion.</returns>
    IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTarget();
}


/// <summary>Selects the insert form used for a source row absent from the target.</summary>
public interface IMergeNotMatchedByTargetThenBuilder
{
    /// <summary>Chooses explicit insert values and requires the first target-column assignment.</summary>
    /// <returns>The first-assignment stage.</returns>
    IMergeNotMatchedByTargetFirstInsertBuilder ThenInsert();

    /// <summary>Chooses a default-values insert for each qualifying source row where supported by the target database.</summary>
    /// <returns>The stage offering not-matched-by-source actions or completion.</returns>
    IMergeNotMatchedBySourceBuilder ThenInsertDefaultValues();
}

/// <summary>Requires the first target-column assignment for a merge insert.</summary>
public interface IMergeNotMatchedByTargetFirstInsertBuilder : IUpdateSetter<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>
{

}

/// <summary>Allows more insert assignments or advancement to the not-matched-by-source stage.</summary>
public interface IMergeNotMatchedByTargetInsertBuilder : IMergeNotMatchedByTargetFirstInsertBuilder, IMergeNotMatchedBySourceBuilder
{
}

/// <summary>Offers actions for target rows that have no matching source row, or completion of the merge.</summary>
public interface IMergeNotMatchedBySourceBuilder : IMergeBuilderDone
{
    /// <summary>Starts a target-only action restricted by an additional target-row predicate.</summary>
    /// <param name="filter">An additional condition evaluated for target rows absent from the source.</param>
    /// <returns>The stage that selects update or delete.</returns>
    IMergeMatchedBySourceThenBuilder WhenNotMatchedBySourceAnd(ExprBoolean filter);

    /// <summary>Starts an action for every target row that has no matching source row.</summary>
    /// <returns>The stage that selects update or delete.</returns>
    IMergeMatchedBySourceThenBuilder WhenNotMatchedBySource();
}

/// <summary>Selects the action applied to target rows absent from the source.</summary>
public interface IMergeMatchedBySourceThenBuilder
{
    /// <summary>Chooses update for source-missing target rows and requires its first assignment.</summary>
    /// <returns>The first-assignment stage.</returns>
    IMergeMatchedBySourceThenFirstUpdateBuilder ThenUpdate();

    /// <summary>Chooses deletion of target rows absent from the source.</summary>
    /// <returns>The merge-completion stage.</returns>
    IMergeBuilderDone ThenDelete();
}

/// <summary>Requires the first assignment for a target row absent from the source.</summary>
public interface IMergeMatchedBySourceThenFirstUpdateBuilder : IUpdateSetter<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>
{

}

/// <summary>Allows more target-only update assignments or completion of the merge.</summary>
public interface IMergeMatchedBySourceThenUpdateBuilder : IMergeMatchedBySourceThenFirstUpdateBuilder, IMergeBuilderDone
{

}


/// <summary>Represents a merge with enough clauses to produce an executable expression.</summary>
public interface IMergeBuilderDone : IExprExecFinal
{
    /// <summary>Materializes the configured merge actions as an executable syntax tree without running them.</summary>
    /// <returns>The completed merge expression.</returns>
    new ExprMerge Done();

    /// <summary>Starts a row-returning <c>OUTPUT</c> clause and requires its first projected affected-row value.</summary>
    /// <returns>The first-output-item stage.</returns>
    IOutputDoneFirst Output();
}

/// <summary>Requires the first item in a merge <c>OUTPUT</c> clause.</summary>
public interface IOutputDoneFirst : IOutputSetter<IOutputDone>
{

}

/// <summary>Allows more output items or completion as a row-returning merge expression.</summary>
public interface IOutputDone : IOutputSetter<IOutputDone>, IExprQueryFinal
{
    /// <summary>Materializes the merge and its output projection as a query syntax tree without executing it.</summary>
    /// <returns>The completed row-returning merge expression.</returns>
    new ExprMergeOutput Done();
}
