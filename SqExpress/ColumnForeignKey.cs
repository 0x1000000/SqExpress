using System;

namespace SqExpress;

public sealed class ColumnForeignKey
{
    public TableColumn ReferencedColumn { get; }

    public ForeignKeyDeleteAction OnDelete { get; }

    public ColumnForeignKey(TableColumn referencedColumn, ForeignKeyDeleteAction onDelete)
    {
        this.ReferencedColumn = referencedColumn ?? throw new ArgumentNullException(nameof(referencedColumn));
        if (onDelete != ForeignKeyDeleteAction.NoAction && onDelete != ForeignKeyDeleteAction.Cascade)
        {
            throw new ArgumentOutOfRangeException(nameof(onDelete));
        }
        this.OnDelete = onDelete;
    }
}
