namespace SqExpress.CodeGenUtil.Model
{
    internal record TableColumnRawModel
    {
        public TableColumnRawModel(ColumnRef dbName, bool identity, bool nullable, string typeName, string? defaultValue, int? size, int? precision, int? scale)
        {
            this.DbName = dbName;
            this.Identity = identity;
            this.Nullable = nullable;
            this.TypeName = typeName;
            this.DefaultValue = defaultValue;
            this.Size = size;
            this.Precision = precision;
            this.Scale = scale;
        }

        public ColumnRef DbName { get; }
        public bool Identity { get; }
        public bool Nullable { get; }
        public string TypeName { get; }
        public string? DefaultValue { get; }
        public int? Size { get; }
        public int? Precision { get; }
        public int? Scale { get; }
    }
}