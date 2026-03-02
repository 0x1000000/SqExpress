using System.Text.RegularExpressions;

namespace SqExpress.SqlParser.Internal.Parsing
{
    internal static class SqlTextNormalizer
    {
        public static string Normalize(string sql)
            => NormalizeUpdateAlias(NormalizeCteName(sql));

        private static string NormalizeCteName(string sql)
        {
            return Regex.Replace(
                sql,
                @"^\s*WITH\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s+AS\s*\(",
                m => "WITH [" + m.Groups["name"].Value + "] AS(",
                RegexOptions.IgnoreCase);
        }

        private static string NormalizeUpdateAlias(string sql)
        {
            var updateAlias = Regex.Match(
                sql,
                @"^\s*UPDATE\s+(?<alias>[A-Za-z_][A-Za-z0-9_]*)\s+SET\s+",
                RegexOptions.IgnoreCase);

            if (!updateAlias.Success)
            {
                return sql;
            }

            var alias = updateAlias.Groups["alias"].Value;
            var normalized = Regex.Replace(
                sql,
                @"^\s*UPDATE\s+" + Regex.Escape(alias) + @"\s+SET\s+",
                "UPDATE [" + alias + "] SET ",
                RegexOptions.IgnoreCase);

            normalized = Regex.Replace(
                normalized,
                @"\b" + Regex.Escape(alias) + @"\.\[",
                "[" + alias + "].[",
                RegexOptions.IgnoreCase);

            return normalized;
        }
    }
}
