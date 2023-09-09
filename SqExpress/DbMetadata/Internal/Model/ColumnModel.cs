using System.Collections.Generic;

namespace SqExpress.DbMetadata.Internal.Model
{
    internal class ColumnModel
    {
        public ColumnModel(string name, ColumnRef dbName, int ordinalPosition, ColumnType columnType, PkInfo? pk, bool identity, DefaultValue? defaultValue, List<ColumnRef>? fk)
        {
            Name = name;
            DbName = dbName;
            OrdinalPosition = ordinalPosition;
            ColumnType = columnType;
            Pk = pk;
            Identity = identity;
            DefaultValue = defaultValue;
            Fk = fk;
        }

        public string Name { get; }
        public ColumnRef DbName { get; }
        public int OrdinalPosition { get; }
        public ColumnType ColumnType { get; }
        public PkInfo? Pk { get; }
        public bool Identity { get; }
        public DefaultValue? DefaultValue { get; }
        public List<ColumnRef>? Fk { get; }

        public ColumnModel WithName(string newName) =>
            new ColumnModel(
                name: newName,
                dbName: DbName,
                ordinalPosition: OrdinalPosition,
                columnType: ColumnType,
                pk: Pk,
                identity: Identity,
                defaultValue: DefaultValue,
                fk: Fk);
    }

    internal readonly struct PkInfo
    {
        public PkInfo(int index, bool descending)
        {
            Index = index;
            Descending = descending;
        }

        public readonly int Index;
        public readonly bool Descending;
    }

    internal readonly struct DefaultValue
    {
        public DefaultValue(DefaultValueType type, string? rawValue)
        {
            Type = type;
            RawValue = rawValue;
        }

        public readonly DefaultValueType Type;
        public readonly string? RawValue;
    }

    internal enum DefaultValueType
    {
        Raw,
        Null,
        Integer,
        String,
        GetUtcDate
    }
}