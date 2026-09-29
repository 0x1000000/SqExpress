using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal;

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

            if (column.ColumnMeta.ForeignKeys != null)
            {
                foreach (var foreignKey in column.ColumnMeta.ForeignKeys)
                {
                    var foreignKeyColumn = foreignKey.ReferencedColumn;
                    var foreignTable = foreignKeyColumn.Table.FullName;

                    if (!this.Fks.ContainsKey(foreignTable))
                    {
                        this.Fks.Add(foreignTable, new List<ColumnRelationship>(4));
                    }
                    var relationships = this.Fks[foreignTable];
                    if (relationships.Count > 0 && relationships[0].OnDelete != foreignKey.OnDelete)
                    {
                        throw new SqExpressException(
                            $"Conflicting ON DELETE actions for columns {relationships[0].Internal.Name} and {column.ColumnName.Name} in foreign key from {column.Table.FullName.TableName} to {foreignTable.TableName}");
                    }
                    relationships.Add(new ColumnRelationship(@internal: column.ColumnName, external: foreignKeyColumn.ColumnName, foreignKey.OnDelete));
                }
            }
        }
    }

    public readonly struct ColumnRelationship
    {
        public readonly ExprColumnName Internal;
        public readonly ExprColumnName External;
        public readonly ForeignKeyDeleteAction OnDelete;

        public ColumnRelationship(ExprColumnName @internal, ExprColumnName external, ForeignKeyDeleteAction onDelete)
        {
            this.Internal = @internal;
            this.External = external;
            this.OnDelete = onDelete;
        }
    }
}
