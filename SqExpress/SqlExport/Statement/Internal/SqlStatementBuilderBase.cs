using System.Linq;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal abstract class SqlStatementBuilderBase : IStatementVisitor
    {
        protected SqlFormattingWriter FormattingWriter { get; }

        protected abstract SqlBuilderBase ExprBuilder { get; }

        protected readonly SqlBuilderOptions Options;

        protected SqlStatementBuilderBase(SqlBuilderOptions? options)
            : this(
                options,
                new SqlFormattingWriter(
                    SqlFormattingProfile.Unformatted))
        {
        }

        protected SqlStatementBuilderBase(SqlBuilderOptions? options, SqlFormattingWriter formattingWriter)
        {
            this.Options = options ?? SqlBuilderOptions.Default;
            this.FormattingWriter = formattingWriter;
        }

        protected void AppendName(string name) => this.ExprBuilder.AppendName(name);

        protected void AppendTable(StatementCreateTable statementCreateTable)
        {
            var table = statementCreateTable.Table;
            this.FormattingWriter.Append("CREATE ");
            this.AppendTempKeyword(table.FullName);
            this.FormattingWriter.Append("TABLE ");
            statementCreateTable.Table.FullName.Accept(this.ExprBuilder, null);
            this.FormattingWriter.Append('(');

            ColumnAnalysis analysis = ColumnAnalysis.Build();

            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (i != 0)
                {
                    this.FormattingWriter.Append(',');
                }
                var column = table.Columns[i];

                analysis.Analyze(column);

                this.AppendColumn(column: column);
            }
            this.AppendPkConstraints(table, analysis);
            this.AppendFkConstraints(table, analysis);

            this.AppendIndexesInside(table);

            this.FormattingWriter.Append(')');
            this.FormattingWriter.Append(';');

            this.AppendIndexesOutside(table);
        }

        protected abstract void AppendColumn(TableColumn column);

        protected abstract void AppendTempKeyword(IExprTableFullName tableName);

        private void AppendPkConstraints(TableBase table, ColumnAnalysis analysis)
        {
            if (analysis.Pk.Count < 1)
            {
                return;
            }

            this.FormattingWriter.Append(",CONSTRAINT");

            if (this.IsNamedPk())
            {
                this.FormattingWriter.Append(' ');
                this.AppendName(this.BuildPkName(table.FullName));
            }

            this.FormattingWriter.Append(" PRIMARY KEY ");
            this.ExprBuilder.AcceptListComaSeparatedPar('(', analysis.Pk, ')', null);
        }

        private void AppendFkConstraints(TableBase table, ColumnAnalysis analysis)
        {
            foreach (var analysisFk in analysis.Fks)
            {
                var foreignTable = analysisFk.Key;
                var pairList = analysisFk.Value;
                this.FormattingWriter.Append(",CONSTRAINT ");

                this.AppendName(this.BuildFkName(table.FullName, foreignTable));

                this.FormattingWriter.Append(" FOREIGN KEY ");
                this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.SelectToReadOnlyList(i => i.Internal), ')', null);

                this.FormattingWriter.Append(" REFERENCES ");
                foreignTable.Accept(this.ExprBuilder, null);
                this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.SelectToReadOnlyList(i => i.External), ')', null);
            }
        }

        protected abstract void AppendIndexesInside(TableBase table);

        protected abstract void AppendIndexesOutside(TableBase table);

        protected abstract bool IsNamedPk();

        protected void AppendIndexColumnList(IndexMeta tableIndex)
        {
            tableIndex.Columns.AssertNotEmpty("Table index has to contain at least one column");

            this.FormattingWriter.Append('(');
            for (var index = 0; index < tableIndex.Columns.Count; index++)
            {
                var column = tableIndex.Columns[index];
                if (index != 0)
                {
                    this.FormattingWriter.Append(',');
                }

                column.Column.ColumnName.Accept(this.ExprBuilder, null);
                if (column.Descending)
                {
                    this.FormattingWriter.Append(" DESC");
                }
            }

            this.FormattingWriter.Append(')');
        }

        protected string BuildIndexName(IExprTableFullName tableIn, IndexMeta index)
        {
            if (index.Name != null && !string.IsNullOrEmpty(index.Name))
            {
                return index.Name;
            }

            var table = tableIn.AsExprTableFullName();

            var schemaName = table.DbSchema != null ? this.Options.MapSchema(table.DbSchema.Schema.Name) + "_" : null;

            var columns = string.Join("_", index.Columns.Select(c => c.Column.ColumnName.Name + (c.Descending ? "_DESC" : null)));

            return $"IX_{schemaName}{table.TableName.Name}_{columns}";
        }

        private string BuildPkName(IExprTableFullName tableIn)
        {
            var table = tableIn.AsExprTableFullName();

            var schemaName = table.DbSchema != null ? this.Options.MapSchema(table.DbSchema.Schema.Name) + "_" : null;

            return $"PK_{schemaName}{table.TableName.Name}";
        }

        private string BuildFkName(IExprTableFullName tableIn, IExprTableFullName foreignTableIn)
        {
            ExprTableFullName table = tableIn.AsExprTableFullName();

            ExprTableFullName foreignTable = foreignTableIn.AsExprTableFullName();

            var schemaName = table.DbSchema != null ? this.Options.MapSchema(table.DbSchema.Schema.Name) + "_" : null;

            return "FK_"
                + (schemaName == null ? null : schemaName + "_")
                + table.TableName.Name
                + "_to_"
                + (schemaName == null ? null : schemaName + "_")
                + foreignTable.TableName.Name;
        }

        public abstract void VisitCreateTable(StatementCreateTable statementCreateTable);
        public abstract void VisitDropTable(StatementDropTable statementDropTable);
        public abstract void VisitIf(StatementIf statementIf);

        public void VisitStatementList(StatementList statementList)
        {
            statementList.Statements.AssertNotEmpty("Statement list cannot be empty");
            for (int i = 0; i < statementList.Statements.Count; i++)
            {
                statementList.Statements[i].Accept(this);
            }

        }
        public abstract void VisitIfTableExists(StatementIfTableExists statementIfExists);
        public abstract void VisitIfTempTableExists(StatementIfTempTableExists statementIfTempTableExists);
    }
}
