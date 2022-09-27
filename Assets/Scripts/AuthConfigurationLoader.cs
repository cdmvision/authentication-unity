using Cdm.Authentication.OAuth2;
using Newtonsoft.Json;
using UnityEngine;

public static class AuthConfigurationLoader
{
    public static bool TryLoad(out AuthorizationCodeFlow.Configuration configuration)
    {
        configuration = default;
        
        var configurationText = Resources.Load<TextAsset>("Configuration");
        if (configurationText == null)
        {
            Debug.LogError("Auth client configuration could not found at Resources/Configuration.json.");
            return false;
        }
        
        configuration = JsonConvert.DeserializeObject<AuthorizationCodeFlow.Configuration>(configurationText.text);
        return true;
    }
}