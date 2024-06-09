using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.Syntax.Names;

namespace SqExpress;

public static class TableHierarchyExtensions {

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
