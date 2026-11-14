using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Cdm.Authentication.Browser
{
    /// <summary>
    /// OAuth 2.0 verification browser for desktops (RFC 8252, section 7.3): opens the sign-in page in the system
    /// browser and listens on the loopback address for the redirect that carries the authorization response.
    /// </summary>
    public class StandaloneBrowser : IBrowser
    {
        /// <summary>
        /// Gets or sets the close page response. This HTML response is shown to the user after redirection is done.
        /// It is sent as it is: nothing from the request is put into it.
        /// </summary>
        public string closePageResponse { get; set; } =
            "<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";

        /// <summary>
        /// Opens the sign-in page. <see cref="Application.OpenURL"/> by default.
        /// </summary>
        public Action<string> openUrl { get; set; } = Application.OpenURL;

        /// <summary>
        /// A loopback port that is free at the moment of the call, for a redirect URL such as
        /// <c>http://127.0.0.1:{port}/callback</c>.
        /// </summary>
        public static int GetAvailableLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        /// <param name="loginUrl">The sign-in page.</param>
        /// <param name="redirectUrl">
        /// The redirect URL: <c>http</c> on a loopback host, for example <c>http://127.0.0.1:51234/callback</c>. Only a
        /// GET of its path ends the wait. Any other request (a favicon, another path, another method) is answered
        /// 404, and the wait goes on.
        /// </param>
        /// <param name="cancellationToken">Ends the wait with a <see cref="TaskCanceledException"/>.</param>
        /// <exception cref="ArgumentNullException">A URL is null or empty.</exception>
        /// <exception cref="UriFormatException"><paramref name="redirectUrl"/> is not a valid URL.</exception>
        /// <exception cref="ArgumentException"><paramref name="redirectUrl"/> is not http on a loopback host.</exception>
        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(loginUrl))
                throw new ArgumentNullException(nameof(loginUrl));

            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            var redirectUri = new Uri(redirectUrl);
            if (redirectUri.Scheme != Uri.UriSchemeHttp || !redirectUri.IsLoopback)
                throw new ArgumentException("The redirect URL has to be http on a loopback host.", nameof(redirectUrl));

            if (cancellationToken.IsCancellationRequested)
                return await Task.FromCanceled<BrowserResult>(cancellationToken);

            var completion =
                new TaskCompletionSource<BrowserResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Closed once, in the finally below, and never stopped first: on Mono, closing a stopped listener binds
            // the port again to remove the prefix, which fails while the browser still holds its keep-alive
            // connection ("Only one usage of each socket address").
            var httpListener = new HttpListener();

            try
            {
                httpListener.Prefixes.Add($"{redirectUri.Scheme}://{redirectUri.Authority}/");
                httpListener.Start();

                using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
                {
                    _ = ListenAsync(httpListener, redirectUri.AbsolutePath, completion);

                    openUrl?.Invoke(loginUrl);

                    return await completion.Task;
                }
            }
            finally
            {
                CloseQuietly(httpListener);
            }
        }

        /// <summary>
        /// Answers requests until the redirect arrives. Off Unity's main thread, so the browser gets its answer also
        /// while the application does not run in the background. Closing the listener ends a pending wait.
        /// </summary>
        private async Task ListenAsync(HttpListener httpListener, string redirectPath,
            TaskCompletionSource<BrowserResult> completion)
        {
            var expectedPath = redirectPath.TrimEnd('/');

            try
            {
                while (!completion.Task.IsCompleted)
                {
                    var context = await httpListener.GetContextAsync().ConfigureAwait(false);
                    var request = context.Request;
                    var isRedirect = request.HttpMethod == "GET" &&
                                     string.Equals(request.Url.AbsolutePath.TrimEnd('/'), expectedPath,
                                         StringComparison.Ordinal);

                    if (!isRedirect)
                    {
                        Answer(context.Response, 404, string.Empty);
                        continue;
                    }

                    Answer(context.Response, 200, closePageResponse);
                    completion.TrySetResult(new BrowserResult(BrowserStatus.Success, request.Url.ToString()));
                }
            }
            catch (Exception e)
            {
                // After a cancel or a result the listener is closed on purpose and this does nothing.
                completion.TrySetException(e);
            }
        }

        private static void Answer(HttpListenerResponse response, int statusCode, string html)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(html ?? string.Empty);
                response.StatusCode = statusCode;
                response.ContentType = "text/html; charset=utf-8";

                // No connection is kept for a next request: there is none, and on Mono closing the listener writes an
                // unasked "200, Content-Length: 0" onto a connection the browser kept open.
                response.KeepAlive = false;
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception e)
            {
                // The browser closing its connection early does not change the result.
                Debug.LogWarning($"[StandaloneBrowser] Could not answer the browser: {e.Message}");
            }
        }

        private static void CloseQuietly(HttpListener httpListener)
        {
            try
            {
                httpListener.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StandaloneBrowser] Could not close the listener: {e.Message}");
            }
        }
    }
}
