using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Export
{
    [TestFixture]
    public class SqlFormattingProfileTest
    {
        private static SqlFormattingProfile Spacious => SqlFormattingProfile.Spacious.WithOptions(
            new SqlFormattingOptions { NewLineStyle = SqlNewLineStyle.Lf });

        private static ISqlExporter Exporter(int dialect, SqlFormattingProfile? profile)
        {
            var options = SqlBuilderOptions.Default.WithFormatting(profile);
            return dialect switch
            {
                0 => new TSqlExporter(options),
                1 => new PgSqlExporter(options),
                2 => new MySqlExporter(options, MySqlFlavor.MariaDb),
                3 => new MySqlExporter(options, MySqlFlavor.Oracle),
                _ => new SqliteExporter(options)
            };
        }

        [Test]
        public void PatchesAreSnapshotsAndInheritOmittedValues()
        {
            var options = new SqlFormattingOptions { IndentationSize = 0, NewLineBeforeClauses = false };
            var profile = Spacious.WithOptions(options);
            options.IndentationSize = 99;
            options.SelectBody = SqlClauseBodyPlacement.Inline;
            Assert.AreEqual(0, profile.IndentationSize);
            Assert.IsFalse(profile.NewLineBeforeClauses);
            Assert.AreEqual(SqlClauseBodyPlacement.NextLineIndented, profile.SelectBody);
            var second = profile.WithOptions(new SqlFormattingOptions { NewLineBeforeJoins = false });
            Assert.AreEqual(0, second.IndentationSize);
            Assert.IsFalse(second.NewLineBeforeClauses);
            Assert.IsFalse(second.NewLineBeforeJoins);
            Assert.IsTrue(profile.NewLineBeforeJoins);
            Assert.AreEqual(4, SqlFormattingProfile.Spacious.IndentationSize);
            Assert.IsTrue(SqlFormattingProfile.Spacious.NewLineBeforeClauses);
            Assert.AreEqual(4, SqlFormattingProfile.Unformatted.IndentationSize);
            Assert.AreEqual(SqlNewLineStyle.Platform, SqlFormattingProfile.Unformatted.NewLineStyle);
        }

        [Test]
        public void RejectsNullNegativeIndentationAndEveryUndefinedEnum()
        {
            Assert.Throws<ArgumentNullException>(() => Spacious.WithOptions(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => Spacious.WithOptions(new SqlFormattingOptions { IndentationSize = -1 }));
            foreach (var property in typeof(SqlFormattingOptions).GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType)!;
                if (!type.IsEnum) continue;
                var options = new SqlFormattingOptions();
                property.SetValue(options, Enum.ToObject(type, 999));
                Assert.Throws<ArgumentOutOfRangeException>(() => Spacious.WithOptions(options), property.Name);
            }
        }

        [TestCase(SqlClauseBodyPlacement.Inline, SqlListLayout.Inline, "SELECT 1,2")]
        [TestCase(SqlClauseBodyPlacement.Inline, SqlListLayout.OneItemPerLine, "SELECT 1,\n  2")]
        [TestCase(SqlClauseBodyPlacement.NextLineIndented, SqlListLayout.Inline, "SELECT\n  1,2")]
        [TestCase(SqlClauseBodyPlacement.NextLineIndented, SqlListLayout.OneItemPerLine, "SELECT\n  1,\n  2")]
        public void FirstItemAndContinuationLayoutAreIndependent(SqlClauseBodyPlacement body, SqlListLayout list, string expected)
        {
            var profile = SqlFormattingProfile.Unformatted.WithOptions(new SqlFormattingOptions
            {
                IndentationSize = 2, NewLineStyle = SqlNewLineStyle.Lf, SelectBody = body, SelectList = list
            });
            for (int dialect = 0; dialect < 5; dialect++)
                Assert.AreEqual(expected, Select(Literal(1), Literal(2)).Done().ToSql(Exporter(dialect, profile)));
        }

        [TestCase(SqlNewLineStyle.Lf, "\n")]
        [TestCase(SqlNewLineStyle.CrLf, "\r\n")]
        public void NewlinesAndZeroIndentation(SqlNewLineStyle style, string newline)
        {
            var profile = Spacious.WithOptions(new SqlFormattingOptions { IndentationSize = 0, NewLineStyle = style });
            Assert.AreEqual("SELECT" + newline + "1", Select(Literal(1)).Done().ToSql(Exporter(0, profile)));
        }

        [TestCase(SqlBooleanOperatorPlacement.Inline, "[Id]=1 OR [Id]=2")]
        [TestCase(SqlBooleanOperatorPlacement.LineStart, "[Id]=1\n    OR [Id]=2")]
        [TestCase(SqlBooleanOperatorPlacement.SeparateLine, "[Id]=1\n    OR\n    [Id]=2")]
        public void BooleanOperatorsFollowConditionIndentation(SqlBooleanOperatorPlacement placement, string expected)
        {
            var profile = Spacious.WithOptions(new SqlFormattingOptions { BooleanOperatorPlacement = placement });
            var id = Column("Id");
            Assert.AreEqual("SELECT\n    1\nFROM [dbo].[user]\nWHERE\n    " + expected,
                Select(Literal(1)).From(Tables.User(Alias.Empty)).Where(id == 1 | id == 2).Done().ToSql(Exporter(0, profile)));
        }

        [Test]
        public void CompactBooleanParenthesisSpacingAndLineStartSpacing()
        {
            var id = Column("Id");
            var predicate = (id == 1 | id == 2) & (id == 3 | id == 4);
            Assert.That(predicate.ToSql(Exporter(0, SqlFormattingProfile.Unformatted)), Does.Contain(")AND("));
            var profile = SqlFormattingProfile.Unformatted.WithOptions(new SqlFormattingOptions
            {
                BooleanOperatorPlacement = SqlBooleanOperatorPlacement.LineStart, NewLineStyle = SqlNewLineStyle.Lf
            });
            Assert.That(predicate.ToSql(Exporter(0, profile)), Does.Contain(")\nAND ("));
        }

        [Test]
        public void ReusedSubqueryIndentsPerOccurrence()
        {
            var inner = Select(Literal(1)).Done();
            var outer = Select(new ExprValueQuery(inner)).Done();
            var query = Select(new ExprValueQuery(inner), new ExprValueQuery(outer)).Done();
            Assert.AreEqual("SELECT\n    (\n        SELECT\n            1\n    ),\n    (\n        SELECT\n            (\n                SELECT\n                    1\n            )\n    )",
                query.ToSql(Exporter(0, Spacious)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void PaginationPreservesTokensAndErrors(int dialect)
        {
            var user = Tables.User("U");
            var queries = new IExpr[]
            {
                SelectTop(7, user.UserId).From(user).OrderBy(user.UserId).Offset(5).Done(),
                SelectTop(7, user.UserId).From(user).Where(user.UserId > 0).Offset(5).Done(),
                Select(user.UserId).From(user).OrderBy(user.UserId).OffsetFetch(5, 9).Done()
            };
            foreach (var query in queries) AssertEquivalent(query, dialect, Spacious);
            var invalid = SelectTop(7, user.UserId).From(user).OrderBy(user.UserId).OffsetFetch(5, 9).Done();
            Assert.Throws<SqExpressException>(() => invalid.ToSql(Exporter(dialect, null)));
            Assert.Throws<SqExpressException>(() => invalid.ToSql(Exporter(dialect, Spacious)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EveryOptionPreservesSqlTokens(int dialect)
        {
            var user = Tables.User("U");
            var other = Tables.User("V");
            var inner = Select(Literal(1)).Done();
            var queries = new IExpr[]
            {
                Select(user.UserId, user.UserId, new ExprValueQuery(inner), Literal("a \n b")).From(user)
                    .InnerJoin(other, user.UserId == other.UserId)
                    .Where((user.UserId == 1 | user.UserId == 2) & user.UserId.In(1, 2))
                    .GroupBy(user.UserId).OrderBy(user.UserId).Done(),
                Select(Literal(1)).UnionAll(Select(Literal(2))).Done(),
                InsertInto(user, user.FirstName, user.LastName).Values("a", "b").Values("c", "d").DoneWithValues(),
                Update(user).Set(user.FirstName, "x").Set(user.LastName, "y").Where(user.UserId == 1),
                Delete(user).Where(user.UserId == 1),
                new ExprQueryList(new IExprComplete[] { Delete(user).All(), inner })
            };
            foreach (var query in queries)
            {
                Assert.AreEqual(query.ToSql(Exporter(dialect, null)), query.ToSql(Exporter(dialect, SqlFormattingProfile.Unformatted)));
                AssertEquivalent(query, dialect, Spacious);
                foreach (var property in typeof(SqlFormattingOptions).GetProperties())
                {
                    var type = Nullable.GetUnderlyingType(property.PropertyType)!;
                    var options = new SqlFormattingOptions();
                    object value = type == typeof(bool) ? false : type == typeof(int) ? 0 : Enum.ToObject(type, 0);
                    property.SetValue(options, value);
                    AssertEquivalent(query, dialect, Spacious.WithOptions(options));
                }
            }
        }

        [Test]
        public void QueryListSeparatesStatementsWithoutFinalNewline()
        {
            var query = Select(Literal(1)).Done();
            var list = new ExprQueryList(new IExprComplete[] { Delete(Tables.User(Alias.Empty)).All(), query });
            Assert.AreEqual("DELETE [dbo].[user];\nSELECT\n    1", list.ToSql(Exporter(0, Spacious)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void RecursiveAndMultipleCtesHaveStableWhitespace(int dialect)
        {
            var first = new FormattingCte("First", true);
            var second = new FormattingCte("Second", false);
            var query = Select(first.Num, second.Num).From(first).CrossJoin(second).Done();
            AssertEquivalent(query, dialect, Spacious);
            var sql = query.ToSql(Exporter(dialect, Spacious));
            Assert.That(sql, Does.Contain("AS(\n    SELECT\n"));
            Assert.That(sql, Does.Contain("),\n"));
            Assert.That(sql, Does.Contain(")\nSELECT\n"));
            Assert.That(sql, Does.Not.Contain("\n\n"));
        }

        private sealed class FormattingCte : CteBase
        {
            private readonly bool _recursive;
            public FormattingCte(string name, bool recursive) : base(name, SqExpress.Alias.Empty)
            {
                this._recursive = recursive;
                this.Num = this.CreateInt32Column("Num");
            }
            public Int32CustomColumn Num { get; }
            public override IExprSubQuery CreateQuery()
                => this._recursive
                    ? Select(Literal(1).As(this.Num)).UnionAll(Select(this.Num + 1).From(this).Where(this.Num < 3)).Done()
                    : Select(Literal(2).As(this.Num)).Done();
        }

        private static void AssertEquivalent(IExpr query, int dialect, SqlFormattingProfile profile)
        {
            var compact = query.ToSql(Exporter(dialect, null));
            var formatted = query.ToSql(Exporter(dialect, profile));
            Assert.AreEqual(Tokens(compact), Tokens(formatted), formatted);
            // Ignore payload whitespace when checking formatter-created trailing spaces.
            Assert.That(formatted, Does.Not.EndWith("\n"));
            Assert.That(formatted.Replace("'a \n b'", "'payload'"), Does.Not.Contain(" \n"));
        }

        // Preserve quoted tokens verbatim, removing only whitespace outside them.
        private static string Tokens(string sql)
        {
            var result = new StringBuilder();
            char end = '\0';
            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                if (end != '\0')
                {
                    result.Append(c);
                    if (c == end)
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == end) result.Append(sql[++i]);
                        else end = '\0';
                    }
                    else if (c == '\\' && i + 1 < sql.Length) result.Append(sql[++i]);
                }
                else if (c == '\'' || c == '"' || c == '`' || c == '[')
                {
                    end = c == '[' ? ']' : c;
                    result.Append(c);
                }
                else if (!char.IsWhiteSpace(c)) result.Append(c);
            }
            return result.ToString();
        }
    }
}
