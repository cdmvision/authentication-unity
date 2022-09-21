using System;

namespace Cdm.Authorization
{
    public class AuthorizationException : Exception
    {
        public AuthorizationError error { get; }

        public AuthorizationException(AuthorizationError error)
        {
            this.error = error;
        }

        public AuthorizationException(AuthorizationError error, string message) : base(message)
        {
            this.error = error;
        }

        public AuthorizationException(AuthorizationError error, string message, Exception innerException)
            : base(message, innerException)
        {
            this.error = error;
        }
    }
}