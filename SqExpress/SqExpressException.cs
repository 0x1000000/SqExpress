using System;
#if NETSTANDARD
using System.Runtime.Serialization;
#endif

namespace SqExpress
{
    public class SqExpressException : Exception
    {
        public SqExpressException()
        {
        }

#if NETSTANDARD
        protected SqExpressException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif

        public SqExpressException(string message) : base(message)
        {
        }

        public SqExpressException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
