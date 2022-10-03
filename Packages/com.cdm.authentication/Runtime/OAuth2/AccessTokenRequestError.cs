using System.Runtime.Serialization;

namespace Cdm.Authentication.OAuth2
{
    /// <summary>
    /// OAuth 2.0 model for a unsuccessful access token response as specified in
    /// http://tools.ietf.org/html/rfc6749#section-5.2.
    /// </summary>
    [DataContract]
    public class AccessTokenRequestError
    {
        /// <summary>
        /// Gets or sets the error code as specified in http://tools.ietf.org/html/rfc6749#section-5.2.
        /// </summary>
        [DataMember(IsRequired = true, Name = "error")]
        public AccessTokenRequestErrorCode code { get; set; }

        /// <summary>
        /// Gets or sets a human-readable text which provides additional information used to assist the client
        /// developer in understanding the error occurred.
        /// </summary>
        [DataMember(Name = "error_description")]
        public string description { get; set; }

        /// <summary>
        /// Gets or sets the URI identifying a human-readable web page with provides information about the error.
        /// </summary>
        [DataMember(Name = "error_uri")]
        public string uri { get; set; }
    }
}