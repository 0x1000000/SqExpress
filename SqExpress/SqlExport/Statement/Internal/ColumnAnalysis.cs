using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal readonly struct ColumnAnalysis
    {
        public readonly List<ExprColumnName> Pk;

        public readonly Dictionary<ExprTableFullName, List<(ExprColumnName Internal, ExprColumnName External)>> Fks;

        public static ColumnAnalysis Build() => new ColumnAnalysis(new List<ExprColumnName>(4), new Dictionary<ExprTableFullName, List<(ExprColumnName Internal, ExprColumnName External)>>(4));

        private ColumnAnalysis(List<ExprColumnName> pk, Dictionary<ExprTableFullName, List<(ExprColumnName Internal, ExprColumnName External)>> fks)
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
                        this.Fks.Add(foreignTable, new List<(ExprColumnName Internal, ExprColumnName External)>(4));
                    }
                    this.Fks[foreignTable].Add((Internal: column.ColumnName, External: foreignKeyColumn.ColumnName));
                }
            }
        }
    }
}