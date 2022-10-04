public class AppAuth
{
    public const string ClientId = "my_client_id";
    public const string ClientSecret = "my_client_secret";
    public const string Scope = "openid email profile";

#if UNITY_IOS && !UNITY_EDITOR
    public const string RedirectUri = "com.cdm.myauthapp:/oauth2";
#else
    public const string RedirectUri = "http://localhost:8080/myauthapp/oauth2/";
#endif
}