namespace SqExpress.StatementSyntax
{
    public interface IStatement
    {
        void Accept(IStatementVisitor visitor);
    }
}