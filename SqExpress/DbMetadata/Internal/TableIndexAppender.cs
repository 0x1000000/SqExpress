using SqExpress.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SqExpress.DbMetadata.Internal;

internal class TableIndexAppender : ITableIndexAppender
{
    private readonly List<IndexMeta> _trace = new List<IndexMeta>();

    public TableIndexAppender(IReadOnlyList<TableColumn> columns)
    {
        this.Columns = columns;
    }

    private ITableIndexAppender Append(IndexMeta index)
    {
        this._trace.Add(index);
        return this;
    }

    public IndexMeta CreateIndex(params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, false);
    }

    public IndexMeta CreateIndex(string name, params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, false);
    }

    public IndexMeta CreateUniqueIndex(params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, false);
    }

    public IndexMeta CreateUniqueIndex(string name, params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, true, false);
    }

    public IndexMeta CreateClusteredIndex(params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, false, true);
    }

    public IndexMeta CreateClusteredIndex(string name, params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, false, true);
    }

    public IndexMeta CreateUniqueClusteredIndex(params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), null, true, true);
    }

    public IndexMeta CreateUniqueClusteredIndex(string name, params IndexMetaColumn[] columns)
    {
        return new IndexMeta(AssertIndexColumnsNotEmpty(columns), name, true, true);
    }

    public IReadOnlyList<TableColumn> Columns { get; }

    public IndexMetaColumn Asc(string columnName)
    {
        return new IndexMetaColumn(this.FindColumnByName(columnName), false);
    }

    public IndexMetaColumn Desc(string columnName)
    {
        return new IndexMetaColumn(this.FindColumnByName(columnName), true);
    }

    public ITableIndexAppender AppendIndexes(IEnumerable<IndexMeta> indexes)
    {
        this._trace.AddRange(indexes);
        return this;
    }

    public ITableIndexAppender AppendIndex(params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateIndex(columns));
    }

    public ITableIndexAppender AppendIndex(string name, params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateIndex(name, columns));
    }

    public ITableIndexAppender AddUniqueIndex(params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateUniqueIndex(columns));
    }

    public ITableIndexAppender AddUniqueIndex(string name, params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateUniqueIndex(name, columns));
    }

    public ITableIndexAppender AddClusteredIndex(params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateClusteredIndex(columns));
    }

    public ITableIndexAppender AddClusteredIndex(string name, params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateClusteredIndex(name, columns));
    }

    public ITableIndexAppender AddUniqueClusteredIndex(params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateUniqueClusteredIndex(columns));
    }

    public ITableIndexAppender AddUniqueClusteredIndex(string name, params IndexMetaColumn[] columns)
    {
        return this.Append(this.CreateUniqueClusteredIndex(name, columns));
    }

    public IEnumerator<IndexMeta> GetEnumerator()
    {
        return this._trace.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    private TableColumn FindColumnByName(string columnName)
    {
        var c = this.Columns.FirstOrDefault(c => c.ColumnName.Name == columnName);
        if (ReferenceEquals(c, null))
        {
            throw new SqExpressException($"Could not find a column with name \"{columnName}\".");
        }

        return c;
    }

    private static IndexMetaColumn[] AssertIndexColumnsNotEmpty(IndexMetaColumn[] columns)
    {
        columns.AssertNotEmpty("Table index has to contain at least one column");
        return columns;
    }
}
