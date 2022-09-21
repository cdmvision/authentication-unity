namespace Cdm.Authorization.Clients
{
    public class GoogleOAuth2 : OAuth2
    {
        public GoogleOAuth2(OAuth2Configuration configuration) : base(configuration)
        {
        }

        public override string authorizationUrl => "https://accounts.google.com/o/oauth2/auth";
        public override string accessTokenUrl => "https://accounts.google.com/o/oauth2/token";
    }
}