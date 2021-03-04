using SqExpress;
using SqExpress.Syntax.Type;

namespace SqExpress.IntTest.Tables
{
    public class TableItAllColumnTypes : TableBase
    {
        public TableItAllColumnTypes(bool postgres) : this(postgres, alias: SqExpress.Alias.Auto)
        {
        }

        public TableItAllColumnTypes(bool postgres, Alias alias): base(schema: "dbo", name: "ItAllColumnTypes", alias: alias)
        {
            this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey().Identity());
            this.ColBoolean = this.CreateBooleanColumn("ColBoolean", null);
            this.ColNullableBoolean = this.CreateNullableBooleanColumn("ColNullableBoolean", null);
            if (!postgres)
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
            this.ColStringUnicode = this.CreateStringColumn(name: "ColStringUnicode", size: null, isUnicode: true, isText: false, columnMeta: null);
            this.ColNullableStringUnicode = this.CreateNullableStringColumn(name: "ColNullableStringUnicode", size: null, isUnicode: true, isText: false, columnMeta: null);
            this.ColStringMax = this.CreateStringColumn(name: "ColStringMax", size: null, isUnicode: false, isText: false, columnMeta: null);
            this.ColNullableStringMax = this.CreateNullableStringColumn(name: "ColNullableStringMax", size: null, isUnicode: false, isText: false, columnMeta: null);
            this.ColString5 = this.CreateStringColumn(name: "ColString5", size: 5, isUnicode: false, isText: false, columnMeta: null);
            this.ColByteArraySmall = this.CreateByteArrayColumn("ColByteArraySmall", 255, null);
            this.ColByteArrayBig = this.CreateByteArrayColumn("ColByteArrayBig", null, null);
            this.ColNullableByteArraySmall = this.CreateNullableByteArrayColumn("ColNullableByteArraySmall", 255, null);
            this.ColNullableByteArrayBig = this.CreateNullableByteArrayColumn("ColNullableByteArrayBig", null, null);
            this.ColFixedSizeString = this.CreateFixedSizeStringColumn(name: "ColFixedSizeString", size: 3, isUnicode: false, columnMeta: null);
            this.ColNullableFixedSizeString = this.CreateNullableFixedSizeStringColumn(name: "ColNullableFixedSizeString", size: 3, isUnicode: true, columnMeta: null);
            this.ColFixedSizeByteArray = this.CreateFixedSizeByteArrayColumn("ColFixedSizeByteArray", 2, null);
            this.ColNullableFixedSizeByteArray = this.CreateNullableFixedSizeByteArrayColumn("ColNullableFixedSizeByteArray", 2, null);
            this.ColXml = this.CreateXmlColumn("ColXml", null);
            this.ColNullableXml = this.CreateNullableXmlColumn("ColNullableXml", null);
        }

        public Int32TableColumn Id { get; }

        public BooleanTableColumn ColBoolean { get; }

        public NullableBooleanTableColumn ColNullableBoolean { get; }

        public ByteTableColumn ColByte { get; }

        public NullableByteTableColumn ColNullableByte { get; }

        public Int16TableColumn ColInt16 { get; }

        public NullableInt16TableColumn ColNullableInt16 { get; }

        public Int32TableColumn ColInt32 { get; }

        public NullableInt32TableColumn ColNullableInt32 { get; }

        public Int64TableColumn ColInt64 { get; }

        public NullableInt64TableColumn ColNullableInt64 { get; }

        public DecimalTableColumn ColDecimal { get; }

        public NullableDecimalTableColumn ColNullableDecimal { get; }

        public DoubleTableColumn ColDouble { get; }

        public NullableDoubleTableColumn ColNullableDouble { get; }

        public DateTimeTableColumn ColDateTime { get; }

        public NullableDateTimeTableColumn ColNullableDateTime { get; }

        public GuidTableColumn ColGuid { get; }

        public NullableGuidTableColumn ColNullableGuid { get; }

        public StringTableColumn ColStringUnicode { get; }

        public NullableStringTableColumn ColNullableStringUnicode { get; }

        public StringTableColumn ColStringMax { get; }

        public NullableStringTableColumn ColNullableStringMax { get; }

        public StringTableColumn ColString5 { get; }

        public ByteArrayTableColumn ColByteArraySmall { get; }

        public ByteArrayTableColumn ColByteArrayBig { get; }

        public NullableByteArrayTableColumn ColNullableByteArraySmall { get; }

        public NullableByteArrayTableColumn ColNullableByteArrayBig { get; }

        public StringTableColumn ColFixedSizeString { get; }

        public NullableStringTableColumn ColNullableFixedSizeString { get; }

        public ByteArrayTableColumn ColFixedSizeByteArray { get; }

        public NullableByteArrayTableColumn ColNullableFixedSizeByteArray { get; }

        public StringTableColumn ColXml { get; }

        public NullableStringTableColumn ColNullableXml { get; }
    }
}