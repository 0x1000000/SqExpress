using System.Text;
using System.Text.RegularExpressions;

namespace SqExpress.SqlExport.Internal
{
    public static class SqlInjectionChecker
    {
        private static readonly Regex tSqlFunctionName = new Regex(@"([a-zA-Z][\w_]+|[@][a-zA-Z][\w_]*|[@][@][a-zA-Z][\w_]*)");

        public static void AppendStringEscapeClosingSquare(StringBuilder builder, string original) 
            => AppendStringEscape(builder, original, ']');

        public static void AppendStringEscapeSingleQuote(StringBuilder builder, string original) 
            => AppendStringEscape(builder, original, '\'');

        public static void AppendStringEscapeDoubleQuote(StringBuilder builder, string original) 
            => AppendStringEscape(builder, original, '"');

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