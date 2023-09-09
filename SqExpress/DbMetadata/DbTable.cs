using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.DbMetadata;

internal sealed class DbTable : TableBase
{
    internal DbTable(string? schema, string name, IEnumerable<TableColumn> columns, IEnumerable<IndexMeta> indexes, Alias alias = default) : base(schema, name, alias)
    {
        this.AddColumns(columns);
        this.AddIndexes(indexes);
    }
}