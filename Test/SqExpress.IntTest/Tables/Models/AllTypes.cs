using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;

namespace SqExpress.IntTest.Tables.Models
{
    public class AllTypes
    {
        public AllTypes(int id, bool colBoolean, bool? colNullableBoolean, byte colByte, byte? colNullableByte, short colInt16, short? colNullableInt16, int colInt32, int? colNullableInt32, long colInt64, long? colNullableInt64, decimal colDecimal, decimal? colNullableDecimal, double colDouble, double? colNullableDouble, DateTime colDateTime, DateTime? colNullableDateTime, Guid colGuid, Guid? colNullableGuid, string colStringUnicode, string? colNullableStringUnicode, string colStringMax, string? colNullableStringMax, string colString5, byte[] colByteArraySmall, byte[] colByteArrayBig, byte[]? colNullableByteArraySmall, byte[]? colNullableByteArrayBig, string colFixedSizeString, string? colNullableFixedSizeString, byte[] colFixedSizeByteArray, byte[]? colNullableFixedSizeByteArray, string colXml, string? colNullableXml)
        {
            this.Id = id;
            this.ColBoolean = colBoolean;
            this.ColNullableBoolean = colNullableBoolean;
            this.ColByte = colByte;
            this.ColNullableByte = colNullableByte;
            this.ColInt16 = colInt16;
            this.ColNullableInt16 = colNullableInt16;
            this.ColInt32 = colInt32;
            this.ColNullableInt32 = colNullableInt32;
            this.ColInt64 = colInt64;
            this.ColNullableInt64 = colNullableInt64;
            this.ColDecimal = colDecimal;
            this.ColNullableDecimal = colNullableDecimal;
            this.ColDouble = colDouble;
            this.ColNullableDouble = colNullableDouble;
            this.ColDateTime = colDateTime;
            this.ColNullableDateTime = colNullableDateTime;
            this.ColGuid = colGuid;
            this.ColNullableGuid = colNullableGuid;
            this.ColStringUnicode = colStringUnicode;
            this.ColNullableStringUnicode = colNullableStringUnicode;
            this.ColStringMax = colStringMax;
            this.ColNullableStringMax = colNullableStringMax;
            this.ColString5 = colString5;
            this.ColByteArraySmall = colByteArraySmall;
            this.ColByteArrayBig = colByteArrayBig;
            this.ColNullableByteArraySmall = colNullableByteArraySmall;
            this.ColNullableByteArrayBig = colNullableByteArrayBig;
            this.ColFixedSizeString = colFixedSizeString;
            this.ColNullableFixedSizeString = colNullableFixedSizeString;
            this.ColFixedSizeByteArray = colFixedSizeByteArray;
            this.ColNullableFixedSizeByteArray = colNullableFixedSizeByteArray;
            this.ColXml = colXml;
            this.ColNullableXml = colNullableXml;
        }

        public static AllTypes Read(ISqDataRecordReader record, TableItAllColumnTypes table)
        {
            return new AllTypes(id: table.Id.Read(record), colBoolean: table.ColBoolean.Read(record), colNullableBoolean: table.ColNullableBoolean.Read(record), colByte: table.ColByte.Read(record), colNullableByte: table.ColNullableByte.Read(record), colInt16: table.ColInt16.Read(record), colNullableInt16: table.ColNullableInt16.Read(record), colInt32: table.ColInt32.Read(record), colNullableInt32: table.ColNullableInt32.Read(record), colInt64: table.ColInt64.Read(record), colNullableInt64: table.ColNullableInt64.Read(record), colDecimal: table.ColDecimal.Read(record), colNullableDecimal: table.ColNullableDecimal.Read(record), colDouble: table.ColDouble.Read(record), colNullableDouble: table.ColNullableDouble.Read(record), colDateTime: table.ColDateTime.Read(record), colNullableDateTime: table.ColNullableDateTime.Read(record), colGuid: table.ColGuid.Read(record), colNullableGuid: table.ColNullableGuid.Read(record), colStringUnicode: table.ColStringUnicode.Read(record), colNullableStringUnicode: table.ColNullableStringUnicode.Read(record), colStringMax: table.ColStringMax.Read(record), colNullableStringMax: table.ColNullableStringMax.Read(record), colString5: table.ColString5.Read(record), colByteArraySmall: table.ColByteArraySmall.Read(record), colByteArrayBig: table.ColByteArrayBig.Read(record), colNullableByteArraySmall: table.ColNullableByteArraySmall.Read(record), colNullableByteArrayBig: table.ColNullableByteArrayBig.Read(record), colFixedSizeString: table.ColFixedSizeString.Read(record), colNullableFixedSizeString: table.ColNullableFixedSizeString.Read(record), colFixedSizeByteArray: table.ColFixedSizeByteArray.Read(record), colNullableFixedSizeByteArray: table.ColNullableFixedSizeByteArray.Read(record), colXml: table.ColXml.Read(record), colNullableXml: table.ColNullableXml.Read(record));
        }

