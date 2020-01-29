using System;
using System.Net;
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
        private readonly CancellationTokenSource _source;

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

    public class WsIntegrationTests : IClassFixture<WsServerFixture>
    {
        public readonly ClientWebSocket _client;

        public WsIntegrationTests(WsServerFixture _)
        {
            _client = new ClientWebSocket();
            _client.Options.SetRequestHeader("SomeHeader", "SomeValue");
            _client.Options.AddSubProtocol(Extensions.SupportedProtocol);
        }

        [Theory]
        [InlineData("ws://localhost:5001/ws")]
        [InlineData("ws://localhost:5001/api/ws")]
        [InlineData("ws://localhost:5001/api/ws2")]
        public async Task CanDoWebSockets(string server)
        {
            const string send1 = "TEST1";
            var expected1 = $"[{send1}]";

            const string send2 = "TEST2";
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
            const string send1 = "PLEASE_KILL";

            await _client.ConnectAsync(new Uri("ws://localhost:5001/ws"), CancellationToken.None);

            // Send a message.
            await _client.SendShortMessageAsync(send1);

            // Receive failed close.
            var result = await _client.ReceiveAsync(new ArraySegment<byte>(new byte[4096]), CancellationToken.None);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.EndpointUnavailable, result.CloseStatus);

            await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, Extensions.CloseDescription, CancellationToken.None);
        }

        [Fact]
        public async Task CanIntercept()
        {
            const string message = "The server returned status code '200' when status code '101' was expected.";

            var client = new ClientWebSocket();
            client.Options.AddSubProtocol("interceptedProtocol");

            var exception = await Assert.ThrowsAnyAsync<WebSocketException>(() => client.ConnectAsync(new Uri("ws://localhost:5001/ws"), CancellationToken.None));

            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public async Task CanRunBeforeConnectAndHandleFailure()
        {
            // Because there is no protocol attached, the `BeforeConnect` uses a bad endpoint protocol.
            // This causes a failure, and that failure is intercepted by `HandleFailure`.
            var message = "The server returned status code '599' when status code '101' was expected.";

            var client = new ClientWebSocket();

            var exception = await Assert.ThrowsAnyAsync<WebSocketException>(() => client.ConnectAsync(new Uri("ws://localhost:5001/ws"), CancellationToken.None));

            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public async Task CanFailWhenWsRequestIsToHttpProxy()
        {
            const string message = "The server returned status code '502' when status code '101' was expected.";

            var client = new ClientWebSocket();

            var exception = await Assert.ThrowsAnyAsync<WebSocketException>(() => client.ConnectAsync(new Uri("ws://localhost:5001/api/http"), CancellationToken.None));

            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public async Task CanFailWhenHttpRequestIsToWsProxy()
        {
            var client = new HttpClient();

            var result = await client.GetAsync("http://localhost:5001/api/ws");

            Assert.Equal(HttpStatusCode.BadGateway, result.StatusCode);
        }
    }
}