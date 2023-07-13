using System;
using NUnit.Framework;

namespace SqExpress.Test.QueryBuilder;

[TestFixture]
public class MergeTest
{
    [Test]
    public void ValueBuilder_ActualMerge()
    {
        var t = Tables.User();

        var data = new[] { new { Id = 1, FirstName = "Alice" }, new { Id = 2, FirstName = "Bob" } };

        var valueTable = SqQueryBuilder.ValueTable(data, s => s.Set(t.UserId, s.Item.Id).Set(t.FirstName, s.Item.FirstName));


        var expr = SqQueryBuilder
            .MergeInto(t, valueTable)
            .On(t.UserId == valueTable.Column(t.UserId))
            .WhenMatchedAnd(t.FirstName != valueTable.Column(t.FirstName))
                .ThenUpdate()
                    .Set(t.FirstName, valueTable.Column(t.FirstName))
                    .Set(t.Modified, SqQueryBuilder.GetUtcDate())
            .WhenNotMatchedByTarget()
            .ThenInsert()
                .Set(t.UserId, valueTable.Column(t.UserId))
                .Set(t.FirstName, valueTable.Column(t.FirstName))
                .Set(t.Modified, SqQueryBuilder.GetUtcDate())
                .Set(t.Created, SqQueryBuilder.GetUtcDate())
            .WhenNotMatchedBySource()
            .ThenUpdate()
                .Set(t.Modified, SqQueryBuilder.GetUtcDate())
            .Done();
        const string expected = "MERGE [dbo].[user] [A0] USING (VALUES (1,'Alice'),(2,'Bob'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId] WHEN MATCHED AND [A0].[FirstName]!=[A1].[FirstName] THEN UPDATE SET [A0].[FirstName]=[A1].[FirstName],[A0].[Modified]=GETUTCDATE() WHEN NOT MATCHED THEN INSERT([UserId],[FirstName],[Modified],[Created]) VALUES([A1].[UserId],[A1].[FirstName],GETUTCDATE(),GETUTCDATE()) WHEN NOT MATCHED BY SOURCE THEN UPDATE SET [A0].[Modified]=GETUTCDATE();";

        Assert.AreEqual(expected, expr.ToSql());

        Console.WriteLine(expr.ToMySql());
    }

    [Test]
    public void ValueBuilder_DeleteDelete()
    {
        var t = Tables.User();

        var data = new[] { new { Id = 1, FirstName = "Alice" }, new { Id = 2, FirstName = "Bob" } };

        var valueTable = SqQueryBuilder.ValueTable(data, s => s.Set(t.UserId, s.Item.Id).Set(t.FirstName, s.Item.FirstName));

        var sql = SqQueryBuilder
            .MergeInto(t, valueTable)
            .On(t.UserId == valueTable.Column(t.UserId))
            .WhenMatched()
                .ThenDelete()
            .WhenNotMatchedBySourceAnd(t.UserId > 100)
                .ThenDelete()
            .Done()
            .ToSql();
        const string expected = "MERGE [dbo].[user] [A0] USING (VALUES (1,'Alice'),(2,'Bob'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId] WHEN MATCHED THEN  DELETE WHEN NOT MATCHED BY SOURCE AND [A0].[UserId]>100 THEN  DELETE;";

        Assert.AreEqual(expected, sql);
    }

}