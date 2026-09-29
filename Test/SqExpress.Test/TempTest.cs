using System;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlParser;

namespace SqExpress.Test;

public class TempTest
{
    [Test]
    public void Test()
    {
        var t1 = SqTable.Create("dbo", "T1", cb=>cb.AppendInt32Column("Id").AppendInt32Column("Value"), null, Alias.Empty);
        var t2 = SqTable.Create("dbo", "T2", cb=>cb.AppendInt32Column("Id").AppendInt32Column("Value"), null, Alias.Empty);
        
        
#pragma warning disable SQEX011
        var expr = SqTSqlParser.Parse("SELECT [T1].[Id], [T1].[Value] FROM [dbo].[T1] INNER JOIN [dbo].[T2] ON [T1].[Id] = [T2].[Id]",[t1, t2]);
#pragma warning restore SQEX011
        
        Assert.AreEqual("SELECT [T1].[Id],[T1].[Value] FROM [dbo].[T1] JOIN [dbo].[T2] ON [T1].[Id]=[T2].[Id]", expr.ToSql());
    }
}

