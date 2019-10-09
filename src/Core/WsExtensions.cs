using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy
{
    internal static class WsExtensions
    {
        internal static async Task ExecuteWsProxyOperationAsync(this HttpContext context, string uri, ProxyOptions options = null)
        {
            using (var socketToEndpoint = new ClientWebSocket())
            {
                foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
                {
                    socketToEndpoint.Options.AddSubProtocol(protocol);
                }
                
                foreach (var headerEntry in context.Request.Headers)
                    if (!Helpers.WebSocketNotForwardedHeaders.Contains(headerEntry.Key, StringComparer.OrdinalIgnoreCase))
                        socketToEndpoint.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);

                // TODO: Add a proxy options for keep alive and set it here.
                //client.Options.KeepAliveInterval = proxyService.Options.WebSocketKeepAliveInterval.Value;

                // TODO make a proxy option action to edit the web socket options.

                try
                {
                    await socketToEndpoint.ConnectAsync(new Uri(uri), context.RequestAborted);
                }
                catch (Exception e)
                {
                    if (options?.HandleFailure == null)
                    {
                        // If the failures are not caught, then write a generic response.
                        context.Response.StatusCode = 502;
                        await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}.").ConfigureAwait(false);
                        return;
                    }

                    await options.HandleFailure(context, e).ConfigureAwait(false);
                }

                using (var socketToClient = await context.WebSockets.AcceptWebSocketAsync(socketToEndpoint.SubProtocol))
                {
                    // TODO: Add a buffer size option and set it here.
                    var bufferSize = 4096;
                    await Task.WhenAll(PumpWebSocket(socketToEndpoint, socketToClient, bufferSize, context.RequestAborted), PumpWebSocket(socketToClient, socketToEndpoint, bufferSize, context.RequestAborted));
                }
            }
        }

        private static async Task PumpWebSocket(WebSocket source, WebSocket destination, int bufferSize, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];

            while (true)
            {
                WebSocketReceiveResult result;

                try
                {
                    result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, cancellationToken);
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellationToken);
                    return;
                }

                // TODO: Add handlers here to allow the developer to edit message before forwarding, and vice versa?

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }
    }
}