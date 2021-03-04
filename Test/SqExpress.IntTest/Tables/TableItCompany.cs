using SqExpress;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableItCompany : TableBase
    {
        public TableItCompany(): this(alias: SqExpress.Alias.Auto)
        {
        }

        public TableItCompany(Alias alias): base(schema: "dbo", name: "ItCompany", alias: alias)
        {
            this.CompanyId = this.CreateInt32Column("CompanyId", ColumnMeta.PrimaryKey().Identity());
            this.ExternalId = this.CreateGuidColumn("ExternalId", null);
            this.CompanyName = this.CreateStringColumn(name: "CompanyName", size: 250, isUnicode: false, isText: false, columnMeta: null);
            this.Version = this.CreateInt32Column("Version", null);
            this.Created = this.CreateDateTimeColumn("Created", false, null);
            this.Modified = this.CreateDateTimeColumn("Modified", false, null);
            this.AddUniqueIndex(this.ExternalId);
            this.AddIndex(this.CompanyName);
        }

        public Int32TableColumn CompanyId { get; }

        public GuidTableColumn ExternalId { get; }

        public StringTableColumn CompanyName { get; }

        public Int32TableColumn Version { get; }

        public DateTimeTableColumn Created { get; }

        public DateTimeTableColumn Modified { get; }
    }
}