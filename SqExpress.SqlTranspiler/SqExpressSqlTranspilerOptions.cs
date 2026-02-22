namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlTranspilerOptions
    {
        public string NamespaceName { get; set; } = "SqExpress.SqlTranspiler.Generated";

        public string ClassName { get; set; } = "TranspiledQuery";

        public string MethodName { get; set; } = "Build";

        public string QueryVariableName { get; set; } = "query";
    }
}
