using System;
using System.Collections.Generic;

namespace SqExpress;

/// <summary>Specifies how join discovery handles multiple equal shortest paths.</summary>
public enum AmbiguousJoinPathBehavior
{
    /// <summary>Selects candidate zero, preserving deterministic table and foreign-key discovery order.</summary>
    DeterministicFirst,

    /// <summary>Treats ambiguity as an unsuccessful join attempt.</summary>
    Fail,

    /// <summary>Delegates candidate selection to <see cref="TablesGraphJoinOptions.AmbiguousPathResolver"/>.</summary>
    Callback
}

/// <summary>Configures shortest-path selection performed by <see cref="TablesGraph"/> join operations.</summary>
/// <remarks>
/// Candidate paths contain canonical graph descriptors in traversal order. With ordered intermediate tables, the
/// ambiguity policy is applied independently to every segment. Invalid callback configuration throws
/// <see cref="ArgumentException"/> rather than being interpreted as a failed path search.
/// </remarks>
/// <example>
/// <code>
/// var options = new TablesGraphJoinOptions
/// {
///     AmbiguousPathBehavior = AmbiguousJoinPathBehavior.Callback,
///     AmbiguousPathResolver = paths =&gt; paths
///         .Select((path, index) =&gt; (path, index))
///         .First(item =&gt; item.path.Any(table =&gt; table.FullName.TableName.Name == "PreferredHub"))
///         .index
/// };
///
/// graph.TryToJoinTables(order, country, out var source, options);
/// </code>
/// </example>
public sealed class TablesGraphJoinOptions
{
    /// <summary>Gets or sets the policy for equal shortest join paths.</summary>
    public AmbiguousJoinPathBehavior AmbiguousPathBehavior { get; set; }
        = AmbiguousJoinPathBehavior.DeterministicFirst;

    /// <summary>Gets or sets the callback that returns the zero-based candidate index to use.</summary>
    /// <remarks>
    /// Required only when <see cref="AmbiguousPathBehavior"/> is <see cref="AmbiguousJoinPathBehavior.Callback"/>.
    /// Returning a negative or out-of-range index is invalid configuration.
    /// </remarks>
    public Func<IReadOnlyList<IReadOnlyList<TableBase>>, int>? AmbiguousPathResolver { get; set; }
}
