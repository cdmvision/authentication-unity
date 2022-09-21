using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cdm.Authentication.Browser
{
    public class CrossPlatformBrowser : IBrowser
    {
        private readonly IBrowser _browser;
        
        public CrossPlatformBrowser()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            _browser = new StandaloneBrowser();
#elif UNITY_IOS
            //_browser = new ASWebAuthenticationSessionBrowser();
            _browser = new WKWebViewAuthenticationSessionBrowser();
#elif UNITY_ANDROID
            // TODO:
#endif
        }
        
        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, CancellationToken cancellationToken = default)
        {
            if (_browser == null)
                throw new NotSupportedException("Platform browser does not supported.");
            
            return await _browser.StartAsync(loginUrl, redirectUrl, cancellationToken);
        }
    }
}