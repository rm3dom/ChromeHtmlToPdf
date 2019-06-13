using System;
using System.Runtime.Serialization;

namespace ChromeHtmlToPdfLib.Exceptions
{
    public abstract class ChromePdfConverterException : ApplicationException
    {
        protected ChromePdfConverterException()
        {
        }

        protected ChromePdfConverterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected ChromePdfConverterException(string message) : base(message)
        {
        }

        protected ChromePdfConverterException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}