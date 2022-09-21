using System;
using System.Collections.Specialized;
using System.Web;

namespace Cdm.Authorization.Utils
{
    public class UrlBuilder
    {
        private readonly UriBuilder _uriBuilder;
        private readonly NameValueCollection _query;
        
        private UrlBuilder(string url)
        {
            _uriBuilder = new UriBuilder(url);
            _query = HttpUtility.ParseQueryString("");
        }
        
        public static UrlBuilder New(string url)
        {
            return new UrlBuilder(url);
        }

        public UrlBuilder SetQueryParameter(string key, string value)
        {
            _query.SetIfNotEmpty(key, value);
            return this;
        }

        public override string ToString()
        {
            _uriBuilder.Query = _query.ToString();
            return _uriBuilder.Uri.ToString();
        }
        
        public static string Build(string url, NameValueCollection query)
        {
            var uriBuilder = new UriBuilder(url);
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.ToString();
        }
    }
}