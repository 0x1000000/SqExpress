using System;

namespace SqExpress.SqlParser
{
    public sealed class SqExpressTSqlParserException : SqExpressException
    {
        public SqExpressTSqlParserException(string message) : base(message)
        {
        }

        public SqExpressTSqlParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
