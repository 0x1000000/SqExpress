using System.Text;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal abstract class SqlStatementBuilderBase
    {
        protected readonly StringBuilder Builder = new StringBuilder();

        protected abstract SqlBuilderBase ExprBuilder { get; }

        protected readonly SqlBuilderOptions Options;

        protected SqlStatementBuilderBase(SqlBuilderOptions? options)
        {
            this.Options = options ?? SqlBuilderOptions.Default;
        }

        protected void AppendName(string name) => this.ExprBuilder.AppendName(name);

        protected void AppendTable(StatementCreateTable statementCreateTable)
        {
            var table = statementCreateTable.Table;
            this.Builder.Append("CREATE TABLE ");
            statementCreateTable.Table.FullName.Accept(this.ExprBuilder, null);
            this.Builder.Append('(');

            ColumnAnalysis analysis = ColumnAnalysis.Build();

            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (i != 0)
                {
                    this.Builder.Append(',');
                }
                var column = table.Columns[i];

                analysis.Analyze(column);

                this.AppendColumn(column: column);
            }
            this.AppendPkConstraints(table, analysis);
            this.AppendFkConstraints(table, analysis);

            this.Builder.Append(')');
            this.Builder.Append(';');

        }

        protected abstract void AppendColumn(TableColumn column);

        private void AppendPkConstraints(TableBase table, ColumnAnalysis analysis)
        {
            if (analysis.Pk.Count < 1)
            {
                return;
            }

            this.Builder.Append(",CONSTRAINT ");

            this.AppendName(this.BuildPkName(table.FullName));

            this.Builder.Append(" PRIMARY KEY ");
            this.ExprBuilder.AcceptListComaSeparatedPar('(', analysis.Pk, ')', null);
        }

        private void AppendFkConstraints(TableBase table, ColumnAnalysis analysis)
        {
            foreach (var analysisFk in analysis.Fks)
            {
                var foreignTable = analysisFk.Key;
                var pairList = analysisFk.Value;
                this.Builder.Append(",CONSTRAINT ");

                this.AppendName(this.BuildFkName(table.FullName, foreignTable));

                this.Builder.Append(" FOREIGN KEY ");
                this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.SelectToReadOnlyList(i => i.Internal), ')', null);

                this.Builder.Append(" REFERENCES ");
                foreignTable.Accept(this.ExprBuilder, null);
                this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.SelectToReadOnlyList(i => i.External), ')', null);
            }
        }

        private string BuildPkName(ExprTableFullName table)
        {
            return $"PK_{this.Options.MapSchema(table.Schema.Name)}_{table.TableName.Name}";
        }

        private string BuildFkName(ExprTableFullName table, ExprTableFullName foreignTable)
        {
            StringBuilder nameBuilder = new StringBuilder();

            nameBuilder.Append("FK_");
            nameBuilder.Append(this.Options.MapSchema(table.Schema.Name));
            nameBuilder.Append('_');
            nameBuilder.Append(table.TableName.Name);
            nameBuilder.Append("_to_");
            nameBuilder.Append(this.Options.MapSchema(foreignTable.Schema.Name));
            nameBuilder.Append('_');
            nameBuilder.Append(foreignTable.TableName.Name);

            return nameBuilder.ToString();
        }
    }
}