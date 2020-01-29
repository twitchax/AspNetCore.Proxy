using System;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using AspNetCore.Proxy.Options;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class HttpProxy
    {
        [Fact]
        public async Task CanExerciseHttpProxyBuilder()
        {
            const string endpoint = "any";
            const string clientName = "bogus";

            var httpProxyOptions = HttpProxyOptionsBuilder.Instance.WithHttpClientName(clientName).New();

            // Exercise methods by calling them multiple times.
            var httpProxy = HttpProxyBuilder.Instance
                .New()
                .WithEndpoint(endpoint)
                .WithOptions(null as Action<IHttpProxyOptionsBuilder>)
                .WithOptions(null as IHttpProxyOptionsBuilder)
                .WithOptions(b => b.New())
                .WithOptions(httpProxyOptions)
                .New().Build();

            Assert.Equal(endpoint, await httpProxy.EndpointComputer.Invoke(null, null));
            Assert.Equal(clientName, httpProxy.Options.HttpClientName);
        }

        [Fact]
        public async Task CanHttpProxyBuilderFailOnNullEndpointComputer()
        {
            Assert.ThrowsAny<Exception>(() => {
                var httpProxy = HttpProxyBuilder.Instance.Build();
            });
        }
    }
}