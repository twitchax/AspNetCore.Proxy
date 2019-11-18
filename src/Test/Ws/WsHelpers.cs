using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AspNetCore.Proxy.Extensions;
using AspNetCore.Proxy.Builders;

namespace AspNetCore.Proxy.Tests
{
    public class WsController : ControllerBase
    {
        [Route("api/ws")]
        public Task ProxyWsController()
        {
            return this.WsProxyAsync("ws://localhost:5002/");
        }

        [Route("api/ws2")]
        public Task ProxyWsControllerWithWsProxy()
        {
            // Use spurious http endpoint for code coverage.
            return this.ProxyAsync("http://localhost:5002/", "ws://localhost:5002/");
        }
    }
    
    internal static class WsHelpers
    {
        internal static Task RunWsServers(CancellationToken token)
        {
            var proxiedServerTask = WebHost.CreateDefaultBuilder()
                .SuppressStatusMessages(true)
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(5002);
                })
                .Configure(app => app.UseWebSockets().Run(context =>
                {
                    return context.SocketBoomerang();
                }))
                .Build().RunAsync(token);

            var proxyServerTask = WebHost.CreateDefaultBuilder()
                .SuppressStatusMessages(true)
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(5001);
                })
                .ConfigureServices(services => services.AddProxies().AddRouting().AddControllers())
                .Configure(app => 
                {
                    app.UseWebSockets().UseRouting().UseEndpoints(end => end.MapControllers());
                    
                    app.UseProxies(proxies =>
                    {
                        proxies.Map("/ws", proxy => proxy.UseWs("ws://localhost:5002/"));
                    });
                })
                .Build().RunAsync(token);

            return Task.WhenAll(proxiedServerTask, proxyServerTask);
        }
    }
}