        public int Id { get; }

        public bool ColBoolean { get; }

        public bool? ColNullableBoolean { get; }

        public byte ColByte { get; }

        public byte? ColNullableByte { get; }

        public short ColInt16 { get; }

        public short? ColNullableInt16 { get; }

        public int ColInt32 { get; }

        public int? ColNullableInt32 { get; }

        public long ColInt64 { get; }

        public long? ColNullableInt64 { get; }

        public decimal ColDecimal { get; }

        public decimal? ColNullableDecimal { get; }

        public double ColDouble { get; }

        public double? ColNullableDouble { get; }

        public DateTime ColDateTime { get; }

        public DateTime? ColNullableDateTime { get; }

        public Guid ColGuid { get; }

        public Guid? ColNullableGuid { get; }

        public string ColStringUnicode { get; }

        public string? ColNullableStringUnicode { get; }

        public string ColStringMax { get; }

        public string? ColNullableStringMax { get; }

        public string ColString5 { get; }

        public byte[] ColByteArraySmall { get; }

        public byte[] ColByteArrayBig { get; }

        public byte[]? ColNullableByteArraySmall { get; }

        public byte[]? ColNullableByteArrayBig { get; }

        public string ColFixedSizeString { get; }

        public string? ColNullableFixedSizeString { get; }

        public byte[] ColFixedSizeByteArray { get; }

        public byte[]? ColNullableFixedSizeByteArray { get; }

        public string ColXml { get; }

        public string? ColNullableXml { get; }

