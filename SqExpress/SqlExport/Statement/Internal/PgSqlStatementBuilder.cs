using System;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal class PgSqlStatementBuilder : SqlStatementBuilderBase
    {
        private readonly PgSqlBuilder _exprBuilder;

        public PgSqlStatementBuilder(SqlBuilderOptions? options) : base(options)
        {
            this._exprBuilder = new PgSqlBuilder(this.Options, this.FormattingWriter);
        }

        internal PgSqlStatementBuilder(SqlBuilderOptions? options, SqlFormattingWriter formattingWriter)
            : base(options, formattingWriter)
        {
            this._exprBuilder = new PgSqlBuilder(this.Options, formattingWriter);
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
                    this.FormattingWriter.Append("  GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 )");
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
                this.FormattingWriter.Append("TEMP ");
            }
        }

        protected override void AppendIndexesInside(TableBase table)
        {
            //Postgres requires separate statements for indexes 
        }

        protected override void AppendIndexesOutside(TableBase table)
        {
            IndexMeta? clusteredIndex = null;
            foreach (var tableIndex in table.Indexes)
            {
                this.FormattingWriter.Append("CREATE ");
                if (tableIndex.Unique)
                {
                    this.FormattingWriter.Append("UNIQUE ");
                }
                this.FormattingWriter.Append("INDEX ");
                if (tableIndex.Clustered)
                {
                    if (clusteredIndex != null)
                    {
                        throw new SqExpressException("Table can have only one clustered index");
                    }

                    clusteredIndex = tableIndex;
                }
                this.AppendName(this.BuildIndexName(table.FullName, tableIndex));
                this.FormattingWriter.Append(" ON ");
                table.FullName.Accept(this.ExprBuilder, null);

                this.AppendIndexColumnList(tableIndex: tableIndex);
                this.FormattingWriter.Append(";");
            }

            if (clusteredIndex != null)
            {
                this.FormattingWriter.Append(";CLUSTER ");
                table.FullName.Accept(this.ExprBuilder, null);
                this.FormattingWriter.Append(" USING ");
                this.AppendName(this.BuildIndexName(table.FullName, clusteredIndex));
                this.FormattingWriter.Append(";");
            }
        }

        protected override bool IsNamedPk() => true;

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
