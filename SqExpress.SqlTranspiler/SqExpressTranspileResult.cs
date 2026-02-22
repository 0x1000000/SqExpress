using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressTranspileResult
    {
        public SqExpressTranspileResult(string statementKind, CompilationUnitSyntax ast, string cSharpCode)
        {
            this.StatementKind = statementKind;
            this.Ast = ast;
            this.CSharpCode = cSharpCode;
        }

        public string StatementKind { get; }

        public CompilationUnitSyntax Ast { get; }

        public string CSharpCode { get; }
    }
}
