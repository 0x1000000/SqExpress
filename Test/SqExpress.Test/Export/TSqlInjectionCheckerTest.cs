using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SqExpress;
using SqExpress.SqlExport;
using SqExpress.SqlExport.Internal;

namespace SqExpress.Test.Export
{
    [TestFixture]
    public class TSqlInjectionCheckerTest
    {
        private static readonly ISqlExporter[] Exporters = { TSqlExporter.Default, PgSqlExporter.Default, MySqlExporter.Default };

        [Test]
        public void AppendStringEscape_Basic()
        {
            Assert.AreEqual("", AppendStringEscape(""));
            Assert.AreEqual(@"''", AppendStringEscape(@"'"));
            Assert.AreEqual(@"z''", AppendStringEscape(@"z'"));
            Assert.AreEqual(@"''z", AppendStringEscape(@"'z"));
            Assert.AreEqual(@"z''z", AppendStringEscape(@"z'z"));
            Assert.AreEqual(@"z''z''", AppendStringEscape(@"z'z'"));
            Assert.AreEqual(@"''z''z''", AppendStringEscape(@"'z'z'"));

            Assert.AreEqual(@"''''", AppendStringEscape(@"''"));
            Assert.AreEqual(@"z''''", AppendStringEscape(@"z''"));
            Assert.AreEqual(@"''''z", AppendStringEscape(@"''z"));
            Assert.AreEqual(@"z''''z", AppendStringEscape(@"z''z"));
            Assert.AreEqual(@"z''''z''''", AppendStringEscape(@"z''z''"));
            Assert.AreEqual(@"''''z''''z''''", AppendStringEscape(@"''z''z''"));

            Assert.AreEqual(@"xz''", AppendStringEscape(@"xz'"));
            Assert.AreEqual(@"''xz", AppendStringEscape(@"'xz"));
            Assert.AreEqual(@"xz''xz", AppendStringEscape(@"xz'xz"));
            Assert.AreEqual(@"xz''xz''", AppendStringEscape(@"xz'xz'"));
            Assert.AreEqual(@"''xz''xz''", AppendStringEscape(@"'xz'xz'"));

            Assert.AreEqual(@"xz''''", AppendStringEscape(@"xz''"));
            Assert.AreEqual(@"''''xz", AppendStringEscape(@"''xz"));
            Assert.AreEqual(@"xz''''xz", AppendStringEscape(@"xz''xz"));
            Assert.AreEqual(@"xz''''xz''''", AppendStringEscape(@"xz''xz''"));
            Assert.AreEqual(@"''''xz''''xz''''", AppendStringEscape(@"''xz''xz''"));
            Assert.AreEqual(@"'') or (''1''=''1--", AppendStringEscape(@"') or ('1'='1--"));
            Assert.AreEqual(@"''; EXEC xp_cmdshell(''whoami'');--", AppendStringEscape(@"'; EXEC xp_cmdshell('whoami');--"));
            Assert.AreEqual(@"abc]];DROP TABLE [T];--", AppendStringEscape(@"abc]];DROP TABLE [T];--"));
            Assert.AreEqual(@"abc``;DROP TABLE `T`;--", AppendStringEscape(@"abc``;DROP TABLE `T`;--"));
        }

        private static string AppendStringEscape(string original)
        {
            var sql = SqQueryBuilder.Literal(original).ToMySql();
            return sql.Substring(1, sql.Length - 2);
        }

        [Test]
        public void AppendStringEscapeMySql_Backslash()
        {
            Assert.AreEqual("''\\\\''", AppendStringEscapeMySql("'\\'"));
            Assert.AreEqual("a\\\\b''c", AppendStringEscapeMySql("a\\b'c"));
        }

