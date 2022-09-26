using System.Runtime.Serialization;

namespace Cdm.Authorization
{
    /// <summary>
    /// OAuth 2.0 request to refresh an access token using a refresh token as specified in
    /// http://tools.ietf.org/html/rfc6749#section-6.
    /// </summary>
    [DataContract]
    public class RefreshTokenRequest
    {
        /// <summary>
        /// The grant type as 'refresh_token'.
        /// </summary>
        [DataMember(IsRequired = true, Name = "grant_type")]
        public string grantType => "refresh_token";

        /// <summary>
        /// REQUIRED. The refresh token issued to the client.
        /// </summary>
        [DataMember(IsRequired = true, Name = "refresh_token")]
        public string refreshToken { get; set; }

        /// <summary>
        /// OPTIONAL. The scope of the access request as described by Section 3.3.  The requested scope MUST NOT
        /// include any scope not originally granted by the resource owner, and if omitted is treated as equal to
        /// the scope originally granted by the resource owner.
        /// </summary>
        [DataMember(Name = "scope")]
        public string scope { get; set; }
    }
}