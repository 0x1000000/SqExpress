using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations.Internal;

namespace SqExpress.Test;

public class TempTest
{
    [Test]
    public void Test()
    {
        var u = new User();

        var sql = SqQueryBuilder
            .Select(SqQueryBuilder.Sum(SqQueryBuilder.AsValue(SqQueryBuilder.Sum(u.UserId))).Over())
            .From(u);
        
       Console.WriteLine(sql);
    }
}

