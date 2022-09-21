using System.Runtime.Serialization;

namespace Cdm.Authorization
{
    [DataContract]
    public class AccessTokenResponse
    {
        /// <summary>
        /// REQUIRED. The access token issued by the authorization server.
        /// </summary>
        [DataMember(IsRequired = true, Name = "access_token")]
        public string accessToken { get; set; }
        
        /// <summary>
        /// OPTIONAL. The refresh token, which can be used to obtain new access tokens using the same
        /// authorization grant as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-6">Section 6</a>.
        /// </summary>
        [DataMember(Name = "refresh_token")]
        public string refreshToken { get; set; }
        
        /// <summary>
        /// REQUIRED. The type of the token issued as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-7.1">Section 7.1</a>. Value is case insensitive.
        /// </summary>
        [DataMember(IsRequired = true, Name = "token_type")]
        public string tokenType { get; set; }
        
        /// <summary>
        /// RECOMMENDED. The lifetime in seconds of the access token. For example, the value "3600" denotes that the
        /// access token will expire in one hour from the time the response was generated. If omitted,
        /// the authorization server SHOULD provide the expiration time via other means or document the default value.
        /// </summary>
        [DataMember(Name = "expires_in")]
        public int expiresIn { get; set; }
        
        /// <summary>
        /// OPTIONAL, if identical to the scope requested by the client; otherwise, REQUIRED. The scope of
        /// the access token as described by
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-3.3">Section 3.3</a>.
        /// </summary>
        [DataMember(Name = "scope")]
        public string scope { get; set; }
    }
}