        [Test]
        public void CheckTSqlBuildInFunctionName_Valid()
        {
            string[] values =
            {
                "GETDATE",
                "DATEADD",
                "Coalesce",
                "@localVar",
                "@@ROWCOUNT",
                "A",
                "@a",
                "@@a"
            };

            foreach (var value in values)
            {
                Assert.IsTrue(SqlInjectionChecker.CheckTSqlBuildInFunctionName(value), value);
                Assert.DoesNotThrow(() => SqlInjectionChecker.AssertValidBuildInFunctionName(value), value);
            }
        }

        [Test]
        public void CheckTSqlBuildInFunctionName_RejectsInjectionVectors()
        {
            string[] values =
            {
                "",
                "GETDATE;DROP TABLE T",
                "GETDATE --comment",
                "GET DATE",
                "1GETDATE",
                "GETDATE()",
                "GETDATE,DATEADD",
                "GETDATE/*x*/",
                "@@ROWCOUNT;SELECT 1"
            };

            foreach (var value in values)
            {
                Assert.IsFalse(SqlInjectionChecker.CheckTSqlBuildInFunctionName(value), value);
                Assert.Throws<SqExpressException>(() => SqlInjectionChecker.AssertValidBuildInFunctionName(value), value);
            }
        }

        [Test]
        public void LiteralEscaping_FuzzRoundTrip_AllExporters()
        {
            var random = new Random(123456);

            for (int i = 0; i < 500; i++)
            {
                var payload = NextPayload(random);

                foreach (var exporter in Exporters)
                {
                    var sql = SqQueryBuilder.Literal(payload).ToSql(exporter);
                    var restored = exporter switch
                    {
                        TSqlExporter => RestoreLiteral(sql, '\''),
                        PgSqlExporter => RestoreLiteral(sql, '\''),
                        MySqlExporter => RestoreLiteral(sql, '\'', '\\'),
                        _ => throw new NotSupportedException(exporter.GetType().Name)
                    };

                    Assert.AreEqual(payload, restored, $"Payload: '{payload}', Exporter: {exporter.GetType().Name}, Sql: {sql}");
                }
            }
        }

        [Test]
        public void DangerousMarkers_DoNotEscapeLiteralBoundary()
        {
            string[] payloads =
            {
                "'; EXEC xp_cmdshell('whoami');--",
                "/*comment*/' OR 'x'='x",
                "UNION SELECT username,password FROM members--",
                "abc]];DROP TABLE [T];--",
                "abc``;DROP TABLE `T`;--",
                "abc\"\";DROP TABLE \"T\";--",
                "1); WAITFOR DELAY '00:00:05'--"
            };

            string[] markers =
            {
                "xp_cmdshell",
                "union select",
                "drop table",
                "waitfor delay",
                "/*",
                "--"
            };

            var expr = SqQueryBuilder.Select(payloads.Select((p, i) => SqQueryBuilder.Literal(p).As($"v{i}")).ToList()).Done();

            foreach (var exporter in Exporters)
            {
                var sql = exporter.ToSql(expr);
                foreach (var marker in markers)
                {
                    Assert.IsFalse(
                        ContainsOutsideSingleQuotedLiteral(sql, marker),
                        $"Marker '{marker}' escaped literal boundary for {exporter.GetType().Name}: {sql}");
                }
            }
        }

        [Test]
        public void CheckTSqlBuildInFunctionName_FuzzInvalid()
        {
            var random = new Random(98765);
            int tested = 0;

            for (int i = 0; i < 3000; i++)
            {
                var candidate = NextFunctionNameCandidate(random);
                if (IsExpectedValidFunctionName(candidate))
                {
                    continue;
                }

                tested++;
                Assert.IsFalse(SqlInjectionChecker.CheckTSqlBuildInFunctionName(candidate), candidate);
                Assert.Throws<SqExpressException>(() => SqlInjectionChecker.AssertValidBuildInFunctionName(candidate), candidate);
            }

            Assert.Greater(tested, 1000, "Not enough invalid fuzz samples were generated.");
        }

