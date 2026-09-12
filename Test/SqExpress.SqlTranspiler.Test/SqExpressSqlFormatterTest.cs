using System;
using NUnit.Framework;

namespace SqExpress.SqlTranspiler.Test;

[TestFixture]
public class SqExpressSqlFormatterTest
{
    [Test]
    public void Format_BasicSelect_FormatsAndKeepsSqlValid()
    {
        var formatter = new SqExpressSqlFormatter();
        var transpiler = new SqExpressSqlTranspiler();

        var formatted = formatter.Format("select u.UserId,u.Name from dbo.Users u where u.IsActive=1 order by u.Name desc");

        Assert.That(formatted, Does.StartWith("SELECT" + Environment.NewLine + "    [u].[UserId],"));
        Assert.That(formatted, Does.Contain(Environment.NewLine + "FROM [dbo].[Users]"));
        Assert.That(formatted, Does.Contain(Environment.NewLine + "WHERE" + Environment.NewLine + "    [u].[IsActive]=1"));
        Assert.That(formatted, Does.Contain(Environment.NewLine + "ORDER BY" + Environment.NewLine + "    [u].[Name] DESC"));
        Assert.DoesNotThrow(() => transpiler.Transpile(formatted));
    }

    [Test]
    public void Format_InvalidSql_Throws()
    {
        var formatter = new SqExpressSqlFormatter();

        var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => formatter.Format("SELECT FROM"));

        Assert.That(ex?.Message, Does.Contain("Could not parse SQL"));
    }

    [Test]
    public void Format_Parameters_PreservesNamesIncludingOffsetFetch()
    {
        var formatter = new SqExpressSqlFormatter();

        var formatted = formatter.Format(
            "SELECT o.OrderId, o.CustomerId, o.TotalAmount, COUNT(1) OVER() AS TotalRows " +
            "FROM dbo.Orders o " +
            "WHERE o.OrderDate >= @fromDate " +
            "ORDER BY o.OrderDate DESC " +
            "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;");

        Assert.That(formatted, Does.Contain("@fromDate"));
        Assert.That(formatted, Does.Contain("OFFSET (@offset) ROW"));
        Assert.That(formatted, Does.Contain("FETCH NEXT (@pageSize) ROW ONLY"));
        Assert.That(formatted, Does.Not.Contain("Could not process pure parameter"));
    }

    [Test]
    public void Format_Merge_PreservesRequiredTerminatingSemicolon()
    {
        var formatter = new SqExpressSqlFormatter();

        var formatted = formatter.Format(
            "MERGE dbo.FeatureFlags AS trg " +
            "USING (VALUES " +
            "(1, 'BetaDashboard', 1), " +
            "(2, 'SmartSearch', 0), " +
            "(3, 'OpsMode', 1)) AS src(FlagId, FlagName, IsEnabled) " +
            "ON trg.FlagId = src.FlagId " +
            "WHEN MATCHED THEN " +
            "UPDATE SET trg.FlagName = src.FlagName, trg.IsEnabled = src.IsEnabled " +
            "WHEN NOT MATCHED BY TARGET THEN " +
            "INSERT (FlagId, FlagName, IsEnabled) VALUES (src.FlagId, src.FlagName, src.IsEnabled);");

        Assert.That(formatted, Does.StartWith("MERGE "));
        Assert.That(formatted, Does.EndWith(";"));
        Assert.That(formatted, Does.Not.Contain(";;"));
    }

    [Test]
    public void Format_SeveralStatements_FormatsWholeScript()
    {
        var formatter = new SqExpressSqlFormatter();

        var formatted = formatter.Format("select 1 as A; select 2 as B");

        Assert.That(formatted, Does.Contain("SELECT" + Environment.NewLine + "    1"));
        Assert.That(formatted, Does.Contain("[A]"));
        Assert.That(formatted, Does.Contain(";" + Environment.NewLine + "SELECT" + Environment.NewLine + "    2"));
        Assert.That(formatted, Does.Contain("[B]"));
    }
}