using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.Syntax.Names;

namespace SqExpress;

/// <summary>Provides lightweight parent/child navigation derived directly from table foreign-key metadata.</summary>
/// <remarks>
/// These helpers return table names rather than canonical descriptors and do not validate graph completeness or
/// cycles. Use <see cref="TablesGraph"/> when canonical resolution, transitive traversal, validation, or join-path
/// construction is required.
/// </remarks>
public static class TableHierarchyExtensions {

    /// <summary>Gets the distinct full names of tables directly referenced by this table's foreign keys.</summary>
    /// <param name="tableBase">The child table whose foreign-key targets are returned.</param>
    /// <returns>A deferred sequence of direct parent table names. Self-references are excluded.</returns>
    /// <example>
    /// <code>
    /// foreach (var parentName in customer.GetParentTables())
    /// {
    ///     Console.WriteLine(parentName.TableName.Name);
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<ExprTableFullName> GetParentTables(this TableBase tableBase)
    {
        return tableBase
            .Columns
            .SelectMany(
                c => c.ColumnMeta?.ForeignKeyColumns?.Select(fk => fk.Table.FullName.AsExprTableFullName())
                    .Where(tn => !Equals(tn, tableBase.FullName.AsExprTableFullName())) ?? Array.Empty<ExprTableFullName>()
            )
            .Distinct();
    }

    /// <summary>Builds a direct parent-to-children lookup from table foreign-key metadata.</summary>
    /// <param name="allTables">The table descriptors to inspect as possible children.</param>
    /// <returns>
    /// A dictionary whose keys are referenced parent table names and whose values are the names of input tables that
    /// directly reference each parent. Tables without children are not included as keys.
    /// </returns>
    /// <remarks>
    /// The lookup is not recursive and does not require referenced parents to appear in <paramref name="allTables"/>.
    /// Child order follows input order.
    /// </remarks>
    /// <example>
    /// <code>
    /// var childrenByParent = allTables.BuildHierarchy();
    /// if (childrenByParent.TryGetValue(company.FullName.AsExprTableFullName(), out var childNames))
    /// {
    ///     // childNames contains tables with foreign keys to company.
    /// }
    /// </code>
    /// </example>
    public static Dictionary<ExprTableFullName, List<ExprTableFullName>> BuildHierarchy(this IReadOnlyList<TableBase> allTables)
    {
        Dictionary<ExprTableFullName, List<ExprTableFullName>> result = new();

        foreach (var child in allTables)
        {
            foreach (var parentTable in child.GetParentTables())
            {
                if (!result.TryGetValue(parentTable, out var list))
                {
                    list = new List<ExprTableFullName>();
                    result.Add(parentTable, list);
                }
                list.Add(child.FullName.AsExprTableFullName());
            }
        }

        return result;
    }
}
