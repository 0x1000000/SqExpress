using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;

namespace SqExpress.StatementSyntax
{
    public class StatementIf : IStatement
    {
        public StatementIf(ExprBoolean condition, StatementList statements, StatementList? elseStatements)
        {
            this.Condition = condition;
            this.Statements = statements;
            this.ElseStatements = elseStatements;
        }

        public ExprBoolean Condition { get; } 

        public StatementList Statements { get; }

        public StatementList? ElseStatements { get; }

        public void Accept(IStatementVisitor visitor)
            => visitor.VisitIf(this);
    }

    public abstract class StatementIfExists : IStatement
    {
        protected StatementIfExists(StatementList statements, StatementList? elseStatements)
        {
            this.Statements = statements;
            this.ElseStatements = elseStatements;
        }

        public StatementList Statements { get; }

        public StatementList? ElseStatements { get; }

        public abstract void Accept(IStatementVisitor visitor);
    }

    public class StatementIfTableExists : StatementIfExists
    {
        public StatementIfTableExists(ExprTableFullName exprTable, StatementList statements, StatementList? elseStatements) 
            : base(statements, elseStatements)
        {
            this.ExprTable = exprTable;
        }

        public ExprTableFullName ExprTable { get; }

        public override void Accept(IStatementVisitor visitor) => visitor.VisitIfTableExists(this);
    }

    public class StatementIfTempTableExists : StatementIfExists
    {
        public StatementIfTempTableExists(ExprTempTableName table, StatementList statements, StatementList? elseStatements) 
            : base(statements, elseStatements)
        {
            this.Table = table;
        }

        public ExprTempTableName Table { get; }

        public override void Accept(IStatementVisitor visitor) => visitor.VisitIfTempTableExists(this);
    }
}