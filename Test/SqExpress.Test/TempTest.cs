using NUnit.Framework;
using SqExpress.SyntaxTreeOperations.Internal;

namespace SqExpress.Test;

public class TempTest
{
    [Test]
    public void Test()
    {
        var u = new User();

        var q = SqQueryBuilder.Select(SqQueryBuilder.Literal(1).As("AAAA"), u.FirstName).From(u).Done();

        var a =  q.ExtractSelecting();

        foreach (var exprSelecting in a)
        {
            var c = exprSelecting.Accept(ExprSelectingToColumnInfo.Instance, null);

            
        }

        Assert.That(a.Count, Is.EqualTo(2));
    }
}
