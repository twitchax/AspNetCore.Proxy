using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AspNetCore.Proxy
{
    internal static class Extensions
    {
        internal static Task ExecuteProxyOperationAsync(this HttpContext context, string uri, ProxyOptions options = null)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                if(!uri.StartsWith("ws", System.StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("A WebSocket request must forward to a WebSocket (ws[s]) endpoint.");

                return context.ExecuteWsProxyOperationAsync(uri, options);
            }

            // Assume HTTP if not WebSocket.
            if(!uri.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("An HTTP request must forward to an HTTP (http[s]) endpoint.");

            return context.ExecuteHttpProxyOperationAsync(uri, options);
        }
    }
}
