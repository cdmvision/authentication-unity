using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Cdm.Authentication.Browser
{
    public class StandaloneBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;
        private HttpListener _httpListener;
        
        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, CancellationToken cancellationToken = default)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            cancellationToken.Register(() =>
            {
                _taskCompletionSource?.TrySetCanceled();
            });

            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(redirectUrl);
                _httpListener.Start();
                _httpListener.BeginGetContext(IncomingHttpRequest, _httpListener);
                
                Application.OpenURL(loginUrl);
                
                return await _taskCompletionSource.Task;
            }
            finally
            {
                _httpListener?.Stop();
                _httpListener = null;
            }
        }

        private void IncomingHttpRequest(IAsyncResult result)
        {
            var httpListener = (HttpListener)result.AsyncState;
            var httpContext = httpListener.EndGetContext(result);
            var httpRequest = httpContext.Request;
            
#if DEBUG
            Debug.Log($"URL: {httpRequest.Url.OriginalString}");
            Debug.Log($"Raw URL: {httpRequest.RawUrl}");
            Debug.Log($"Query: {httpRequest.QueryString}");

            foreach (string q in httpRequest.QueryString)
            {
                Debug.Log($"{q}: {httpRequest.QueryString.Get(q)}");
            }
            //Debug.Log($"Incoming http request url: {httpRequest.}");
#endif
            // TODO: compare with initial redirect url!
            
            // build a response to send an "ok" back to the browser for the user to see
            var httpResponse = httpContext.Response;
            var responseString = "<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            // send the output to the client browser
            httpResponse.ContentLength64 = buffer.Length;
            var output = httpResponse.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            _taskCompletionSource.SetResult(
                new BrowserResult(BrowserStatus.Success, httpRequest.RawUrl));
        }
    }
}