using System;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlTranspilerException : Exception
    {
        public SqExpressSqlTranspilerException(string message) : base(message)
        {
        }
    }
}
