using System;
using System.Runtime.Serialization;

namespace NineDigit.NWS4
{
    public class SignatureExpiredException : Exception
    {
        public SignatureExpiredException()
        {
        }

        public SignatureExpiredException(string message) : base(message)
        {
        }

        public SignatureExpiredException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SignatureExpiredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
