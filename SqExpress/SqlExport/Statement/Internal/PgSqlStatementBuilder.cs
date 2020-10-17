using System;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal class PgSqlStatementBuilder : SqlStatementBuilderBase, IStatementVisitor
    {
        private readonly PgSqlBuilder _exprBuilder;

        public PgSqlStatementBuilder(SqlBuilderOptions? options) : base(options)
        {
            this._exprBuilder = new PgSqlBuilder(this.Options, this.Builder);
        }

        public string Build() => this.Builder.ToString();

        public void VisitCreateTable(StatementCreateTable statementCreateTable)
        {
            this.AppendTable(statementCreateTable);
        }

        protected override void AppendColumn(TableColumn column)
        {
            column.ColumnName.Accept(this.ExprBuilder, null);
            this.Builder.Append(' ');


            column.SqlType.Accept(this.ExprBuilder, null);

            if (!column.IsNullable)
            {
                this.Builder.Append(" NOT NULL");
            }

            if (column.ColumnMeta != null)
            {
                if (column.ColumnMeta.IsIdentity)
                {
                    this.Builder.Append("  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 )");
                }
            }
        }

        protected override void AppendTempKeyword(IExprTableFullName tableName)
        {
            if (tableName is ExprTempTableName)
            {
                this.Builder.Append("TEMP ");
            }
        }

        public void VisitDropTable(StatementDropTable statementDropTable)
        {
            this.Builder.Append("DROP TABLE ");
            if (statementDropTable.IfExists)
            {
                this.Builder.Append("IF EXISTS ");
            }
            statementDropTable.Table.Accept(this.ExprBuilder, null);
            this.Builder.Append(';');
        }

        public void VisitIf(StatementIf statementIf)
        {
            throw new NotSupportedException("Not supported");
        }

        public void VisitStatementList(StatementList statementList)
        {
            statementList.Statements.AssertNotEmpty("Statement list cannot be empty");
            foreach (var s in statementList.Statements)
            {
                s.Accept(this);
            }
        }

        public void VisitIfTableExists(StatementIfTableExists statementIfExists)
        {
            throw new NotSupportedException("Not supported");
        }

        public void VisitIfTempTableExists(StatementIfTempTableExists statementIfTempTableExists)
        {
            throw new NotSupportedException("Not supported");
        }

        protected override SqlBuilderBase ExprBuilder => this._exprBuilder;
    }
}