using SqExpress;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Type;
using static SqExpress.IntTest.Tables.Helpers;

namespace SqExpress.IntTest.Tables
{
    public class TableItAllColumnTypes : TableBase
    {
        public TableItAllColumnTypes(SqlDialect sqlDialect) : this(sqlDialect, alias: SqExpress.Alias.Auto)
        {
        }

        public TableItAllColumnTypes(SqlDialect sqlDialect, Alias alias): base(schema: "dbo", name: "ItAllColumnTypes", alias: alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey().Identity());
            this.ColBoolean = this.CreateBooleanColumn("ColBoolean", null);
            this.ColNullableBoolean = this.CreateNullableBooleanColumn("ColNullableBoolean", null);
            if (sqlDialect != SqlDialect.PgSql)
            {
                this.ColByte = this.CreateByteColumn("ColByte", null);
                this.ColNullableByte = this.CreateNullableByteColumn("ColNullableByte", null);
            }
            this.ColInt16 = this.CreateInt16Column("ColInt16", null);
            this.ColNullableInt16 = this.CreateNullableInt16Column("ColNullableInt16", null);
            this.ColInt32 = this.CreateInt32Column("ColInt32", null);
            this.ColNullableInt32 = this.CreateNullableInt32Column("ColNullableInt32", null);
            this.ColInt64 = this.CreateInt64Column("ColInt64", null);
            this.ColNullableInt64 = this.CreateNullableInt64Column("ColNullableInt64", null);
            this.ColDecimal = this.CreateDecimalColumn("ColDecimal", new DecimalPrecisionScale(precision: 10, scale: 6), null);
            this.ColNullableDecimal = this.CreateNullableDecimalColumn("ColNullableDecimal", new DecimalPrecisionScale(precision: 10, scale: 6), null);
            this.ColDouble = this.CreateDoubleColumn("ColDouble", null);
            this.ColNullableDouble = this.CreateNullableDoubleColumn("ColNullableDouble", null);
            this.ColDateTime = this.CreateDateTimeColumn("ColDateTime", false, null);
            this.ColNullableDateTime = this.CreateNullableDateTimeColumn("ColNullableDateTime", false, null);
            this.ColGuid = this.CreateGuidColumn("ColGuid", null);
            this.ColNullableGuid = this.CreateNullableGuidColumn("ColNullableGuid", null);
            this.ColStringUnicode = this.CreateStringColumn(name: "ColStringUnicode", size: null, IsUnicode(true, sqlDialect), isText: false, columnMeta: null);
            this.ColNullableStringUnicode = this.CreateNullableStringColumn(name: "ColNullableStringUnicode", size: null, IsUnicode(true, sqlDialect), isText: false, columnMeta: null);
            this.ColStringMax = this.CreateStringColumn(name: "ColStringMax", size: null, IsUnicode(false, sqlDialect), isText: false, columnMeta: null);
            this.ColNullableStringMax = this.CreateNullableStringColumn(name: "ColNullableStringMax", size: null, IsUnicode(false, sqlDialect), isText: false, columnMeta: null);
            this.ColString5 = this.CreateStringColumn(name: "ColString5", size: 5, IsUnicode(false, sqlDialect), isText: false, columnMeta: null);
            this.ColByteArraySmall = this.CreateByteArrayColumn("ColByteArraySmall", ArrayLimit(255, sqlDialect), null);
            this.ColByteArrayBig = this.CreateByteArrayColumn("ColByteArrayBig", null, null);
            this.ColNullableByteArraySmall = this.CreateNullableByteArrayColumn("ColNullableByteArraySmall", ArrayLimit(255, sqlDialect), null);
            this.ColNullableByteArrayBig = this.CreateNullableByteArrayColumn("ColNullableByteArrayBig", null, null);
            this.ColFixedSizeString = this.CreateFixedSizeStringColumn(name: "ColFixedSizeString", size: 3, IsUnicode(false, sqlDialect), columnMeta: null);
            this.ColNullableFixedSizeString = this.CreateNullableFixedSizeStringColumn(name: "ColNullableFixedSizeString", size: 3, IsUnicode(true, sqlDialect), columnMeta: null);

