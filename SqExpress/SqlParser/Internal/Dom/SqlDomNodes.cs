using System.Collections.Generic;

namespace SqExpress.SqlParser.Internal.Dom
{
    internal abstract class TSqlStatement
    {
    }

    internal enum SqlDomStatementKind
    {
        Unknown,
        Select,
        Insert,
        Update,
        Delete,
        Merge,
    }

    internal sealed class SqlDomStatement : TSqlStatement
    {
        public SqlDomStatement(
            SqlDomStatementKind kind,
            string rawSql,
            string normalizedSql,
            SqlDomWithClause? withClause,
            SqlDomSelectClause? topLevelSelect,
            IReadOnlyList<SqlDomTableReference> tableReferences,
            IReadOnlyList<SqlDomColumnReference> columnReferences)
        {
            this.Kind = kind;
            this.RawSql = rawSql;
            this.NormalizedSql = normalizedSql;
            this.WithClause = withClause;
            this.TopLevelSelect = topLevelSelect;
            this.TableReferences = tableReferences;
            this.ColumnReferences = columnReferences;
        }

        public SqlDomStatementKind Kind { get; }

        public string RawSql { get; }

        public string NormalizedSql { get; }

        public SqlDomWithClause? WithClause { get; }

        public SqlDomSelectClause? TopLevelSelect { get; }

        public IReadOnlyList<SqlDomTableReference> TableReferences { get; }

        public IReadOnlyList<SqlDomColumnReference> ColumnReferences { get; }
    }

    internal sealed class SqlDomWithClause
    {
        public SqlDomWithClause(IReadOnlyList<SqlDomCte> ctes)
        {
            this.Ctes = ctes;
        }

        public IReadOnlyList<SqlDomCte> Ctes { get; }
    }

    internal sealed class SqlDomCte
    {
        public SqlDomCte(string name, string querySql)
        {
            this.Name = name;
            this.QuerySql = querySql;
        }

        public string Name { get; }

        public string QuerySql { get; }
    }

    internal sealed class SqlDomSelectClause
    {
        public SqlDomSelectClause(
            IReadOnlyList<SqlDomSelectItem> items,
            SqlDomTableSource? from,
            string? whereSql,
            string? groupBySql,
            string? havingSql,
            string? orderBySql,
            string? offsetFetchSql,
            bool isDistinct,
            string? topSql,
            bool hasSetOperation)
        {
            this.Items = items;
            this.From = from;
            this.WhereSql = whereSql;
            this.GroupBySql = groupBySql;
            this.HavingSql = havingSql;
            this.OrderBySql = orderBySql;
            this.OffsetFetchSql = offsetFetchSql;
            this.IsDistinct = isDistinct;
            this.TopSql = topSql;
            this.HasSetOperation = hasSetOperation;
        }

        public IReadOnlyList<SqlDomSelectItem> Items { get; }

        public SqlDomTableSource? From { get; }

        public string? WhereSql { get; }

        public string? GroupBySql { get; }

        public string? HavingSql { get; }

        public string? OrderBySql { get; }

        public string? OffsetFetchSql { get; }

        public bool IsDistinct { get; }

        public string? TopSql { get; }

        public bool HasSetOperation { get; }
    }

    internal sealed class SqlDomSelectItem
    {
        public SqlDomSelectItem(string sql, string? alias)
        {
            this.Sql = sql;
            this.Alias = alias;
        }

        public string Sql { get; }

        public string? Alias { get; }
    }

    internal abstract class SqlDomTableSource
    {
    }

    internal sealed class SqlDomNamedTableSource : SqlDomTableSource
    {
        public SqlDomNamedTableSource(string? schema, string table, string? alias)
        {
            this.Schema = schema;
            this.Table = table;
            this.Alias = alias;
        }

        public string? Schema { get; }

        public string Table { get; }

        public string? Alias { get; }
    }

    internal sealed class SqlDomDerivedTableSource : SqlDomTableSource
    {
        public SqlDomDerivedTableSource(string sql, string? alias)
        {
            this.Sql = sql;
            this.Alias = alias;
        }

        public string Sql { get; }

        public string? Alias { get; }
    }

    internal sealed class SqlDomValuesTableSource : SqlDomTableSource
    {
        public SqlDomValuesTableSource(string sql, string? alias, IReadOnlyList<string> columnAliases)
        {
            this.Sql = sql;
            this.Alias = alias;
            this.ColumnAliases = columnAliases;
        }

        public string Sql { get; }

        public string? Alias { get; }

        public IReadOnlyList<string> ColumnAliases { get; }
    }

    internal sealed class SqlDomFunctionTableSource : SqlDomTableSource
    {
        public SqlDomFunctionTableSource(string name, string argumentsSql, string? alias)
        {
            this.Name = name;
            this.ArgumentsSql = argumentsSql;
            this.Alias = alias;
        }

        public string Name { get; }

        public string ArgumentsSql { get; }

        public string? Alias { get; }
    }

    internal enum SqlDomJoinType
    {
        Inner,
        Left,
        Right,
        Full,
        Cross,
        CrossApply,
        OuterApply,
    }

    internal sealed class SqlDomJoinedTableSource : SqlDomTableSource
    {
        public SqlDomJoinedTableSource(SqlDomTableSource left, SqlDomTableSource right, SqlDomJoinType joinType, string? onSql)
        {
            this.Left = left;
            this.Right = right;
            this.JoinType = joinType;
            this.OnSql = onSql;
        }

        public SqlDomTableSource Left { get; }

        public SqlDomTableSource Right { get; }

        public SqlDomJoinType JoinType { get; }

        public string? OnSql { get; }
    }

    internal sealed class SqlDomTableReference
    {
        public SqlDomTableReference(string? schema, string table, string? alias)
        {
            this.Schema = schema;
            this.Table = table;
            this.Alias = alias;
        }

        public string? Schema { get; }

        public string Table { get; }

        public string? Alias { get; }
    }

    internal sealed class SqlDomColumnReference
    {
        public SqlDomColumnReference(string sourceAlias, string columnName)
        {
            this.SourceAlias = sourceAlias;
            this.ColumnName = columnName;
        }

        public string SourceAlias { get; }

        public string ColumnName { get; }
    }
}
