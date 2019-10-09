using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class WsServerFixture : IDisposable
    {
        private CancellationTokenSource _source;

        public WsServerFixture()
        {
            _source = new CancellationTokenSource();
            WsHelpers.RunWsServers(_source.Token);
        }

        public void Dispose()
        {
            _source.Cancel();
        }
    }

    public class WsUnitTests : IClassFixture<WsServerFixture>
    {
        public readonly ClientWebSocket _client;

        public WsUnitTests(WsServerFixture fixture)
        {
            _client = new ClientWebSocket();
            _client.Options.SetRequestHeader("SomeHeader", "SomeValue");
            _client.Options.AddSubProtocol(Extensions.SupportedProtocol);
        }

        [Theory]
        [InlineData("ws://localhost:5001/ws")]
        [InlineData("ws://localhost:5001/api/ws")]
        public async Task CanDoWebSockets(string server)
        {
            var send1 = "TEST1";
            var expected1 = $"[{send1}]";

            var send2 = "TEST2";
            var expected2 = $"[{send2}]";

            await _client.ConnectAsync(new Uri(server), CancellationToken.None);
            Assert.Equal(Extensions.SupportedProtocol, _client.SubProtocol);

            // Send a message.
            await _client.SendShortMessageAsync(send1);
            await _client.SendShortMessageAsync(send2);
            await _client.SendShortMessageAsync(Extensions.CloseMessage);

            // Receive responses.
            var response1 = await _client.ReceiveShortMessageAsync();
            Assert.Equal(expected1, response1);
            var response2 = await _client.ReceiveShortMessageAsync();
            Assert.Equal(expected2, response2);

            // Receive close.
            var result = await _client.ReceiveAsync(new ArraySegment<byte>(new byte[4096]), CancellationToken.None);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
            Assert.Equal(Extensions.CloseDescription, result.CloseStatusDescription);

            await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, Extensions.CloseDescription, CancellationToken.None);
        }

        [Fact]
        public async Task CanCatchAbruptClose()
        {
            var send1 = "PLEASE_KILL";
            var expected1 = $"[{send1}]";

            var send2 = "TEST2";
            var expected2 = $"[{send2}]";

            await _client.ConnectAsync(new Uri("ws://localhost:5001/ws"), CancellationToken.None);

            // Send a message.
            await _client.SendShortMessageAsync(send1);

            // Receive failed close.
            var result = await _client.ReceiveAsync(new ArraySegment<byte>(new byte[4096]), CancellationToken.None);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.EndpointUnavailable, result.CloseStatus);

            await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, Extensions.CloseDescription, CancellationToken.None);
        }
    }
}