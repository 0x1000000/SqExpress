using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations.Internal;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test;

public class TempTest
{
    [Test]
    public void Test()
    {
        var tUser = new User();

        var source = Select(
                Literal(1),
                Literal("AA").As("BB"),
                tUser.UserId,
                GetUtcDate())
            .From(tUser)
            .Where((Literal(1) == Literal(1)) & tUser.UserId.In(1, 2))
            .Done()
            .As(TableAlias("S"));

        var sql = MergeInto(tUser, source)
            .On(tUser.UserId == tUser.UserId.WithSource(source.Alias))
            .WhenMatched().ThenUpdate().Set(tUser.FirstName, "AA")
            .Done()
            .ToSql();

        Console.WriteLine(sql);
    }
}

