using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressTranspileResult
    {
        public SqExpressTranspileResult(string statementKind, string queryCSharpCode, string declarationsCSharpCode)
        {
            this.StatementKind = statementKind;
            this.QueryCSharpCode = queryCSharpCode;
            this.DeclarationsCSharpCode = declarationsCSharpCode;
            this.QueryAst = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(queryCSharpCode).GetRoot();
            this.DeclarationsAst = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(declarationsCSharpCode).GetRoot();
        }

        public string StatementKind { get; }

        public string QueryCSharpCode { get; }

        public string DeclarationsCSharpCode { get; }

        public CompilationUnitSyntax QueryAst { get; }

        public CompilationUnitSyntax DeclarationsAst { get; }

        public CompilationUnitSyntax Ast => this.QueryAst;

        public string CSharpCode => this.QueryCSharpCode;
    }
}
