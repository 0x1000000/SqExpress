using System;
using System.Runtime.Serialization;

namespace SqExpress.CodeGenUtil
{
    public class SqExpressCodeGenException : Exception
    {
        public SqExpressCodeGenException()
        {
        }

        protected SqExpressCodeGenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SqExpressCodeGenException(string? message) : base(message)
        {
        }

        public SqExpressCodeGenException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}