using System.Runtime.Serialization;

namespace Cdm.Authorization
{
    [DataContract]
    public class AuthorizationRequest
    {
        /// <summary>
        /// REQUIRED. Value MUST be set to "code".
        /// </summary>
        [DataMember(IsRequired = true, Name = "response_type")]
        public string responseType { get; set; }
        
        /// <summary>
        /// REQUIRED. The client identifier as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-2.2">Section 2.2</a>.
        /// </summary>
        [DataMember(IsRequired = true, Name = "client_id")]
        public string clientId { get; set; }
        
        /// <summary>
        /// OPTIONAL. The client secret.
        /// </summary>
        [DataMember(Name = "client_secret")]
        public string clientSecret { get; set; }
        
        /// <summary>
        /// OPTIONAL. The redirect URI registered by the client as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-3.1.2">Section 3.1.2</a>.
        /// </summary>
        [DataMember(Name = "redirect_uri")]
        public string redirectUri { get; set; }
        
        /// <summary>
        /// OPTIONAL. The scope of the access request as described by
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-3.3">Section 3.3</a>.
        /// </summary>
        [DataMember(Name = "scope")]
        public string scope { get; set; }

        /// <summary>
        /// RECOMMENDED. An opaque value used by the client to maintain state between the request and callback.
        /// The authorization server includes this value when redirecting the user-agent back to the client.
        /// The parameter SHOULD be used for preventing cross-site request forgery as described in
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-10.12">Section 10.12</a>.
        /// </summary>
        [DataMember(Name = "state")]
        public string state { get; set; }
    }
}