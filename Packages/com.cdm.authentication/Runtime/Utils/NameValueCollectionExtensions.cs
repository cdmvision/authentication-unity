using System.Collections.Specialized;

namespace Cdm.Authorization.Utils
{
    public static class NameValueCollectionExtensions
    {
        public static void SetIfNotEmpty(this NameValueCollection collection, string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                collection[key] = value;
            }
            else
            {
                collection.Remove(key);
            }
        }

        public static void AddIfNotEmpty(this NameValueCollection collection, string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                collection.Add(key, value);
            }
        }
    }
}