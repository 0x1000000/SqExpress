namespace SqExpress.DbMetadata.Internal.Model
{
    internal class ColumnRawModel
    {
        public ColumnRawModel(ColumnRef dbName, int ordinalPosition, bool identity, bool nullable, string typeName, string? defaultValue, int? size, int? precision, int? scale,
            object? extra)
        {
            this.DbName = dbName;
            this.OrdinalPosition = ordinalPosition;
            this.Identity = identity;
            this.Nullable = nullable;
            this.TypeName = typeName;
            this.DefaultValue = defaultValue;
            this.Size = size;
            this.Precision = precision;
            this.Scale = scale;
            this.Extra = extra;
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
        public object? Extra { get; }
    }
}