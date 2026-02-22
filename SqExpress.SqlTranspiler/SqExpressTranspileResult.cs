using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressTranspileResult
    {
        public SqExpressTranspileResult(
            string statementKind,
            CompilationUnitSyntax queryAst,
            CompilationUnitSyntax declarationsAst)
        {
            this.StatementKind = statementKind;
            this.QueryAst = queryAst;
            this.DeclarationsAst = declarationsAst;
            this.QueryCSharpCode = queryAst.ToFullString();
            this.DeclarationsCSharpCode = declarationsAst.ToFullString();
        }

        public string StatementKind { get; }

        public CompilationUnitSyntax QueryAst { get; }

        public CompilationUnitSyntax DeclarationsAst { get; }

        public string QueryCSharpCode { get; }

        public string DeclarationsCSharpCode { get; }

        //Backwards compatibility
        public CompilationUnitSyntax Ast => this.QueryAst;

        //Backwards compatibility
        public string CSharpCode => this.QueryCSharpCode;
    }
}
