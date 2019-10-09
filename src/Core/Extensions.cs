using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AspNetCore.Proxy
{
    internal static class Extensions
    {
        internal static async Task ExecuteProxyOperationAsync(this HttpContext context, string uri, ProxyOptions options = null)
        {
            try
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    if(!uri.StartsWith("ws", System.StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("A WebSocket request must forward to a WebSocket (ws[s]) endpoint.");

                    await context.ExecuteWsProxyOperationAsync(uri, options).ConfigureAwait(false);
                    return;
                }

                // Assume HTTP if not WebSocket.
                if(!uri.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("An HTTP request must forward to an HTTP (http[s]) endpoint.");

                await context.ExecuteHttpProxyOperationAsync(uri, options).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if(!context.Response.HasStarted)
                {
                    if (options?.HandleFailure == null)
                    {
                        // If the failures are not caught, then write a generic response.
                        context.Response.StatusCode = 502;
                        await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}").ConfigureAwait(false);
                        return;
                    }

                    await options.HandleFailure(context, e).ConfigureAwait(false);
                }
            }
        }
    }
}
