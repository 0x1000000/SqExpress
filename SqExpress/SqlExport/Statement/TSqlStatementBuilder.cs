using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;

namespace SqExpress.SqlExport.Statement
{
    public class TSqlStatementBuilder : SqlStatementBuilderBase, IStatementVisitor
    {
        private readonly TSqlBuilder _exprBuilder;

        public TSqlStatementBuilder(SqlBuilderOptions? options) : base(options)
        {
            this._exprBuilder = new TSqlBuilder(this.Options, this.Builder);
        }

        public void VisitCreateTable(StatementCreateTable statementCreateTable)
        {
            this.AppendTable(statementCreateTable);
        }

        protected override void AppendColumn(TableColumn column)
        {
            column.ColumnName.Accept(this.ExprBuilder);
            this.Builder.Append(' ');

            column.SqlType.Accept(this.ExprBuilder);

            if (!column.IsNullable)
            {
                this.Builder.Append(" NOT NULL");
            }

            if (column.ColumnMeta != null)
            {
                if (column.ColumnMeta.IsIdentity)
                {
                    this.Builder.Append("  IDENTITY (1, 1)");
                }
            }
        }

        public void VisitDropTable(StatementDropTable statementDropTable)
        {
            if (!statementDropTable.IfExists)
            {
                this.Builder.Append("DROP TABLE ");
                statementDropTable.Table.FullName.Accept(this.ExprBuilder);
            }
            else
            {
                new StatementIfTableExists(statementDropTable.Table, StatementList.Combine(new StatementDropTable(statementDropTable.Table, false)), null).Accept(this);
            }
        }

        public void VisitIf(StatementIf statementIf)
        {
            this.Builder.Append("IF ");
            statementIf.Condition.Accept(this.ExprBuilder);

            bool ifBlock = statementIf.Statements.Count() > 1;
            if (ifBlock)
            {
                this.Builder.Append("BEGIN");
            }
            this.Builder.Append(' ');
            statementIf.Statements.Accept(this);
            if (ifBlock)
            {
                this.Builder.Append(" END");
            }

            if (statementIf.ElseStatements != null)
            {
                this.Builder.Append(" ELSE ");
                ifBlock = statementIf.ElseStatements.Count() > 1;
                if (ifBlock)
                {
                    this.Builder.Append("BEGIN ");
                }
                statementIf.ElseStatements.Accept(this);
                this.Builder.Append(" END");
            }
        }

        public void VisitStatementList(StatementList statementList)
        {
            for (int i = 0; i < statementList.Statements.Count; i++)
            {
                statementList.Statements[i].Accept(this);
            }
        }

        public void VisitIfTableExists(StatementIfTableExists statementIfExists)
        {
            var tbl = new InformationSchemaTables(Alias.Empty);
            var test = SqQueryBuilder.SelectTopOne()
                .From(tbl)
                .Where(tbl.TableSchema == this.Options.MapSchema(statementIfExists.Table.FullName.Schema.Name) &
                       tbl.TableName == statementIfExists.Table.FullName.TableName.Name);

            new StatementIf(SqQueryBuilder.Exists(test), statementIfExists.Statements, statementIfExists.ElseStatements).Accept(this);
        }

        public string Build() => this.Builder.ToString();

        protected override SqlBuilderBase ExprBuilder => this._exprBuilder;
    }
}