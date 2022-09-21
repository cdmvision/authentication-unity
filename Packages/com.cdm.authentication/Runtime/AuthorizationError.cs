using System.Collections.Specialized;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Cdm.Authorization
{
    [DataContract]
    public class AuthorizationError
    {
        public const string CodeKey = "error";
        public const string DescriptionKey = "error_description";
        public const string UriKey = "error_uri";
        
        [DataMember(IsRequired = true, Name = CodeKey)]
        public AuthorizationErrorCode code { get; set; }

        /// <summary>
        /// OPTIONAL. Human-readable ASCII [<a href="https://www.rfc-editor.org/rfc/rfc6749#ref-USASCII">USASCII</a>]
        /// text providing additional information, used to assist the client developer in understanding
        /// the error that occurred.
        /// </summary>
        [DataMember(Name = DescriptionKey)]
        public string description { get; set; }

        /// <summary>
        /// OPTIONAL. A URI identifying a human-readable web page with information about the error, used to provide
        /// the client developer with additional information about the error.
        /// </summary>
        [DataMember(Name = UriKey)]
        public string uri { get; set; }

        public static bool TryGetFromQuery(NameValueCollection query, out AuthorizationError error)
        {
            if (!string.IsNullOrEmpty(query.Get(CodeKey)))
            {
                var errorJson = new JObject();
                errorJson[CodeKey] = query.Get(CodeKey);
                errorJson[DescriptionKey] = query.Get(DescriptionKey);
                errorJson[UriKey] = query.Get(UriKey);
                
                error = errorJson.ToObject<AuthorizationError>();
                return true;
            }

            error = null;
            return false;
        }
    }
}