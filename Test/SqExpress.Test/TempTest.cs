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

        var sql = SqTSqlParser.Parse("SELECT 'Hi,' + @userName + '!'", [tUser]).WithParams("userName", "Alice").ToSql();

        Console.WriteLine(sql);
    }
}

