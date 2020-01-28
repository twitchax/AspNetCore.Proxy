using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy
{
    internal static class WsExtensions
    {
        internal static readonly int CloseMessageMaxSize = 123;

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

                if (options?.BeforeWebSocketConnect != null)
                {
                    await options.BeforeWebSocketConnect(context, socketToEndpoint.Options);
                }

                await socketToEndpoint.ConnectAsync(new Uri(uri), context.RequestAborted);

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
                catch (Exception e)
                {
                    var closeMessageBytes = Encoding.UTF8.GetBytes($"WebSocket failure.\n\n{e.Message}\n\n{e.StackTrace}");
                    var closeMessage = Encoding.UTF8.GetString(closeMessageBytes, 0, Math.Min(closeMessageBytes.Length, CloseMessageMaxSize));
                    await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, closeMessage, cancellationToken);
                    return;
                }

                if(destination.State != WebSocketState.Open && destination.State != WebSocketState.CloseReceived)
                    return;
                    
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