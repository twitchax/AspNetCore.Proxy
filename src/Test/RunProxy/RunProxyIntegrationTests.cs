using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class RunProxyServerFixture : IDisposable
    {
        private readonly CancellationTokenSource _source;

        public RunProxyServerFixture()
        {
            _source = new CancellationTokenSource();
            RunProxyHelpers.RunProxyServers(_source.Token);
        }

        public void Dispose()
        {
            _source.Cancel();
        }
    }

    public class RunProxyIntegrationTests : IClassFixture<RunProxyServerFixture>
    {
        private readonly ClientWebSocket _wsClient;
        private readonly HttpClient _httpClient;

        public RunProxyIntegrationTests(RunProxyServerFixture _)
        {
            _wsClient = new ClientWebSocket();
            _wsClient.Options.AddSubProtocol(Extensions.SupportedProtocol);

            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task CanDoWebSockets()
        {
            var send1 = "TEST1";
            var expected1 = $"[{send1}]";

            var send2 = "TEST2";
            var expected2 = $"[{send2}]";

            await _wsClient.ConnectAsync(new Uri("ws://localhost:5003/to/random/path"), CancellationToken.None);
            Assert.Equal(Extensions.SupportedProtocol, _wsClient.SubProtocol);

            // Send a message.
            await _wsClient.SendShortMessageAsync(send1);
            await _wsClient.SendShortMessageAsync(send2);
            await _wsClient.SendShortMessageAsync(Extensions.CloseMessage);

            // Receive responses.
            var response1 = await _wsClient.ReceiveShortMessageAsync();
            Assert.Equal(expected1, response1);
            var response2 = await _wsClient.ReceiveShortMessageAsync();
            Assert.Equal(expected2, response2);

            // Receive close.
            var result = await _wsClient.ReceiveAsync(new ArraySegment<byte>(new byte[4096]), CancellationToken.None);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
            Assert.Equal(Extensions.CloseDescription, result.CloseStatusDescription);

            await _wsClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, Extensions.CloseDescription, CancellationToken.None);

            // HTTP test.
            var response = await _httpClient.PostAsync("http://localhost:5003/at/some/path", new StringContent(send1));
            Assert.Equal(expected1, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanDoSimpleServer()
        {
            var send1 = "TEST1";
            var expected1 = $"[{send1}]";

            var response = await _httpClient.PostAsync("http://localhost:5007/at/some/other/path", new StringContent(send1));
            Assert.Equal(expected1, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanFailWhenWebSocketNotDefined()
        {
            var message = "The server returned status code '502' when status code '101' was expected.";

            var exception = await Assert.ThrowsAnyAsync<WebSocketException>(() => _wsClient.ConnectAsync(new Uri("ws://localhost:5007/to/random/path"), CancellationToken.None));

            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public async Task CanFailOnIncorrectForwardToWs()
        {
            var response = await _httpClient.GetAsync("http://localhost:5003/should/forward/to/ws");
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }

        [Fact]
        public async Task CanFailOnIncorrectForwardToHttp()
        {
            await Assert.ThrowsAnyAsync<WebSocketException>(async () => await _wsClient.ConnectAsync(new Uri("ws://localhost:5003/should/forward/to/http"), CancellationToken.None));
        }
    }
}