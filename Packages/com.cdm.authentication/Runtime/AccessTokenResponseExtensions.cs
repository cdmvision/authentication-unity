using System.Net.Http.Headers;

namespace Cdm.Authorization
{
    public static class AccessTokenResponseExtensions
    {
        public static AuthenticationHeaderValue GetAuthenticationHeader(this AccessTokenResponse accessTokenResponse)
        {
            return accessTokenResponse != null
                ? new AuthenticationHeaderValue(accessTokenResponse.tokenType, accessTokenResponse.accessToken)
                : null;
        }

        public static bool IsNullOrExpired(this AccessTokenResponse accessTokenResponse)
        {
            return accessTokenResponse == null || accessTokenResponse.IsExpired();
        }
    }
}