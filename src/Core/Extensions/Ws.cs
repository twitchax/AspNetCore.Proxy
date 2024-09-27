using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy
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
                    {
                        if (!WebHeaderCollection.IsRestricted(headerEntry.Key))
                        {
                            socketToEndpoint.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
                        }
                    }
                        
                }

                if(options?.BeforeConnect != null)
                    await options.BeforeConnect(context, socketToEndpoint.Options).ConfigureAwait(false);

                await socketToEndpoint.ConnectAsync(new Uri(uri), context.RequestAborted).ConfigureAwait(false);

                using var socketToClient = await context.WebSockets.AcceptWebSocketAsync(socketToEndpoint.SubProtocol).ConfigureAwait(false);

                var bufferSize = options?.BufferSize ?? 4096;
                await Task.WhenAll(
                    PumpWebSocket(socketToEndpoint, socketToClient, WsProxyDataDirection.Downstream, wsProxy, bufferSize, context.RequestAborted), 
                    PumpWebSocket(socketToClient, socketToEndpoint, WsProxyDataDirection.Upstream, wsProxy, bufferSize, context.RequestAborted)
                ).ConfigureAwait(false);
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

        private static async Task PumpWebSocket(WebSocket source, WebSocket destination, WsProxyDataDirection direction, WsProxy wsProxy, int bufferSize, CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();
            var receiveBuffer = WebSocket.CreateServerBuffer(bufferSize);

            while (true)
            {
                WebSocketReceiveResult result;

                try
                {
                    ms.SetLength(0);

                    do
                    {
                        result = await source.ReceiveAsync(receiveBuffer, cancellationToken).ConfigureAwait(false);
                        ms.Write(receiveBuffer.Array!, receiveBuffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);
                }
                catch (Exception e)
                {
                    var closeMessageBytes = Encoding.UTF8.GetBytes($"WebSocket failure.\n\n{e.Message}\n\n{e.StackTrace}");
                    var closeMessage = Encoding.UTF8.GetString(closeMessageBytes, 0, Math.Min(closeMessageBytes.Length, CloseMessageMaxSize));
                    await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, closeMessage, cancellationToken).ConfigureAwait(false);

                    return;
                }

                if (destination.State != WebSocketState.Open && destination.State != WebSocketState.CloseReceived)
                {
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var closeStatus = source.CloseStatus ?? WebSocketCloseStatus.Empty;
                    await destination.CloseOutputAsync(closeStatus, source.CloseStatusDescription, cancellationToken).ConfigureAwait(false);

                    return;
                }

                var sendBuffer = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);

                // If the data intercept is set, then invoke it.
                if(wsProxy.Options?.DataIntercept != null)
                {
                    await wsProxy.Options.DataIntercept(sendBuffer, direction, result.MessageType).ConfigureAwait(false);
                }

                await destination.SendAsync(sendBuffer, result.MessageType, result.EndOfMessage, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}