namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlTranspilerOptions
    {
        public string NamespaceName { get; set; } = "SqExpress.SqlTranspiler.Generated";

        public string? DeclarationsNamespaceName { get; set; }

        public string ClassName { get; set; } = "TranspiledQuery";

        public string MethodName { get; set; } = "Build";

        public string QueryVariableName { get; set; } = "query";

        public string TableDescriptorClassPrefix { get; set; } = "Table";

        public string TableDescriptorClassSuffix { get; set; } = string.Empty;

        public string DefaultSchemaName { get; set; } = "dbo";

        public bool UseStaticSqQueryBuilderUsing { get; set; } = true;

        internal string EffectiveDeclarationsNamespaceName =>
            string.IsNullOrWhiteSpace(this.DeclarationsNamespaceName)
                ? this.NamespaceName + ".Declarations"
                : this.DeclarationsNamespaceName!;

        internal string? EffectiveDefaultSchemaName =>
            string.IsNullOrWhiteSpace(this.DefaultSchemaName)
                ? null
                : this.DefaultSchemaName;
    }
}
