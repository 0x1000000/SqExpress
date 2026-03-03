using System;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScParametrizationTypes : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var table = new ParamTypesTable(context.Dialect);

            await table.Script.DropIfExist().Exec(context.Database);
            await table.Script.Create().Exec(context.Database);

            var guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var dt1 = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Unspecified);
            var dt2 = new DateTime(2024, 01, 03, 04, 05, 06, DateTimeKind.Unspecified);
            var dto1 = new DateTimeOffset(new DateTime(2024, 01, 02, 03, 04, 05), TimeSpan.FromHours(3));
            var dto2 = new DateTimeOffset(new DateTime(2024, 01, 03, 04, 05, 06), TimeSpan.FromHours(-5));

            if (context.Dialect == SqlDialect.MySql)
            {
                await InsertInto(
                        table,
                        table.Id,
                        table.GuidValue,
                        table.NullableGuidValue,
                        table.DateTimeValue,
                        table.NullableDateTimeValue
                    )
                    .Values(1, guid1, (Guid?)guid2, dt1, (DateTime?)dt2)
                    .Values(2, guid2, (Guid?)null, dt2, (DateTime?)null)
                    .DoneWithValues()
                    .Exec(context.Database);
            }
            else
            {
                await InsertInto(
                        table,
                        table.Id,
                        table.GuidValue,
                        table.NullableGuidValue,
                        table.DateTimeValue,
                        table.NullableDateTimeValue,
                        table.DateTimeOffsetValue!,
                        table.NullableDateTimeOffsetValue!
                    )
                    .Values(1, guid1, (Guid?)guid2, dt1, (DateTime?)dt2, dto1, (DateTimeOffset?)dto2)
                    .Values(2, guid2, (Guid?)null, dt2, (DateTime?)null, dto2, (DateTimeOffset?)null)
                    .DoneWithValues()
                    .Exec(context.Database);
            }

            var row1 = await SelectOneRow(context, table, 1);
            var row2 = await SelectOneRow(context, table, 2);

            if (row1.GuidValue != guid1 || row1.NullableGuidValue != guid2)
            {
                throw new Exception("GUID parameter mapping failed");
            }

            if (row1.DateTimeValue != dt1 || row1.NullableDateTimeValue != dt2)
            {
                throw new Exception("DateTime parameter mapping failed");
            }

            if (row2.GuidValue != guid2 || row2.NullableGuidValue != null)
            {
                throw new Exception("Nullable GUID parameter mapping failed");
            }

            if (row2.DateTimeValue != dt2 || row2.NullableDateTimeValue != null)
            {
                throw new Exception("Nullable DateTime parameter mapping failed");
            }

            if (context.Dialect != SqlDialect.MySql)
            {
                if (!row1.DateTimeOffsetValue.HasValue || !row1.NullableDateTimeOffsetValue.HasValue)
                {
                    throw new Exception("DateTimeOffset parameter mapping failed");
                }

                if (row1.DateTimeOffsetValue.Value.ToUniversalTime() != dto1.ToUniversalTime())
                {
                    throw new Exception("DateTimeOffset UTC instant mismatch");
                }

                if (row1.NullableDateTimeOffsetValue.Value.ToUniversalTime() != dto2.ToUniversalTime())
                {
                    throw new Exception("Nullable DateTimeOffset UTC instant mismatch");
                }

                if (!row2.DateTimeOffsetValue.HasValue || row2.DateTimeOffsetValue.Value.ToUniversalTime() != dto2.ToUniversalTime())
                {
                    throw new Exception("DateTimeOffset parameter mapping for second row failed");
                }

                if (row2.NullableDateTimeOffsetValue != null)
                {
                    throw new Exception("Nullable DateTimeOffset should be null");
                }
            }

            await table.Script.Drop().Exec(context.Database);
        }

        private static async Task<RowRead> SelectOneRow(IScenarioContext context, ParamTypesTable table, int id)
        {
            return await Select(table.AllColumns())
                .From(table)
                .Where(table.Id == id)
                .Query(context.Database, default(RowRead?), (acc, r) =>
                    new RowRead(
                        guidValue: table.GuidValue.Read(r),
                        nullableGuidValue: table.NullableGuidValue.Read(r),
                        dateTimeValue: table.DateTimeValue.Read(r),
                        nullableDateTimeValue: table.NullableDateTimeValue.Read(r),
                        dateTimeOffsetValue: table.DateTimeOffsetValue?.Read(r),
                        nullableDateTimeOffsetValue: table.NullableDateTimeOffsetValue?.Read(r)
                    )
                ) ?? throw new Exception($"Could not find row by Id={id}");
        }

        private readonly struct RowRead
        {
            public readonly Guid GuidValue;
            public readonly Guid? NullableGuidValue;
            public readonly DateTime DateTimeValue;
            public readonly DateTime? NullableDateTimeValue;
            public readonly DateTimeOffset? DateTimeOffsetValue;
            public readonly DateTimeOffset? NullableDateTimeOffsetValue;

            public RowRead(
                Guid guidValue,
                Guid? nullableGuidValue,
                DateTime dateTimeValue,
                DateTime? nullableDateTimeValue,
                DateTimeOffset? dateTimeOffsetValue,
                DateTimeOffset? nullableDateTimeOffsetValue)
            {
                this.GuidValue = guidValue;
                this.NullableGuidValue = nullableGuidValue;
                this.DateTimeValue = dateTimeValue;
                this.NullableDateTimeValue = nullableDateTimeValue;
                this.DateTimeOffsetValue = dateTimeOffsetValue;
                this.NullableDateTimeOffsetValue = nullableDateTimeOffsetValue;
            }
        }

        private class ParamTypesTable : TempTableBase
        {
            public ParamTypesTable(SqlDialect dialect, Alias alias = default) : base("ParamTypesProbe", alias)
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
                this.GuidValue = this.CreateGuidColumn("GuidValue");
                this.NullableGuidValue = this.CreateNullableGuidColumn("NullableGuidValue");
                this.DateTimeValue = this.CreateDateTimeColumn("DateTimeValue");
                this.NullableDateTimeValue = this.CreateNullableDateTimeColumn("NullableDateTimeValue");

                if (dialect != SqlDialect.MySql)
                {
                    this.DateTimeOffsetValue = this.CreateDateTimeOffsetColumn("DateTimeOffsetValue");
                    this.NullableDateTimeOffsetValue = this.CreateNullableDateTimeOffsetColumn("NullableDateTimeOffsetValue");
                }
            }

            public Int32TableColumn Id { get; }

            public GuidTableColumn GuidValue { get; }

            public NullableGuidTableColumn NullableGuidValue { get; }

            public DateTimeTableColumn DateTimeValue { get; }

            public NullableDateTimeTableColumn NullableDateTimeValue { get; }

            public DateTimeOffsetTableColumn? DateTimeOffsetValue { get; }

            public NullableDateTimeOffsetTableColumn? NullableDateTimeOffsetValue { get; }
        }
    }
}
