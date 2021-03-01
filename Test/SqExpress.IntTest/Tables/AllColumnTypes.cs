namespace SqExpress.IntTest.Tables
{
    public class AllColumnTypes : TableBase
    {
        public AllColumnTypes(bool postgres, Alias alias = default) : base("public", "ItAllColumnTypes", alias)
        {
            this.Id = this.CreateInt32Column(nameof(Id), ColumnMeta.Identity().PrimaryKey());

            this.ColBoolean = this.CreateBooleanColumn(nameof(ColBoolean));
            this.ColNullableBoolean = this.CreateNullableBooleanColumn(nameof(ColNullableBoolean));
            if (!postgres)
            {
                this.ColByte = this.CreateByteColumn(nameof(ColByte));
                this.ColNullableByte = this.CreateNullableByteColumn(nameof(ColNullableByte));
            }
            this.ColInt16 = this.CreateInt16Column(nameof(ColInt16));
            this.ColNullableInt16 = this.CreateNullableInt16Column(nameof(ColNullableInt16));
            this.ColInt32 = this.CreateInt32Column(nameof(ColInt32));
            this.ColNullableInt32 = this.CreateNullableInt32Column(nameof(ColNullableInt32));
            this.ColInt64 = this.CreateInt64Column(nameof(ColInt64));
            this.ColNullableInt64 = this.CreateNullableInt64Column(nameof(ColNullableInt64));
            this.ColDecimal = this.CreateDecimalColumn(nameof(ColDecimal), (10, 6));
            this.ColNullableDecimal = this.CreateNullableDecimalColumn(nameof(ColNullableDecimal), (10, 6));
            this.ColDouble = this.CreateDoubleColumn(nameof(ColDouble));
            this.ColNullableDouble = this.CreateNullableDoubleColumn(nameof(ColNullableDouble));
            this.ColDateTime = this.CreateDateTimeColumn(nameof(ColDateTime));
            this.ColNullableDateTime = this.CreateNullableDateTimeColumn(nameof(ColNullableDateTime));
            this.ColGuid = this.CreateGuidColumn(nameof(ColGuid));
            this.ColNullableGuid = this.CreateNullableGuidColumn(nameof(ColNullableGuid));

            this.ColStringUnicode = this.CreateStringColumn(nameof(ColStringUnicode), null, true);
            this.ColNullableStringUnicode = this.CreateNullableStringColumn(nameof(ColNullableStringUnicode), null, true);

            this.ColStringMax = this.CreateStringColumn(nameof(ColStringMax), null);
            this.ColNullableStringMax = this.CreateNullableStringColumn(nameof(ColNullableStringMax), null);

            this.ColString5 = this.CreateStringColumn(nameof(ColString5), 5);


            this.ColByteArraySmall = this.CreateByteArrayColumn(nameof(ColByteArraySmall), 255);
            this.ColByteArrayBig = this.CreateByteArrayColumn(nameof(ColByteArrayBig), null);

            this.ColNullableByteArraySmall = this.CreateNullableByteArrayColumn(nameof(ColNullableByteArraySmall), 255);
            this.ColNullableByteArrayBig = this.CreateNullableByteArrayColumn(nameof(ColNullableByteArrayBig), null);

            this.ColFixedSizeString = this.CreateFixedSizeStringColumn(nameof(this.ColFixedSizeString), 3, false);
            this.ColNullableFixedSizeString = this.CreateNullableFixedSizeStringColumn(nameof(this.ColNullableFixedSizeString), 3, true);

            this.ColFixedSizeByteArray = this.CreateFixedSizeByteArrayColumn(nameof(ColFixedSizeByteArray), 2);
            this.ColNullableFixedSizeByteArray = this.CreateNullableFixedSizeByteArrayColumn(nameof(ColNullableFixedSizeByteArray), 2);


        }

        public NullableByteArrayTableColumn ColNullableFixedSizeByteArray { get; set; }
        public ByteArrayTableColumn ColFixedSizeByteArray { get; set; }

        public NullableStringTableColumn ColNullableFixedSizeString { get; set; }
        public StringTableColumn ColFixedSizeString { get; set; }

        public ByteArrayTableColumn ColByteArraySmall { get; }
        public ByteArrayTableColumn ColByteArrayBig { get; }

        public NullableByteArrayTableColumn ColNullableByteArraySmall { get; }
        public NullableByteArrayTableColumn ColNullableByteArrayBig { get; }

        public Int32TableColumn Id { get; set; }

        public StringTableColumn ColString5 { get; set; }

        public NullableStringTableColumn ColNullableStringMax { get; set; }

        public StringTableColumn ColStringMax { get; set; }

        public NullableStringTableColumn ColNullableStringUnicode { get; set; }

        public StringTableColumn ColStringUnicode { get; set; }

        public NullableGuidTableColumn ColNullableGuid { get; set; }

        public GuidTableColumn ColGuid { get; set; }

        public NullableDateTimeTableColumn ColNullableDateTime { get; set; }

        public DateTimeTableColumn ColDateTime { get; set; }

        public NullableDoubleTableColumn ColNullableDouble { get; set; }

        public DoubleTableColumn ColDouble { get; set; }

        public NullableDecimalTableColumn ColNullableDecimal { get; set; }

        public DecimalTableColumn ColDecimal { get; set; }

        public NullableInt64TableColumn ColNullableInt64 { get; set; }

        public Int64TableColumn ColInt64 { get; set; }

        public NullableInt32TableColumn ColNullableInt32 { get; set; }

        public Int32TableColumn ColInt32 { get; set; }

        public NullableInt16TableColumn ColNullableInt16 { get; set; }

        public Int16TableColumn ColInt16 { get; set; }

        public NullableByteTableColumn? ColNullableByte { get; set; }

        public ByteTableColumn? ColByte { get; set; }

        public NullableBooleanTableColumn ColNullableBoolean { get; set; }

        public BooleanTableColumn ColBoolean { get; set; }
    }
}