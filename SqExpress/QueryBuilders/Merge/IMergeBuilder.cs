using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Merge;

/// <summary>Requires the join condition that matches target rows to source rows.</summary>
public interface IMergeBuilderCondition
{
    /// <summary>Sets the target-to-source match condition and opens the <c>WHEN</c> clause stages.</summary>
    IMergeMatchedBuilder On(ExprBoolean on);
}

/// <summary>Offers actions for source rows that match target rows.</summary>
public interface IMergeMatchedBuilder : IMergeNotMatchedByTargetBuilder
{
    /// <summary>Starts a matched action restricted by an additional predicate.</summary>
    IMergeMatchedThenBuilder WhenMatchedAnd(ExprBoolean filter);

    /// <summary>Starts an unconditional matched action.</summary>
    IMergeMatchedThenBuilder WhenMatched();
}

/// <summary>Selects the action applied to matched target and source rows.</summary>
public interface IMergeMatchedThenBuilder
{
    /// <summary>Starts the assignments for a matched-row update.</summary>
    IMergeMatchedThenFirstUpdateBuilder ThenUpdate();

    /// <summary>Deletes matched target rows and advances to the next merge clause.</summary>
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
    IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTargetAnd(ExprBoolean filter);

    /// <summary>Starts an unconditional insert action for source rows absent from the target.</summary>
    IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTarget();
}


/// <summary>Selects the insert form used for a source row absent from the target.</summary>
public interface IMergeNotMatchedByTargetThenBuilder
{
    /// <summary>Starts explicit target-column and value assignments for the insert.</summary>
    IMergeNotMatchedByTargetFirstInsertBuilder ThenInsert();

    /// <summary>Inserts a row using the target table's default values.</summary>
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
    IMergeMatchedBySourceThenBuilder WhenNotMatchedBySourceAnd(ExprBoolean filter);

    /// <summary>Starts an unconditional action for target rows absent from the source.</summary>
    IMergeMatchedBySourceThenBuilder WhenNotMatchedBySource();
}

/// <summary>Selects the action applied to target rows absent from the source.</summary>
public interface IMergeMatchedBySourceThenBuilder
{
    /// <summary>Starts the assignments for updating unmatched target rows.</summary>
    IMergeMatchedBySourceThenFirstUpdateBuilder ThenUpdate();

    /// <summary>Deletes target rows that are absent from the source.</summary>
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
    /// <summary>Completes the builder and returns the merge syntax tree.</summary>
    new ExprMerge Done();

    /// <summary>Starts a SQL <c>OUTPUT</c> clause and requires at least one output item.</summary>
    IOutputDoneFirst Output();
}

/// <summary>Requires the first item in a merge <c>OUTPUT</c> clause.</summary>
public interface IOutputDoneFirst : IOutputSetter<IOutputDone>
{

}

/// <summary>Allows more output items or completion as a row-returning merge expression.</summary>
public interface IOutputDone : IOutputSetter<IOutputDone>, IExprQueryFinal
{
    /// <summary>Completes the builder and returns the row-returning merge syntax tree.</summary>
    new ExprMergeOutput Done();
}
