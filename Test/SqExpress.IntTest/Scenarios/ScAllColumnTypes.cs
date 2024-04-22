using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Data;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.SqlExport;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScAllColumnTypes : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            bool isPostgres = context.Dialect == SqlDialect.PgSql;
            var table = new TableItAllColumnTypes(context.Dialect);
            await context.Database.Statement(table.Script.DropAndCreate());

            var testData = GetTestData(context.Dialect);

            await InsertDataInto(table, testData)
                .MapData(Mapping)
                .Exec(context.Database);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.AddDataReaderMapping();
                var map = cfg.CreateMap<IDataRecord, AllColumnTypesDto>();

                map
                    .ForMember(nameof(table.ColByteArraySmall), c => c.Ignore())
                    .ForMember(nameof(table.ColByteArrayBig), c => c.Ignore())
                    .ForMember(nameof(table.ColNullableByteArraySmall), c => c.Ignore())
                    .ForMember(nameof(table.ColNullableByteArrayBig), c => c.Ignore())
                    .ForMember(nameof(table.ColNullableFixedSizeByteArray), c => c.Ignore())
                    .ForMember(nameof(table.ColFixedSizeByteArray), c => c.Ignore())
                    .ForMember(nameof(table.ColDateTimeOffset), c => c.Ignore())
                    .ForMember(nameof(table.ColNullableDateTimeOffset), c => c.Ignore());

                if (isPostgres)
                {
                    map
                        .ForMember(nameof(table.ColByte), c => c.Ignore())
                        .ForMember(nameof(table.ColNullableByte), c => c.Ignore())
                        .ForMember(nameof(table.ColNullableFixedSizeByteArray), c => c.Ignore())
                        .ForMember(nameof(table.ColFixedSizeByteArray), c => c.Ignore());
                }
                if (context.Dialect == SqlDialect.MySql)
                {
                    map
                        .ForMember(nameof(table.ColBoolean), c => c.MapFrom((r, dto) => r.GetBoolean(r.GetOrdinal(nameof(table.ColBoolean)))))
                        .ForMember(nameof(table.ColNullableBoolean), c => c.MapFrom((r, dto) => r.IsDBNull(r.GetOrdinal(nameof(table.ColNullableBoolean))) ? (bool?)null : r.GetBoolean(r.GetOrdinal(nameof(table.ColNullableBoolean)))))
                        .ForMember(nameof(table.ColGuid), c => c.MapFrom((r, dto) => r.GetGuid(r.GetOrdinal(nameof(table.ColGuid)))))
                        .ForMember(nameof(table.ColNullableGuid), c=>c.MapFrom((r, dto) => r.IsDBNull(r.GetOrdinal(nameof(table.ColNullableGuid)))? (Guid?)null : r.GetGuid(r.GetOrdinal(nameof(table.ColNullableGuid)))));
                }
            }));

            var expr = Select(table.Columns)
                .From(table).Done();

            var result = await expr
                .QueryList(context.Database, r =>
                {
                    var allColumnTypesDto = mapper.Map<IDataRecord, AllColumnTypesDto>(r);

                    allColumnTypesDto.ColByteArrayBig = StreamToByteArray(table.ColByteArrayBig.GetStream(r));
                    allColumnTypesDto.ColByteArraySmall = table.ColByteArraySmall.Read(r);
                    allColumnTypesDto.ColNullableByteArrayBig = table.ColNullableByteArrayBig.Read(r);
                    allColumnTypesDto.ColNullableByteArraySmall = table.ColNullableByteArraySmall.Read(r);
                    allColumnTypesDto.ColFixedSizeByteArray = table.ColFixedSizeByteArray?.Read(r) ?? Array.Empty<byte>();
                    allColumnTypesDto.ColNullableFixedSizeByteArray = table.ColNullableFixedSizeByteArray?.Read(r);
                    if (!ReferenceEquals(table.ColDateTimeOffset, null))
                    {
                        allColumnTypesDto.ColDateTimeOffset = table.ColDateTimeOffset.Read(r);
                    }

                    if (!ReferenceEquals(table.ColNullableDateTimeOffset, null))
                    {
                        allColumnTypesDto.ColNullableDateTimeOffset = table.ColNullableDateTimeOffset.Read(r);
                    }

                    return allColumnTypesDto;
                });

            static byte[] StreamToByteArray(Stream stream)
            {
                var buffer = new byte[stream.Length];

                using MemoryStream ms = new MemoryStream(buffer);

                stream.CopyTo(ms);

                var result = buffer;

                stream.Dispose();

                return result;
            }

            for (int i = 0; i < testData.Length; i++)
            {
                if (!Equals(testData[i], result[i]))
                {
                    var props = typeof(AllColumnTypesDto).GetProperties();
                    foreach (var propertyInfo in props)
                    {
                        context.WriteLine($"{propertyInfo.Name}: {PrintObjectValue(propertyInfo.GetValue(testData[i]))} - {PrintObjectValue(propertyInfo.GetValue(result[i]))}");
                    }

                    throw new Exception("Input and output are not identical!");
                }
            }

            static string PrintObjectValue(object? obj)
            {
                if (obj == null)
                {
                    return "NULL";
                }
                if (obj is IEnumerable list)
                {
                    return $"[{string.Join(',', list.OfType<object>().Select(PrintObjectValue).Take(10))}]";
                }
                return obj.ToString() ?? "NULL";
            }

            if (context.Dialect == SqlDialect.TSql)
            {
                var data = await Select(AllTypes.GetColumns(table))
                    .From(table)
                    .QueryList(context.Database, (r) => AllTypes.Read(r, table));

                if (data.Count != 2)
                {
                    throw new Exception("Incorrect reading using models");
                }

                await InsertDataInto(table, data)
                    .MapData(AllTypes.GetMapping)
                    .AlsoInsert(m=>m
                        .Set(m.Target.ColByte!, 0)
                        .Set(m.Target.ColDateTimeOffset!, new DateTimeOffset(new DateTime(2022, 07, 10, 18, 10, 45), TimeSpan.FromHours(3)))
                        .Set(m.Target.ColFixedSizeByteArray!, new byte[]{255, 0}))
                    .Exec(context.Database);
            }


            Console.WriteLine("'All Column Type Test' is passed");
        }

        private static IRecordSetterNext Mapping(IDataMapSetter<TableItAllColumnTypes, AllColumnTypesDto> s)
        {
            var recordSetterNext = s
                .Set(s.Target.ColBoolean, s.Source.ColBoolean)
                .Set(s.Target.ColNullableBoolean, s.Source.ColNullableBoolean);

            if (!ReferenceEquals(s.Target.ColByte, null))
            {
                recordSetterNext = recordSetterNext.Set(s.Target.ColByte, s.Source.ColByte);
            }

            if (!ReferenceEquals(s.Target.ColNullableByte, null))
            {
                recordSetterNext = recordSetterNext.Set(s.Target.ColNullableByte, s.Source.ColNullableByte);
            }

            if (!ReferenceEquals(s.Target.ColDateTimeOffset, null))
            {
                recordSetterNext = recordSetterNext.Set(s.Target.ColDateTimeOffset, s.Source.ColDateTimeOffset);
            }

            if (!ReferenceEquals(s.Target.ColNullableDateTimeOffset, null))
            {
                recordSetterNext = recordSetterNext.Set(s.Target.ColNullableDateTimeOffset, s.Source.ColNullableDateTimeOffset);
            }

            if (!ReferenceEquals(s.Target.ColFixedSizeByteArray, null))
            {
                recordSetterNext = recordSetterNext.Set(s.Target.ColFixedSizeByteArray, s.Source.ColFixedSizeByteArray);
            }

            if (!ReferenceEquals(s.Target.ColNullableFixedSizeByteArray, null))
            {
                recordSetterNext = recordSetterNext.Set(s.Target.ColNullableFixedSizeByteArray, s.Source.ColNullableFixedSizeByteArray);
            }
         
            recordSetterNext = recordSetterNext
                .Set(s.Target.ColInt16, s.Source.ColInt16)
                .Set(s.Target.ColNullableInt16, s.Source.ColNullableInt16)
                .Set(s.Target.ColInt32, s.Source.ColInt32)
                .Set(s.Target.ColNullableInt32, s.Source.ColNullableInt32)
                .Set(s.Target.ColInt64, s.Source.ColInt64)
                .Set(s.Target.ColNullableInt64, s.Source.ColNullableInt64)
                .Set(s.Target.ColDecimal, s.Source.ColDecimal)
                .Set(s.Target.ColNullableDecimal, s.Source.ColNullableDecimal)
                .Set(s.Target.ColDouble, s.Source.ColDouble)
                .Set(s.Target.ColNullableDouble, s.Source.ColNullableDouble)
                .Set(s.Target.ColDateTime, s.Source.ColDateTime)
                .Set(s.Target.ColNullableDateTime, s.Source.ColNullableDateTime)
                .Set(s.Target.ColGuid, s.Source.ColGuid)
                .Set(s.Target.ColNullableGuid, s.Source.ColNullableGuid)

                .Set(s.Target.ColStringMax, s.Source.ColStringMax)
                .Set(s.Target.ColNullableStringMax, s.Source.ColNullableStringMax)

                .Set(s.Target.ColStringUnicode, s.Source.ColStringUnicode)
                .Set(s.Target.ColNullableStringUnicode, s.Source.ColNullableStringUnicode)
                .Set(s.Target.ColString5, s.Source.ColString5)
                .Set(s.Target.ColByteArraySmall, s.Source.ColByteArraySmall)
                .Set(s.Target.ColNullableByteArraySmall, s.Source.ColNullableByteArraySmall)
                .Set(s.Target.ColByteArrayBig, s.Source.ColByteArrayBig)
                .Set(s.Target.ColNullableByteArrayBig, s.Source.ColNullableByteArrayBig)

                .Set(s.Target.ColFixedSizeString, s.Source.ColFixedSizeString)
                .Set(s.Target.ColNullableFixedSizeString, s.Source.ColNullableFixedSizeString)

                .Set(s.Target.ColXml, s.Source.ColXml)
                .Set(s.Target.ColNullableXml, s.Source.ColNullableXml)
                ;

            return recordSetterNext;
        }

        private static AllColumnTypesDto[] GetTestData(SqlDialect dialect)
        {
            byte[] GenerateTestArray(int shift, int size)
            {
                byte[] result = new byte[size];
                for(int i = 0; i< size; i++)
                {
                    unchecked
                    {
                        result[i] = (byte)(i + shift);
                    }
                }

                return result;
            }

            var result = new[]
            {
                new AllColumnTypesDto
                {
                    ColBoolean = true,
                    ColByte = dialect == SqlDialect.PgSql ? default : byte.MaxValue,
                    ColDateTime = new DateTime(2020, 10, 13),
                    ColDecimal = 2.123456m,
                    ColDouble = 2.123456,
                    ColGuid = Guid.Parse("e580d8df-78ed-4add-ac20-4c32bc8d94fc"),
                    ColInt16 = short.MaxValue,
                    ColInt32 = int.MaxValue,
                    ColInt64 = long.MaxValue,
                    ColStringMax = "abcdef",
                    ColStringUnicode = "\u0430\u0431\u0441\u0434\u0435\u0444",
                    ColString5 = "abcd",
                    ColByteArraySmall = GenerateTestArray(3, 255),
                    ColByteArrayBig = GenerateTestArray(29, 65535*2),

                    ColNullableBoolean = true,
                    ColNullableByte = dialect == SqlDialect.PgSql ? (byte?)null : byte.MaxValue,
                    ColNullableDateTime = new DateTime(2020, 10, 13),
                    ColNullableDecimal = 2.123456m,
                    ColNullableDouble = 2.123456,
                    ColNullableGuid = Guid.Parse("e580d8df-78ed-4add-ac20-4c32bc8d94fc"),
                    ColNullableInt16 = short.MaxValue,
                    ColNullableInt32 = int.MaxValue,
                    ColNullableInt64 = long.MaxValue,
                    ColNullableStringMax = "abcdef",
                    ColNullableStringUnicode = "\u0430\u0431\u0441\u0434\u0435\u0444",
                    ColNullableByteArraySmall = GenerateTestArray(17, 255),
                    ColNullableByteArrayBig = GenerateTestArray(17, 65535*2),

                    ColFixedSizeByteArray = dialect == SqlDialect.PgSql ? Array.Empty<byte>() : new byte[]{255, 0},
                    ColFixedSizeString = "123",

                    ColNullableFixedSizeByteArray = dialect == SqlDialect.PgSql ? null : new byte[]{255, 0},
                    ColNullableFixedSizeString = "321",

                    ColXml = "<root><Item2 /></root>",
                    ColNullableXml = "<root><Item /></root>",

                    ColDateTimeOffset = dialect == SqlDialect.MySql ? default : new DateTimeOffset(new DateTime(2022, 07, 10, 18, 10, 45), TimeSpan.FromHours(3)),
                    ColNullableDateTimeOffset = dialect == SqlDialect.MySql ? (DateTimeOffset?)null : new DateTimeOffset(new DateTime(2022, 07, 10, 18, 10, 46), TimeSpan.FromHours(3))
                },

                new AllColumnTypesDto
                {
                    ColBoolean = false,
                    ColByte = dialect == SqlDialect.PgSql ? default : byte.MinValue,
                    ColDateTime = new DateTime(2020, 10, 14),
                    ColDecimal = -2.123456m,
                    ColDouble = -2.123456,
                    ColGuid = Guid.Parse("0CFF587D-2A78-4891-83F6-5EE291221DFC"),
                    ColInt16 = short.MinValue,
                    ColInt32 = int.MinValue,
                    ColInt64 = long.MinValue,
                    ColStringMax = "",
                    ColStringUnicode = "",
                    ColString5 = "",
                    ColByteArraySmall = GenerateTestArray(7, 13),
                    ColByteArrayBig = GenerateTestArray(13, 17),
                    ColXml = "<root><Item3 /></root>",

                    ColNullableBoolean = null,
                    ColNullableByte = null,
                    ColNullableDateTime = null,
                    ColNullableDecimal = null,
                    ColNullableDouble = null,
                    ColNullableGuid = null,
                    ColNullableInt16 = null,
                    ColNullableInt32 = null,
                    ColNullableInt64 = null,
                    ColNullableStringMax = null,
                    ColNullableStringUnicode = null,
                    ColNullableByteArraySmall = null,
                    ColNullableByteArrayBig = null,


                    ColFixedSizeByteArray = dialect == SqlDialect.PgSql ? Array.Empty<byte>(): new byte[]{128, 128},
                    ColFixedSizeString = "abc",

                    ColNullableFixedSizeByteArray = null,
                    ColNullableFixedSizeString = null,
                    ColNullableXml = null,

                    ColDateTimeOffset = dialect == SqlDialect.MySql ? default : new DateTimeOffset(new DateTime(2022, 07, 10, 18, 10, 45), TimeSpan.FromHours(3)),

                    ColNullableDateTimeOffset = null
                }
            };
            return result;
        }

        public class AllColumnTypesDto : IEquatable<AllColumnTypesDto>
        {
            public int Id { get; set; }

            public string? ColNullableStringMax { get; set; }

            public string ColString5 { get; set; } = string.Empty;

            public string ColStringMax { get; set; } = string.Empty;

            public string? ColNullableStringUnicode { get; set; }

            public string ColStringUnicode { get; set; } = string.Empty;

            public Guid? ColNullableGuid { get; set; }

            public Guid ColGuid { get; set; }

            public DateTime? ColNullableDateTime { get; set; }

            public DateTime ColDateTime { get; set; }

            public double? ColNullableDouble { get; set; }

            public double ColDouble { get; set; }

            public decimal? ColNullableDecimal { get; set; }

            public decimal ColDecimal { get; set; }

            public long? ColNullableInt64 { get; set; }

            public long ColInt64 { get; set; }

            public int? ColNullableInt32 { get; set; }

            public int ColInt32 { get; set; }

            public short? ColNullableInt16 { get; set; }

            public short ColInt16 { get; set; }

            public byte? ColNullableByte { get; set; }

            public byte ColByte { get; set; }

            public bool? ColNullableBoolean { get; set; }

            public bool ColBoolean { get; set; }

            public byte[] ColByteArraySmall { get; set; } = new byte[0];

            public byte[]? ColNullableByteArraySmall { get; set; }

            public byte[] ColByteArrayBig { get; set; } = new byte[0];

            public byte[]? ColNullableByteArrayBig { get; set; }

            public byte[]? ColNullableFixedSizeByteArray { get; set; }
            public byte[] ColFixedSizeByteArray { get; set; } = new byte[0];

            public string? ColNullableFixedSizeString { get; set; }
            public string ColFixedSizeString { get; set; } = "";

            public string ColXml { get; set; } = "";
            public string? ColNullableXml { get; set; }

            public DateTimeOffset ColDateTimeOffset { get; set; }
            public DateTimeOffset? ColNullableDateTimeOffset { get; set; }

            public bool Equals(AllColumnTypesDto? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return this.ColNullableStringMax == other.ColNullableStringMax &&
                       this.ColString5 == other.ColString5 &&
                       this.ColStringMax == other.ColStringMax &&
                       this.ColNullableStringUnicode == other.ColNullableStringUnicode &&
                       this.ColStringUnicode == other.ColStringUnicode &&
                       Nullable.Equals(this.ColNullableGuid, other.ColNullableGuid) &&
                       this.ColGuid.Equals(other.ColGuid) &&
                       Nullable.Equals(this.ColNullableDateTime, other.ColNullableDateTime) &&
                       this.ColDateTime.Equals(other.ColDateTime) &&
                       Nullable.Equals(this.ColNullableDouble, other.ColNullableDouble) &&
                       this.ColDouble.Equals(other.ColDouble) &&
                       this.ColNullableDecimal == other.ColNullableDecimal &&
                       this.ColDecimal == other.ColDecimal &&
                       this.ColNullableInt64 == other.ColNullableInt64 &&
                       this.ColInt64 == other.ColInt64 &&
                       this.ColNullableInt32 == other.ColNullableInt32 &&
                       this.ColInt32 == other.ColInt32 &&
                       this.ColNullableInt16 == other.ColNullableInt16 &&
                       this.ColInt16 == other.ColInt16 &&
                       this.ColNullableByte == other.ColNullableByte &&
                       this.ColByte == other.ColByte &&
                       this.ColNullableBoolean == other.ColNullableBoolean &&
                       this.ColBoolean == other.ColBoolean &&
                       this.ColXml == other.ColXml &&
                       this.ColNullableXml == other.ColNullableXml &&
                       CompareArrays(this.ColByteArrayBig, other.ColByteArrayBig) &&
                       CompareArrays(this.ColByteArraySmall, other.ColByteArraySmall) &&
                       CompareArrays(this.ColNullableByteArrayBig, other.ColNullableByteArrayBig) &&
                       CompareArrays(this.ColNullableByteArraySmall, other.ColNullableByteArraySmall) &&

                       this.ColNullableFixedSizeString == other.ColNullableFixedSizeString &&
                       this.ColFixedSizeString == other.ColFixedSizeString &&

                       CompareArrays(this.ColNullableFixedSizeByteArray, other.ColNullableFixedSizeByteArray) &&
                       CompareArrays(this.ColFixedSizeByteArray, other.ColFixedSizeByteArray) &&

                       this.ColDateTimeOffset == other.ColDateTimeOffset &&
                       this.ColNullableDateTimeOffset == other.ColNullableDateTimeOffset
                       ;


                static bool CompareArrays(byte[]? arr1, byte[]? arr2)
                {
                    if (arr1 == arr2)
                    {
                        return true;
                    }

                    if (arr1 == null || arr2 == null)
                    {
                        return false;
                    }

                    if (arr1.Length != arr2.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < arr1.Length; i++)
                    {
                        if (arr1[i] != arr2[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((AllColumnTypesDto) obj);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(this.ColNullableStringMax);
                hashCode.Add(this.ColString5);
                hashCode.Add(this.ColStringMax);
                hashCode.Add(this.ColNullableStringUnicode);
                hashCode.Add(this.ColStringUnicode);
                hashCode.Add(this.ColNullableGuid);
                hashCode.Add(this.ColGuid);
                hashCode.Add(this.ColNullableDateTime);
                hashCode.Add(this.ColDateTime);
                hashCode.Add(this.ColNullableDouble);
                hashCode.Add(this.ColDouble);
                hashCode.Add(this.ColNullableDecimal);
                hashCode.Add(this.ColDecimal);
                hashCode.Add(this.ColNullableInt64);
                hashCode.Add(this.ColInt64);
                hashCode.Add(this.ColNullableInt32);
                hashCode.Add(this.ColInt32);
                hashCode.Add(this.ColNullableInt16);
                hashCode.Add(this.ColInt16);
                hashCode.Add(this.ColNullableByte);
                hashCode.Add(this.ColByte);
                hashCode.Add(this.ColNullableBoolean);
                hashCode.Add(this.ColBoolean);
                hashCode.Add(this.ColByteArrayBig);
                hashCode.Add(this.ColByteArraySmall);
                hashCode.Add(this.ColNullableByteArrayBig);
                hashCode.Add(this.ColNullableByteArraySmall);

                hashCode.Add(this.ColNullableFixedSizeString);
                hashCode.Add(this.ColFixedSizeString);
                hashCode.Add(this.ColNullableFixedSizeByteArray);
                hashCode.Add(this.ColFixedSizeByteArray);

                hashCode.Add(this.ColXml);
                hashCode.Add(this.ColNullableXml);

                hashCode.Add(this.ColDateTimeOffset);
                hashCode.Add(this.ColNullableDateTimeOffset);
                return hashCode.ToHashCode();
            }
        }
    }
}