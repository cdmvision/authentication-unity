using System;
using System.Net;

namespace Cdm.Authorization
{
    public class AccessTokenException : Exception
    {
        public HttpStatusCode statusCode { get; }
        public AccessTokenError error { get; }

        public AccessTokenException(AccessTokenError error, HttpStatusCode statusCode)
        {
            this.error = error;
            this.statusCode = statusCode;
        }

        public AccessTokenException(AccessTokenError error, HttpStatusCode statusCode, string message) : base(message)
        {
            this.error = error;
            this.statusCode = statusCode;
        }

        public AccessTokenException(AccessTokenError error, HttpStatusCode statusCode,
            string message, Exception innerException) : base(message, innerException)
        {
            this.error = error;
            this.statusCode = statusCode;
        }
    }
}