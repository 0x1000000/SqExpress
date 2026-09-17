#if NET
using System;
using System.Data.SqlClient;
using System.IO;
using CommandLine;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.DataAccess;
using SqExpress.SqlExport;

namespace SqExpress.Test.CodeGenUtil;

[TestFixture]
public class ViewDiscoveryOptionsTest
{
    [TestCase(false)]
    [TestCase(true)]
    public void IncludeViews_IsParsed(bool includeViews)
    {
        using var parser = new Parser(settings => { settings.HelpWriter = null; settings.CaseInsensitiveEnumValues = true; });
        var args = includeViews
            ? new[] { "gentables", "mssql", "fake", "--include-views", "--skip-unknown-column-types" }
            : new[] { "gentables", "mssql", "fake" };
        GenTablesOptions? parsed = null;
        parser.ParseArguments<GenTablesOptions, GenModelsOptions>(args).WithParsed<GenTablesOptions>(options => parsed = options);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.IncludeViews, Is.EqualTo(includeViews));
        Assert.That(parsed.SkipUnknownColumnTypes, Is.EqualTo(includeViews));
    }

    [Test]
    public void EfMode_RejectsViewsBeforeCreatingOutput()
    {
        var output = Path.Combine(Path.GetTempPath(), "sqexpress-views-" + Guid.NewGuid().ToString("N"));
        var options = new GenTablesOptions(ConnectionType.Ef, "missing.csproj", "Table", output,
            "Test.Tables", Verbosity.Quiet, false, false, "", "", false, false, null, null, true);
        var exception = Assert.ThrowsAsync<SqExpressCodeGenException>(() => Program.RunGenTablesOptions(options));
        Assert.That(exception!.Message, Does.Contain("--include-views is not supported in EF mode"));
        Assert.That(Directory.Exists(output), Is.False);
    }

    [Test]
    public void Defaults_ExcludeViewsAndRejectUnknownTypes()
    {
        var options = new SqGetTablesOptions();
        Assert.That(options.IncludeViews, Is.False);
        Assert.That(options.SkipUnknownColumnTypes, Is.False);
        var legacy = new GenTablesOptions(ConnectionType.MsSql, "fake", "Table", "", "Test.Tables", Verbosity.Quiet);
        Assert.That(legacy.IncludeViews, Is.False);
    }

    [Test]
    public void GetTables_NullOptionsAreRejectedBeforeOpeningConnection()
    {
        using var database = new SqDatabase<SqlConnection>(new SqlConnection(),
            (connection, sql) => new SqlCommand(sql, connection), TSqlExporter.Default, ParametrizationMode.None);
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => database.GetTables((SqGetTablesOptions)null!));
        Assert.That(exception!.ParamName, Is.EqualTo("options"));
    }
}
#endif