            if (sqlDialect != SqlDialect.PgSql)
            {
                this.ColFixedSizeByteArray = this.CreateFixedSizeByteArrayColumn("ColFixedSizeByteArray", 2, null);
                this.ColNullableFixedSizeByteArray = this.CreateNullableFixedSizeByteArrayColumn("ColNullableFixedSizeByteArray", 2, null);
            }

            this.ColXml = this.CreateXmlColumn("ColXml", null);
            this.ColNullableXml = this.CreateNullableXmlColumn("ColNullableXml", null);
            if (sqlDialect != SqlDialect.MySql)
            {
                this.ColDateTimeOffset = this.CreateDateTimeOffsetColumn("ColDateTimeOffset");
                this.ColNullableDateTimeOffset = this.CreateNullableDateTimeOffsetColumn("ColNullableDateTimeOffset");
            }
        }

        [SqModel("AllTypes")]
        public Int32TableColumn Id { get; }

        [SqModel("AllTypes")]
        public BooleanTableColumn ColBoolean { get; }

        [SqModel("AllTypes")]
        public NullableBooleanTableColumn ColNullableBoolean { get; }

        public ByteTableColumn? ColByte { get; }

        public NullableByteTableColumn? ColNullableByte { get; }

        [SqModel("AllTypes")]
        public Int16TableColumn ColInt16 { get; }

        [SqModel("AllTypes")]
        public NullableInt16TableColumn ColNullableInt16 { get; }

        [SqModel("AllTypes")]
        public Int32TableColumn ColInt32 { get; }

        [SqModel("AllTypes")]
        public NullableInt32TableColumn ColNullableInt32 { get; }

        [SqModel("AllTypes")]
        public Int64TableColumn ColInt64 { get; }

        [SqModel("AllTypes")]
        public NullableInt64TableColumn ColNullableInt64 { get; }

        [SqModel("AllTypes")]
        public DecimalTableColumn ColDecimal { get; }

        [SqModel("AllTypes")]
        public NullableDecimalTableColumn ColNullableDecimal { get; }

        [SqModel("AllTypes")]
        public DoubleTableColumn ColDouble { get; }

        [SqModel("AllTypes")]
        public NullableDoubleTableColumn ColNullableDouble { get; }

        [SqModel("AllTypes")]
        public DateTimeTableColumn ColDateTime { get; }

        [SqModel("AllTypes")]
        public NullableDateTimeTableColumn ColNullableDateTime { get; }

        [SqModel("AllTypes")]
        public GuidTableColumn ColGuid { get; }

        [SqModel("AllTypes")]
        public NullableGuidTableColumn ColNullableGuid { get; }

        [SqModel("AllTypes")]
        public StringTableColumn ColStringUnicode { get; }

        [SqModel("AllTypes")]
        public NullableStringTableColumn ColNullableStringUnicode { get; }

        [SqModel("AllTypes")]
        public StringTableColumn ColStringMax { get; }

        [SqModel("AllTypes")]
        public NullableStringTableColumn ColNullableStringMax { get; }

        [SqModel("AllTypes")]
        public StringTableColumn ColString5 { get; }

        [SqModel("AllTypes")]
        public ByteArrayTableColumn ColByteArraySmall { get; }

        [SqModel("AllTypes")]
        public ByteArrayTableColumn ColByteArrayBig { get; }

        [SqModel("AllTypes")]
        public NullableByteArrayTableColumn ColNullableByteArraySmall { get; }

        [SqModel("AllTypes")]
        public NullableByteArrayTableColumn ColNullableByteArrayBig { get; }

        [SqModel("AllTypes")]
        public StringTableColumn ColFixedSizeString { get; }

        [SqModel("AllTypes")]
        public NullableStringTableColumn ColNullableFixedSizeString { get; }

        public ByteArrayTableColumn? ColFixedSizeByteArray { get; }

        public NullableByteArrayTableColumn? ColNullableFixedSizeByteArray { get; }

        [SqModel("AllTypes")]
        public StringTableColumn ColXml { get; }

        [SqModel("AllTypes")]
        public NullableStringTableColumn ColNullableXml { get; }

        public DateTimeOffsetTableColumn? ColDateTimeOffset { get; set; }

        public NullableDateTimeOffsetTableColumn? ColNullableDateTimeOffset { get; set; }
    }
}