        [Test]
        public void UnsafeValue_EmitsRawSql()
        {
            const string payload = "1; DROP TABLE Users --";

            var unsafeSql = SqQueryBuilder
                .Select(SqQueryBuilder.UnsafeValue(payload))
                .Done()
                .ToSql(TSqlExporter.Default);

            Assert.IsTrue(unsafeSql.Contains(payload, StringComparison.Ordinal));
            Assert.IsTrue(ContainsOutsideSingleQuotedLiteral(unsafeSql, "drop table"), unsafeSql);

            var safeSql = SqQueryBuilder
                .Select(SqQueryBuilder.Literal(payload))
                .Done()
                .ToSql(TSqlExporter.Default);

            Assert.IsFalse(ContainsOutsideSingleQuotedLiteral(safeSql, "drop table"), safeSql);
        }

        private static string AppendStringEscapeMySql(string original)
        {
            var sql = SqQueryBuilder.Literal(original).ToMySql();
            return sql.Substring(1, sql.Length - 2);
        }

        private static string RestoreLiteral(string sql, params char[] escapedChars)
        {
            var quoteStart = sql.StartsWith("N'", StringComparison.Ordinal) ? 1 : 0;
            Assert.IsTrue(sql.Length >= quoteStart + 2 && sql[quoteStart] == '\'' && sql[^1] == '\'', sql);

            var escaped = sql.Substring(quoteStart + 1, sql.Length - quoteStart - 2);
            return UnescapeDoubledChars(escaped, escapedChars);
        }

        private static string UnescapeDoubledChars(string escaped, params char[] escapedChars)
        {
            var chars = escapedChars.ToHashSet();
            var sb = new StringBuilder(escaped.Length);

            for (int i = 0; i < escaped.Length; i++)
            {
                var ch = escaped[i];
                if (chars.Contains(ch) && i + 1 < escaped.Length && escaped[i + 1] == ch)
                {
                    sb.Append(ch);
                    i++;
                    continue;
                }

                sb.Append(ch);
            }

            return sb.ToString();
        }

        private static bool ContainsOutsideSingleQuotedLiteral(string sql, string marker)
        {
            if (string.IsNullOrEmpty(marker))
            {
                return false;
            }

            bool inLiteral = false;
            for (int i = 0; i <= sql.Length - marker.Length; i++)
            {
                if (sql[i] == '\'')
                {
                    if (inLiteral && i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i++;
                        continue;
                    }

                    inLiteral = !inLiteral;
                    continue;
                }

                if (!inLiteral && sql.AsSpan(i).StartsWith(marker.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NextPayload(Random random)
        {
            const string alphabet =
                "abcdefghijklmnopqrstuvwxyz" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "0123456789" +
                " '\"`\\;/*-_=+[](){}:,.$#@!?%\r\n\t\b";

            var len = random.Next(0, 40);
            var sb = new StringBuilder(len + 4);
            for (int i = 0; i < len; i++)
            {
                sb.Append(alphabet[random.Next(alphabet.Length)]);
            }

            if (random.Next(4) == 0) sb.Append('\u00A9');
            if (random.Next(5) == 0) sb.Append('\u20AC');
            if (random.Next(6) == 0) sb.Append('\u4E2D');

            return sb.ToString();
        }

        private static string NextFunctionNameCandidate(Random random)
        {
            const string alphabet =
                "abcdefghijklmnopqrstuvwxyz" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "0123456789" +
                "_@$-;/* ()\t\r\n,.`'\"[]{}:=+!\\";

            var len = random.Next(0, 25);
            var sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                sb.Append(alphabet[random.Next(alphabet.Length)]);
            }

            return sb.ToString();
        }

        private static bool IsExpectedValidFunctionName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            int index;
            if (value.StartsWith("@@", StringComparison.Ordinal))
            {
                index = 2;
            }
            else if (value.StartsWith("@", StringComparison.Ordinal))
            {
                index = 1;
            }
            else
            {
                index = 0;
            }

            if (index >= value.Length || !char.IsLetter(value[index]))
            {
                return false;
            }

            for (int i = index + 1; i < value.Length; i++)
            {
                var ch = value[i];
                if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
