using System;
using System.Collections.Specialized;
using System.Web;

namespace Cdm.Authentication.OAuth2
{
    /// <summary>
    /// The parameters an authorization server put on the redirect back to the app (RFC 6749, section 4.1.2): the
    /// code and state, or an error. Reading only: what to check, and in which order, is the caller's.
    /// </summary>
    public sealed class AuthorizationRedirect
    {
        /// <summary>The query parameters of the redirect, decoded.</summary>
        internal NameValueCollection query { get; }

        /// <summary>The authorization code, or null.</summary>
        public string code => query["code"];

        /// <summary>The state the server sent back, or null.</summary>
        public string state => query["state"];

        /// <summary>The error code, or null. Any string: servers may use codes beyond RFC 6749's.</summary>
        public string error => query["error"];

        /// <summary>The error description, or null.</summary>
        public string errorDescription => query["error_description"];

        private AuthorizationRedirect(NameValueCollection query)
        {
            this.query = query;
        }

        /// <summary>
        /// Reads a redirect URL: http on loopback for desktops, or an app's own scheme such as
        /// <c>com.example.app:/oauth/callback?code=...</c> on mobile.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="redirectUrl"/> is null.</exception>
        /// <exception cref="UriFormatException"><paramref name="redirectUrl"/> is not an absolute URL.</exception>
        public static AuthorizationRedirect Parse(string redirectUrl)
        {
            if (redirectUrl == null)
                throw new ArgumentNullException(nameof(redirectUrl));

            return new AuthorizationRedirect(HttpUtility.ParseQueryString(new Uri(redirectUrl).Query));
        }

        /// <summary>Whether the redirect carries exactly <paramref name="expectedState"/>.</summary>
        public bool HasState(string expectedState)
        {
            return !string.IsNullOrEmpty(expectedState) && string.Equals(state, expectedState, StringComparison.Ordinal);
        }
    }
}
