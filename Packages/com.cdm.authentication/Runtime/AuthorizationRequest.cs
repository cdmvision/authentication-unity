using System.Runtime.Serialization;

namespace Cdm.Authorization
{
    public static class AuthorizationRequest
    {
        /// <summary>
        /// REQUIRED. Value MUST be set to "code".
        /// </summary>
        public const string ResponseType = "response_type";

        /// <summary>
        /// REQUIRED. The client identifier as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-2.2">Section 2.2</a>.
        /// </summary>
        public const string ClientId = "client_id";
        
        /// <summary>
        /// OPTIONAL. The client secret.
        /// </summary>
        public const string ClientSecret = "client_secret";

        /// <summary>
        /// OPTIONAL. The redirect URI registered by the client as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-3.1.2">Section 3.1.2</a>.
        /// </summary>
        public const string RedirectUri = "redirect_uri";

        /// <summary>
        /// OPTIONAL. The scope of the access request as described by
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-3.3">Section 3.3</a>.
        /// </summary>
        public const string Scope = "scope";

        /// <summary>
        /// RECOMMENDED. An opaque value used by the client to maintain state between the request and callback.
        /// The authorization server includes this value when redirecting the user-agent back to the client.
        /// The parameter SHOULD be used for preventing cross-site request forgery as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-10.12">Section 10.12</a>.
        /// </summary>
        public const string State = "state";
    }
}