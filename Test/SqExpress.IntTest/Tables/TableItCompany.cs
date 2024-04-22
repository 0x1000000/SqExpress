using SqExpress;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableItCompany : TableBase
    {
        public TableItCompany() : this(SqlDialect.TSql)
        {
        }

        public TableItCompany(SqlDialect sqlDialect) : this(sqlDialect, alias: SqExpress.Alias.Auto)
        {
        }

        public TableItCompany(SqlDialect sqlDialect, Alias alias) : base(schema: "dbo", name: "ItCompany", alias: alias)
        {
            this.CompanyId = this.CreateInt32Column("CompanyId", ColumnMeta.PrimaryKey().Identity());
            this.ExternalId = this.CreateGuidColumn("ExternalId", null);
            this.CompanyName = this.CreateStringColumn(name: "CompanyName", size: 250, isUnicode: Helpers.IsUnicode(false, sqlDialect), isText: false, columnMeta: null);
            this.Version = this.CreateInt32Column("Version", null);
            this.Created = this.CreateDateTimeColumn("Created", false, null);
            this.Modified = this.CreateDateTimeColumn("Modified", false, null);
            this.AddUniqueIndex(this.ExternalId);
            this.AddIndex(this.CompanyName);
        }

        [SqModel("CompanyName", PropertyName = "Id")]
        [SqModel("CompanyInitData", PropertyName = "Id")]
        public Int32TableColumn CompanyId { get; }

        [SqModel("CompanyInitData")]
        public GuidTableColumn ExternalId { get; }

        [SqModel("CompanyName", PropertyName = "Name")]
        [SqModel("CompanyInitData", PropertyName = "Name")]
        public StringTableColumn CompanyName { get; }

        [SqModel("Audit")]
        public Int32TableColumn Version { get; }

        [SqModel("Audit")]
        public DateTimeTableColumn Created { get; }

        [SqModel("Audit")]
        public DateTimeTableColumn Modified { get; }
    }
}