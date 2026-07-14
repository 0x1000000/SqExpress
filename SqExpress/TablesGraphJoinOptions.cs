using System;
using System.Collections.Generic;

namespace SqExpress;

public enum AmbiguousJoinPathBehavior
{
    DeterministicFirst,
    Fail,
    Callback
}

public sealed class TablesGraphJoinOptions
{
    public AmbiguousJoinPathBehavior AmbiguousPathBehavior { get; set; }
        = AmbiguousJoinPathBehavior.DeterministicFirst;

    public Func<IReadOnlyList<IReadOnlyList<TableBase>>, int>? AmbiguousPathResolver { get; set; }
}
