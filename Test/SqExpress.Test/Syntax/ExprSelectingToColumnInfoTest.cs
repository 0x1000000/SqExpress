using NUnit.Framework;
using SqExpress.Syntax;
using SqExpress.Syntax.Type;
using SqExpress.SyntaxTreeOperations.Internal;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Syntax;

[TestFixture]
public class ExprSelectingToColumnInfoTest
{
    [Test]
    public void AliasedLiteral_ProducesTypedCustomColumn()
    {
        var info = Literal(1).As("Id").Accept(ExprSelectingToColumnInfo.Instance, null);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.IsColumn, Is.True);
        Assert.That(info.AsColumn(), Is.TypeOf<Int32CustomColumn>());
        Assert.That(info.AsColumn().ColumnName.Name, Is.EqualTo("Id"));
    }

    [Test]
    public void AliasedCast_ProducesNullableTypedCustomColumn()
    {
        var info = Cast(Literal(1), SqlType.Int32).As("Id").Accept(ExprSelectingToColumnInfo.Instance, null);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.IsColumn, Is.True);
        Assert.That(info.AsColumn(), Is.TypeOf<NullableInt32CustomColumn>());
        Assert.That(info.AsColumn().ColumnName.Name, Is.EqualTo("Id"));
    }

    [Test]
    public void AliasedXmlCast_ProducesStringCustomColumn()
    {
        var info = Cast(Literal("<x/>"), ExprTypeXml.Instance).As("Payload").Accept(ExprSelectingToColumnInfo.Instance, null);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.IsColumn, Is.True);
        Assert.That(info.AsColumn(), Is.TypeOf<NullableStringCustomColumn>());
        Assert.That(info.AsColumn().ColumnName.Name, Is.EqualTo("Payload"));
    }
}
