using System;
using NUnit.Framework;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.SyntaxTreeOperations.Internal;
using SqExpress.Utils;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Syntax;

[TestFixture]
public class CustomColumnTypePreservationTest
{
    [Test]
    public void ToCustomColumn_PreservesMatchingKindsAndDetailedTypes()
    {
        var table = new ExtraTypesTable();

        var byteArray = table.Blob.ToCustomColumn(null);
        Assert.That(byteArray, Is.TypeOf<ByteArrayCustomColumn>());
        Assert.That(((TypedColumn)byteArray).IsNullable, Is.False);
        Assert.That(((ByteArrayCustomColumn)byteArray).SqlType, Is.TypeOf<ExprTypeFixSizeByteArray>());

        var dateTimeOffset = table.UpdatedAt.ToCustomColumn(null);
        Assert.That(dateTimeOffset, Is.TypeOf<DateTimeOffsetCustomColumn>());
        Assert.That(((TypedColumn)dateTimeOffset).IsNullable, Is.False);

        var date = table.CreatedOn.ToCustomColumn(null);
        Assert.That(date, Is.TypeOf<DateTimeCustomColumn>());
        Assert.That(((TypedColumn)date).IsNullable, Is.False);
        Assert.That(((DateTimeCustomColumn)date).SqlType.IsDate, Is.True);

        var xml = table.Payload.ToCustomColumn(null);
        Assert.That(xml, Is.TypeOf<StringCustomColumn>());
        Assert.That(((TypedColumn)xml).IsNullable, Is.False);
        Assert.That(((StringCustomColumn)xml).SqlType, Is.TypeOf<ExprTypeXml>());

        var amount = table.Amount.ToCustomColumn(null);
        Assert.That(amount, Is.TypeOf<DecimalCustomColumn>());
        Assert.That(((TypedColumn)amount).IsNullable, Is.False);
        Assert.That(((DecimalCustomColumn)amount).SqlType.PrecisionScale, Is.EqualTo(new DecimalPrecisionScale(10, 2)));
    }

    [Test]
    public void AliasedCasts_PreserveDetailedTypesInTempTableMaterialization()
    {
        var source = Select(
                Cast(Literal(new byte[] { 1, 2 }), SqlType.ByteArrayFixedSize(4)).As("Blob"),
                Cast(Literal(new DateTime(2024, 1, 2)), SqlType.DateTime(isDate: true)).As("CreatedOn"),
                Cast(Literal(12.8m), SqlType.Decimal(new DecimalPrecisionScale(10, 2))).As("Amount"),
                Cast(Literal("AB"), new ExprTypeFixSizeString(5, true)).As("Code"),
                Cast(Literal("<x/>"), ExprTypeXml.Instance).As("Payload"))
            .Done()
            .As(TableAlias("S"));

        var temp = TempTableData.FromTableSource(source, null);

        Assert.That(temp.Columns[0], Is.TypeOf<NullableByteArrayTableColumn>());
        Assert.That(((NullableByteArrayTableColumn)temp.Columns[0]).SqlType, Is.TypeOf<ExprTypeFixSizeByteArray>());

        Assert.That(temp.Columns[1], Is.TypeOf<NullableDateTimeTableColumn>());
        Assert.That(((NullableDateTimeTableColumn)temp.Columns[1]).IsDate, Is.True);

        Assert.That(temp.Columns[2], Is.TypeOf<NullableDecimalTableColumn>());
        Assert.That(((NullableDecimalTableColumn)temp.Columns[2]).PrecisionScale, Is.EqualTo(new DecimalPrecisionScale(10, 2)));

        Assert.That(temp.Columns[3], Is.TypeOf<NullableStringTableColumn>());
        Assert.That(((NullableStringTableColumn)temp.Columns[3]).SqlType, Is.TypeOf<ExprTypeFixSizeString>());

        Assert.That(temp.Columns[4], Is.TypeOf<NullableStringTableColumn>());
        Assert.That(((NullableStringTableColumn)temp.Columns[4]).SqlType, Is.TypeOf<ExprTypeXml>());
    }

    [Test]
    public void AliasedLiteral_ProducesCustomColumnWithSqlTypeAndNullability()
    {
        var info = Literal(1).As("Id").Accept(ExprSelectingToColumnInfo.Instance, null);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.AsColumn(), Is.TypeOf<Int32CustomColumn>());
        Assert.That(((TypedColumn)info.AsColumn()).SqlType, Is.TypeOf<ExprTypeInt32>());
        Assert.That(((TypedColumn)info.AsColumn()).IsNullable, Is.False);
    }

    private sealed class ExtraTypesTable : TableBase
    {
        public ExtraTypesTable(Alias alias = default) : base("dbo", "ExtraTypes", alias)
        {
            this.Blob = this.CreateFixedSizeByteArrayColumn(nameof(this.Blob), 4);
            this.UpdatedAt = this.CreateDateTimeOffsetColumn(nameof(this.UpdatedAt));
            this.CreatedOn = this.CreateDateTimeColumn(nameof(this.CreatedOn), isDate: true);
            this.Payload = this.CreateXmlColumn(nameof(this.Payload));
            this.Amount = this.CreateDecimalColumn(nameof(this.Amount), new DecimalPrecisionScale(10, 2));
        }

        public ByteArrayTableColumn Blob { get; }

        public DateTimeOffsetTableColumn UpdatedAt { get; }

        public DateTimeTableColumn CreatedOn { get; }

        public StringTableColumn Payload { get; }

        public DecimalTableColumn Amount { get; }
    }
}
