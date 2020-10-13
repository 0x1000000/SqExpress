using SqExpress.Syntax.Names;

namespace SqExpress.StatementSyntax
{
    public class StatementDropTable : IStatement
    {
        public StatementDropTable(ExprTable table, bool ifExists)
        {
            this.Table = table;
            this.IfExists = ifExists;
        }

        public bool IfExists { get; }

        public ExprTable Table { get; }

        public void Accept(IStatementVisitor visitor)
            => visitor.VisitDropTable(this);
    }
}