using System;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using AspNetCore.Proxy.Options;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class WsProxy
    {
        [Fact]
        public async Task CanExerciseWsProxyBuilder()
        {
            const string endpoint = "any";
            const int bufferSize = 52978;

            var wsProxyOptions = WsProxyOptionsBuilder.Instance.WithBufferSize(bufferSize).New();

            // Exercise methods by calling them multiple times.
            var wsProxy = WsProxyBuilder.Instance
                .New()
                .WithEndpoint(endpoint)
                .WithOptions(null as Action<IWsProxyOptionsBuilder>)
                .WithOptions(null as IWsProxyOptionsBuilder)
                .WithOptions(b => b.New())
                .WithOptions(wsProxyOptions)
                .New().Build();

            Assert.Equal(endpoint, await wsProxy.EndpointComputer.Invoke(null, null));
            Assert.Equal(52978, wsProxy.Options.BufferSize);
        }

        [Fact]
        public void CanWsProxyBuilderFailOnNullEndpointComputer()
        {
            Assert.ThrowsAny<Exception>(() => {
                var wsProxy = WsProxyBuilder.Instance.Build();
            });
        }
    }
}