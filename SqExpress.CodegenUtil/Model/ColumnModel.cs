using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model
{
    internal class ColumnModel
    {
        public ColumnModel(string name, ColumnRef dbName, ColumnType columnType, PkInfo? pk, bool identity, DefaultValue? defaultValue, List<ColumnRef>? fk)
        {
            this.Name = name;
            this.DbName = dbName;
            this.ColumnType = columnType;
            this.Pk = pk;
            this.Identity = identity;
            this.DefaultValue = defaultValue;
            this.Fk = fk;
        }

        public string Name { get; }
        public ColumnRef DbName { get; }
        public ColumnType ColumnType { get; }
        public PkInfo? Pk { get; }
        public bool Identity { get; }
        public DefaultValue? DefaultValue { get; }
        public List<ColumnRef>? Fk { get; }

        public ColumnModel WithName(string newName) =>
            new ColumnModel(
                name: newName,
                dbName: this.DbName,
                columnType: this.ColumnType,
                pk: this.Pk,
                identity: this.Identity,
                defaultValue: this.DefaultValue,
                fk: this.Fk);
    }

    internal readonly struct PkInfo
    {
        public PkInfo(int index, bool descending)
        {
            this.Index = index;
            this.Descending = descending;
        }

        public readonly int Index;
        public readonly bool Descending;
    }

    internal readonly struct DefaultValue
    {
        public DefaultValue(DefaultValueType type, string? rawValue)
        {
            this.Type = type;
            this.RawValue = rawValue;
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