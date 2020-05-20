using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Moq;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class BasicExtensions
    {
        [Fact]
        public void CanExerciseRunProxy()
        {
            const string endpoint = "garbage";

            var app = Mock.Of<IApplicationBuilder>();

            AspNetCore.Proxy.Basic.RunProxy(app, (c, a) => new ValueTask<string>(endpoint), (c, a) => new ValueTask<string>(endpoint));
            AspNetCore.Proxy.Basic.RunProxy(app, (c, a) => endpoint, (c, a) =>  endpoint);
            AspNetCore.Proxy.Basic.RunProxy(app, endpoint, endpoint);

            AspNetCore.Proxy.Basic.RunHttpProxy(app, b => b.WithEndpoint(endpoint));
            AspNetCore.Proxy.Basic.RunHttpProxy(app, (c, a) => new ValueTask<string>(endpoint));
            AspNetCore.Proxy.Basic.RunHttpProxy(app, (c, a) =>  endpoint);
            AspNetCore.Proxy.Basic.RunHttpProxy(app, endpoint);

            AspNetCore.Proxy.Basic.RunWsProxy(app, b => b.WithEndpoint(endpoint));
            AspNetCore.Proxy.Basic.RunWsProxy(app, (c, a) => new ValueTask<string>(endpoint));
            AspNetCore.Proxy.Basic.RunWsProxy(app, (c, a) =>  endpoint);
            AspNetCore.Proxy.Basic.RunWsProxy(app, endpoint);
        }

        [Fact]
        public async Task CanRemoveTrailingSlashes()
        {
            const string expected = "http://myaddresswithtoomanyslashes.com";

            var result = AspNetCore.Proxy.Helpers.TrimTrailingSlashes($"{expected}////");

            Assert.Equal(expected, result);
        }
    }
}