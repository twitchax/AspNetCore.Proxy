using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy.Extensions
{
    internal static class WsExtensions
    {
        internal static readonly int CloseMessageMaxSize = 123;

        internal static async Task ExecuteWsProxyOperationAsync(this HttpContext context, WsProxy wsProxy)
        {
            var uri = await context.GetEndpointFromComputerAsync(wsProxy.EndpointComputer).ConfigureAwait(false);
            var options = wsProxy.Options;

            try
            {
                if(!context.WebSockets.IsWebSocketRequest)
                    throw new InvalidOperationException("An HTTP request cannot be routed as a WebSocket proxy operation.");

                if(!uri.StartsWith("ws", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only forwarded addresses starting with 'ws://' or 'ws://' are supported for WebSocket requests.");

                // If `true`, this proxy call has been intercepted.
                if(options?.Intercept != null && await options.Intercept(context).ConfigureAwait(false))
                    return;

                using var socketToEndpoint = new ClientWebSocket();

                foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
                    socketToEndpoint.Options.AddSubProtocol(protocol);

                foreach (var headerEntry in context.Request.Headers)
                {
                    if (!Helpers.WebSocketNotForwardedHeaders.Contains(headerEntry.Key, StringComparer.OrdinalIgnoreCase))
                        socketToEndpoint.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
                }

                if(options?.BeforeConnect != null)
                    await options.BeforeConnect(context, socketToEndpoint.Options).ConfigureAwait(false);

                await socketToEndpoint.ConnectAsync(new Uri(uri), context.RequestAborted).ConfigureAwait(false);

                using var socketToClient = await context.WebSockets.AcceptWebSocketAsync(socketToEndpoint.SubProtocol).ConfigureAwait(false);

                var bufferSize = options?.BufferSize ?? 4096;
                await Task.WhenAll(PumpWebSocket(socketToEndpoint, socketToClient, bufferSize, context.RequestAborted), PumpWebSocket(socketToClient, socketToEndpoint, bufferSize, context.RequestAborted)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if(!context.Response.HasStarted)
                {
                    if (options?.HandleFailure == null)
                    {
                        // If the failures are not caught, then write a generic response.
                        context.Response.StatusCode = 502 /* BAD GATEWAY */;
                        await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}").ConfigureAwait(false);
                        return;
                    }

                    await options.HandleFailure(context, e).ConfigureAwait(false);
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
                    result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    var closeMessageBytes = Encoding.UTF8.GetBytes($"WebSocket failure.\n\n{e.Message}\n\n{e.StackTrace}");
                    var closeMessage = Encoding.UTF8.GetString(closeMessageBytes, 0, Math.Min(closeMessageBytes.Length, CloseMessageMaxSize));
                    await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, closeMessage, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if(destination.State != WebSocketState.Open && destination.State != WebSocketState.CloseReceived)
                    return;

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellationToken).ConfigureAwait(false);
                    return;
                }

                // TODO: Add handlers here to allow the developer to edit message before forwarding, and vice versa?
                // Possibly in the future, if deemed useful.

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}