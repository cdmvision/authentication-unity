namespace Cdm.Authorization.AuthorizationCode
{
    public class AccessTokenRequest
    {
        // authorization_code
        public const string GrantType = "grant_type";
        
        /// <summary>
        /// REQUIRED. The authorization code received from the authorization server.
        /// </summary>
        public const string Code = "code";

        /// <summary>
        /// REQUIRED, if the "redirect_uri" parameter was included in the authorization request as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-4.1.1">Section 4.1.1</a>,
        /// and their values MUST be identical.
        /// </summary>
        public const string RedirectUri = "redirect_uri";

        /// <summary>
        /// REQUIRED, if the client is not authenticating with the authorization server as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-3.2.1">Section 3.2.1</a>.
        /// </summary>
        public const string ClientId = "client_id";
        
        /// <summary>
        /// The client secret.
        /// </summary>
        public const string ClientSecret = "client_secret";
    }
}