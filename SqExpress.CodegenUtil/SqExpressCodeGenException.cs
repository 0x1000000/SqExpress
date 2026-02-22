using System;
#if NETSTANDARD
using System.Runtime.Serialization;
#endif

namespace SqExpress.CodeGenUtil
{
    public class SqExpressCodeGenException : Exception
    {
        public SqExpressCodeGenException()
        {
        }

#if NETSTANDARD
        protected SqExpressCodeGenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif

        public SqExpressCodeGenException(string? message) : base(message)
        {
        }

        public SqExpressCodeGenException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
