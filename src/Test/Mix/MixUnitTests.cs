using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class MixServerFixture : IDisposable
    {
        private CancellationTokenSource _source;

        public MixServerFixture()
        {
            _source = new CancellationTokenSource();
            MixHelpers.RunMixServers(_source.Token);
        }

        public void Dispose()
        {
            _source.Cancel();
        }
    }

    public class MixUnitTests : IClassFixture<MixServerFixture>
    {
        public readonly ClientWebSocket _client;

        public MixUnitTests(MixServerFixture fixture)
        {
            _client = new ClientWebSocket();
            _client.Options.AddSubProtocol(Extensions.SupportedProtocol);
        }

        [Fact]
        public async Task CanDoWebSockets()
        {
            var send1 = "TEST1";
            var expected1 = $"[{send1}]";

            var send2 = "TEST2";
            var expected2 = $"[{send2}]";

            await _client.ConnectAsync(new Uri("ws://localhost:5003/to/random/path"), CancellationToken.None);
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

            // HTTP test.
            HttpClient client = new HttpClient();
            var response = await client.PostAsync("http://localhost:5003/at/some/path", new StringContent(send1));
            Assert.Equal(expected1, await response.Content.ReadAsStringAsync());
        }
    }
}