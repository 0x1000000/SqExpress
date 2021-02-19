using System;
using System.Data;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Data;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.QueryBuilders.RecordSetter;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    class CustomMapper: IValueConverter<IDataRecord, object>
    {
        public object Convert(IDataRecord sourceMember, ResolutionContext context)
        {
            throw new NotImplementedException();
        }
    }
    public class ScAllColumnTypes : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            bool isPostgres = context.Dialect == SqlDialect.PgSql;
            var table = new AllColumnTypes(isPostgres);
            await context.Database.Statement(table.Script.DropAndCreate());

            var testData = GetTestData(isPostgres);

            await InsertDataInto(table, testData)
                .MapData(Mapping)
                .Exec(context.Database);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.AddDataReaderMapping();
                var map = cfg.CreateMap<IDataRecord, AllColumnTypesDto>();

                if (isPostgres)
                {
                    map
                        .ForMember(nameof(table.ColByte), c => c.Ignore())
                        .ForMember(nameof(table.ColNullableByte), c => c.Ignore());
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

            var result = await Select(table.Columns)
                .From(table)
                .QueryList(context.Database, r => mapper.Map<IDataRecord, AllColumnTypesDto>(r));

            for (int i = 0; i < testData.Length; i++)
            {
                if (!Equals(testData[i], result[i]))
                {
                    var props = typeof(AllColumnTypesDto).GetProperties();
                    foreach (var propertyInfo in props)
                    {
                        context.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(testData[i])} - {propertyInfo.GetValue(result[i])}");
                    }

                    throw new Exception("Input and output are not identical!");
                }
            }

            Console.WriteLine("'All Column Type Test' is passed");
        }

        private static IRecordSetterNext Mapping(IDataMapSetter<AllColumnTypes, AllColumnTypesDto> s)
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
                .Set(s.Target.ColString5, s.Source.ColString5);

            return recordSetterNext;
        }

        private static AllColumnTypesDto[] GetTestData(bool isPostgres)
        {
            var result = new[]
            {
                new AllColumnTypesDto
                {
                    ColBoolean = true,
                    ColByte = isPostgres ? default : byte.MaxValue,
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

                    ColNullableBoolean = true,
                    ColNullableByte = isPostgres ? (byte?)null : byte.MaxValue,
                    ColNullableDateTime = new DateTime(2020, 10, 13),
                    ColNullableDecimal = 2.123456m,
                    ColNullableDouble = 2.123456,
                    ColNullableGuid = Guid.Parse("e580d8df-78ed-4add-ac20-4c32bc8d94fc"),
                    ColNullableInt16 = short.MaxValue,
                    ColNullableInt32 = int.MaxValue,
                    ColNullableInt64 = long.MaxValue,
                    ColNullableStringMax = "abcdef",
                    ColNullableStringUnicode = "\u0430\u0431\u0441\u0434\u0435\u0444"
                },

                new AllColumnTypesDto
                {
                    ColBoolean = false,
                    ColByte = isPostgres ? default : byte.MinValue,
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
                    ColNullableStringUnicode = null
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
                       this.ColBoolean == other.ColBoolean;
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
                return hashCode.ToHashCode();
            }
        }
    }
}