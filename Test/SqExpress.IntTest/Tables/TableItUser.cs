using SqExpress;
using SqExpress.IntTest.Tables.Models;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableItUser : TableBase
    {
        public TableItUser(): this(alias: SqExpress.Alias.Auto)
        {
        }

        public TableItUser(Alias alias): base(schema: "dbo", name: "ItUser", alias: alias)
        {
            this.UserId = this.CreateInt32Column("UserId", ColumnMeta.PrimaryKey().Identity());
            this.ExternalId = this.CreateGuidColumn("ExternalId", null);
            this.FirstName = this.CreateStringColumn(name: "FirstName", size: 255, isUnicode: false, isText: false, columnMeta: null);
            this.LastName = this.CreateStringColumn(name: "LastName", size: 255, isUnicode: false, isText: false, columnMeta: null);
            this.Email = this.CreateStringColumn(name: "Email", size: 255, isUnicode: false, isText: false, columnMeta: null);
            this.RegDate = this.CreateDateTimeColumn("RegDate", false, null);
            this.Version = this.CreateInt32Column("Version", ColumnMeta.DefaultValue(0));
            this.Created = this.CreateDateTimeColumn("Created", false, ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));
            this.Modified = this.CreateDateTimeColumn("Modified", false, ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));
            this.AddUniqueClusteredIndex(this.ExternalId);
            this.AddIndex(this.FirstName);
            this.AddIndex(IndexMetaColumn.Desc(this.LastName));
        }

        [SqModel("UserName", PropertyName = "Id", CastType = typeof(EntUser))]
        [SqModel("UserEmail", PropertyName = "Id", CastType = typeof(EntUser))]
        public Int32TableColumn UserId { get; }

        public GuidTableColumn ExternalId { get; }

        [SqModel("UserName")]
        public StringTableColumn FirstName { get; }

        [SqModel("UserName")]
        public StringTableColumn LastName { get; }

        [SqModel("UserEmail")]
        public StringTableColumn Email { get; }

        public DateTimeTableColumn RegDate { get; }

        [SqModel("Audit")]
        public Int32TableColumn Version { get; }

        [SqModel("Audit")]
        public DateTimeTableColumn Created { get; }

        [SqModel("Audit")]
        public DateTimeTableColumn Modified { get; }
    }
}