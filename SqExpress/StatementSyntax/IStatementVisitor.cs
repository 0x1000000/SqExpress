namespace SqExpress.StatementSyntax
{
    public interface IStatementVisitor
    {
        void VisitCreateTable(StatementCreateTable statementCreateTable);

        void VisitDropTable(StatementDropTable statementDropTable);

        void VisitIf(StatementIf statementIf);

        void VisitStatementList(StatementList statementList);

        void VisitIfTableExists(StatementIfTableExists statementIfExists);

        void VisitIfTempTableExists(StatementIfTempTableExists statementIfTempTableExists);
    }
}