using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Merge;

public interface IMergeBuilderCondition
{
    IMergeMatchedBuilder On(ExprBoolean on);
}

public interface IMergeMatchedBuilder : IMergeNotMatchedByTargetBuilder
{
    IMergeMatchedThenBuilder WhenMatchedAnd(ExprBoolean filter);

    IMergeMatchedThenBuilder WhenMatched();
}

public interface IMergeMatchedThenBuilder
{
    IMergeMatchedThenFirstUpdateBuilder ThenUpdate();

    IMergeNotMatchedByTargetBuilder ThenDelete();
}

public interface IMergeMatchedThenFirstUpdateBuilder : IUpdateSetter<IMergeMatchedThenUpdateBuilder, ExprColumn>
{

}

public interface IMergeMatchedThenUpdateBuilder : IMergeMatchedThenFirstUpdateBuilder, IMergeNotMatchedByTargetBuilder
{

}

public interface IMergeNotMatchedByTargetBuilder : IMergeNotMatchedBySourceBuilder
{
    IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTargetAnd(ExprBoolean filter);

    IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTarget();
}


public interface IMergeNotMatchedByTargetThenBuilder
{
    IMergeNotMatchedByTargetFirstInsertBuilder ThenInsert();

    IMergeNotMatchedBySourceBuilder ThenInsertDefaultValues();
}

public interface IMergeNotMatchedByTargetFirstInsertBuilder : IUpdateSetter<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>
{

}

public interface IMergeNotMatchedByTargetInsertBuilder : IMergeNotMatchedByTargetFirstInsertBuilder, IMergeNotMatchedBySourceBuilder
{
}

public interface IMergeNotMatchedBySourceBuilder : IMergeBuilderDone
{
    IMergeMatchedBySourceThenBuilder WhenNotMatchedBySourceAnd(ExprBoolean filter);

    IMergeMatchedBySourceThenBuilder WhenNotMatchedBySource();
}

public interface IMergeMatchedBySourceThenBuilder
{
    IMergeMatchedBySourceThenFirstUpdateBuilder ThenUpdate();

    IMergeBuilderDone ThenDelete();
}

public interface IMergeMatchedBySourceThenFirstUpdateBuilder : IUpdateSetter<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>
{

}

public interface IMergeMatchedBySourceThenUpdateBuilder : IMergeMatchedBySourceThenFirstUpdateBuilder, IMergeBuilderDone
{

}


public interface IMergeBuilderDone : IExprExecFinal
{
    new ExprMerge Done();

    IOutputDoneFirst Output();
}

public interface IOutputDoneFirst : IOutputSetter<IOutputDone>
{

}

public interface IOutputDone : IOutputSetter<IOutputDone>, IExprQueryFinal
{
    new ExprMergeOutput Done();
}
