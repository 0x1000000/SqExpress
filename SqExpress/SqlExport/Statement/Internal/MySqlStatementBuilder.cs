using System;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal class MySqlStatementBuilder : SqlStatementBuilderBase
    {
        private readonly MySqlBuilder _exprBuilder;

        public MySqlStatementBuilder(SqlBuilderOptions? options) : base(options)
        {
            this._exprBuilder = new MySqlBuilder(this.Options, this.Builder);
        }

        public string Build() => this.Builder.ToString();

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
                    this.Builder.Append(" AUTO_INCREMENT");
                }

                if (!ReferenceEquals(column.ColumnMeta.ColumnDefaultValue, null))
                {
                    this.Builder.Append(" DEFAULT (");
                    column.ColumnMeta.ColumnDefaultValue.Accept(this.ExprBuilder, null);
                    this.Builder.Append(')');
                }
            }
        }

        protected override void AppendTempKeyword(IExprTableFullName tableName)
        {
            if (tableName is ExprTempTableName)
            {
                this.Builder.Append("TEMPORARY ");
            }
        }

        protected override void AppendIndexesInside(TableBase table)
        {
            foreach (var tableIndex in table.Indexes)
            {
                if (!tableIndex.Unique)
                {
                    this.Builder.Append(",INDEX ");
                }
                else
                {
                    this.Builder.Append(",UNIQUE KEY ");
                }

                this.AppendName(this.BuildIndexName(table.FullName, tableIndex));

                this.AppendIndexColumnList(tableIndex: tableIndex);
            }
        }

        protected override void AppendIndexesOutside(TableBase table)
        {
            //All indexes are created inside CREATE TABLE
        }

        public override void VisitCreateTable(StatementCreateTable statementCreateTable)
        {
            this.AppendTable(statementCreateTable);
        }

        public override void VisitDropTable(StatementDropTable statementDropTable)
        {
            this.Builder.Append("DROP TABLE ");
            if (statementDropTable.IfExists)
            {
                this.Builder.Append("IF EXISTS ");
            }
            statementDropTable.Table.Accept(this.ExprBuilder, null);
            this.Builder.Append(';');
        }

        public override void VisitIf(StatementIf statementIf)
        {
            throw new NotSupportedException("Not supported");
        }


        public override void VisitIfTableExists(StatementIfTableExists statementIfExists)
        {
            throw new NotSupportedException("Not supported");
        }

        public override void VisitIfTempTableExists(StatementIfTempTableExists statementIfTempTableExists)
        {
            throw new NotSupportedException("Not supported");
        }

        protected override SqlBuilderBase ExprBuilder => this._exprBuilder;
    }
}