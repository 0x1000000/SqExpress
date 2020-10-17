using SqExpress.Syntax.Names;

namespace SqExpress.StatementSyntax
{
    public class StatementDropTable : IStatement
    {
        public StatementDropTable(IExprTableFullName table, bool ifExists)
        {
            this.Table = table;
            this.IfExists = ifExists;
        }

        public bool IfExists { get; }

        public IExprTableFullName Table { get; }

        public void Accept(IStatementVisitor visitor)
            => visitor.VisitDropTable(this);
    }
}