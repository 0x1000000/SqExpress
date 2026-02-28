using System.Data;

namespace SqExpress.SqlExport.Internal;

internal readonly struct DbParameterValue
{
    public DbParameterValue(object? value, DbType type)
    {
        this.Value = value;
        this.Type = type;
    }

    public object? Value { get; }
    public DbType Type { get; }
}

