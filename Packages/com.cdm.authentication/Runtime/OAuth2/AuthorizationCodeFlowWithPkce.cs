using System.Collections.Generic;

namespace Cdm.Authentication.OAuth2
{
    /// <summary>
    /// OAuth 2.0 'Authorization Code' flow with PKCE (Proof Key for Code Exchange).
    /// 
    /// PKCE (RFC 7636) is an extension to the Authorization Code flow to prevent CSRF and authorization code injection
    /// attacks. PKCE is not a form of client authentication, and PKCE is not a replacement for a client secret or
    /// other client authentication. PKCE is recommended even if a client is using a client secret or other form
    /// of client authentication like private_key_jwt.
    /// 
    /// https://www.rfc-editor.org/rfc/rfc7636
    /// </summary>
    public abstract class AuthorizationCodeFlowWithPkce : AuthorizationCodeFlow
    {
        private string _codeVerifier;

        protected AuthorizationCodeFlowWithPkce(Configuration configuration) : base(configuration)
        {
        }

        protected override Dictionary<string, string> GetAuthorizationUrlParameters()
        {
            var parameters = base.GetAuthorizationUrlParameters();

            _codeVerifier = Pkce.CreateCodeVerifier();

            parameters.Add("code_challenge", Pkce.CreateCodeChallenge(_codeVerifier));
            parameters.Add("code_challenge_method", Pkce.ChallengeMethod);

            return parameters;
        }

        protected override Dictionary<string, string> GetAccessTokenParameters(string code)
        {
            var parameters = base.GetAccessTokenParameters(code);
            parameters.Add("code_verifier", _codeVerifier);
            return parameters;
        }
    }
}
