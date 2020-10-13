using System;
using System.Runtime.Serialization;

namespace SqExpress
{
    public class SqExpressException : Exception
    {
        public SqExpressException()
        {
        }

        protected SqExpressException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SqExpressException(string message) : base(message)
        {
        }

        public SqExpressException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}