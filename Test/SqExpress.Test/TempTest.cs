using System;
using NUnit.Framework;
using SqExpress.SqlParser;

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

