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
        public async Task CanExerciseHttpProxyBuilder()
        {
            var endpoint = "any";
            var clientName = "bogus";

            var httpProxyOptions = HttpProxyOptionsBuilder.Instance.WithHttpClientName(clientName).New();
            var httpProxy = HttpProxyBuilder.Instance.WithEndpoint(endpoint).WithOptions(httpProxyOptions).New().Build();

            Assert.Equal(endpoint, await httpProxy.EndpointComputer.Invoke(null, null));
            Assert.Equal(clientName, httpProxy.Options.HttpClientName);
        }

        [Fact]
        public async Task CanHttpProxyBuilderFailOnNullEndpointComputer()
        {
            Assert.ThrowsAny<Exception>(() => {
                var httpProxy = HttpProxyBuilder.Instance.WithOptions(null as Action<IHttpProxyOptionsBuilder>).New().Build();
            });
        }
    }
}