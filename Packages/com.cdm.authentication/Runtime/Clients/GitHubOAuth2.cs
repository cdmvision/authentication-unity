using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cdm.Authorization.Utils;

namespace Cdm.Authorization.Clients
{
    public class GitHubOAuth2 : OAuth2, IUserInfoProvider
    {
        public GitHubOAuth2(OAuth2Configuration configuration) : base(configuration)
        {
        }

        public override string authorizationUrl => "https://github.com/login/oauth/authorize";
        public override string accessTokenUrl => "https://github.com/login/oauth/access_token";
        public string userInfoUrl => "https://www.googleapis.com/oauth2/v1/userinfo";

        public async Task<IUserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            return await UserInfoParser.GetUserInfoAsync<GitHubUserInfo>(
                    httpClient, userInfoUrl, GetAuthenticationHeader(), cancellationToken);
        }

        [DataContract]
        public class GitHubUserInfo : IUserInfo
        {
            [DataMember(Name = "id", IsRequired = true)]
            public string id { get; set; }

            [DataMember(Name = "name")] 
            public string name { get; set; }

            [DataMember(Name = "email")] 
            public string email { get; set; }

            [DataMember(Name = "avatar_url")] 
            public string avatarUrl { get; set; }

            public string picture => avatarUrl;
        }
    }
}