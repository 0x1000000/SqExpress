using System;
using NUnit.Framework;
using SqExpress.SqlExport;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Export
{
    [TestFixture]
    public class SqlFormattingTest
    {
        private static readonly SqlBuilderOptions FormattedOptions = SqlBuilderOptions.Default
            .WithFormatting(SqlFormattingProfile.Spacious);

        [Test]
        public void SpaciousFormatsQueryStructure()
        {
            var user = Tables.User("U");
            var other = Tables.User("U2");
            var query = Select(user.UserId, user.FirstName)
                .From(user)
                .InnerJoin(other, other.UserId == user.UserId)
                .Where(user.UserId == 1 & user.FirstName == "First")
                .OrderBy(user.UserId)
                .Done();

            var nl = Environment.NewLine;
            var expected = "SELECT" + nl
                + "    [U].[UserId]," + nl
                + "    [U].[FirstName]" + nl
                + "FROM [dbo].[user]" + nl
                + "    [U]" + nl
                + "JOIN [dbo].[user]" + nl
                + "    [U2] ON" + nl
                + "    [U2].[UserId]=[U].[UserId]" + nl
                + "WHERE" + nl
                + "    [U].[UserId]=1" + nl
                + "    AND" + nl
                + "    [U].[FirstName]='First'" + nl
                + "ORDER BY" + nl
                + "    [U].[UserId]";

            Assert.AreEqual(expected, query.ToSql(new TSqlExporter(FormattedOptions)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void SpaciousFormatsEveryDialect(int dialect)
        {
            var query = Select(Literal(1), Literal(2)).Done();
            ISqlExporter exporter = dialect switch
            {
                0 => new TSqlExporter(FormattedOptions),
                1 => new PgSqlExporter(FormattedOptions),
                2 => new MySqlExporter(FormattedOptions, MySqlFlavor.MariaDb),
                _ => new SqliteExporter(FormattedOptions)
            };

            var nl = Environment.NewLine;
            Assert.AreEqual("SELECT" + nl + "    1," + nl + "    2", query.ToSql(exporter));
        }

        [Test]
        public void CompactOutputIsUnchanged()
        {
            var query = Select(Literal(1), Literal(2)).Done();

            Assert.AreEqual("SELECT 1,2", query.ToSql(TSqlExporter.Default));
            Assert.AreEqual("SELECT 1,2", query.ToSql(new TSqlExporter(SqlBuilderOptions.Default.WithFormatting(null))));
        }

        [Test]
        public void ReusedColumnReceivesTriviaPerRenderLocation()
        {
            var user = Tables.User("U");
            var query = Select(user.UserId, user.UserId)
                .From(user)
                .Where(user.UserId == 1)
                .OrderBy(user.UserId)
                .Done();

            var sql = query.ToSql(new TSqlExporter(FormattedOptions));

            Assert.That(sql, Does.Contain("    [U].[UserId]," + Environment.NewLine + "    [U].[UserId]"));
            Assert.That(sql, Does.Contain("WHERE" + Environment.NewLine + "    [U].[UserId]=1"));
            Assert.That(sql, Does.EndWith("ORDER BY" + Environment.NewLine + "    [U].[UserId]"));
        }

        [Test]
        public void SpaciousHasNoTrailingWhitespaceOrFinalNewline()
        {
            var sql = Select(Literal(1), Literal(2)).Done().ToSql(new TSqlExporter(FormattedOptions));

            Assert.That(sql, Does.Not.EndWith(Environment.NewLine));
            foreach (var line in sql.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                Assert.That(line, Is.EqualTo(line.TrimEnd()));
            }
        }

        [Test]
        public void SpaciousFormatsUpdateAndDelete()
        {
            var user = Tables.User(Alias.Empty);
            var exporter = new TSqlExporter(FormattedOptions);
            var nl = Environment.NewLine;

            var update = Update(user)
                .Set(user.FirstName, "First")
                .Set(user.LastName, "Last")
                .Where(user.UserId.In(1, 2))
                .ToSql(exporter);

            Assert.AreEqual(
                "UPDATE [dbo].[user]" + nl
                + "SET" + nl
                + "    [FirstName]='First'," + nl
                + "    [LastName]='Last'" + nl
                + "WHERE" + nl
                + "    [UserId] IN(1,2)",
                update);

            var delete = Delete(user).Where(user.UserId.In(1, 2)).ToSql(exporter);
            Assert.AreEqual(
                "DELETE [dbo].[user]" + nl
                + "WHERE" + nl
                + "    [UserId] IN(1,2)",
                delete);
        }

        [Test]
        public void SpaciousFormatsInsertRowsButKeepsRowValuesCompact()
        {
            var user = Tables.User(Alias.Empty);
            var insert = InsertInto(user, user.FirstName, user.LastName)
                .Values("First", "Last")
                .Values("Second", "User")
                .DoneWithValues();

            var nl = Environment.NewLine;
            Assert.AreEqual(
                "INSERT INTO [dbo].[user]([FirstName],[LastName])" + nl
                + "VALUES" + nl
                + "    ('First','Last')," + nl
                + "    ('Second','User')",
                insert.ToSql(new TSqlExporter(FormattedOptions)));
        }

        [Test]
        public void SpaciousFormatsSetOperators()
        {
            var query = Select(Literal(1)).UnionAll(Select(Literal(2))).Done();
            var nl = Environment.NewLine;

            Assert.AreEqual(
                "SELECT" + nl
                + "    1" + nl
                + "UNION ALL" + nl
                + "SELECT" + nl
                + "    2",
                query.ToSql(new TSqlExporter(FormattedOptions)));
        }

        [Test]
        public void SpaciousFormatsOnlyExporterRequiredBooleanParentheses()
        {
            var user = Tables.User(Alias.Empty);
            var filter = (user.UserId == 1 | user.UserId == 2) & user.UserId == 3;
            var query = Select(user.UserId).From(user).Where(filter).Done();
            var nl = Environment.NewLine;

            Assert.AreEqual(
                "SELECT" + nl
                + "    [UserId]" + nl
                + "FROM [dbo].[user]" + nl
                + "WHERE" + nl
                + "    (" + nl
                + "        [UserId]=1" + nl
                + "        OR" + nl
                + "        [UserId]=2" + nl
                + "    )" + nl
                + "    AND" + nl
                + "    [UserId]=3",
                query.ToSql(new TSqlExporter(FormattedOptions)));
        }

        [Test]
        public void FormattingPreservesOtherBuilderOptions()
        {
            var options = SqlBuilderOptions.Default
                .WithAvoidQuoteName(true)
                .WithFormatting(SqlFormattingProfile.Spacious);

            Assert.That(options.AvoidNameQuoting, Is.True);
            Assert.That(options.FormattingProfile, Is.SameAs(SqlFormattingProfile.Spacious));
        }
    }
}
