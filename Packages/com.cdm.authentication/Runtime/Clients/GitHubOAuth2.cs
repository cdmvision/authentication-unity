namespace Cdm.Authorization.Clients
{
    public class GitHubOAuth2 : OAuth2
    {
        public GitHubOAuth2(OAuth2Configuration configuration) : base(configuration)
        {
        }

        public override string authorizationUrl => "https://github.com/login/oauth/authorize";
        public override string accessTokenUrl => "https://github.com/login/oauth/access_token";
    }
}