using System.Runtime.Serialization;

namespace Cdm.Authorization
{
    [DataContract]
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets the access token issued by the authorization server.
        /// </summary>
        [DataMember(IsRequired = true, Name = "access_token")]
        public string accessToken { get; set; }
        
        /// <summary>
        /// Gets or sets the refresh token which can be used to obtain a new access token.
        /// </summary>
        [DataMember(Name = "refresh_token")]
        public string refreshToken { get; set; }
        
        /// <summary>
        /// Gets or sets the token type as specified in http://tools.ietf.org/html/rfc6749#section-7.1.
        /// </summary>
        [DataMember(IsRequired = true, Name = "token_type")]
        public string tokenType { get; set; }
        
        /// <summary>
        /// Gets or sets the lifetime in seconds of the access token.
        /// </summary>
        [DataMember(Name = "expires_in")]
        public long? expiresIn { get; set; }
        
        /// <summary>
        /// Gets or sets the scope of the access token as specified in http://tools.ietf.org/html/rfc6749#section-3.3.
        /// </summary>
        [DataMember(Name = "scope")]
        public string scope { get; set; }
    }
}