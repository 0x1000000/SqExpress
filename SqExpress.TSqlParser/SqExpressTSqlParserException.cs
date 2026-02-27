using System;

namespace SqExpress.TSqlParser
{
    public sealed class SqExpressTSqlParserException : Exception
    {
        public SqExpressTSqlParserException(string message) : base(message)
        {
        }
    }
}
