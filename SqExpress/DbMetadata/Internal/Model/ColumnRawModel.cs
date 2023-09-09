namespace SqExpress.DbMetadata.Internal.Model
{
    internal class ColumnRawModel
    {
        public ColumnRawModel(ColumnRef dbName, int ordinalPosition, bool identity, bool nullable, string typeName, string? defaultValue, int? size, int? precision, int? scale)
        {
            DbName = dbName;
            OrdinalPosition = ordinalPosition;
            Identity = identity;
            Nullable = nullable;
            TypeName = typeName;
            DefaultValue = defaultValue;
            Size = size;
            Precision = precision;
            Scale = scale;
        }

        public ColumnRef DbName { get; }
        public int OrdinalPosition { get; }
        public bool Identity { get; }
        public bool Nullable { get; }
        public string TypeName { get; }
        public string? DefaultValue { get; }
        public int? Size { get; }
        public int? Precision { get; }
        public int? Scale { get; }
    }
}