using System.Runtime.Serialization;

namespace Cdm.Authorization
{
    /// <summary>
    /// The authorization server responds with an HTTP 400 (Bad Request) status code (unless specified otherwise) and
    /// includes the following parameters.
    /// </summary>
    [DataContract]
    public class AccessTokenError
    {
        [DataMember(IsRequired = true, Name = "error")]
        public AccessTokenErrorCode code { get; set; }

        /// <summary>
        /// OPTIONAL. Human-readable ASCII [<a href="https://www.rfc-editor.org/rfc/rfc6749#ref-USASCII">USASCII</a>]
        /// text providing additional information, used to assist the client developer in understanding
        /// the error that occurred.
        /// </summary>
        [DataMember(Name = "error_description")]
        public string description { get; set; }

        /// <summary>
        /// OPTIONAL. A URI identifying a human-readable web page with information about the error, used to provide
        /// the client developer with additional information about the error.
        /// </summary>
        [DataMember(Name = "error_uri")]
        public string uri { get; set; }
    }
}