        public static TableColumn[] GetColumns(TableItAllColumnTypes table)
        {
            return new TableColumn[]{table.Id, table.ColBoolean, table.ColNullableBoolean, table.ColByte, table.ColNullableByte, table.ColInt16, table.ColNullableInt16, table.ColInt32, table.ColNullableInt32, table.ColInt64, table.ColNullableInt64, table.ColDecimal, table.ColNullableDecimal, table.ColDouble, table.ColNullableDouble, table.ColDateTime, table.ColNullableDateTime, table.ColGuid, table.ColNullableGuid, table.ColStringUnicode, table.ColNullableStringUnicode, table.ColStringMax, table.ColNullableStringMax, table.ColString5, table.ColByteArraySmall, table.ColByteArrayBig, table.ColNullableByteArraySmall, table.ColNullableByteArrayBig, table.ColFixedSizeString, table.ColNullableFixedSizeString, table.ColFixedSizeByteArray, table.ColNullableFixedSizeByteArray, table.ColXml, table.ColNullableXml};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItAllColumnTypes, AllTypes> s)
        {
            return s.Set(s.Target.ColBoolean, s.Source.ColBoolean).Set(s.Target.ColNullableBoolean, s.Source.ColNullableBoolean).Set(s.Target.ColByte, s.Source.ColByte).Set(s.Target.ColNullableByte, s.Source.ColNullableByte).Set(s.Target.ColInt16, s.Source.ColInt16).Set(s.Target.ColNullableInt16, s.Source.ColNullableInt16).Set(s.Target.ColInt32, s.Source.ColInt32).Set(s.Target.ColNullableInt32, s.Source.ColNullableInt32).Set(s.Target.ColInt64, s.Source.ColInt64).Set(s.Target.ColNullableInt64, s.Source.ColNullableInt64).Set(s.Target.ColDecimal, s.Source.ColDecimal).Set(s.Target.ColNullableDecimal, s.Source.ColNullableDecimal).Set(s.Target.ColDouble, s.Source.ColDouble).Set(s.Target.ColNullableDouble, s.Source.ColNullableDouble).Set(s.Target.ColDateTime, s.Source.ColDateTime).Set(s.Target.ColNullableDateTime, s.Source.ColNullableDateTime).Set(s.Target.ColGuid, s.Source.ColGuid).Set(s.Target.ColNullableGuid, s.Source.ColNullableGuid).Set(s.Target.ColStringUnicode, s.Source.ColStringUnicode).Set(s.Target.ColNullableStringUnicode, s.Source.ColNullableStringUnicode).Set(s.Target.ColStringMax, s.Source.ColStringMax).Set(s.Target.ColNullableStringMax, s.Source.ColNullableStringMax).Set(s.Target.ColString5, s.Source.ColString5).Set(s.Target.ColByteArraySmall, s.Source.ColByteArraySmall).Set(s.Target.ColByteArrayBig, s.Source.ColByteArrayBig).Set(s.Target.ColNullableByteArraySmall, s.Source.ColNullableByteArraySmall).Set(s.Target.ColNullableByteArrayBig, s.Source.ColNullableByteArrayBig).Set(s.Target.ColFixedSizeString, s.Source.ColFixedSizeString).Set(s.Target.ColNullableFixedSizeString, s.Source.ColNullableFixedSizeString).Set(s.Target.ColFixedSizeByteArray, s.Source.ColFixedSizeByteArray).Set(s.Target.ColNullableFixedSizeByteArray, s.Source.ColNullableFixedSizeByteArray).Set(s.Target.ColXml, s.Source.ColXml).Set(s.Target.ColNullableXml, s.Source.ColNullableXml);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItAllColumnTypes, AllTypes> s)
        {
            return s.Set(s.Target.Id, s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItAllColumnTypes, AllTypes> s)
        {
            return s.Set(s.Target.ColBoolean, s.Source.ColBoolean).Set(s.Target.ColNullableBoolean, s.Source.ColNullableBoolean).Set(s.Target.ColByte, s.Source.ColByte).Set(s.Target.ColNullableByte, s.Source.ColNullableByte).Set(s.Target.ColInt16, s.Source.ColInt16).Set(s.Target.ColNullableInt16, s.Source.ColNullableInt16).Set(s.Target.ColInt32, s.Source.ColInt32).Set(s.Target.ColNullableInt32, s.Source.ColNullableInt32).Set(s.Target.ColInt64, s.Source.ColInt64).Set(s.Target.ColNullableInt64, s.Source.ColNullableInt64).Set(s.Target.ColDecimal, s.Source.ColDecimal).Set(s.Target.ColNullableDecimal, s.Source.ColNullableDecimal).Set(s.Target.ColDouble, s.Source.ColDouble).Set(s.Target.ColNullableDouble, s.Source.ColNullableDouble).Set(s.Target.ColDateTime, s.Source.ColDateTime).Set(s.Target.ColNullableDateTime, s.Source.ColNullableDateTime).Set(s.Target.ColGuid, s.Source.ColGuid).Set(s.Target.ColNullableGuid, s.Source.ColNullableGuid).Set(s.Target.ColStringUnicode, s.Source.ColStringUnicode).Set(s.Target.ColNullableStringUnicode, s.Source.ColNullableStringUnicode).Set(s.Target.ColStringMax, s.Source.ColStringMax).Set(s.Target.ColNullableStringMax, s.Source.ColNullableStringMax).Set(s.Target.ColString5, s.Source.ColString5).Set(s.Target.ColByteArraySmall, s.Source.ColByteArraySmall).Set(s.Target.ColByteArrayBig, s.Source.ColByteArrayBig).Set(s.Target.ColNullableByteArraySmall, s.Source.ColNullableByteArraySmall).Set(s.Target.ColNullableByteArrayBig, s.Source.ColNullableByteArrayBig).Set(s.Target.ColFixedSizeString, s.Source.ColFixedSizeString).Set(s.Target.ColNullableFixedSizeString, s.Source.ColNullableFixedSizeString).Set(s.Target.ColFixedSizeByteArray, s.Source.ColFixedSizeByteArray).Set(s.Target.ColNullableFixedSizeByteArray, s.Source.ColNullableFixedSizeByteArray).Set(s.Target.ColXml, s.Source.ColXml).Set(s.Target.ColNullableXml, s.Source.ColNullableXml);
        }

        public AllTypes WithId(int id)
        {
            return new AllTypes(id: id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColBoolean(bool colBoolean)
        {
            return new AllTypes(id: this.Id, colBoolean: colBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableBoolean(bool? colNullableBoolean)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: colNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColByte(byte colByte)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: colByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableByte(byte? colNullableByte)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: colNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColInt16(short colInt16)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: colInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableInt16(short? colNullableInt16)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: colNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColInt32(int colInt32)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: colInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableInt32(int? colNullableInt32)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: colNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColInt64(long colInt64)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: colInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableInt64(long? colNullableInt64)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: colNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColDecimal(decimal colDecimal)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: colDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableDecimal(decimal? colNullableDecimal)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: colNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColDouble(double colDouble)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: colDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableDouble(double? colNullableDouble)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: colNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColDateTime(DateTime colDateTime)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: colDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableDateTime(DateTime? colNullableDateTime)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: colNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColGuid(Guid colGuid)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: colGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableGuid(Guid? colNullableGuid)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: colNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColStringUnicode(string colStringUnicode)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: colStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableStringUnicode(string? colNullableStringUnicode)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: colNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColStringMax(string colStringMax)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: colStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableStringMax(string? colNullableStringMax)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: colNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColString5(string colString5)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: colString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColByteArraySmall(byte[] colByteArraySmall)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: colByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColByteArrayBig(byte[] colByteArrayBig)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: colByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableByteArraySmall(byte[]? colNullableByteArraySmall)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: colNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableByteArrayBig(byte[]? colNullableByteArrayBig)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: colNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColFixedSizeString(string colFixedSizeString)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: colFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableFixedSizeString(string? colNullableFixedSizeString)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: colNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColFixedSizeByteArray(byte[] colFixedSizeByteArray)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: colFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableFixedSizeByteArray(byte[]? colNullableFixedSizeByteArray)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: colNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColXml(string colXml)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: colXml, colNullableXml: this.ColNullableXml);
        }

        public AllTypes WithColNullableXml(string? colNullableXml)
        {
            return new AllTypes(id: this.Id, colBoolean: this.ColBoolean, colNullableBoolean: this.ColNullableBoolean, colByte: this.ColByte, colNullableByte: this.ColNullableByte, colInt16: this.ColInt16, colNullableInt16: this.ColNullableInt16, colInt32: this.ColInt32, colNullableInt32: this.ColNullableInt32, colInt64: this.ColInt64, colNullableInt64: this.ColNullableInt64, colDecimal: this.ColDecimal, colNullableDecimal: this.ColNullableDecimal, colDouble: this.ColDouble, colNullableDouble: this.ColNullableDouble, colDateTime: this.ColDateTime, colNullableDateTime: this.ColNullableDateTime, colGuid: this.ColGuid, colNullableGuid: this.ColNullableGuid, colStringUnicode: this.ColStringUnicode, colNullableStringUnicode: this.ColNullableStringUnicode, colStringMax: this.ColStringMax, colNullableStringMax: this.ColNullableStringMax, colString5: this.ColString5, colByteArraySmall: this.ColByteArraySmall, colByteArrayBig: this.ColByteArrayBig, colNullableByteArraySmall: this.ColNullableByteArraySmall, colNullableByteArrayBig: this.ColNullableByteArrayBig, colFixedSizeString: this.ColFixedSizeString, colNullableFixedSizeString: this.ColNullableFixedSizeString, colFixedSizeByteArray: this.ColFixedSizeByteArray, colNullableFixedSizeByteArray: this.ColNullableFixedSizeByteArray, colXml: this.ColXml, colNullableXml: colNullableXml);
        }
    }
}