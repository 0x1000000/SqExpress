namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlTranspilerOptions
    {
        public string NamespaceName { get; set; } = "SqExpress.SqlTranspiler.Generated";

        public string? DeclarationsNamespaceName { get; set; }

        public string ClassName { get; set; } = "TranspiledQuery";

        public string MethodName { get; set; } = "Build";

        public string QueryVariableName { get; set; } = "query";

        internal string EffectiveDeclarationsNamespaceName =>
            string.IsNullOrWhiteSpace(this.DeclarationsNamespaceName)
                ? this.NamespaceName + ".Declarations"
                : this.DeclarationsNamespaceName!;
    }
}
