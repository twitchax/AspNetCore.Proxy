using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Moq;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class BasicExtensions
    {
        [Fact]
        public async Task CanExerciseRunProxy()
        {
            const string endpoint = "garbage";

            var app = Mock.Of<IApplicationBuilder>();

            AspNetCore.Proxy.Extensions.Basic.RunProxy(app, (c, a) => new ValueTask<string>(endpoint), (c, a) => new ValueTask<string>(endpoint));
            AspNetCore.Proxy.Extensions.Basic.RunProxy(app, (c, a) => endpoint, (c, a) =>  endpoint);
            AspNetCore.Proxy.Extensions.Basic.RunProxy(app, endpoint, endpoint);

            AspNetCore.Proxy.Extensions.Basic.RunHttpProxy(app, b => b.WithEndpoint(endpoint));
            AspNetCore.Proxy.Extensions.Basic.RunHttpProxy(app, (c, a) => new ValueTask<string>(endpoint));
            AspNetCore.Proxy.Extensions.Basic.RunHttpProxy(app, (c, a) =>  endpoint);
            AspNetCore.Proxy.Extensions.Basic.RunHttpProxy(app, endpoint);

            AspNetCore.Proxy.Extensions.Basic.RunWsProxy(app, b => b.WithEndpoint(endpoint));
            AspNetCore.Proxy.Extensions.Basic.RunWsProxy(app, (c, a) => new ValueTask<string>(endpoint));
            AspNetCore.Proxy.Extensions.Basic.RunWsProxy(app, (c, a) =>  endpoint);
            AspNetCore.Proxy.Extensions.Basic.RunWsProxy(app, endpoint);
        }
    }
}