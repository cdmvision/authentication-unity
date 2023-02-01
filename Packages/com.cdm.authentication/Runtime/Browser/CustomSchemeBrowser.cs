using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Cdm.Authentication.Browser
{
    /// <summary>
    /// OAuth 2.0 verification browser that waits for a call with
    /// the authorization verification code throught a custom scheme (aka protocol)
    /// </summary>
    public class CustomSchemeBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;

        public async Task<BrowserResult> StartAsync(string loginUrl, string redirectUrl, CancellationToken cancellationToken = default)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            cancellationToken.Register(() =>
            {
                _taskCompletionSource?.TrySetCanceled();
            });

            Application.deepLinkActivated += onDeepLinkActivated;

            try
            {
                Application.OpenURL(loginUrl);
                return await _taskCompletionSource.Task;
            }
            finally
            {
                Application.deepLinkActivated -= onDeepLinkActivated;
            }
        }

        private void onDeepLinkActivated(string url)
        {
            _taskCompletionSource.SetResult(
                new BrowserResult(BrowserStatus.Success, url));
        }
    }
}