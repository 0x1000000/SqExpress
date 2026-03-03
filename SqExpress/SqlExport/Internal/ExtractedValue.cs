using System.Data;

namespace SqExpress.SqlExport.Internal;

internal readonly struct DbParameterValue
{
    public DbParameterValue(object? value, DbType type, string? name)
    {
        this.Value = value;
        this.Type = type;
        this.Name = name;
    }

    public object? Value { get; }

    public DbType Type { get; }

    public string? Name { get; }
}

