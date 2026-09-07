using System;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal class MySqlStatementBuilder : SqlStatementBuilderBase
    {
        private readonly MySqlBuilder _exprBuilder;

        public MySqlStatementBuilder(SqlBuilderOptions? options, MySqlFlavor flavor) : base(options)
        {
            this._exprBuilder = new MySqlBuilder(this.Options, flavor, this.FormattingWriter);
        }

        internal MySqlStatementBuilder(
            SqlBuilderOptions? options,
            MySqlFlavor flavor,
            SqlFormattingWriter formattingWriter)
            : base(options, formattingWriter)
        {
            this._exprBuilder = new MySqlBuilder(this.Options, flavor, formattingWriter);
        }

        public string Build() => this.FormattingWriter.ToString();

        protected override void AppendColumn(TableColumn column)
        {
            column.ColumnName.Accept(this.ExprBuilder, null);
            this.FormattingWriter.Append(' ');

            column.SqlType.Accept(this.ExprBuilder, null);

            if (!column.IsNullable)
            {
                this.FormattingWriter.Append(" NOT NULL");
            }

            if (column.ColumnMeta != null)
            {
                if (column.ColumnMeta.IsIdentity)
                {
                    this.FormattingWriter.Append(" AUTO_INCREMENT");
                }

                if (!ReferenceEquals(column.ColumnMeta.ColumnDefaultValue, null))
                {
                    this.FormattingWriter.Append(" DEFAULT (");
                    column.ColumnMeta.ColumnDefaultValue.Accept(this.ExprBuilder, null);
                    this.FormattingWriter.Append(')');
                }
            }
        }

        protected override void AppendTempKeyword(IExprTableFullName tableName)
        {
            if (tableName is ExprTempTableName)
            {
                this.FormattingWriter.Append("TEMPORARY ");
            }
        }

        protected override void AppendIndexesInside(TableBase table)
        {
            foreach (var tableIndex in table.Indexes)
            {
                if (!tableIndex.Unique)
                {
                    this.FormattingWriter.Append(",INDEX ");
                }
                else
                {
                    this.FormattingWriter.Append(",UNIQUE KEY ");
                }

                this.AppendName(this.BuildIndexName(table.FullName, tableIndex));

                this.AppendIndexColumnList(tableIndex: tableIndex);
            }
        }

        protected override void AppendIndexesOutside(TableBase table)
        {
            //All indexes are created inside CREATE TABLE
        }

        protected override bool IsNamedPk() => false;

        public override void VisitCreateTable(StatementCreateTable statementCreateTable)
        {
            this.AppendTable(statementCreateTable);
        }

        public override void VisitDropTable(StatementDropTable statementDropTable)
        {
            this.FormattingWriter.Append("DROP TABLE ");
            if (statementDropTable.IfExists)
            {
                this.FormattingWriter.Append("IF EXISTS ");
            }
            statementDropTable.Table.Accept(this.ExprBuilder, null);
            this.FormattingWriter.Append(';');
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
