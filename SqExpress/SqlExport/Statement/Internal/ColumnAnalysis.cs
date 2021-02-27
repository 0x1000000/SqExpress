using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal readonly struct ColumnAnalysis
    {
        public readonly List<ExprColumnName> Pk;

        public readonly Dictionary<IExprTableFullName, List<ColumnRelationship>> Fks;

        public static ColumnAnalysis Build() => new ColumnAnalysis(new List<ExprColumnName>(4), new Dictionary<IExprTableFullName, List<ColumnRelationship>>(4));

        private ColumnAnalysis(List<ExprColumnName> pk, Dictionary<IExprTableFullName, List<ColumnRelationship>> fks)
        {
            this.Pk = pk;
            this.Fks = fks;
        }

        public void Analyze(TableColumn column)
        {
            if (column.ColumnMeta != null)
            {
                if (column.ColumnMeta.IsPrimaryKey)
                {
                    this.Pk.Add(column);
                }

                var foreignKeyColumn = column.ColumnMeta.ForeignKeyColumn;

                if (!ReferenceEquals(foreignKeyColumn, null))
                {
                    var foreignTable = foreignKeyColumn.Table.FullName;

                    if (!this.Fks.ContainsKey(foreignTable))
                    {
                        this.Fks.Add(foreignTable, new List<ColumnRelationship>(4));
                    }
                    this.Fks[foreignTable].Add(new ColumnRelationship(@internal: column.ColumnName, external: foreignKeyColumn.ColumnName));
                }
            }
        }

        public readonly struct ColumnRelationship
        {
            public readonly ExprColumnName Internal;
            public readonly ExprColumnName External;

            public ColumnRelationship(ExprColumnName @internal, ExprColumnName external)
            {
                this.Internal = @internal;
                this.External = external;
            }
        }
    }
}