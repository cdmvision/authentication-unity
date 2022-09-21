using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Cdm.Authorization.AuthorizationCode;
using Cdm.Authorization.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cdm.Authorization
{
    /// <summary>
    /// Supports 'Authorization Code' flow. Enables user sign-in and access to web APIs on behalf of the user.
    /// </summary>
    public abstract class OAuth2 : IDisposable
    {
        /// <summary>
        /// The endpoint for authorization server. This is used to get the authorization code.
        /// </summary>
        public abstract string authorizationUrl { get; }

        /// <summary>
        /// The endpoint for authentication server. This is used to exchange the authorization code for an access token.
        /// </summary>
        public abstract string accessTokenUrl { get; }
        
        /// <summary>
        /// State (any additional information that was provided by application and is posted back by service).
        /// </summary>
        public string state { get; private set; }

        /// <summary>
        /// The access token returned by provider.
        /// </summary>
        public string accessToken { get; private set; }

        /// <summary>
        /// The refresh token returned by provider.
        /// </summary>
        public string refreshToken { get; private set; }

        /// <summary>
        /// The token type returned by provider.
        /// </summary>
        public string tokenType { get; private set; }

        public string scope { get; private set; }
        
        /// <summary>
        /// Seconds till the <see cref="accessToken"/> expires returned by provider.
        /// </summary>
        public DateTime expiresAt { get; private set; }
        
        /// <summary>
        /// The lifetime in seconds of the access token.
        /// </summary>
        public int expiresIn { get; private set; }

        public OAuth2Configuration configuration { get; }
        
        private readonly HttpClient _httpClient;
        
        protected OAuth2(OAuth2Configuration configuration)
        {
            this.configuration = configuration;
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
        }
        
        public virtual Task<string> GetAuthorizationUrlAsync(CancellationToken cancellationToken = default)
        {
            // Generate new state.
            state = Guid.NewGuid().ToString("D");

            var url = UrlBuilder.New(authorizationUrl)
                .SetQueryParameter(AuthorizationRequest.ResponseType, "code")
                .SetQueryParameter(AuthorizationRequest.ClientId, configuration.clientId)
                .SetQueryParameter(AuthorizationRequest.ClientSecret, configuration.clientSecret)
                .SetQueryParameter(AuthorizationRequest.RedirectUri, configuration.redirectUri)
                .SetQueryParameter(AuthorizationRequest.Scope, configuration.scope)
                .SetQueryParameter(AuthorizationRequest.State, state)
                .ToString();
            
            return Task.FromResult(url);
        }

        public virtual async Task<string> GetAccessTokenAsync(string authorizationResponse, 
            CancellationToken cancellationToken = default)
        {
            var index = authorizationResponse.IndexOf("?", StringComparison.Ordinal);
            if (index >= 0)
            {
                authorizationResponse = authorizationResponse.Substring(index).Remove(0, 1);
            }
            
            var query = HttpUtility.ParseQueryString(authorizationResponse);

            // Is there any error?
            if (AuthorizationError.TryGetFromQuery(query, out var authorizationError))
                throw new AuthorizationException(authorizationError);
            
            // Validate state.
            if (!string.IsNullOrEmpty(state))
            {
                if (state != query.Get(AuthorizationResponse.State))
                    throw new SecurityException("State must be the same.");
            }

            var code = query.Get(AuthorizationResponse.Code);
            if (string.IsNullOrEmpty(code))
                throw new Exception("Authorization code does not exist.");

            var authString = $"{configuration.clientId}:{configuration.clientSecret}";
            var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
            
            var parameters = new Dictionary<string, string>();
            parameters.Add(AccessTokenRequest.GrantType, "authorization_code");
            parameters.Add(AccessTokenRequest.Code, code);
            parameters.Add(AccessTokenRequest.ClientId, configuration.clientId);
            parameters.Add(AccessTokenRequest.RedirectUri, configuration.redirectUri);

            if (!string.IsNullOrEmpty(configuration.clientSecret))
            {
                parameters.Add(AccessTokenRequest.ClientSecret, configuration.clientSecret);    
            }

            var accessTokenRequest = new HttpRequestMessage(HttpMethod.Post, accessTokenUrl);
            accessTokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64AuthString);
            accessTokenRequest.Content = new FormUrlEncodedContent(parameters);
            
            Debug.Log($"{accessTokenRequest}");
            Debug.Log($"{await accessTokenRequest.Content.ReadAsStringAsync()}");
            
            var response = await _httpClient.SendAsync(accessTokenRequest, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseJson);

                accessToken = accessTokenResponse.accessToken;
                refreshToken = accessTokenResponse.refreshToken;
                tokenType = accessTokenResponse.tokenType;
                expiresIn = accessTokenResponse.expiresIn;
                expiresAt = DateTime.Now + TimeSpan.FromSeconds(expiresIn);
                scope = accessTokenResponse.scope;
                return accessToken;    
            }

            AccessTokenError error = null;
            try
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                error = JsonConvert.DeserializeObject<AccessTokenError>(errorJson);
            }
            catch (Exception)
            {
                // ignored
            }

            throw new AccessTokenException(error, response.StatusCode);
        }

       /* public virtual async Task<string> GetAccessTokenAsync(bool forceRefresh = false, 
            CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && expiresAt > DateTime.Now && !string.IsNullOrEmpty(accessToken))
            {
                return accessToken;
            }
            
            
        }*/

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}