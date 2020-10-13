using System.Collections.Generic;

namespace SqExpress.StatementSyntax
{
    public class StatementList : IStatement
    {
        public StatementList(IReadOnlyList<IStatement> statements)
        {
            this.Statements = statements;
        }

        public IReadOnlyList<IStatement> Statements { get; }

        public void Accept(IStatementVisitor visitor) => visitor.VisitStatementList(this);

        public int Count()
        {
            int counter = 0;
            foreach (var statement in this.Statements)
            {
                if (statement is StatementList sl)
                {
                    counter += sl.Count();
                }
                else
                {
                    counter++;
                }
            }
            return counter;
        }

        public static StatementList Combine(IStatement statement, params IStatement[] statements)
        {
            List<IStatement> result = new List<IStatement>((statements.Length + 1) * 2);
            if (statement is StatementList sli)
            {
                result.AddRange(sli.Statements);
            }
            else
            {
                result.Add(statement);
            }

            foreach (var statementI in statements)
            {
                if (statementI is StatementList sl)
                {
                    result.AddRange(sl.Statements);
                }
                else
                {
                    result.Add(statementI);
                }
            }
            return new StatementList(result);
        }

        public static StatementList Combine(IReadOnlyList<IStatement> statements)
        {
            List<IStatement> result = new List<IStatement>((statements.Count + 1) * 2);

            foreach (var statementI in statements)
            {
                if (statementI is StatementList sl)
                {
                    result.AddRange(sl.Statements);
                }
                else
                {
                    result.Add(statementI);
                }
            }
            return new StatementList(result);
        }
    }
}