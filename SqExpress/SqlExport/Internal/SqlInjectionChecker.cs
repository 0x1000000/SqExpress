using System.Text;
using System.Text.RegularExpressions;

namespace SqExpress.SqlExport.Internal
{
    internal static class SqlInjectionChecker
    {
        private static readonly Regex tSqlFunctionName = new Regex(@"([a-zA-Z][\w_]+|[@][a-zA-Z][\w_]*|[@][@][a-zA-Z][\w_]*)");

        public static void AppendStringEscapeClosingSquare(StringBuilder builder, string original) 
            => AppendStringEscape(builder, original, ']');

        public static void AppendStringEscapeSingleQuote(StringBuilder builder, string original) 
            => AppendStringEscape(builder, original, '\'');

        public static void AppendStringEscapeSingleQuoteAndBackslash(StringBuilder builder, string original) 
            => AppendStringEscape2(builder, original, '\'', '\\');

        public static void AppendStringEscapeDoubleQuote(StringBuilder builder, string original) 
            => AppendStringEscape(builder, original, '"');

        public static void AppendStringEscapeBacktick(StringBuilder builder, string original) 
            => AppendStringEscape(builder, original, '`');

        public static void AppendStringEscape(StringBuilder builder, string original, char escape)
        {
            int start = 0;
            int index;
            do
            {
                index = original.IndexOf(escape, start);
                if (index >= 0)
                {
                    if (start < index)
                    {
                        builder.Append(original, start, index-start);
                    }
                    builder.Append(escape, 2);
                    start = index + 1;
                }
            } while (index >= 0);

            if (start < original.Length)
            {
                builder.Append(original, start, original.Length-start);
            }
        }

        public static void AppendStringEscape2(StringBuilder builder, string original, char escape1, char escape2)
        {
            int start = 0;
            int index;
            do
            {
                index = original.Index2Of(escape1, escape2, start, out var escape);
                if (index >= 0)
                {
                    if (start < index)
                    {
                        builder.Append(original, start, index-start);
                    }
                    builder.Append(escape, 2);
                    start = index + 1;
                }
            } while (index >= 0);

            if (start < original.Length)
            {
                builder.Append(original, start, original.Length-start);
            }
        }

        private static int Index2Of(this string str, char ch1, char ch2, int start, out char ch)
        {
            for (int i = start; i < str.Length; i++)
            {
                if (str[i] == ch1)
                {
                    ch = ch1;
                    return i;
                }
                if (str[i] == ch2)
                {
                    ch = ch2;
                    return i;
                }
            }

            ch = default;
            return -1;
        }

        public static bool CheckTSqlBuildInFunctionName(string name)
        {
            return tSqlFunctionName.IsMatch(name);
        }

        public static void AssertValidBuildInFunctionName(string functionName)
        {
            if (!CheckTSqlBuildInFunctionName(functionName))
            {
                throw new SqExpressException($"'{functionName}' is not recognized ad a built in function");
            }
        }
    }
}