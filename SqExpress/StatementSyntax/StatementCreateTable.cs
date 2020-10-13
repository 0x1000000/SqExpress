namespace SqExpress.StatementSyntax
{
    public class StatementCreateTable : IStatement
    {
        public StatementCreateTable(TableBase table)
        {
            this.Table = table;
        }

        public TableBase Table { get; }

        public void Accept(IStatementVisitor visitor)
            => visitor.VisitCreateTable(this);
    }
}