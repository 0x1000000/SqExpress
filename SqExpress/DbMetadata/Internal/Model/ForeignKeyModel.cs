namespace SqExpress.DbMetadata.Internal.Model;

internal readonly struct ForeignKeyModel
{
    public ForeignKeyModel(ColumnRef column, ForeignKeyDeleteAction onDelete)
    {
        this.Column = column;
        this.OnDelete = onDelete;
    }

    public ColumnRef Column { get; }

    public ForeignKeyDeleteAction OnDelete { get; }
}
