using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace AspNetCore.Proxy.Tests
{
    internal static class Extensions
    {
        const int BUFFER_SIZE = 4096;
        internal static readonly string SupportedProtocol = "MyProtocol1";
        internal static readonly string CloseMessage = "PLEASE_CLOSE";
        internal static readonly string KillMessage = "PLEASE_KILL";
        internal static readonly string CloseDescription = "ARBITRARY";

        internal static Task SendShortMessageAsync(this WebSocket socket, string message)
        {
            if(message.Length > BUFFER_SIZE / 8)
                throw new InvalidOperationException($"Must send a short message (less than {BUFFER_SIZE / 8} characters).");

            return socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        internal static async Task<string> ReceiveShortMessageAsync(this WebSocket socket)
        {
            var buffer = new byte[BUFFER_SIZE];
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

            if(!result.EndOfMessage)
                throw new InvalidOperationException($"Must send a short message (less than {BUFFER_SIZE / 8} characters).");
            
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        internal static async Task SocketBoomerang(this HttpContext context)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync(SupportedProtocol);

            while(true)
            {
                var message = await socket.ReceiveShortMessageAsync();

                if(message == CloseMessage)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, CloseDescription, context.RequestAborted);
                    break;
                }

                if(message == KillMessage)
                {
                    throw new Exception();
                }
                
                // Basically, this server just always sends back a message that is the message it received wrapped with "[]".
                await socket.SendShortMessageAsync($"[{message}]");
            }
        }

        internal static async Task HttpBoomerang(this HttpContext context)
        {
            var message = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var uri = context.Request.GetDisplayUrl();
            await context.Response.WriteAsync($"({uri})[{message}]");
        }
    }
}