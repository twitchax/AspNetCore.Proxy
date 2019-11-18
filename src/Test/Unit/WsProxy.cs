using System;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using AspNetCore.Proxy.Options;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public partial class UnitTests
    {
        [Fact]
        public async Task CanExerciseWsProxyBuilder()
        {
            var endpoint = "any";
            var bufferSize = 52978;

            var wsProxyOptions = WsProxyOptionsBuilder.Instance.WithBufferSize(bufferSize).New();
            var wsProxy = WsProxyBuilder.Instance.WithEndpoint(endpoint).WithOptions(wsProxyOptions).New().Build();

            Assert.Equal(endpoint, await wsProxy.EndpointComputer.Invoke(null, null));
            Assert.Equal(52978, wsProxy.Options.BufferSize);
        }

        [Fact]
        public async Task CanWsProxyBuilderFailOnNullEndpointComputer()
        {
            Assert.ThrowsAny<Exception>(() => {
                var httpProxy = HttpProxyBuilder.Instance.WithOptions(null as Action<IHttpProxyOptionsBuilder>).New().Build();
            });
        }
    }
}