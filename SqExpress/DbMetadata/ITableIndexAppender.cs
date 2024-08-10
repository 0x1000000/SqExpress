using System.Collections.Generic;

namespace SqExpress.DbMetadata;

public interface ITableIndexAppender : ITableIndexFactory, IEnumerable<IndexMeta>
{
    IReadOnlyList<TableColumn> Columns { get; }

    IndexMetaColumn Asc(string columnName);
    IndexMetaColumn Desc(string columnName);

    ITableIndexAppender AppendIndexes(IEnumerable<IndexMeta> indexes);

    ITableIndexAppender AppendIndex(params IndexMetaColumn[] columns);
    ITableIndexAppender AppendIndex(string name, params IndexMetaColumn[] columns);

    ITableIndexAppender AddUniqueIndex(params IndexMetaColumn[] columns);
    ITableIndexAppender AddUniqueIndex(string name, params IndexMetaColumn[] columns);

    ITableIndexAppender AddClusteredIndex(params IndexMetaColumn[] columns);
    ITableIndexAppender AddClusteredIndex(string name, params IndexMetaColumn[] columns);

    ITableIndexAppender AddUniqueClusteredIndex(params IndexMetaColumn[] columns);
    ITableIndexAppender AddUniqueClusteredIndex(string name, params IndexMetaColumn[] columns);
}


public interface ITableIndexFactory
{
    IndexMeta CreateIndex(params IndexMetaColumn[] columns);
    IndexMeta CreateIndex(string name, params IndexMetaColumn[] columns);

    IndexMeta CreateUniqueIndex(params IndexMetaColumn[] columns);
    IndexMeta CreateUniqueIndex(string name, params IndexMetaColumn[] columns);

    IndexMeta CreateClusteredIndex(params IndexMetaColumn[] columns);
    IndexMeta CreateClusteredIndex(string name, params IndexMetaColumn[] columns);

    IndexMeta CreateUniqueClusteredIndex(params IndexMetaColumn[] columns);
    IndexMeta CreateUniqueClusteredIndex(string name, params IndexMetaColumn[] columns);
}