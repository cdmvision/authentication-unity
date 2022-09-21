namespace Cdm.Authorization
{
    /// <summary>
    /// The configuration of third-party authentication service client.
    /// </summary>
    public struct OAuth2Configuration
    {
        /// <summary>
        /// The client identifier issued to the client during the registration process described by
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-2.2">Section 2.2</a>.
        /// </summary>
        public string clientId { get; set; }
        
        /// <summary>
        /// The client secret. The client MAY omit the parameter if the client secret is an empty string.
        /// </summary>
        public string clientSecret { get; set; }
        
        /// <summary>
        /// The authorization and token endpoints allow the client to specify the scope of the access request using
        /// the "scope" request parameter.  In turn, the authorization server uses the "scope" response parameter to
        /// inform the client of the scope of the access token issued. The value of the scope parameter is expressed
        /// as a list of space- delimited, case-sensitive strings.  The strings are defined by the authorization server.
        /// If the value contains multiple space-delimited strings, their order does not matter, and each string adds an
        /// additional access range to the requested scope.
        /// </summary>
        public string scope { get; set; }
        
        /// <summary>
        /// After completing its interaction with the resource owner, the authorization server directs the resource
        /// owner's user-agent back to the client. The authorization server redirects the user-agent to the client's
        /// redirection endpoint previously established with the authorization server during the client registration
        /// process or when making the authorization request.
        /// </summary>
        /// <remarks>
        /// The redirection endpoint URI MUST be an absolute URI as defined by
        /// <a href="https://www.rfc-editor.org/rfc/rfc3986#section-4.3">[RFC3986] Section 4.3</a>.
        /// The endpoint URI MAY include an "application/x-www-form-urlencoded" formatted (per
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#appendix-B">Appendix B</a>) query
        /// component (<a href="https://www.rfc-editor.org/rfc/rfc3986#section-3.4">[RFC3986] Section 3.4</a>),
        /// which MUST be retained when adding additional query parameters. The endpoint URI MUST NOT include
        /// a fragment component.
        /// </remarks>
        public string redirectUri { get; set; }
    }
}