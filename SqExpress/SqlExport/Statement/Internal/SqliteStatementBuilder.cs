using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal
{
    internal class SqliteStatementBuilder : SqlStatementBuilderBase
    {
        private readonly SqliteBuilder _exprBuilder;

        public SqliteStatementBuilder(SqlBuilderOptions? options, StringBuilder? externalBuilder) : base(options, externalBuilder)
        {
            this._exprBuilder = new SqliteBuilder(this.Options, this.Builder);
        }

        public string Build() => this.Builder.ToString();

        protected override void AppendColumn(TableColumn column)
        {
            column.ColumnName.Accept(this.ExprBuilder, null);

            if (column.ColumnMeta?.IsIdentity == true && column.ColumnMeta.PrimaryKeyAutoIncrementIsAllowed())
            {
                this.Builder.Append(" INTEGER PRIMARY KEY AUTOINCREMENT");
                return;
            }

            this.Builder.Append(' ');
            column.SqlType.Accept(this.ExprBuilder, null);

            if (!column.IsNullable)
            {
                this.Builder.Append(" NOT NULL");
            }

            if (column.ColumnMeta != null && !ReferenceEquals(column.ColumnMeta.ColumnDefaultValue, null))
            {
                this.Builder.Append(" DEFAULT (");
                column.ColumnMeta.ColumnDefaultValue.Accept(this.ExprBuilder, null);
                this.Builder.Append(')');
            }
        }

        protected override void AppendTempKeyword(IExprTableFullName tableName)
        {
            if (tableName is ExprTempTableName)
            {
                this.Builder.Append("TEMP ");
            }
        }

        protected override void AppendIndexesInside(TableBase table)
        {
        }

        protected override void AppendIndexesOutside(TableBase table)
        {
            foreach (var tableIndex in table.Indexes)
            {
                if (tableIndex.Columns.Count == 1 && tableIndex.Columns[0].Column.ColumnMeta?.IsIdentity == true)
                {
                    continue;
                }

                this.Builder.Append("CREATE ");
                if (tableIndex.Unique)
                {
                    this.Builder.Append("UNIQUE ");
                }

                this.Builder.Append("INDEX ");
                this.AppendName(this.BuildIndexName(table.FullName, tableIndex));
                this.Builder.Append(" ON ");
                table.FullName.Accept(this.ExprBuilder, null);
                this.AppendIndexColumnList(tableIndex);
                this.Builder.Append(';');
            }
        }

        protected override bool IsNamedPk() => false;

        public override void VisitCreateTable(StatementCreateTable statementCreateTable)
        {
            var table = statementCreateTable.Table;

            this.Builder.Append("CREATE ");
            this.AppendTempKeyword(table.FullName);
            this.Builder.Append("TABLE ");
            table.FullName.Accept(this.ExprBuilder, null);
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
                this.AppendColumn(column);
            }

            var inlineIdentityPk = new HashSet<string>(
                table.Columns
                    .Where(c => c.ColumnMeta?.IsIdentity == true && c.ColumnMeta.IsPrimaryKey)
                    .Select(c => c.ColumnName.Name),
                StringComparer.Ordinal);

            var remainingPk = analysis.Pk
                .Where(c => !inlineIdentityPk.Contains(c.Name))
                .ToList();
            if (remainingPk.Count > 0)
            {
                this.Builder.Append(",PRIMARY KEY ");
                this.ExprBuilder.AcceptListComaSeparatedPar('(', remainingPk, ')', null);
            }

            foreach (var analysisFk in analysis.Fks)
            {
                var foreignTable = analysisFk.Key;
                var pairList = analysisFk.Value;

                this.Builder.Append(",FOREIGN KEY ");
                this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.Select(p => p.Internal).ToList(), ')', null);
                this.Builder.Append(" REFERENCES ");
                foreignTable.Accept(this.ExprBuilder, null);
                this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.Select(p => p.External).ToList(), ')', null);
            }

            this.Builder.Append(')');
            this.Builder.Append(';');

            this.AppendIndexesOutside(table);
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

    internal static class SqliteColumnMetaExtensions
    {
        public static bool PrimaryKeyAutoIncrementIsAllowed(this ColumnMeta columnMeta)
        {
            return columnMeta.IsPrimaryKey;
        }
    }
}
